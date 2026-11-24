using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Sistema de Checkpoints (Faroles de la Cava) para el Nivel 3.
/// - Al pasar junto a un farol apagado, se enciende con fuego esmeralda-dorado, cura ligeramente al jugador
///   y guarda tanto la posición de reaparición como los pedazos de botella ya reunidos.
/// - Si el jugador muere y pulsa "JUGAR DE NUEVO", reaparece directamente en el último checkpoint activado
///   conservando las piezas de botella que ya había conseguido.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Collider2D))]
public class CheckpointCava : MonoBehaviour
{
    public static bool TieneCheckpointActivo { get; private set; } = false;
    public static string EscenaCheckpoint { get; private set; } = "";
    public static Vector3 PosicionRespawn { get; private set; } = Vector3.zero;
    public static int IndiceUltimoCheckpoint { get; private set; } = -1;
    public static bool[] PiezasGuardadas { get; private set; } = null;

    private static bool respawnAplicadoEnEstaCarga = false;

    [Header("Identificación del Checkpoint")]
    public int indiceCheckpoint = 0;
    public string nombreZona = "Cava Subterránea";

    [Header("Visuales del Farol")]
    public Sprite spriteApagado;
    public Sprite spriteEncendido;
    public SpriteRenderer haloLuz;

    [Header("Beneficio y Audio")]
    [Tooltip("Curación pequeña al encender por primera vez este checkpoint")]
    public float curacionAlActivar = 25f;
    public AudioClip sonidoActivar;

    private SpriteRenderer sr;
    private bool estaActivado = false;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        LimpiarCheckpoint();
    }

    public static void LimpiarCheckpoint()
    {
        TieneCheckpointActivo = false;
        EscenaCheckpoint = "";
        PosicionRespawn = Vector3.zero;
        IndiceUltimoCheckpoint = -1;
        PiezasGuardadas = null;
        respawnAplicadoEnEstaCarga = false;
    }

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;

        // Al recargar la escena, permitir aplicar respawn una sola vez
        string escenaActual = SceneManager.GetActiveScene().name;
        if (TieneCheckpointActivo && EscenaCheckpoint != escenaActual)
        {
            LimpiarCheckpoint();
        }
        respawnAplicadoEnEstaCarga = false;
    }

    private void Start()
    {
        string escenaActual = SceneManager.GetActiveScene().name;

        // Si ya habíamos pasado este checkpoint (o uno posterior), mostrarlo encendido
        if (TieneCheckpointActivo && EscenaCheckpoint == escenaActual && indiceCheckpoint <= IndiceUltimoCheckpoint)
        {
            estaActivado = true;
            AplicarVisualEncendido();
        }
        else
        {
            AplicarVisualApagado();
        }

        // Aplicar el teletransporte al último checkpoint una sola vez por carga de escena
        if (TieneCheckpointActivo && EscenaCheckpoint == escenaActual && !respawnAplicadoEnEstaCarga)
        {
            respawnAplicadoEnEstaCarga = true;
            StartCoroutine(RestaurarJugadorYProgresoRoutine());
        }
    }

    private void Update()
    {
        if (estaActivado && haloLuz != null)
        {
            float pulse = 0.62f + Mathf.Sin(Time.time * 4.5f + indiceCheckpoint) * 0.16f;
            haloLuz.color = new Color(0.42f, 1.0f, 0.58f, pulse);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other == null) return;
        if (!other.CompareTag("Player") && other.GetComponent<MovimientoJugador>() == null)
            return;

        // Guardar siempre el progreso más reciente de piezas de botella al pasar por un checkpoint alcanzado
        GuardarEstadoActual(other.gameObject);
    }

    private void GuardarEstadoActual(GameObject playerObj)
    {
        bool eraNuevo = !estaActivado;
        estaActivado = true;
        AplicarVisualEncendido();

        // Actualizar checkpoint si es nuevo o más avanzado que el anterior
        if (!TieneCheckpointActivo || indiceCheckpoint >= IndiceUltimoCheckpoint)
        {
            TieneCheckpointActivo = true;
            EscenaCheckpoint = SceneManager.GetActiveScene().name;
            IndiceUltimoCheckpoint = indiceCheckpoint;
            PosicionRespawn = new Vector3(transform.position.x, transform.position.y + 0.15f, 0f);
        }

        // Guardar las piezas de botella que ya lleva reunidas el jugador
        GestorBotellaNivel3 gestor = GestorBotellaNivel3.Instancia ?? FindAnyObjectByType<GestorBotellaNivel3>();
        if (gestor != null && gestor.piezasRecogidas != null)
        {
            PiezasGuardadas = new bool[gestor.piezasRecogidas.Length];
            for (int i = 0; i < gestor.piezasRecogidas.Length; i++)
            {
                PiezasGuardadas[i] = gestor.piezasRecogidas[i];
            }
        }

        if (eraNuevo)
        {
            if (curacionAlActivar > 0f && playerObj != null)
            {
                HealthSystem hs = playerObj.GetComponent<HealthSystem>();
                if (hs != null) hs.Heal(curacionAlActivar);
            }

            if (sonidoActivar != null)
            {
                CorazonVida.ReproducirSonidoPersistente(sonidoActivar, transform.position, 0.85f);
            }

            EnemyDeathFeedback.SpawnRuntimeGrapeJuiceSplash(
                transform.position + new Vector3(0f, 1.1f, 0f),
                20,
                new Color(0.35f, 1.0f, 0.55f, 1f),
                new Color(1.0f, 0.90f, 0.35f, 1f)
            );

            if (gestor != null)
            {
                gestor.MostrarMensajeTemporal($"¡CHECKPOINT ACTIVADO! {nombreZona} (+{Mathf.RoundToInt(curacionAlActivar)} HP)", 3.2f);
            }
        }
    }

    private IEnumerator RestaurarJugadorYProgresoRoutine()
    {
        // Esperar 1 frame para que todos los Awake/Start (jugador, cámara, GestorBotellaNivel3) hayan inicializado
        yield return null;

        GameObject character = GameObject.Find("Character");
        if (character == null)
        {
            MovimientoJugador mov = FindAnyObjectByType<MovimientoJugador>();
            if (mov != null) character = mov.gameObject;
        }

        if (character != null)
        {
            character.transform.position = PosicionRespawn;
            Rigidbody2D rb = character.GetComponent<Rigidbody2D>();
            if (rb != null) rb.linearVelocity = Vector2.zero;
        }

        if (Camera.main != null)
        {
            Camera.main.transform.position = new Vector3(PosicionRespawn.x, PosicionRespawn.y + 1.8f, -10f);
        }

        GameObject cm = GameObject.Find("CinemachineCamera");
        if (cm != null)
        {
            cm.transform.position = new Vector3(PosicionRespawn.x, PosicionRespawn.y + 1.8f, -10f);
        }

        GestorBotellaNivel3 gestor = GestorBotellaNivel3.Instancia ?? FindAnyObjectByType<GestorBotellaNivel3>();
        if (gestor != null)
        {
            if (PiezasGuardadas != null)
            {
                gestor.RestaurarPiezasGuardadas(PiezasGuardadas);
            }
            gestor.MostrarMensajeTemporal($"Reapareciste en el Checkpoint #{IndiceUltimoCheckpoint + 1} (Botella: {gestor.piezasRecogidasCount}/{gestor.totalPiezas})", 3.2f);
        }
    }

    private void AplicarVisualApagado()
    {
        if (sr != null && spriteApagado != null) sr.sprite = spriteApagado;
        if (haloLuz != null) haloLuz.enabled = false;
    }

    private void AplicarVisualEncendido()
    {
        if (sr != null && spriteEncendido != null) sr.sprite = spriteEncendido;
        if (haloLuz != null) haloLuz.enabled = true;
    }
}
