using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(AudioSource))]
public class MetaNivel : MonoBehaviour
{
    public static int UvasEliminadas { get; private set; } = 0;
    public static int PuntosAcumuladosUvas { get; private set; } = 0;

    [Header("Configuración de Puntuación")]
    [Tooltip("Título personalizado del panel al cruzar la meta (si está vacío detecta Nivel 1 o Nivel 2 automáticamente)")]
    public string tituloPersonalizado = "";

    [Tooltip("Puntos base otorgados por cada uva eliminada en el nivel")]
    public int puntosPorUva = 100;

    [Tooltip("Multiplicador para calcular el bono según la vida restante (ej. 100 HP x 5 = 500 pts)")]
    public float multiplicadorBonoVida = 5f;

    [Header("Referencias UI - Pantalla '¡Nivel 1 Completado!'")]
    [Tooltip("Panel emergente del Canvas que se muestra al llegar a la meta")]
    public GameObject panelNivelCompletado;

    [Tooltip("CanvasGroup opcional del panel emergente")]
    public CanvasGroup panelCanvasGroup;

    [Tooltip("Texto del título ('¡Nivel 1 Completado!')")]
    public Text textoTitulo;

    [Tooltip("Texto con el desglose de puntos (Uvas eliminadas + Bono de vida)")]
    public Text textoDesglosePuntos;

    [Tooltip("Texto con la Puntuación Total final")]
    public Text textoPuntuacionTotal;

    [Tooltip("Botón funcional de 'Siguiente Nivel'")]
    public Button botonSiguienteNivel;

    [Header("Audio (SFX de Meta / Victoria)")]
    public AudioSource audioSource;

    [Tooltip("Sonido opcional al cruzar la meta y completar el nivel")]
    public AudioClip sonidoMetaCompletada;

    [Range(0f, 1f)]
    public float volumenMeta = 0.9f;

    private bool nivelCompletado = false;
    private Vector3 buttonBaseScale = Vector3.one;
    private float targetButtonScale = 1f;

    /// <summary>
    /// Registra la muerte de una uva enemiga para el cálculo dinámico de puntuación al llegar a la meta.
    /// </summary>
    public static void RegistrarUvaEliminada(int puntos = 100)
    {
        UvasEliminadas++;
        PuntosAcumuladosUvas += Mathf.Max(0, puntos);
        Debug.Log($"[MetaNivel] Uva eliminada registrada. Total uvas: {UvasEliminadas} (+{PuntosAcumuladosUvas} pts)");
    }

    public static void ReiniciarContadores()
    {
        UvasEliminadas = 0;
        PuntosAcumuladosUvas = 0;
    }

    private void Awake()
    {
        ReiniciarContadores();

        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.isTrigger = true;
        }

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;

        EnsureEventSystemExists();
        BuscarReferenciasUISiFaltan();

        if (panelNivelCompletado != null)
        {
            panelNivelCompletado.SetActive(false);
        }

        if (botonSiguienteNivel != null)
        {
            buttonBaseScale = botonSiguienteNivel.transform.localScale;
            botonSiguienteNivel.onClick.RemoveListener(CargarSiguienteNivel);
            botonSiguienteNivel.onClick.AddListener(CargarSiguienteNivel);
            ConfigurarEfectoHoverBoton(botonSiguienteNivel.gameObject);
        }
    }

    private void Update()
    {
        if (!nivelCompletado || botonSiguienteNivel == null)
            return;

        // Animación fluida del botón aunque Time.timeScale sea 0
        botonSiguienteNivel.transform.localScale = Vector3.Lerp(
            botonSiguienteNivel.transform.localScale,
            buttonBaseScale * targetButtonScale,
            Time.unscaledDeltaTime * 14f
        );
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (nivelCompletado)
            return;

        if (other.CompareTag("Player") || other.GetComponent<MovimientoJugador>() != null)
        {
            // En Nivel 3 (La Cava), exigir que los 5 pedazos de la botella estén reunidos antes de completar
            GestorBotellaNivel3 gestorBotella = GestorBotellaNivel3.Instancia ?? FindAnyObjectByType<GestorBotellaNivel3>();
            if (gestorBotella != null && !gestorBotella.BotellaCompleta)
            {
                gestorBotella.MostrarAvisoFaltanPiezas();
                return;
            }

            CompletarNivel(other.gameObject);
        }
    }

    public void CompletarNivel(GameObject playerObj)
    {
        if (nivelCompletado)
            return;

        nivelCompletado = true;

        // 1. Obtener la vida con la que el jugador llegó a la meta
        float vidaRestante = 100f;
        HealthSystem hs = playerObj != null ? playerObj.GetComponent<HealthSystem>() : null;
        if (hs == null)
            hs = FindAnyObjectByType<HealthSystem>();

        if (hs != null)
        {
            vidaRestante = Mathf.Max(0f, hs.currentHealth);
        }

        // 2. Calcular puntuación total dinámicamente:
        //    Puntos por uvas eliminadas + Bono por vida restante
        int puntosUvas = PuntosAcumuladosUvas > 0 ? PuntosAcumuladosUvas : (UvasEliminadas * puntosPorUva);
        int bonoVida = Mathf.RoundToInt(vidaRestante * multiplicadorBonoVida);
        int puntuacionTotal = puntosUvas + bonoVida;

        // 3. Reproducir sonido de victoria si está asignado
        if (sonidoMetaCompletada != null)
        {
            if (audioSource != null)
                audioSource.PlayOneShot(sonidoMetaCompletada, volumenMeta);
            else
                CorazonVida.ReproducirSonidoPersistente(sonidoMetaCompletada, transform.position, volumenMeta);
        }

        // 4. Desactivar controles de movimiento/ataque del jugador
        if (playerObj != null)
        {
            MovimientoJugador mov = playerObj.GetComponent<MovimientoJugador>();
            if (mov != null) mov.enabled = false;

            PlayerAttack atk = playerObj.GetComponent<PlayerAttack>();
            if (atk != null) atk.enabled = false;

            Rigidbody2D rb = playerObj.GetComponent<Rigidbody2D>();
            if (rb != null) rb.linearVelocity = Vector2.zero;
        }

        // 5. Actualizar textos y activar el panel emergente de UI
        BuscarReferenciasUISiFaltan();

        if (textoTitulo != null)
        {
            string escenaNombre = SceneManager.GetActiveScene().name;
            if (!string.IsNullOrEmpty(tituloPersonalizado))
            {
                textoTitulo.text = tituloPersonalizado;
            }
            else if (!string.IsNullOrEmpty(escenaNombre) && escenaNombre.Contains("4"))
            {
                textoTitulo.text = "¡JUEGO COMPLETADO!";
            }
            else if (!string.IsNullOrEmpty(escenaNombre) && escenaNombre.Contains("3"))
            {
                textoTitulo.text = "¡Nivel 3 Completado!";
            }
            else if (!string.IsNullOrEmpty(escenaNombre) && escenaNombre.Contains("2"))
            {
                textoTitulo.text = "¡Nivel 2 Completado!";
            }
            else
            {
                textoTitulo.text = "¡Nivel 1 Completado!";
            }

            if (botonSiguienteNivel != null && !string.IsNullOrEmpty(escenaNombre) && escenaNombre.Contains("4"))
            {
                Text btnTxt = botonSiguienteNivel.GetComponentInChildren<Text>();
                if (btnTxt != null) btnTxt.text = "JUGAR DE NUEVO";
            }
        }

        if (textoDesglosePuntos != null)
        {
            textoDesglosePuntos.text =
                $"Uvas Eliminadas ({UvasEliminadas}): +{puntosUvas} pts\n" +
                $"Bono de Salud ({Mathf.CeilToInt(vidaRestante)} HP): +{bonoVida} pts";
        }

        if (textoPuntuacionTotal != null)
        {
            textoPuntuacionTotal.text = $"PUNTUACIÓN TOTAL: {puntuacionTotal} PTS";
        }

        if (panelNivelCompletado != null)
        {
            panelNivelCompletado.SetActive(true);
        }

        if (panelCanvasGroup != null)
        {
            panelCanvasGroup.alpha = 1f;
            panelCanvasGroup.interactable = true;
            panelCanvasGroup.blocksRaycasts = true;
        }

        // 6. Pausar el juego (Time.timeScale = 0f)
        Time.timeScale = 0f;

        Debug.Log($"[MetaNivel] ¡Nivel 1 Completado! Uvas: {UvasEliminadas} ({puntosUvas} pts) + Bono Vida ({bonoVida} pts) = TOTAL: {puntuacionTotal} pts.");
    }

    /// <summary>
    /// Conectado al evento OnClick() del botón "Siguiente Nivel".
    /// Restablece Time.timeScale = 1f y carga la siguiente escena en Build Settings (o recarga la actual si no existe la escena 2).
    /// </summary>
    public void CargarSiguienteNivel()
    {
        Time.timeScale = 1f;
        ReiniciarContadores();
        CheckpointCava.LimpiarCheckpoint();

        Scene escenaActual = SceneManager.GetActiveScene();
        int siguienteIndice = escenaActual.buildIndex + 1;

        if (escenaActual.buildIndex >= 0 && siguienteIndice < SceneManager.sceneCountInBuildSettings)
        {
            SceneManager.LoadScene(siguienteIndice);
        }
        else if (SceneManager.sceneCountInBuildSettings > 0)
        {
            SceneManager.LoadScene(0);
        }
        else if (!string.IsNullOrEmpty(escenaActual.name))
        {
            SceneManager.LoadScene(escenaActual.name);
        }
    }

    private void BuscarReferenciasUISiFaltan()
    {
        if (panelNivelCompletado == null)
        {
            Canvas canvas = FindAnyObjectByType<Canvas>();
            if (canvas != null)
            {
                Transform panelTrans = canvas.transform.Find("LevelCompletePanel");
                if (panelTrans != null)
                {
                    panelNivelCompletado = panelTrans.gameObject;
                }
            }
        }

        if (panelNivelCompletado != null)
        {
            if (panelCanvasGroup == null)
                panelCanvasGroup = panelNivelCompletado.GetComponent<CanvasGroup>();

            if (textoTitulo == null)
            {
                Transform t = panelNivelCompletado.transform.Find("LevelCompleteArtwork/TitleText");
                if (t != null) textoTitulo = t.GetComponent<Text>();
            }

            if (textoDesglosePuntos == null)
            {
                Transform t = panelNivelCompletado.transform.Find("LevelCompleteArtwork/BreakdownText");
                if (t != null) textoDesglosePuntos = t.GetComponent<Text>();
            }

            if (textoPuntuacionTotal == null)
            {
                Transform t = panelNivelCompletado.transform.Find("LevelCompleteArtwork/TotalScoreText");
                if (t != null) textoPuntuacionTotal = t.GetComponent<Text>();
            }

            if (botonSiguienteNivel == null)
            {
                Transform t = panelNivelCompletado.transform.Find("LevelCompleteArtwork/NextLevelButton");
                if (t != null) botonSiguienteNivel = t.GetComponent<Button>();
            }
        }
    }

    private void ConfigurarEfectoHoverBoton(GameObject btnObj)
    {
        EventTrigger trigger = btnObj.GetComponent<EventTrigger>();
        if (trigger == null)
            trigger = btnObj.AddComponent<EventTrigger>();

        trigger.triggers.Clear();

        EventTrigger.Entry enterEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        enterEntry.callback.AddListener((_) => { targetButtonScale = 1.05f; });
        trigger.triggers.Add(enterEntry);

        EventTrigger.Entry exitEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
        exitEntry.callback.AddListener((_) => { targetButtonScale = 1.0f; });
        trigger.triggers.Add(exitEntry);

        EventTrigger.Entry downEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
        downEntry.callback.AddListener((_) => { targetButtonScale = 0.95f; });
        trigger.triggers.Add(downEntry);

        EventTrigger.Entry upEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
        upEntry.callback.AddListener((_) => { targetButtonScale = 1.05f; });
        trigger.triggers.Add(upEntry);
    }

    private void EnsureEventSystemExists()
    {
        if (FindAnyObjectByType<EventSystem>() == null)
        {
            GameObject esObj = new GameObject("EventSystem");
            esObj.AddComponent<EventSystem>();
            esObj.AddComponent<InputSystemUIInputModule>();
        }
    }
}
