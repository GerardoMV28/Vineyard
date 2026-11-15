using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Controla la Pantalla Inicial / Opener ("Grapefall - START GAME") en máxima calidad HD,
/// incluyendo música chill de fondo, luciérnagas/brillo ambiental del farol,
/// sonido al presionar START GAME, cinemática de acercamiento al viñedo y transición tenue al Nivel 1.
/// </summary>
public class TitleScreenOpener : MonoBehaviour
{
    /// <summary>
    /// Permite que al reiniciar tras un Game Over no vuelva a salir el Opener,
    /// pero que siempre aparezca al iniciar el juego desde cero.
    /// </summary>
    public static bool OmitirOpenerEnReinicio = false;

    [Header("Referencias UI de la Pantalla Opener (HD)")]
    [Tooltip("Panel raíz que contiene toda la pantalla de inicio y la transición")]
    public GameObject openerRootPanel;

    [Tooltip("CanvasGroup del contenedor de arte para el desvanecimiento tenue hacia el Nivel 1")]
    public CanvasGroup artworkCanvasGroup;

    [Tooltip("RectTransform de la ilustración HD 'Grapefall' que realiza el zoom cinemático")]
    public RectTransform artworkRectTransform;

    [Tooltip("Botón interactivo sobre '-> START GAME'")]
    public Button startGameButton;

    [Tooltip("Imagen del botón '-> START GAME' para efectos de pulso y destello dorado")]
    public Image startGameButtonImage;

    [Tooltip("Brillo cálido sobre el farol del viñedo que se intensifica en la cinemática")]
    public Image lanternGlowImage;

    [Tooltip("Velo suave para la transición tenue al comenzar el Nivel 1")]
    public Image softTransitionOverlay;

    [Tooltip("Barra cinemática superior (Letterbox)")]
    public RectTransform topCinematicBar;

    [Tooltip("Barra cinemática inferior (Letterbox)")]
    public RectTransform bottomCinematicBar;

    [Header("Audio - Música Chill y Sonido de Start Game")]
    [Tooltip("AudioSource dedicado a la melodía chill de fondo en bucle")]
    public AudioSource musicAudioSource;

    [Tooltip("AudioSource dedicado a los efectos de sonido (clic en Start Game)")]
    public AudioSource sfxAudioSource;

    [Tooltip("Melodía o música chill de fondo mientras estás en la pantalla de inicio")]
    public AudioClip musicaChillFondo;

    [Range(0f, 1f)]
    public float volumenMusicaChill = 0.65f;

    [Tooltip("Sonido al seleccionar 'START GAME' y comenzar la cinemática")]
    public AudioClip sonidoStartGame;

    [Range(0f, 1f)]
    public float volumenSonidoStart = 0.95f;

    [Header("Configuración de la Cinemática y Transición Tenue")]
    [Tooltip("Duración del acercamiento cinemático hacia el sendero del viñedo (segundos)")]
    public float duracionCinematica = 1.65f;

    [Tooltip("Escala final del zoom cinemático sobre la ilustración HD")]
    public float escalaZoomCinematica = 1.24f;

    [Tooltip("Desplazamiento suave de cámara hacia el farol/sendero central durante el zoom")]
    public Vector2 desplazamientoZoom = new Vector2(-18f, 24f);

    [Tooltip("Duración del desvanecimiento tenue para revelar el Nivel 1 (segundos)")]
    public float duracionTransicionTenue = 1.15f;

    [Tooltip("Si está activo, carga otra escena al terminar la cinemática; si no, revela el Nivel 1 actual")]
    public bool cargarEscenaAlTerminar = false;

    [Tooltip("Nombre de la escena del Nivel 1 si se usa desde una escena de menú separada")]
    public string nombreEscenaNivel1 = "nivel1nocompleto";

    private bool haIniciadoCinematica = false;
    private bool estaHoverBoton = false;
    private Vector3 escalaBaseBoton = Vector3.one;
    private Vector2 posicionBaseArtwork = Vector2.zero;
    private RectTransform[] luciernagasUI;
    private Vector2[] posBaseLuciernagas;
    private float[] faseLuciernagas;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticStateOnPlay()
    {
        OmitirOpenerEnReinicio = false;
    }

    private void Awake()
    {
        EnsureEventSystemExists();
        ConfigurarFuentesDeAudio();
        AutoVincularReferenciasSiFaltan();

        // Si venimos de darle "Jugar de nuevo" en Game Over, omitimos la portada para ir directo a la acción
        if (OmitirOpenerEnReinicio && !cargarEscenaAlTerminar)
        {
            OmitirOpenerEnReinicio = false;
            Time.timeScale = 1f;
            if (openerRootPanel != null)
                openerRootPanel.SetActive(false);
            enabled = false;
            return;
        }

        if (openerRootPanel != null)
            openerRootPanel.SetActive(true);

        if (artworkCanvasGroup != null)
        {
            artworkCanvasGroup.alpha = 1f;
            artworkCanvasGroup.interactable = true;
            artworkCanvasGroup.blocksRaycasts = true;
        }

        if (artworkRectTransform != null)
        {
            artworkRectTransform.localScale = Vector3.one;
            posicionBaseArtwork = artworkRectTransform.anchoredPosition;
        }

        if (softTransitionOverlay != null)
        {
            // Pequeño fade-in inicial desde negro al abrir el juego
            softTransitionOverlay.gameObject.SetActive(true);
            softTransitionOverlay.color = new Color(0.06f, 0.02f, 0.08f, 0f);
            softTransitionOverlay.raycastTarget = false;
        }

        if (topCinematicBar != null)
            topCinematicBar.sizeDelta = new Vector2(topCinematicBar.sizeDelta.x, 0f);
        if (bottomCinematicBar != null)
            bottomCinematicBar.sizeDelta = new Vector2(bottomCinematicBar.sizeDelta.x, 0f);

        if (startGameButton != null)
        {
            escalaBaseBoton = startGameButton.transform.localScale;
            startGameButton.onClick.RemoveListener(AlPresionarStartGame);
            startGameButton.onClick.AddListener(AlPresionarStartGame);
            ConfigurarEventosHoverBoton(startGameButton.gameObject);
        }

        CrearLuciernagasAmbientales();

        // Pausar la física del nivel de fondo mientras el jugador está en la pantalla Opener
        Time.timeScale = 0f;
    }

    private void Start()
    {
        if (!enabled)
            return;

        Time.timeScale = 0f;

        // Iniciar la melodía chill de fondo en bucle
        if (musicAudioSource != null && musicaChillFondo != null)
        {
            musicAudioSource.clip = musicaChillFondo;
            musicAudioSource.loop = true;
            musicAudioSource.volume = volumenMusicaChill;
            musicAudioSource.Play();
        }
    }

    private void Update()
    {
        float t = Time.unscaledTime;

        // Animar las luciérnagas cálidas del atardecer en el viñedo
        AnimarLuciernagas(t);

        if (!haIniciadoCinematica)
        {
            // Pulso suave del botón "-> START GAME" y resplandor del farol mientras espera en el menú
            if (startGameButton != null)
            {
                float pulso = estaHoverBoton
                    ? 1.07f + Mathf.Sin(t * 7f) * 0.025f
                    : 1.0f + Mathf.Sin(t * 3.2f) * 0.022f;

                startGameButton.transform.localScale = Vector3.Lerp(
                    startGameButton.transform.localScale,
                    escalaBaseBoton * pulso,
                    Time.unscaledDeltaTime * 12f
                );

                if (startGameButtonImage != null)
                {
                    float brillo = estaHoverBoton ? 1.0f : (0.93f + Mathf.Sin(t * 3.2f) * 0.07f);
                    startGameButtonImage.color = new Color(1f, brillo, estaHoverBoton ? 0.86f : 0.92f, 1f);
                }
            }

            if (lanternGlowImage != null)
            {
                float alphaFarol = 0.24f + Mathf.Sin(t * 2.6f) * 0.07f + Mathf.Sin(t * 6.3f) * 0.02f;
                lanternGlowImage.color = new Color(1f, 0.78f, 0.32f, alphaFarol);
                float escalaFarol = 1f + Mathf.Sin(t * 2.6f) * 0.05f;
                lanternGlowImage.rectTransform.localScale = new Vector3(escalaFarol, escalaFarol, 1f);
            }
        }
    }

    /// <summary>
    /// Se ejecuta al hacer clic en "-> START GAME".
    /// Reproduce el sonido de selección, lanza la cinemática de entrada al viñedo y realiza la transición tenue.
    /// </summary>
    public void AlPresionarStartGame()
    {
        if (haIniciadoCinematica)
            return;

        haIniciadoCinematica = true;

        if (startGameButton != null)
            startGameButton.interactable = false;

        // Reproducir sonido de selección de START GAME
        if (sonidoStartGame != null)
        {
            if (sfxAudioSource != null)
                sfxAudioSource.PlayOneShot(sonidoStartGame, volumenSonidoStart);
            else
                CorazonVida.ReproducirSonidoPersistente(sonidoStartGame, Vector3.zero, volumenSonidoStart);
        }

        StartCoroutine(SecuenciaCinematicaYTransicion());
    }

    private IEnumerator SecuenciaCinematicaYTransicion()
    {
        // ====================================================================
        // FASE 1: Destello dorado y animación de impacto en el botón START GAME (0.38s)
        // ====================================================================
        float duracionDestelloBoton = 0.38f;
        float tiempo = 0f;

        while (tiempo < duracionDestelloBoton)
        {
            tiempo += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(tiempo / duracionDestelloBoton);

            if (startGameButton != null)
            {
                // Efecto elástico de selección (punch scale)
                float curvaEscala = 1f + Mathf.Sin(p * Mathf.PI) * 0.22f;
                startGameButton.transform.localScale = escalaBaseBoton * curvaEscala;
            }

            if (startGameButtonImage != null)
            {
                // Parpadeo cálido de confirmación
                float flash = Mathf.Sin(p * Mathf.PI * 3f) * 0.5f + 0.5f;
                startGameButtonImage.color = Color.Lerp(Color.white, new Color(1f, 0.95f, 0.55f, 1f), flash);
            }

            yield return null;
        }

        // ====================================================================
        // FASE 2: Cinemática de entrada al viñedo (Zoom suave hacia el farol +
        //         barras cinemáticas Letterbox + intensificación de luz cálida)
        // ====================================================================
        tiempo = 0f;
        float volumenInicialMusica = musicAudioSource != null ? musicAudioSource.volume : volumenMusicaChill;

        while (tiempo < duracionCinematica)
        {
            tiempo += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(tiempo / Mathf.Max(0.1f, duracionCinematica));
            // Curva suave SmoothStep para movimiento de cámara cinematográfico
            float smooth = p * p * (3f - 2f * p);

            // 1. Zoom progresivo en alta definición hacia el camino central y el farol
            if (artworkRectTransform != null)
            {
                float escalaActual = Mathf.Lerp(1f, escalaZoomCinematica, smooth);
                artworkRectTransform.localScale = new Vector3(escalaActual, escalaActual, 1f);
                artworkRectTransform.anchoredPosition = Vector2.Lerp(posicionBaseArtwork, posicionBaseArtwork + desplazamientoZoom, smooth);
            }

            // 2. Desvanecer suavemente el botón "START GAME" mientras nos adentramos en el paisaje
            if (startGameButtonImage != null)
            {
                float alphaBtn = Mathf.Clamp01(1f - p * 2.2f);
                startGameButtonImage.color = new Color(1f, 0.92f, 0.68f, alphaBtn);
            }

            // 3. Barras cinemáticas (Letterbox) sutiles arriba y abajo
            float alturaBarra = Mathf.Lerp(0f, 36f, Mathf.Clamp01(p * 1.6f));
            if (topCinematicBar != null)
                topCinematicBar.sizeDelta = new Vector2(topCinematicBar.sizeDelta.x, alturaBarra);
            if (bottomCinematicBar != null)
                bottomCinematicBar.sizeDelta = new Vector2(bottomCinematicBar.sizeDelta.x, alturaBarra);

            // 4. El farol del viñedo brilla con más calidez invitando a entrar al nivel
            if (lanternGlowImage != null)
            {
                float alphaFarol = Mathf.Lerp(0.28f, 0.62f, smooth);
                float escalaFarol = Mathf.Lerp(1f, 1.85f, smooth);
                lanternGlowImage.color = new Color(1f, 0.82f, 0.38f, alphaFarol);
                lanternGlowImage.rectTransform.localScale = new Vector3(escalaFarol, escalaFarol, 1f);
            }

            // 5. En el último tramo de la cinemática (p > 0.45), inicia el velo tenue crepuscular
            if (softTransitionOverlay != null && p > 0.45f)
            {
                float pVelo = Mathf.Clamp01((p - 0.45f) / 0.55f);
                float smoothVelo = pVelo * pVelo * (3f - 2f * pVelo);
                softTransitionOverlay.color = new Color(0.07f, 0.03f, 0.09f, smoothVelo);
            }

            // 6. Atenuar gradualmente la música chill del menú
            if (musicAudioSource != null)
            {
                musicAudioSource.volume = Mathf.Lerp(volumenInicialMusica, 0.05f, smooth);
            }

            yield return null;
        }

        // Si estamos en una escena independiente de menú, cargamos la escena del Nivel 1
        if (cargarEscenaAlTerminar && !string.IsNullOrEmpty(nombreEscenaNivel1))
        {
            OmitirOpenerEnReinicio = true;
            Time.timeScale = 1f;
            SceneManager.LoadScene(nombreEscenaNivel1);
            yield break;
        }

        // ====================================================================
        // FASE 3: Transición tenue (Cross-fade suave) revelando el Nivel 1
        // ====================================================================
        // Reactivar el tiempo del juego para que el Nivel 1 comience suavemente
        Time.timeScale = 1f;

        if (artworkCanvasGroup != null)
        {
            artworkCanvasGroup.interactable = false;
            artworkCanvasGroup.blocksRaycasts = false;
        }

        tiempo = 0f;
        float volRestante = musicAudioSource != null ? musicAudioSource.volume : 0f;

        while (tiempo < duracionTransicionTenue)
        {
            tiempo += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(tiempo / Mathf.Max(0.1f, duracionTransicionTenue));
            float smooth = p * p * (3f - 2f * p);

            // Desvanecer la ilustración y el velo tenue al mismo tiempo para revelar el Nivel 1
            if (artworkCanvasGroup != null)
                artworkCanvasGroup.alpha = Mathf.Lerp(1f, 0f, Mathf.Clamp01(p * 1.8f));

            if (softTransitionOverlay != null)
                softTransitionOverlay.color = new Color(0.07f, 0.03f, 0.09f, Mathf.Lerp(1f, 0f, smooth));

            if (topCinematicBar != null)
                topCinematicBar.sizeDelta = new Vector2(topCinematicBar.sizeDelta.x, Mathf.Lerp(36f, 0f, smooth));
            if (bottomCinematicBar != null)
                bottomCinematicBar.sizeDelta = new Vector2(bottomCinematicBar.sizeDelta.x, Mathf.Lerp(36f, 0f, smooth));

            if (musicAudioSource != null)
                musicAudioSource.volume = Mathf.Lerp(volRestante, 0f, smooth);

            yield return null;
        }

        if (musicAudioSource != null)
            musicAudioSource.Stop();

        if (openerRootPanel != null)
            openerRootPanel.SetActive(false);
    }

    private void ConfigurarFuentesDeAudio()
    {
        AudioSource[] sources = GetComponents<AudioSource>();
        if (musicAudioSource == null)
        {
            musicAudioSource = sources.Length > 0 ? sources[0] : gameObject.AddComponent<AudioSource>();
        }
        if (sfxAudioSource == null)
        {
            sfxAudioSource = sources.Length > 1 ? sources[1] : gameObject.AddComponent<AudioSource>();
        }

        musicAudioSource.playOnAwake = false;
        musicAudioSource.loop = true;
        musicAudioSource.spatialBlend = 0f;

        sfxAudioSource.playOnAwake = false;
        sfxAudioSource.loop = false;
        sfxAudioSource.spatialBlend = 0f;
    }

    private void CrearLuciernagasAmbientales()
    {
        if (artworkRectTransform == null)
            return;

        Transform contenedorExistente = artworkRectTransform.Find("LuciernagasAtardecer");
        if (contenedorExistente != null)
            Destroy(contenedorExistente.gameObject);

        GameObject contenedor = new GameObject("LuciernagasAtardecer");
        contenedor.transform.SetParent(artworkRectTransform, false);
        RectTransform contRect = contenedor.AddComponent<RectTransform>();
        contRect.anchorMin = Vector2.zero;
        contRect.anchorMax = Vector2.one;
        contRect.offsetMin = Vector2.zero;
        contRect.offsetMax = Vector2.zero;

        int cantidad = 14;
        luciernagasUI = new RectTransform[cantidad];
        posBaseLuciernagas = new Vector2[cantidad];
        faseLuciernagas = new float[cantidad];

        // Posiciones estratégicas alrededor del farol, las uvas y el camino
        Vector2[] semillas = new Vector2[]
        {
            new Vector2(-40f, -20f), new Vector2(55f, 10f), new Vector2(85f, -35f),
            new Vector2(-120f, -45f), new Vector2(15f, -75f), new Vector2(140f, -15f),
            new Vector2(-180f, 15f), new Vector2(68f, 45f), new Vector2(-75f, -95f),
            new Vector2(110f, -85f), new Vector2(-25f, 55f), new Vector2(195f, -50f),
            new Vector2(-150f, -80f), new Vector2(35f, -110f)
        };

        for (int i = 0; i < cantidad; i++)
        {
            GameObject luc = new GameObject($"Luciernaga_{i}");
            luc.transform.SetParent(contenedor.transform, false);
            RectTransform rt = luc.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            float tam = (i % 3 == 0) ? 5.5f : 4f;
            rt.sizeDelta = new Vector2(tam, tam);
            rt.anchoredPosition = semillas[i % semillas.Length];

            Image img = luc.AddComponent<Image>();
            img.color = new Color(1f, 0.88f, 0.42f, 0.7f);
            img.raycastTarget = false;

            luciernagasUI[i] = rt;
            posBaseLuciernagas[i] = rt.anchoredPosition;
            faseLuciernagas[i] = i * 1.37f;
        }
    }

    private void AnimarLuciernagas(float t)
    {
        if (luciernagasUI == null)
            return;

        float multiplicadorVelocidad = haIniciadoCinematica ? 2.4f : 1.0f;

        for (int i = 0; i < luciernagasUI.Length; i++)
        {
            RectTransform rt = luciernagasUI[i];
            if (rt == null) continue;

            float fase = faseLuciernagas[i];
            float dx = Mathf.Sin(t * 1.2f * multiplicadorVelocidad + fase) * 14f;
            float dy = Mathf.Cos(t * 0.9f * multiplicadorVelocidad + fase * 0.7f) * 10f
                       + (haIniciadoCinematica ? (t % 5f) * 8f : 0f);

            rt.anchoredPosition = posBaseLuciernagas[i] + new Vector2(dx, dy);

            Image img = rt.GetComponent<Image>();
            if (img != null)
            {
                float alpha = (0.35f + Mathf.Sin(t * 3.1f + fase) * 0.35f) * (haIniciadoCinematica ? 1.25f : 1f);
                img.color = new Color(1f, 0.88f, 0.42f, Mathf.Clamp01(alpha));
            }
        }
    }

    private void ConfigurarEventosHoverBoton(GameObject btnObj)
    {
        EventTrigger trigger = btnObj.GetComponent<EventTrigger>();
        if (trigger == null)
            trigger = btnObj.AddComponent<EventTrigger>();

        trigger.triggers.Clear();

        EventTrigger.Entry enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        enter.callback.AddListener((_) => { estaHoverBoton = true; });
        trigger.triggers.Add(enter);

        EventTrigger.Entry exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
        exit.callback.AddListener((_) => { estaHoverBoton = false; });
        trigger.triggers.Add(exit);
    }

    private void AutoVincularReferenciasSiFaltan()
    {
        if (openerRootPanel == null)
        {
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas != null)
            {
                Transform t = canvas.transform.Find("TitleScreenOpenerPanel");
                if (t != null) openerRootPanel = t.gameObject;
            }
        }

        if (openerRootPanel != null)
        {
            Transform artTrans = openerRootPanel.transform.Find("OpenerArtworkContainer/OpenerHDArtwork");
            if (artTrans != null)
            {
                if (artworkRectTransform == null)
                    artworkRectTransform = artTrans.GetComponent<RectTransform>();
                if (artworkCanvasGroup == null)
                    artworkCanvasGroup = artTrans.GetComponent<CanvasGroup>();

                if (startGameButton == null)
                {
                    Transform btnT = artTrans.Find("StartGameButton");
                    if (btnT != null)
                    {
                        startGameButton = btnT.GetComponent<Button>();
                        startGameButtonImage = btnT.GetComponent<Image>();
                    }
                }

                if (lanternGlowImage == null)
                {
                    Transform glowT = artTrans.Find("LanternWarmGlow");
                    if (glowT != null) lanternGlowImage = glowT.GetComponent<Image>();
                }
            }

            if (softTransitionOverlay == null)
            {
                Transform veilT = openerRootPanel.transform.Find("SoftTransitionVeil");
                if (veilT != null) softTransitionOverlay = veilT.GetComponent<Image>();
            }
        }
    }

    private void EnsureEventSystemExists()
    {
        if (FindFirstObjectByType<EventSystem>() == null)
        {
            GameObject es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<InputSystemUIInputModule>();
        }
    }
}
