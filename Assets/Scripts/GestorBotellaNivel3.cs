using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Administra la recolección y ensamblado de los 5 pedazos de la Botella Sagrada en el Nivel 3 (La Cava).
/// - Muestra en el HUD (esquina superior derecha) la silueta de la botella y va encendiendo cada pedazo
///   exacto que recoge el jugador hasta formar la botella completa.
/// - Bloquea el Altar / MetaNivel3 hasta que la botella esté 100% armada (5/5).
/// - Al completarla, enciende la botella armada sobre el Altar final lista para almacenar al Jefe Final.
/// </summary>
public class GestorBotellaNivel3 : MonoBehaviour
{
    public static GestorBotellaNivel3 Instancia { get; private set; }

    [Header("Progreso de la Botella")]
    public int totalPiezas = 5;
    public int piezasRecogidasCount = 0;
    public bool[] piezasRecogidas = new bool[5];

    [Header("Referencias UI del HUD de Botella (Esquina Superior Derecha)")]
    [Tooltip("Las 5 imágenes superpuestas en el HUD que forman la botella completa (de abajo hacia arriba: 0..4)")]
    public Image[] imagenesPiezasHUD = new Image[5];
    public Image imagenBrilloCompletoHUD;
    public Text textoContadorBotella;
    public Text textoAvisoFlotante;

    [Header("Altar Final / MetaNivel3")]
    [Tooltip("SpriteRenderer de la botella completa que aparece brillando sobre el Altar al reunir las 5 piezas")]
    public SpriteRenderer botellaArmadaEnAltar;
    public SpriteRenderer auraAltar;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip sonidoPiezaRecogida;
    public AudioClip sonidoBotellaCompleta;
    public AudioClip sonidoFaltanPiezas;

    public bool BotellaCompleta => piezasRecogidasCount >= totalPiezas;

    private Coroutine avisoCoroutine;

    private void Awake()
    {
        Instancia = this;
        if (piezasRecogidas == null || piezasRecogidas.Length != totalPiezas)
            piezasRecogidas = new bool[totalPiezas];

        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
    }

    private void Start()
    {
        ActualizarVisualesHUD();
        ActualizarAltar();
        MostrarMensajeTemporal("Cava Subterránea: Reúne los 5 pedazos de la botella para encerrar al Jefe Final.", 4.5f);
    }

    public void RegistrarPieza(int indicePieza, string nombrePieza)
    {
        if (indicePieza < 0 || indicePieza >= totalPiezas)
            return;

        if (piezasRecogidas[indicePieza])
            return;

        piezasRecogidas[indicePieza] = true;
        piezasRecogidasCount = 0;
        for (int i = 0; i < totalPiezas; i++)
        {
            if (piezasRecogidas[i]) piezasRecogidasCount++;
        }

        // Dar puntos por cada pedazo encontrado
        MetaNivel.RegistrarUvaEliminada(100);

        ActualizarVisualesHUD();
        ActualizarAltar();

        if (imagenesPiezasHUD != null && indicePieza < imagenesPiezasHUD.Length && imagenesPiezasHUD[indicePieza] != null)
        {
            StartCoroutine(AnimarEncajePiezaHUD(imagenesPiezasHUD[indicePieza].rectTransform));
        }

        if (BotellaCompleta)
        {
            if (sonidoBotellaCompleta != null && audioSource != null)
                audioSource.PlayOneShot(sonidoBotellaCompleta, 0.95f);

            MostrarMensajeTemporal("¡BOTELLA COMPLETA (5/5)! Llévala al Altar final de la Cava.", 5.0f);
        }
        else
        {
            if (sonidoPiezaRecogida != null && audioSource != null)
                audioSource.PlayOneShot(sonidoPiezaRecogida, 0.88f);

            int restantes = totalPiezas - piezasRecogidasCount;
            MostrarMensajeTemporal($"¡{nombrePieza} recogida! ({piezasRecogidasCount}/{totalPiezas}) - Faltan {restantes}.", 3.0f);
        }
    }

    public void MostrarAvisoFaltanPiezas()
    {
        int restantes = Mathf.Max(1, totalPiezas - piezasRecogidasCount);
        if (sonidoFaltanPiezas != null && audioSource != null)
            audioSource.PlayOneShot(sonidoFaltanPiezas, 0.8f);

        MostrarMensajeTemporal($"¡La botella está incompleta! Te faltan {restantes} pedazos en la Cava.", 3.4f);
    }

    /// <summary>
    /// Restaura las piezas de botella que el jugador ya había recogido antes de reaparecer en un Checkpoint.
    /// </summary>
    public void RestaurarPiezasGuardadas(bool[] guardadas)
    {
        if (guardadas == null) return;

        piezasRecogidasCount = 0;
        for (int i = 0; i < totalPiezas && i < guardadas.Length; i++)
        {
            piezasRecogidas[i] = guardadas[i];
            if (piezasRecogidas[i]) piezasRecogidasCount++;
        }

        // Quitar del escenario las piezas que ya estaban recogidas para evitar duplicados
        PiezaBotellaColeccionable[] pickups = FindObjectsByType<PiezaBotellaColeccionable>(FindObjectsSortMode.None);
        for (int i = 0; i < pickups.Length; i++)
        {
            if (pickups[i] != null &&
                pickups[i].indicePieza >= 0 &&
                pickups[i].indicePieza < piezasRecogidas.Length &&
                piezasRecogidas[pickups[i].indicePieza])
            {
                Destroy(pickups[i].gameObject);
            }
        }

        ActualizarVisualesHUD();
        ActualizarAltar();
    }

    private void ActualizarVisualesHUD()
    {
        if (imagenesPiezasHUD != null)
        {
            for (int i = 0; i < imagenesPiezasHUD.Length; i++)
            {
                if (imagenesPiezasHUD[i] == null) continue;
                bool obtenida = i < piezasRecogidas.Length && piezasRecogidas[i];
                // Si no se ha recogido, se ve semitransparente/oscura como silueta; al recogerla se ilumina al 100%
                imagenesPiezasHUD[i].color = obtenida
                    ? Color.white
                    : new Color(0.08f, 0.12f, 0.10f, 0.42f);
            }
        }

        if (imagenBrilloCompletoHUD != null)
        {
            imagenBrilloCompletoHUD.enabled = BotellaCompleta;
        }

        if (textoContadorBotella != null)
        {
            textoContadorBotella.text = BotellaCompleta
                ? $"BOTELLA: {piezasRecogidasCount}/{totalPiezas} ¡LISTA!"
                : $"BOTELLA: {piezasRecogidasCount}/{totalPiezas}";
            textoContadorBotella.color = BotellaCompleta
                ? new Color(1f, 0.92f, 0.35f, 1f)
                : new Color(0.85f, 0.98f, 0.88f, 1f);
        }
    }

    private void ActualizarAltar()
    {
        if (botellaArmadaEnAltar != null)
        {
            botellaArmadaEnAltar.color = BotellaCompleta
                ? Color.white
                : new Color(0.15f, 0.25f, 0.20f, 0.35f);
        }

        if (auraAltar != null)
        {
            auraAltar.color = BotellaCompleta
                ? new Color(0.35f, 1.0f, 0.55f, 0.72f)
                : new Color(0.55f, 0.25f, 0.75f, 0.28f);
        }
    }

    private IEnumerator AnimarEncajePiezaHUD(RectTransform rt)
    {
        if (rt == null) yield break;
        Vector3 baseScale = Vector3.one;
        float t = 0f;
        while (t < 0.22f)
        {
            t += Time.unscaledDeltaTime;
            float s = 1f + Mathf.Sin((t / 0.22f) * Mathf.PI) * 0.28f;
            rt.localScale = baseScale * s;
            yield return null;
        }
        rt.localScale = baseScale;
    }

    public void MostrarMensajeTemporal(string mensaje, float duracion)
    {
        if (textoAvisoFlotante == null) return;
        if (avisoCoroutine != null) StopCoroutine(avisoCoroutine);
        avisoCoroutine = StartCoroutine(MostrarMensajeRoutine(mensaje, duracion));
    }

    private IEnumerator MostrarMensajeRoutine(string mensaje, float duracion)
    {
        textoAvisoFlotante.gameObject.SetActive(true);
        textoAvisoFlotante.text = mensaje;
        textoAvisoFlotante.color = new Color(1f, 0.96f, 0.78f, 1f);

        yield return new WaitForSeconds(duracion);

        float t = 0f;
        while (t < 0.45f)
        {
            t += Time.deltaTime;
            float a = Mathf.Clamp01(1f - (t / 0.45f));
            textoAvisoFlotante.color = new Color(1f, 0.96f, 0.78f, a);
            yield return null;
        }

        textoAvisoFlotante.gameObject.SetActive(false);
    }
}
