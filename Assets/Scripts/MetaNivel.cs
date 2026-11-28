using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(AudioSource))]
public class MetaNivel : MonoBehaviour
{
    public enum TipoMedalla
    {
        Bronce,
        Plata,
        Oro
    }

    public static int UvasEliminadas { get; private set; } = 0;
    public static int PuntosAcumuladosUvas { get; private set; } = 0;

    // Acumulado de campaña a través de los niveles (Nivel 1 -> Nivel 4)
    private static readonly Dictionary<string, int> puntosObtenidosPorNivel = new Dictionary<string, int>();
    private static readonly Dictionary<string, int> puntosMaximosPorNivel = new Dictionary<string, int>();
    private static readonly Dictionary<string, int> enemigosDerrotadosPorNivel = new Dictionary<string, int>();

    private static readonly Dictionary<TipoMedalla, Sprite> spritesMedallaCache = new Dictionary<TipoMedalla, Sprite>();

    [Header("Configuración de Puntuación")]
    [Tooltip("Título personalizado del panel al cruzar la meta (si está vacío detecta Nivel 1..4 automáticamente)")]
    public string tituloPersonalizado = "";

    [Tooltip("Puntos base otorgados por cada uva eliminada en el nivel")]
    public int puntosPorUva = 100;

    [Tooltip("Multiplicador para calcular el bono según la vida restante (ej. 100 HP x 5 = 500 pts)")]
    public float multiplicadorBonoVida = 5f;

    [Header("Evaluación Final por Medallas (Nivel 4)")]
    [Tooltip("Porcentaje superior a este valor otorga Medalla de Oro (ej. > 95% = Oro)")]
    [Range(75f, 100f)]
    public float umbralOroPorcentaje = 95f;

    [Tooltip("Porcentaje mínimo para Medalla de Plata (ej. 70% a 95% = Plata; menor a 70% = Bronce)")]
    [Range(30f, 90f)]
    public float umbralPlataPorcentaje = 70f;

    [Header("Referencias UI - Pantalla '¡Nivel Completado!'")]
    [Tooltip("Panel emergente del Canvas que se muestra al llegar a la meta")]
    public GameObject panelNivelCompletado;

    [Tooltip("CanvasGroup opcional del panel emergente")]
    public CanvasGroup panelCanvasGroup;

    [Tooltip("Texto del título ('¡Nivel Completado!')")]
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
    private bool cargandoSiguienteEscena = false;
    private int puntosMaximosNivelActual = 1000;
    private Vector3 buttonBaseScale = Vector3.one;
    private float targetButtonScale = 1f;
    private RectTransform iconoMedallaAnimado;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticState()
    {
        ReiniciarContadores();
        ReiniciarCampana();
        spritesMedallaCache.Clear();
    }

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

    public static void ReiniciarCampana()
    {
        puntosObtenidosPorNivel.Clear();
        puntosMaximosPorNivel.Clear();
        enemigosDerrotadosPorNivel.Clear();
    }

    private void Awake()
    {
        ReiniciarContadores();

        // Si comenzamos desde el Nivel 1, iniciar una campaña nueva de puntuación
        Scene escenaActual = SceneManager.GetActiveScene();
        if (escenaActual.buildIndex == 0 ||
            (!string.IsNullOrEmpty(escenaActual.name) && escenaActual.name.Contains("1")))
        {
            ReiniciarCampana();
        }

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

    private void Start()
    {
        puntosMaximosNivelActual = CalcularPuntosMaximosNivelActual();
    }

    private void Update()
    {
        if (!nivelCompletado)
            return;

        // Animación fluida del botón aunque Time.timeScale sea 0
        if (botonSiguienteNivel != null)
        {
            botonSiguienteNivel.transform.localScale = Vector3.Lerp(
                botonSiguienteNivel.transform.localScale,
                buttonBaseScale * targetButtonScale,
                Time.unscaledDeltaTime * 14f
            );
        }

        // Pulso sutil en la medalla final (funciona con Time.timeScale = 0)
        if (iconoMedallaAnimado != null)
        {
            float pulse = 1f + Mathf.Sin(Time.unscaledTime * 3.8f) * 0.055f;
            iconoMedallaAnimado.localScale = new Vector3(pulse, pulse, 1f);
        }
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

        // 2. Calcular puntuación del nivel actual:
        //    Puntos por enemigos/objetivos + Bono por vida restante
        int puntosUvas = PuntosAcumuladosUvas > 0 ? PuntosAcumuladosUvas : (UvasEliminadas * puntosPorUva);
        int bonoVida = Mathf.RoundToInt(vidaRestante * multiplicadorBonoVida);
        int puntuacionTotal = puntosUvas + bonoVida;

        // Guardar progreso de este nivel en el acumulado de campaña
        string escenaNombre = SceneManager.GetActiveScene().name;
        string claveNivel = !string.IsNullOrEmpty(escenaNombre)
            ? escenaNombre
            : $"Nivel_{SceneManager.GetActiveScene().buildIndex}";

        puntosObtenidosPorNivel[claveNivel] = puntuacionTotal;
        puntosMaximosPorNivel[claveNivel] = Mathf.Max(1, puntosMaximosNivelActual);
        enemigosDerrotadosPorNivel[claveNivel] = UvasEliminadas;

        ObtenerResumenCampana(out int totalCampana, out int maxCampana, out int totalEnemigosCampana, out int nivelesJugados);
        float porcentajeCampana = maxCampana > 0 ? (totalCampana * 100f) / maxCampana : 100f;
        TipoMedalla medallaFinal = EvaluarMedalla(porcentajeCampana);

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

        bool esNivelFinal = !string.IsNullOrEmpty(escenaNombre) && escenaNombre.Contains("4");

        if (textoTitulo != null)
        {
            if (!string.IsNullOrEmpty(tituloPersonalizado))
            {
                textoTitulo.text = tituloPersonalizado;
            }
            else if (esNivelFinal)
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

            if (botonSiguienteNivel != null && esNivelFinal)
            {
                Text btnTxt = botonSiguienteNivel.GetComponentInChildren<Text>();
                if (btnTxt != null) btnTxt.text = "JUGAR DE NUEVO";
            }
        }

        if (esNivelFinal)
        {
            AplicarPantallaFinalConMedalla(
                medallaFinal,
                porcentajeCampana,
                puntuacionTotal,
                puntosUvas,
                bonoVida,
                vidaRestante,
                totalCampana,
                maxCampana,
                totalEnemigosCampana,
                nivelesJugados
            );
        }
        else
        {
            if (textoDesglosePuntos != null)
            {
                textoDesglosePuntos.resizeTextForBestFit = true;
                textoDesglosePuntos.resizeTextMinSize = 11;
                textoDesglosePuntos.resizeTextMaxSize = 15;

                string desglose =
                    $"Uvas Eliminadas ({UvasEliminadas}): +{puntosUvas} pts\n" +
                    $"Bono de Salud ({Mathf.CeilToInt(vidaRestante)} HP): +{bonoVida} pts";

                if (nivelesJugados > 1)
                {
                    desglose += $"\nAcumulado Campaña: {totalCampana} pts";
                }

                textoDesglosePuntos.text = desglose;
            }

            if (textoPuntuacionTotal != null)
            {
                textoPuntuacionTotal.text = $"PUNTUACIÓN TOTAL: {puntuacionTotal} PTS";
            }
        }

        if (panelNivelCompletado != null)
        {
            panelNivelCompletado.transform.SetAsLastSibling();
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

        Debug.Log(
            $"[MetaNivel] Nivel '{claveNivel}' completado! Nivel: {puntuacionTotal}/{puntosMaximosNivelActual} pts | " +
            $"Campaña ({nivelesJugados} niveles): {totalCampana}/{maxCampana} pts ({porcentajeCampana:F1}%) -> Medalla: {medallaFinal}."
        );
    }

    private int CalcularPuntosMaximosNivelActual()
    {
        int totalMax = 0;

        // MerlotGrapeEnemy incluye también a EnemigoUva (hereda de MerlotGrapeEnemy)
        MerlotGrapeEnemy[] merlots = FindObjectsByType<MerlotGrapeEnemy>(FindObjectsSortMode.None);
        for (int i = 0; i < merlots.Length; i++)
        {
            if (merlots[i] != null)
                totalMax += Mathf.Max(0, merlots[i].puntosAlDerrotar);
        }

        EnemigoCabSau[] cabSaus = FindObjectsByType<EnemigoCabSau>(FindObjectsSortMode.None);
        for (int i = 0; i < cabSaus.Length; i++)
        {
            if (cabSaus[i] != null)
                totalMax += Mathf.Max(0, cabSaus[i].puntosAlDerrotar);
        }

        EnemigoNebbiolo[] nebbiolos = FindObjectsByType<EnemigoNebbiolo>(FindObjectsSortMode.None);
        for (int i = 0; i < nebbiolos.Length; i++)
        {
            if (nebbiolos[i] != null)
                totalMax += Mathf.Max(0, nebbiolos[i].puntosAlDerrotar);
        }

        // Piezas de botella del Nivel 3 (100 pts cada una en GestorBotellaNivel3)
        GestorBotellaNivel3 gestorBotella = FindAnyObjectByType<GestorBotellaNivel3>();
        if (gestorBotella != null)
        {
            totalMax += Mathf.Max(0, gestorBotella.totalPiezas) * 100;
        }

        // Jefe Final Wine Monster (1500 pts al derrotarlo en JefeWineMonster)
        JefeWineMonster[] jefes = FindObjectsByType<JefeWineMonster>(FindObjectsSortMode.None);
        totalMax += jefes.Length * 1500;

        // Bono máximo por vida restante (100% HP)
        float maxVida = 100f;
        HealthSystem hs = FindAnyObjectByType<HealthSystem>();
        if (hs != null && hs.maxHealth > 0f)
            maxVida = hs.maxHealth;

        totalMax += Mathf.RoundToInt(maxVida * multiplicadorBonoVida);

        return Mathf.Max(500, totalMax);
    }

    private static void ObtenerResumenCampana(out int totalPuntos, out int maxPuntos, out int totalEnemigos, out int nivelesJugados)
    {
        totalPuntos = 0;
        maxPuntos = 0;
        totalEnemigos = 0;
        nivelesJugados = puntosObtenidosPorNivel.Count;

        foreach (var kv in puntosObtenidosPorNivel)
            totalPuntos += kv.Value;

        foreach (var kv in puntosMaximosPorNivel)
            maxPuntos += kv.Value;

        foreach (var kv in enemigosDerrotadosPorNivel)
            totalEnemigos += kv.Value;
    }

    private TipoMedalla EvaluarMedalla(float porcentaje)
    {
        // > 95% -> Oro | 70% a 95% -> Plata | < 70% -> Bronce
        if (porcentaje > umbralOroPorcentaje)
            return TipoMedalla.Oro;

        if (porcentaje >= umbralPlataPorcentaje)
            return TipoMedalla.Plata;

        return TipoMedalla.Bronce;
    }

    private void AplicarPantallaFinalConMedalla(
        TipoMedalla medalla,
        float porcentajeReal,
        int puntuacionNivel,
        int puntosUvasNivel,
        int bonoVidaNivel,
        float vidaRestante,
        int totalCampana,
        int maxCampana,
        int totalEnemigosCampana,
        int nivelesJugados)
    {
        float porcentajeMostrar = Mathf.Clamp(porcentajeReal, 0f, 100f);
        int pctRedondeado = Mathf.RoundToInt(porcentajeMostrar);

        // Reacomodar suavemente los elementos dentro del marco de madera para dar espacio a la Medalla
        if (textoTitulo != null)
        {
            RectTransform rt = textoTitulo.rectTransform;
            rt.anchorMin = new Vector2(0.295f, 0.660f);
            rt.anchorMax = new Vector2(0.705f, 0.795f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = Vector2.zero;
        }

        if (textoDesglosePuntos != null)
        {
            RectTransform rt = textoDesglosePuntos.rectTransform;
            rt.anchorMin = new Vector2(0.30f, 0.395f);
            rt.anchorMax = new Vector2(0.70f, 0.498f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = Vector2.zero;

            textoDesglosePuntos.resizeTextForBestFit = true;
            textoDesglosePuntos.resizeTextMinSize = 10;
            textoDesglosePuntos.resizeTextMaxSize = 14;
            textoDesglosePuntos.lineSpacing = 1.25f;

            if (nivelesJugados > 1)
            {
                textoDesglosePuntos.text =
                    $"Enemigos derrotados ({totalEnemigosCampana} total) | Nivel 4: +{puntuacionNivel} pts\n" +
                    $"Campaña ({nivelesJugados} niveles): {totalCampana} / {maxCampana} pts";
            }
            else
            {
                textoDesglosePuntos.text =
                    $"Enemigos y Jefe ({UvasEliminadas}): +{puntosUvasNivel} pts\n" +
                    $"Bono de Salud ({Mathf.CeilToInt(vidaRestante)} HP): +{bonoVidaNivel} pts";
            }
        }

        if (textoPuntuacionTotal != null)
        {
            RectTransform rt = textoPuntuacionTotal.rectTransform;
            rt.anchorMin = new Vector2(0.30f, 0.320f);
            rt.anchorMax = new Vector2(0.70f, 0.390f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = Vector2.zero;

            textoPuntuacionTotal.resizeTextForBestFit = true;
            textoPuntuacionTotal.resizeTextMinSize = 12;
            textoPuntuacionTotal.resizeTextMaxSize = 18;
            textoPuntuacionTotal.text = $"PUNTUACIÓN FINAL: {totalCampana} PTS";
        }

        if (botonSiguienteNivel != null)
        {
            RectTransform rt = botonSiguienteNivel.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin = new Vector2(0.335f, 0.205f);
                rt.anchorMax = new Vector2(0.665f, 0.305f);
                rt.anchoredPosition = Vector2.zero;
                rt.sizeDelta = Vector2.zero;
            }
        }

        CrearOActualizarTarjetaMedalla(medalla, pctRedondeado);
    }

    private void CrearOActualizarTarjetaMedalla(TipoMedalla medalla, int porcentajeMostrar)
    {
        if (panelNivelCompletado == null)
            return;

        Transform artworkTrans = panelNivelCompletado.transform.Find("LevelCompleteArtwork");
        if (artworkTrans == null)
            artworkTrans = panelNivelCompletado.transform;

        Transform existente = artworkTrans.Find("MedalSummaryPanel");
        if (existente != null)
            Destroy(existente.gameObject);

        string nombreMedalla;
        string fraseMedalla;
        Color colorPrincipal;
        Color colorFondo;

        switch (medalla)
        {
            case TipoMedalla.Oro:
                nombreMedalla = "MEDALLA DE ORO";
                fraseMedalla = "¡Maestro Supremo del Viñedo!";
                colorPrincipal = new Color(1.0f, 0.85f, 0.24f, 1f);
                colorFondo = new Color(0.42f, 0.30f, 0.05f, 0.38f);
                break;
            case TipoMedalla.Plata:
                nombreMedalla = "MEDALLA DE PLATA";
                fraseMedalla = "¡Gran Cosecha del Guardián!";
                colorPrincipal = new Color(0.86f, 0.91f, 0.96f, 1f);
                colorFondo = new Color(0.24f, 0.28f, 0.36f, 0.40f);
                break;
            default:
                nombreMedalla = "MEDALLA DE BRONCE";
                fraseMedalla = "¡Victoria! Vuelve por el Oro";
                colorPrincipal = new Color(0.90f, 0.60f, 0.36f, 1f);
                colorFondo = new Color(0.32f, 0.16f, 0.08f, 0.38f);
                break;
        }

        // Contenedor centrado entre el título y el desglose de puntos
        GameObject cardObj = new GameObject("MedalSummaryPanel", typeof(RectTransform), typeof(Image));
        cardObj.transform.SetParent(artworkTrans, false);

        RectTransform cardRt = cardObj.GetComponent<RectTransform>();
        cardRt.anchorMin = new Vector2(0.305f, 0.505f);
        cardRt.anchorMax = new Vector2(0.695f, 0.652f);
        cardRt.anchoredPosition = Vector2.zero;
        cardRt.sizeDelta = Vector2.zero;

        Image cardBg = cardObj.GetComponent<Image>();
        cardBg.color = colorFondo;
        cardBg.raycastTarget = false;

        // Icono de medalla pixel-art a la izquierda
        GameObject iconObj = new GameObject("MedalIcon", typeof(RectTransform), typeof(Image));
        iconObj.transform.SetParent(cardObj.transform, false);

        RectTransform iconRt = iconObj.GetComponent<RectTransform>();
        iconRt.anchorMin = new Vector2(0.04f, 0.08f);
        iconRt.anchorMax = new Vector2(0.26f, 0.92f);
        iconRt.anchoredPosition = Vector2.zero;
        iconRt.sizeDelta = Vector2.zero;

        Image iconImg = iconObj.GetComponent<Image>();
        iconImg.sprite = ObtenerSpriteMedalla(medalla);
        iconImg.preserveAspect = true;
        iconImg.raycastTarget = false;
        iconoMedallaAnimado = iconRt;

        // Texto de medalla y porcentaje a la derecha
        GameObject labelObj = new GameObject("MedalText", typeof(RectTransform), typeof(Text));
        labelObj.transform.SetParent(cardObj.transform, false);

        RectTransform labelRt = labelObj.GetComponent<RectTransform>();
        labelRt.anchorMin = new Vector2(0.28f, 0.08f);
        labelRt.anchorMax = new Vector2(0.97f, 0.92f);
        labelRt.anchoredPosition = Vector2.zero;
        labelRt.sizeDelta = Vector2.zero;

        Text labelTxt = labelObj.GetComponent<Text>();
        labelTxt.font = textoTitulo != null && textoTitulo.font != null
            ? textoTitulo.font
            : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        labelTxt.fontStyle = FontStyle.Bold;
        labelTxt.alignment = TextAnchor.MiddleLeft;
        labelTxt.supportRichText = true;
        labelTxt.resizeTextForBestFit = true;
        labelTxt.resizeTextMinSize = 11;
        labelTxt.resizeTextMaxSize = 18;
        labelTxt.lineSpacing = 1.15f;
        labelTxt.color = colorPrincipal;
        labelTxt.raycastTarget = false;

        string hexColor = ColorUtility.ToHtmlStringRGB(colorPrincipal);
        labelTxt.text =
            $"<color=#{hexColor}>{nombreMedalla}</color>\n" +
            $"<size=13><color=#F5E6C8>{porcentajeMostrar}% de puntos  •  {fraseMedalla}</color></size>";
    }

    private static Sprite ObtenerSpriteMedalla(TipoMedalla tipo)
    {
        if (spritesMedallaCache.TryGetValue(tipo, out Sprite cached) && cached != null)
            return cached;

        const int w = 48;
        const int h = 56;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;

        Color clear = new Color(0f, 0f, 0f, 0f);
        Color outline = new Color(0.14f, 0.07f, 0.09f, 1f);

        Color rimDark, metalMid, metalLight, starColor, ribbonMain, ribbonDark;

        switch (tipo)
        {
            case TipoMedalla.Oro:
                rimDark = new Color(0.68f, 0.44f, 0.06f, 1f);
                metalMid = new Color(0.98f, 0.78f, 0.18f, 1f);
                metalLight = new Color(1.0f, 0.96f, 0.62f, 1f);
                starColor = new Color(1.0f, 0.98f, 0.80f, 1f);
                ribbonMain = new Color(0.76f, 0.12f, 0.24f, 1f);
                ribbonDark = new Color(0.48f, 0.06f, 0.14f, 1f);
                break;
            case TipoMedalla.Plata:
                rimDark = new Color(0.46f, 0.53f, 0.62f, 1f);
                metalMid = new Color(0.80f, 0.86f, 0.92f, 1f);
                metalLight = new Color(0.97f, 0.99f, 1.0f, 1f);
                starColor = new Color(1.0f, 1.0f, 1.0f, 1f);
                ribbonMain = new Color(0.42f, 0.22f, 0.68f, 1f);
                ribbonDark = new Color(0.26f, 0.12f, 0.45f, 1f);
                break;
            default:
                rimDark = new Color(0.46f, 0.24f, 0.10f, 1f);
                metalMid = new Color(0.82f, 0.50f, 0.26f, 1f);
                metalLight = new Color(0.96f, 0.74f, 0.50f, 1f);
                starColor = new Color(0.99f, 0.86f, 0.68f, 1f);
                ribbonMain = new Color(0.22f, 0.52f, 0.34f, 1f);
                ribbonDark = new Color(0.12f, 0.32f, 0.20f, 1f);
                break;
        }

        float cx = w * 0.5f;
        float cy = 20.5f;

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                Color c = clear;

                // 1. Cinta superior en forma de V (y = 35..53)
                if (y >= 35 && y <= 53)
                {
                    float t = (y - 35f) / 18f;
                    float leftInner = cx - 4.5f - t * 6.5f;
                    float leftOuter = leftInner - 7.5f;
                    float rightInner = cx + 4.5f + t * 6.5f;
                    float rightOuter = rightInner + 7.5f;

                    bool inLeft = x >= leftOuter && x <= leftInner;
                    bool inRight = x >= rightInner && x <= rightOuter;

                    if (inLeft || inRight)
                    {
                        bool edge = y == 53 ||
                                    (inLeft && (x < leftOuter + 1.5f || x > leftInner - 1.5f)) ||
                                    (inRight && (x < rightInner + 1.5f || x > rightOuter - 1.5f));
                        if (edge)
                            c = outline;
                        else
                            c = ((inLeft && x < leftOuter + 4f) || (inRight && x > rightOuter - 4f)) ? ribbonDark : ribbonMain;
                    }
                }

                // 2. Broche metálico entre cinta y medalla
                if (y >= 34 && y <= 39 && x >= cx - 7f && x <= cx + 7f)
                {
                    bool border = y == 34 || y == 39 || x <= cx - 6f || x >= cx + 6f;
                    c = border ? outline : (y >= 37 ? metalLight : metalMid);
                }

                // 3. Medallón circular principal
                float dx = x - cx;
                float dy = y - cy;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);

                if (dist <= 17.5f)
                {
                    if (dist >= 15.6f)
                    {
                        c = outline;
                    }
                    else if (dist >= 12.6f)
                    {
                        // Borde biselado con luz arriba-izquierda y sombra abajo-derecha
                        c = (dy - dx > 2f) ? metalLight : (dy - dx < -5f ? rimDark : metalMid);
                    }
                    else if (dist >= 11.4f)
                    {
                        c = rimDark;
                    }
                    else
                    {
                        // Cara interior con brillo diagonal sutil
                        float sheen = Mathf.Abs((dx + dy) - 3.5f);
                        c = sheen < 2.2f ? metalLight : metalMid;

                        // Estrella de 5 puntas en el centro (punta principal hacia arriba)
                        float angle = Mathf.Atan2(dy, dx) - Mathf.PI * 0.5f;
                        float sector = Mathf.Abs(((angle % (Mathf.PI * 0.4f)) + Mathf.PI * 0.4f) % (Mathf.PI * 0.4f) - Mathf.PI * 0.2f);
                        float starRadius = Mathf.Lerp(3.6f, 8.4f, sector / (Mathf.PI * 0.2f));
                        if (dist <= starRadius)
                        {
                            c = dist >= starRadius - 1.15f ? rimDark : starColor;
                        }
                    }
                }

                tex.SetPixel(x, y, c);
            }
        }

        tex.Apply();
        Sprite sprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 48f);
        spritesMedallaCache[tipo] = sprite;
        return sprite;
    }

    /// <summary>
    /// Conectado al evento OnClick() del botón "Siguiente Nivel" / "JUGAR DE NUEVO".
    /// Restablece Time.timeScale = 1f y carga la siguiente escena en Build Settings.
    /// </summary>
    public void CargarSiguienteNivel()
    {
        if (cargandoSiguienteEscena)
            return;
        cargandoSiguienteEscena = true;

        Time.timeScale = 1f;
        ReiniciarContadores();
        CheckpointCava.LimpiarCheckpoint();

        Scene escenaActual = SceneManager.GetActiveScene();
        string nombreActual = escenaActual.name ?? "";

        // Garantizar el orden correcto Nivel1 -> Nivel2 -> Nivel3 -> Nivel4 -> Nivel1
        string siguientePorNombre = null;
        bool reiniciaJuego = false;
        if (nombreActual.Contains("1"))
            siguientePorNombre = "Nivel2";
        else if (nombreActual.Contains("2"))
            siguientePorNombre = "Nivel3";
        else if (nombreActual.Contains("3"))
            siguientePorNombre = "Nivel4";
        else if (nombreActual.Contains("4"))
        {
            siguientePorNombre = "Nivel1";
            reiniciaJuego = true;
        }

        if (!string.IsNullOrEmpty(siguientePorNombre) && Application.CanStreamedLevelBeLoaded(siguientePorNombre))
        {
            if (reiniciaJuego)
                ReiniciarCampana();
            SceneManager.LoadScene(siguientePorNombre);
            return;
        }

        int siguienteIndice = escenaActual.buildIndex + 1;

        if (escenaActual.buildIndex >= 0 && siguienteIndice < SceneManager.sceneCountInBuildSettings)
        {
            SceneManager.LoadScene(siguienteIndice);
        }
        else if (SceneManager.sceneCountInBuildSettings > 0)
        {
            ReiniciarCampana();
            SceneManager.LoadScene(0);
        }
        else if (!string.IsNullOrEmpty(escenaActual.name))
        {
            ReiniciarCampana();
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
