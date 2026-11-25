using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// JEFE FINAL DEL NIVEL 4: WINE MONSTER (3000 HP)
/// - 2 Fases:
///   • Fase 1 (100% -> 50% HP): Patrulla fluida por el suelo, Embestida de Ola de Vino por el piso
///     (el jugador la esquiva subiendo a las plataformas elevadas) e Invocación de los 3 tipos de uvas
///     (Merlot, Cab. Sau y Nebbiolo).
///   • Fase 2 (<= 50% HP): Furia Carmesí — olas e invocaciones más rápidas + ráfaga de Chorro de Vino Tinto
///     capaz de presionar al jugador incluso sobre plataformas elevadas.
/// - Cada 6 enemigos invocados derrotados aparece un Corazón curativo cerca del jugador.
/// - Al llegar a 0 HP, la Botella Sagrada del Nivel 3 desciende sobre la arena y absorbe/sella al Wine Monster
///   en su interior, disparando la pantalla final "¡JUEGO COMPLETADO!".
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class JefeWineMonster : MonoBehaviour
{
    public static JefeWineMonster Instancia { get; private set; }

    public enum EstadoBoss
    {
        Dormido,
        PatrullaOla,
        EmbestidaOla,
        InvocarUvas,
        ChorroVino,
        TransicionFase2,
        SellandoEnBotella
    }

    [Header("Salud y Daño del Jefe (3000 HP)")]
    public int maxHealth = 3000;
    public int currentHealth = 3000;
    [Tooltip("Daño por cada corchazo del jugador (70 HP => ~43 disparos para 3000 HP)")]
    public int danoPorCorchazo = 70;
    public float danoContactoJugador = 24f;
    public float cooldownContacto = 0.85f;
    public float fuerzaEmpujeContacto = 7.5f;

    [Header("Límites de la Arena del Jefe")]
    public float arenaMinX = 214.5f;
    public float arenaMaxX = 255.5f;
    public float sueloY = 0f;
    [Tooltip("Posición X que debe cruzar el jugador para despertar al Wine Monster y sellar la entrada")]
    public float triggerCombateX = 216.5f;
    public GameObject puertaBloqueoArena;

    [Header("Velocidades y Tiempos de Combate")]
    public float velocidadPatrullaFase1 = 4.3f;
    public float velocidadPatrullaFase2 = 5.8f;
    public float velocidadEmbestidaFase1 = 13.5f;
    public float velocidadEmbestidaFase2 = 16.8f;
    public float tiempoEntreAtaquesFase1 = 2.0f;
    public float tiempoEntreAtaquesFase2 = 1.35f;

    [Header("Invocación de las 3 Uvas y Corazón cada 6 Derrotados")]
    public GameObject prefabMerlot;
    public GameObject prefabCabSau;
    public GameObject prefabNebbiolo;
    public GameObject prefabCorazon;
    public int esbirrosParaCorazon = 6;
    public int maxEsbirrosActivosFase1 = 4;
    public int maxEsbirrosActivosFase2 = 6;

    [Header("Sprites del Wine Monster (16 frames)")]
    public Sprite[] spritesIdleMovimiento;
    public Sprite[] spritesPrepararOla;
    public Sprite[] spritesEmbestidaOla;
    public Sprite[] spritesInvocar;
    public Sprite[] spritesPrepararChorro;
    public Sprite[] spritesDispararChorro;
    public Sprite spriteErupcion;

    [Header("Clímax de Victoria: Sellado en Botella Sagrada")]
    public Sprite spriteBotellaSellado;
    public Sprite spriteAuraBotella;
    public MetaNivel metaFinalNivel4;

    [Header("HUD del Jefe Final")]
    public GameObject panelHudBoss;
    public Slider barraVidaBoss;
    public Text textoVidaBoss;
    public Text textoFaseBoss;
    public Text textoEsbirrosCorazon;
    public Text textoBannerAviso;

    [Header("Audio del Jefe")]
    public AudioSource audioSource;
    public AudioClip sonidoRugido;
    public AudioClip sonidoOla;
    public AudioClip sonidoInvocar;
    public AudioClip sonidoChorro;
    public AudioClip sonidoGolpe;
    public AudioClip sonidoCorazonSpawn;
    public AudioClip sonidoSellado;

    public bool EstaDerrotado { get; private set; } = false;
    public bool LimpiandoEsbirros { get; private set; } = false;

    private SpriteRenderer sr;
    private Rigidbody2D rb;
    private Collider2D col;
    private Transform playerTransform;
    private EstadoBoss estadoActual = EstadoBoss.Dormido;

    private bool combateIniciado = false;
    private bool enFase2 = false;
    private float direccionMovimiento = -1f;
    private float tiempoUltimoContacto = -10f;
    private float animTimer = 0f;
    private int animFrame = 0;

    private int esbirrosDerrotadosContador = 0;
    private int contadorAcciones = 0;
    private int siguienteTipoInvocacion = 0;
    private readonly List<GameObject> esbirrosVivos = new List<GameObject>();

    private Coroutine rutinaCombate;
    private Coroutine rutinaFlash;
    private Coroutine rutinaBanner;
    private Color colorBase = Color.white;
    private Vector3 escalaBase = Vector3.one;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Instancia = null;
    }

    private void Awake()
    {
        Instancia = this;
        sr = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();

        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;

        currentHealth = maxHealth;
        escalaBase = transform.localScale;
        colorBase = Color.white;

        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }

        if (puertaBloqueoArena != null)
            puertaBloqueoArena.SetActive(false);

        if (panelHudBoss != null)
            panelHudBoss.SetActive(false);

        if (textoBannerAviso != null)
            textoBannerAviso.gameObject.SetActive(false);

        ActualizarVisualDireccion(-1f);
        ActualizarHUD();
    }

    private void Start()
    {
        BuscarJugador();
        if (metaFinalNivel4 == null)
        {
            metaFinalNivel4 = FindAnyObjectByType<MetaNivel>();
        }
    }

    private void Update()
    {
        if (EstaDerrotado) return;

        if (playerTransform == null) BuscarJugador();

        if (!combateIniciado)
        {
            // Animación latente esperando al jugador
            AnimarSecuencia(spritesIdleMovimiento, 4f);

            // Posición firme sobre el suelo de la arena
            Vector3 p = transform.position;
            p.y = sueloY + 1.95f + Mathf.Sin(Time.time * 2.5f) * 0.08f;
            transform.position = p;

            if (playerTransform != null && playerTransform.position.x >= triggerCombateX)
            {
                IniciarCombate();
            }
            return;
        }

        // Carga pasiva suave del Super Corchazo durante la pelea contra el Jefe
        JugadorGolpeEspecial.NotificarCarga(2.0f * Time.deltaTime);

        // Mantener movimiento fluido en PatrullaOla
        if (estadoActual == EstadoBoss.PatrullaOla)
        {
            float speed = enFase2 ? velocidadPatrullaFase2 : velocidadPatrullaFase1;
            Vector3 pos = transform.position;

            if (playerTransform != null)
            {
                float diffX = playerTransform.position.x - pos.x;
                if (Mathf.Abs(diffX) > 1.2f)
                {
                    direccionMovimiento = Mathf.Sign(diffX);
                }
            }

            pos.x = Mathf.Clamp(pos.x + direccionMovimiento * speed * Time.deltaTime, arenaMinX, arenaMaxX);
            pos.y = sueloY + 1.95f + Mathf.Sin(Time.time * 5.2f) * 0.10f;
            transform.position = pos;

            ActualizarVisualDireccion(direccionMovimiento);
            AnimarSecuencia(spritesIdleMovimiento, enFase2 ? 8f : 6f);
        }
    }

    private void IniciarCombate()
    {
        if (combateIniciado) return;
        combateIniciado = true;
        estadoActual = EstadoBoss.PatrullaOla;

        if (puertaBloqueoArena != null)
            puertaBloqueoArena.SetActive(true);

        if (panelHudBoss != null)
            panelHudBoss.SetActive(true);

        ReproducirSonido(sonidoRugido, 0.95f);
        MostrarMensajeBanner("¡JEFE FINAL: EL WINE MONSTER! (Usa las plataformas elevadas para esquivar su ola)", 4.2f);
        ActualizarHUD();

        rutinaCombate = StartCoroutine(CicloCombateBossRoutine());
    }

    private IEnumerator CicloCombateBossRoutine()
    {
        // Pausa dramática inicial con rugido
        if (spriteErupcion != null) sr.sprite = spriteErupcion;
        yield return new WaitForSeconds(0.85f);

        while (!EstaDerrotado)
        {
            // 1. Patrulla fluida por el suelo
            estadoActual = EstadoBoss.PatrullaOla;
            float espera = enFase2 ? tiempoEntreAtaquesFase2 : tiempoEntreAtaquesFase1;
            float t = 0f;
            while (t < espera && !EstaDerrotado && estadoActual == EstadoBoss.PatrullaOla)
            {
                t += Time.deltaTime;
                yield return null;
            }
            if (EstaDerrotado) yield break;

            // Si entramos en transición de fase 2, esperar a que termine
            while (estadoActual == EstadoBoss.TransicionFase2 && !EstaDerrotado)
            {
                yield return null;
            }
            if (EstaDerrotado) yield break;

            LimpiarReferenciasEsbirrosNulos();
            int maxEsbirros = enFase2 ? maxEsbirrosActivosFase2 : maxEsbirrosActivosFase1;

            // 2. Rotación equilibrada de patrones de ataque
            contadorAcciones++;
            int patron;
            if (!enFase2)
            {
                // Fase 1: Alterna Embestida de Ola por el piso e Invocación de los 3 tipos de uvas
                if (esbirrosVivos.Count < maxEsbirros && (contadorAcciones % 2 == 0))
                    patron = 1; // Invocar
                else
                    patron = 0; // Embestida Ola
            }
            else
            {
                // Fase 2: Rota entre Embestida Ola, Chorro de Vino (ataca plataformas!) e Invocación
                int mod = contadorAcciones % 3;
                if (mod == 1)
                    patron = 2; // Chorro de Vino
                else if (mod == 2 && esbirrosVivos.Count < maxEsbirros)
                    patron = 1; // Invocar
                else
                    patron = 0; // Embestida Ola
            }

            if (patron == 0)
            {
                yield return StartCoroutine(AtaqueEmbestidaOlaRoutine());
            }
            else if (patron == 1)
            {
                yield return StartCoroutine(AtaqueInvocarUvasRoutine());
            }
            else
            {
                yield return StartCoroutine(AtaqueChorroVinoRoutine());
            }
        }
    }

    // =========================================================================
    // ATAQUE 1: EMBESTIDA DE OLA POR EL SUELO (esquivable en plataformas elevadas)
    // =========================================================================
    private IEnumerator AtaqueEmbestidaOlaRoutine()
    {
        estadoActual = EstadoBoss.EmbestidaOla;

        // Elegir dirección hacia el jugador
        if (playerTransform != null)
        {
            float dx = playerTransform.position.x - transform.position.x;
            direccionMovimiento = Mathf.Abs(dx) > 0.4f ? Mathf.Sign(dx) : (transform.position.x > (arenaMinX + arenaMaxX) * 0.5f ? -1f : 1f);
        }
        ActualizarVisualDireccion(direccionMovimiento);

        // Telegrafiado claro antes de embestir
        ReproducirSonido(sonidoOla, 0.85f);
        float prepTime = enFase2 ? 0.48f : 0.68f;
        float t = 0f;
        while (t < prepTime && !EstaDerrotado)
        {
            t += Time.deltaTime;
            AnimarSecuencia(spritesPrepararOla, 7f);
            // Pequeño encogimiento de impulso
            float s = 1f + Mathf.Sin(t * 20f) * 0.06f;
            transform.localScale = new Vector3(escalaBase.x * s, escalaBase.y * (2f - s), 1f);
            yield return null;
        }
        transform.localScale = escalaBase;
        if (EstaDerrotado) yield break;

        // Barrido rápido de lado a lado del suelo de la arena
        float dashSpeed = enFase2 ? velocidadEmbestidaFase2 : velocidadEmbestidaFase1;
        float targetX = direccionMovimiento > 0f ? arenaMaxX : arenaMinX;
        float splashCooldown = 0f;

        while (!EstaDerrotado && Mathf.Abs(transform.position.x - targetX) > 0.35f)
        {
            Vector3 pos = transform.position;
            pos.x = Mathf.MoveTowards(pos.x, targetX, dashSpeed * Time.deltaTime);
            pos.y = sueloY + 1.90f;
            transform.position = pos;

            AnimarSecuencia(spritesEmbestidaOla, 12f);

            splashCooldown -= Time.deltaTime;
            if (splashCooldown <= 0f)
            {
                splashCooldown = 0.14f;
                EnemyDeathFeedback.SpawnRuntimeGrapeJuiceSplash(
                    new Vector3(pos.x - direccionMovimiento * 1.1f, sueloY + 0.5f, 0f),
                    5,
                    new Color(0.55f, 0.03f, 0.15f, 1f),
                    new Color(0.88f, 0.16f, 0.28f, 1f)
                );
            }

            yield return null;
        }

        // En Fase 2 hace un segundo rebote sorpresa si aún está lejos del jugador
        if (enFase2 && !EstaDerrotado)
        {
            direccionMovimiento = -direccionMovimiento;
            ActualizarVisualDireccion(direccionMovimiento);
            ReproducirSonido(sonidoOla, 0.78f);
            yield return new WaitForSeconds(0.28f);

            float halfTarget = Mathf.Lerp(arenaMinX, arenaMaxX, 0.5f) + direccionMovimiento * 11f;
            halfTarget = Mathf.Clamp(halfTarget, arenaMinX, arenaMaxX);
            while (!EstaDerrotado && Mathf.Abs(transform.position.x - halfTarget) > 0.35f)
            {
                Vector3 pos = transform.position;
                pos.x = Mathf.MoveTowards(pos.x, halfTarget, dashSpeed * 0.92f * Time.deltaTime);
                pos.y = sueloY + 1.90f;
                transform.position = pos;
                AnimarSecuencia(spritesEmbestidaOla, 12f);
                yield return null;
            }
        }
    }

    // =========================================================================
    // ATAQUE 2: INVOCACIÓN DE UVAS (Merlot, Cab. Sau y Nebbiolo)
    // =========================================================================
    private IEnumerator AtaqueInvocarUvasRoutine()
    {
        estadoActual = EstadoBoss.InvocarUvas;
        ReproducirSonido(sonidoInvocar, 0.9f);

        // Animación de invocación (uvas emergiendo del cuerpo)
        float t = 0f;
        while (t < 0.75f && !EstaDerrotado)
        {
            t += Time.deltaTime;
            AnimarSecuencia(spritesInvocar, 5f);
            yield return null;
        }
        if (EstaDerrotado) yield break;

        LimpiarReferenciasEsbirrosNulos();
        int maxActivos = enFase2 ? maxEsbirrosActivosFase2 : maxEsbirrosActivosFase1;
        int cantidadAInvocar = enFase2 ? 3 : 2;
        int cupoDisponible = Mathf.Max(0, maxActivos - esbirrosVivos.Count);
        int spawnCount = Mathf.Min(cantidadAInvocar, cupoDisponible);

        // Si ya está al tope, igual invocar al menos 1 si hay menos de maxActivos + 1
        if (spawnCount <= 0 && esbirrosVivos.Count < maxActivos)
            spawnCount = 1;

        for (int i = 0; i < spawnCount; i++)
        {
            GameObject prefabElegido = ElegirSiguientePrefabUva();
            if (prefabElegido == null) continue;

            float offset = (i == 0) ? -3.0f : (i == 1 ? 3.0f : 0f);
            float spawnX = Mathf.Clamp(transform.position.x + offset, arenaMinX + 1.5f, arenaMaxX - 1.5f);
            // Evitar aparecer encima del jugador
            if (playerTransform != null && Mathf.Abs(spawnX - playerTransform.position.x) < 2.2f)
            {
                spawnX = Mathf.Clamp(spawnX + (spawnX > playerTransform.position.x ? 3.2f : -3.2f), arenaMinX + 1.5f, arenaMaxX - 1.5f);
            }

            Vector3 spawnPos = new Vector3(spawnX, sueloY + 1.35f, 0f);
            GameObject minion = Instantiate(prefabElegido, spawnPos, Quaternion.identity);
            minion.name = $"{prefabElegido.name}_Invocado";
            minion.AddComponent<EsbirroInvocadoTracker>();

            // Darles un ligero salto dramático al emerger del vino
            Rigidbody2D mRb = minion.GetComponent<Rigidbody2D>();
            if (mRb != null)
            {
                mRb.linearVelocity = new Vector2((spawnX - transform.position.x) * 0.9f, 5.2f);
            }

            // Hacerlos ligeramente más ágiles en la arena (2 golpes para mantener combate dinámico y obtener corazones!)
            MerlotGrapeEnemy merlot = minion.GetComponent<MerlotGrapeEnemy>();
            if (merlot != null)
            {
                merlot.maxHealth = 2;
                merlot.currentHealth = 2;
                merlot.detectionRange = 18f;
            }
            EnemigoCabSau cab = minion.GetComponent<EnemigoCabSau>();
            if (cab != null)
            {
                cab.maxHealth = 2;
                cab.currentHealth = 2;
                cab.rangoDeteccion = 22f;
                int sueloLayer = LayerMask.NameToLayer("Suelo");
                cab.capaSuelo = sueloLayer >= 0 ? (1 << sueloLayer) : (1 << 6);
            }
            EnemigoNebbiolo neb = minion.GetComponent<EnemigoNebbiolo>();
            if (neb != null)
            {
                neb.maxHealth = 2;
                neb.currentHealth = 2;
                neb.rangoDetonacion = 4.2f;
            }

            esbirrosVivos.Add(minion);

            EnemyDeathFeedback.SpawnRuntimeGrapeJuiceSplash(
                spawnPos,
                14,
                new Color(0.45f, 0.08f, 0.55f, 1f),
                new Color(0.85f, 0.25f, 0.40f, 1f)
            );
        }

        yield return new WaitForSeconds(0.45f);
    }

    private GameObject ElegirSiguientePrefabUva()
    {
        // Garantiza que los 3 tipos (Merlot, Cab. Sau y Nebbiolo) salgan de forma equilibrada
        GameObject[] pool = new GameObject[] { prefabMerlot, prefabCabSau, prefabNebbiolo };
        for (int tries = 0; tries < 3; tries++)
        {
            int idx = (siguienteTipoInvocacion + tries) % 3;
            if (pool[idx] != null)
            {
                siguienteTipoInvocacion = (idx + 1) % 3;
                return pool[idx];
            }
        }
        return null;
    }

    // =========================================================================
    // ATAQUE 3 (FASE 2): CHORRO DE VINO TINTO A DISTANCIA
    // =========================================================================
    private IEnumerator AtaqueChorroVinoRoutine()
    {
        estadoActual = EstadoBoss.ChorroVino;

        if (playerTransform != null)
        {
            float dx = playerTransform.position.x - transform.position.x;
            direccionMovimiento = dx >= 0f ? 1f : -1f;
            ActualizarVisualDireccion(direccionMovimiento);
        }

        // Cargar vórtice de boca (sprite 9)
        ReproducirSonido(sonidoChorro, 0.9f);
        float prep = 0.55f;
        float t = 0f;
        while (t < prep && !EstaDerrotado)
        {
            t += Time.deltaTime;
            AnimarSecuencia(spritesPrepararChorro, 6f);
            yield return null;
        }
        if (EstaDerrotado) yield break;

        // Disparar ráfaga de 5 chorros de vino dirigidos hacia el jugador (alcanza plataformas elevadas!)
        int disparos = 5;
        for (int i = 0; i < disparos; i++)
        {
            if (EstaDerrotado) yield break;

            if (playerTransform != null)
            {
                float dx = playerTransform.position.x - transform.position.x;
                direccionMovimiento = dx >= 0f ? 1f : -1f;
                ActualizarVisualDireccion(direccionMovimiento);
            }

            AnimarSecuencia(spritesDispararChorro, 14f);

            Vector3 bocaPos = transform.position + new Vector3(direccionMovimiento * 1.35f, 0.45f, 0f);
            Vector2 dir = new Vector2(direccionMovimiento, 0.12f).normalized;
            if (playerTransform != null)
            {
                Vector2 toPlayer = (playerTransform.position + new Vector3(0f, 0.35f, 0f)) - bocaPos;
                // Compensar un poco la gravedad ligera del proyectil
                toPlayer.y += Mathf.Clamp(toPlayer.magnitude * 0.14f, 0f, 2.6f);
                dir = toPlayer.normalized;
            }

            // Pequeña dispersión en abanico
            float spreadDeg = (i - 2) * 5.5f;
            dir = Quaternion.Euler(0f, 0f, spreadDeg) * dir;

            ChorroVinoProyectil.Crear(bocaPos, dir * 13.8f, 18f);

            yield return new WaitForSeconds(0.16f);
        }

        yield return new WaitForSeconds(0.35f);
    }

    // =========================================================================
    // DAÑO AL JEFE (3000 HP) Y TRANSICIÓN A FASE 2
    // =========================================================================
    public void RecibirCorchazo(int multiplicadorCorcho = 1)
    {
        if (EstaDerrotado) return;

        // Si el jugador logra dispararle desde el borde antes de cruzar el trigger, despertar combate
        if (!combateIniciado)
        {
            IniciarCombate();
        }

        int danoReal = Mathf.Max(1, multiplicadorCorcho) * Mathf.Max(1, danoPorCorchazo);
        currentHealth = Mathf.Max(0, currentHealth - danoReal);

        ReproducirSonido(sonidoGolpe, 0.72f);

        EnemyDeathFeedback.SpawnRuntimeGrapeJuiceSplash(
            transform.position + new Vector3(Random.Range(-0.8f, 0.8f), Random.Range(-0.4f, 1.0f), 0f),
            8,
            new Color(0.75f, 0.06f, 0.20f, 1f),
            new Color(1.0f, 0.35f, 0.45f, 1f)
        );

        if (rutinaFlash != null) StopCoroutine(rutinaFlash);
        rutinaFlash = StartCoroutine(FlashGolpeRoutine());

        // Verificar entrada a Fase 2 (<= 50% HP = 1500 HP)
        if (!enFase2 && currentHealth > 0 && currentHealth <= maxHealth / 2)
        {
            enFase2 = true;
            StartCoroutine(TransicionFase2Routine());
        }

        ActualizarHUD();

        if (currentHealth <= 0)
        {
            IniciarDerrotaYSellado();
        }
    }

    // Compatibilidad con SendMessage("TakeDamage", int)
    public void TakeDamage(int dmg)
    {
        RecibirCorchazo(dmg);
    }

    /// <summary>
    /// Impacto del golpe especial "¡SUPER CORCHAZO DE RESERVA!" del jugador (350 HP de daño masivo).
    /// </summary>
    public void RecibirSuperCorchazo(int danoEspecial = 350)
    {
        if (EstaDerrotado) return;

        if (!combateIniciado)
        {
            IniciarCombate();
        }

        int danoReal = Mathf.Max(100, danoEspecial);
        currentHealth = Mathf.Max(0, currentHealth - danoReal);

        ReproducirSonido(sonidoRugido, 0.85f);
        ReproducirSonido(sonidoGolpe, 1.0f);

        EnemyDeathFeedback.SpawnRuntimeGrapeJuiceSplash(
            transform.position + new Vector3(0f, 0.4f, 0f),
            26,
            new Color(1.0f, 0.86f, 0.22f, 1f),
            new Color(0.95f, 0.14f, 0.28f, 1f)
        );

        MostrarMensajeBanner($"★ ¡SUPER CORCHAZO DE RESERVA! -{danoReal} HP ★", 2.4f);

        if (rutinaFlash != null) StopCoroutine(rutinaFlash);
        rutinaFlash = StartCoroutine(FlashSuperGolpeRoutine());

        if (!enFase2 && currentHealth > 0 && currentHealth <= maxHealth / 2)
        {
            enFase2 = true;
            StartCoroutine(TransicionFase2Routine());
        }

        ActualizarHUD();

        if (currentHealth <= 0)
        {
            IniciarDerrotaYSellado();
        }
    }

    private IEnumerator FlashSuperGolpeRoutine()
    {
        if (sr == null) yield break;
        Vector3 prevScale = transform.localScale;
        sr.color = new Color(1.0f, 0.96f, 0.55f, 1f);
        transform.localScale = new Vector3(escalaBase.x * 1.14f, escalaBase.y * 0.88f, 1f);
        yield return new WaitForSeconds(0.14f);
        sr.color = colorBase;
        transform.localScale = prevScale;
        rutinaFlash = null;
    }

    private IEnumerator TransicionFase2Routine()
    {
        EstadoBoss prev = estadoActual;
        estadoActual = EstadoBoss.TransicionFase2;

        colorBase = new Color(1.0f, 0.78f, 0.80f, 1f);
        ReproducirSonido(sonidoRugido, 1.0f);
        MostrarMensajeBanner("¡FASE II: FURIA CARMESÍ! ¡Cuidado con el Chorro de Vino!", 3.8f);
        ActualizarHUD();

        if (spriteErupcion != null) sr.sprite = spriteErupcion;

        float t = 0f;
        while (t < 1.0f && !EstaDerrotado)
        {
            t += Time.deltaTime;
            float pulse = 1f + Mathf.Sin(t * 28f) * 0.09f;
            transform.localScale = new Vector3(escalaBase.x * pulse, escalaBase.y * pulse, 1f);
            if (sr != null)
            {
                sr.color = Color.Lerp(Color.red, colorBase, Mathf.PingPong(t * 8f, 1f));
            }
            yield return null;
        }

        transform.localScale = escalaBase;
        if (sr != null) sr.color = colorBase;

        if (!EstaDerrotado)
        {
            estadoActual = prev == EstadoBoss.TransicionFase2 ? EstadoBoss.PatrullaOla : EstadoBoss.PatrullaOla;
        }
    }

    // =========================================================================
    // CONTADOR DE 6 ENEMIGOS INVOCADOS DERROTADOS -> APARICIÓN DE CORAZÓN
    // =========================================================================
    public void NotificarEsbirroDerrotado(Vector3 posicionEnemigo)
    {
        if (EstaDerrotado || LimpiandoEsbirros) return;

        esbirrosDerrotadosContador++;
        // Derrotar esbirros invocados recarga rápidamente el Super Corchazo (+22%)
        JugadorGolpeEspecial.NotificarCarga(22f);

        if (esbirrosDerrotadosContador >= esbirrosParaCorazon)
        {
            esbirrosDerrotadosContador = 0;
            AparecerCorazonCercaDelJugador();
        }

        ActualizarHUD();
    }

    private void AparecerCorazonCercaDelJugador()
    {
        if (prefabCorazon == null) return;

        Vector3 spawnPos = new Vector3((arenaMinX + arenaMaxX) * 0.5f, sueloY + 2.2f, 0f);
        if (playerTransform != null)
        {
            // Aparece muy cerca del jugador para ayudarle en medio de la pelea
            float offsetX = playerTransform.position.x < (arenaMinX + arenaMaxX) * 0.5f ? 2.4f : -2.4f;
            float targetX = Mathf.Clamp(playerTransform.position.x + offsetX, arenaMinX + 1.5f, arenaMaxX - 1.5f);
            float targetY = Mathf.Clamp(playerTransform.position.y + 0.85f, sueloY + 1.2f, sueloY + 9.5f);
            spawnPos = new Vector3(targetX, targetY, 0f);
        }

        GameObject corazon = Instantiate(prefabCorazon, spawnPos, Quaternion.identity);
        corazon.name = "Corazon_Recompensa_Boss";

        ReproducirSonido(sonidoCorazonSpawn, 0.92f);

        EnemyDeathFeedback.SpawnRuntimeGrapeJuiceSplash(
            spawnPos,
            18,
            new Color(1.0f, 0.30f, 0.52f, 1f),
            new Color(1.0f, 0.92f, 0.42f, 1f)
        );

        MostrarMensajeBanner($"¡{esbirrosParaCorazon} enemigos invocados derrotados! ♥ ¡Apareció un Corazón cerca de ti!", 3.5f);
    }

    // =========================================================================
    // DAÑO POR CONTACTO AL JUGADOR
    // =========================================================================
    private void OnTriggerStay2D(Collider2D other)
    {
        IntentarDanarJugadorPorContacto(other);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        IntentarDanarJugadorPorContacto(collision.collider);
    }

    private void IntentarDanarJugadorPorContacto(Collider2D other)
    {
        if (EstaDerrotado || other == null) return;
        if (Time.time - tiempoUltimoContacto < cooldownContacto) return;

        if (!other.CompareTag("Player") && other.GetComponent<MovimientoJugador>() == null)
            return;

        tiempoUltimoContacto = Time.time;

        HealthSystem hs = other.GetComponent<HealthSystem>();
        if (hs != null)
        {
            hs.TakeDamage(danoContactoJugador);
        }

        Rigidbody2D playerRb = other.GetComponent<Rigidbody2D>();
        if (playerRb != null)
        {
            float pushDir = other.transform.position.x >= transform.position.x ? 1f : -1f;
            playerRb.linearVelocity = new Vector2(pushDir * fuerzaEmpujeContacto, 6.8f);
        }
    }

    // =========================================================================
    // DERROTA DEL JEFE: SELLADO EN LA BOTELLA SAGRADA DEL NIVEL 3 + VICTORIA
    // =========================================================================
    private void IniciarDerrotaYSellado()
    {
        if (EstaDerrotado) return;
        EstaDerrotado = true;
        estadoActual = EstadoBoss.SellandoEnBotella;

        if (rutinaCombate != null) StopCoroutine(rutinaCombate);
        if (col != null) col.enabled = false;

        // Limpiar esbirros y proyectiles restantes para una cinemática limpia
        LimpiandoEsbirros = true;
        for (int i = 0; i < esbirrosVivos.Count; i++)
        {
            if (esbirrosVivos[i] != null)
            {
                EnemyDeathFeedback.SpawnRuntimeGrapeJuiceSplash(
                    esbirrosVivos[i].transform.position,
                    10,
                    new Color(0.55f, 0.12f, 0.65f, 1f),
                    new Color(0.95f, 0.78f, 0.30f, 1f)
                );
                Destroy(esbirrosVivos[i]);
            }
        }
        esbirrosVivos.Clear();

        ChorroVinoProyectil[] chorros = FindObjectsByType<ChorroVinoProyectil>(FindObjectsSortMode.None);
        for (int i = 0; i < chorros.Length; i++)
        {
            if (chorros[i] != null) Destroy(chorros[i].gameObject);
        }
        LimpiandoEsbirros = false;

        // Otorgar gran puntuación por derrotar al jefe final
        MetaNivel.RegistrarUvaEliminada(1500);
        ActualizarHUD();

        StartCoroutine(CinematicaSelladoEnBotellaRoutine());
    }

    private IEnumerator CinematicaSelladoEnBotellaRoutine()
    {
        ReproducirSonido(sonidoRugido, 0.9f);
        MostrarMensajeBanner("¡EL WINE MONSTER HA CAÍDO! ¡Sellándolo en la Botella Sagrada!", 4.0f);

        if (spriteErupcion != null) sr.sprite = spriteErupcion;

        // 1. Aparecer la Botella Sagrada del Nivel 3 flotando sobre el jefe
        Vector3 botellaPos = new Vector3(Mathf.Clamp(transform.position.x, arenaMinX + 4f, arenaMaxX - 4f), sueloY + 5.8f, 0f);
        GameObject botellaObj = new GameObject("BotellaSelladoFinal");
        botellaObj.transform.position = botellaPos + new Vector3(0f, 5.5f, 0f);
        botellaObj.transform.localScale = new Vector3(1.65f, 1.65f, 1f);

        SpriteRenderer botellaSr = botellaObj.AddComponent<SpriteRenderer>();
        botellaSr.sprite = spriteBotellaSellado;
        botellaSr.sortingOrder = 10;

        GameObject auraObj = new GameObject("AuraBotellaFinal");
        auraObj.transform.SetParent(botellaObj.transform);
        auraObj.transform.localPosition = Vector3.zero;
        auraObj.transform.localScale = new Vector3(1.6f, 1.6f, 1f);
        SpriteRenderer auraSr = auraObj.AddComponent<SpriteRenderer>();
        auraSr.sprite = spriteAuraBotella;
        auraSr.sortingOrder = 9;
        auraSr.color = new Color(0.45f, 1.0f, 0.65f, 0.8f);

        // Bajar la botella hacia el centro sobre el Wine Monster
        float dropT = 0f;
        Vector3 dropStart = botellaObj.transform.position;
        while (dropT < 0.85f)
        {
            dropT += Time.deltaTime;
            float ease = 1f - Mathf.Pow(1f - Mathf.Clamp01(dropT / 0.85f), 3f);
            botellaObj.transform.position = Vector3.Lerp(dropStart, botellaPos, ease);
            auraObj.transform.Rotate(0f, 0f, 90f * Time.deltaTime);
            yield return null;
        }

        // 2. Succión / sellado en vórtice: el Wine Monster se absorbe dentro de la boca de la botella
        ReproducirSonido(sonidoSellado, 1.0f);
        Vector3 bocaBotella = botellaPos + new Vector3(0f, 1.15f, 0f);
        Vector3 bossStartPos = transform.position;
        float absorbDuration = 2.15f;
        float elapsed = 0f;
        float burstTimer = 0f;

        while (elapsed < absorbDuration)
        {
            elapsed += Time.deltaTime;
            float p = Mathf.Clamp01(elapsed / absorbDuration);
            float easeIn = p * p;

            // Espiral hacia la boca de la botella
            float swirlRadius = (1f - p) * 1.4f;
            float angle = elapsed * 14f;
            Vector3 spiralOffset = new Vector3(Mathf.Cos(angle) * swirlRadius, Mathf.Sin(angle) * swirlRadius * 0.45f, 0f);
            transform.position = Vector3.Lerp(bossStartPos, bocaBotella, easeIn) + spiralOffset;

            float shrink = Mathf.Lerp(1f, 0.04f, easeIn);
            transform.localScale = new Vector3(escalaBase.x * shrink, escalaBase.y * shrink * (1f + p * 0.6f), 1f);
            transform.Rotate(0f, 0f, 420f * Time.deltaTime);

            if (spritesPrepararChorro != null && spritesPrepararChorro.Length > 0 && p > 0.35f)
                sr.sprite = spritesPrepararChorro[0];

            sr.color = Color.Lerp(colorBase, new Color(0.95f, 0.15f, 0.35f, 0.85f), p);

            // Aura brillante girando cada vez más rápido
            auraObj.transform.Rotate(0f, 0f, (140f + p * 300f) * Time.deltaTime);
            botellaObj.transform.position = botellaPos + new Vector3(Mathf.Sin(elapsed * 32f) * 0.08f * p, 0f, 0f);

            burstTimer -= Time.deltaTime;
            if (burstTimer <= 0f)
            {
                burstTimer = 0.16f;
                EnemyDeathFeedback.SpawnRuntimeGrapeJuiceSplash(
                    transform.position,
                    8,
                    new Color(0.85f, 0.12f, 0.28f, 1f),
                    new Color(0.45f, 1.0f, 0.65f, 1f)
                );
            }

            yield return null;
        }

        // Ocultar al jefe (¡ya está sellado dentro de la botella!)
        sr.enabled = false;

        // 3. Destello dorado final de la botella sellada
        botellaSr.color = new Color(1.0f, 0.92f, 0.68f, 1f);
        auraSr.color = new Color(1.0f, 0.88f, 0.35f, 0.95f);
        EnemyDeathFeedback.SpawnRuntimeGrapeJuiceSplash(
            botellaPos,
            32,
            new Color(1.0f, 0.88f, 0.30f, 1f),
            new Color(0.35f, 1.0f, 0.60f, 1f)
        );
        MostrarMensajeBanner("¡WINE MONSTER SELLADO EN LA BOTELLA! ¡VICTORIA TOTAL!", 3.0f);

        // Esperar un instante triunfal antes de mostrar la pantalla final
        float celebrateT = 0f;
        while (celebrateT < 1.35f)
        {
            celebrateT += Time.deltaTime;
            float pop = 1.65f + Mathf.Sin(celebrateT * 6f) * 0.12f;
            botellaObj.transform.localScale = new Vector3(pop, pop, 1f);
            auraObj.transform.Rotate(0f, 0f, 110f * Time.deltaTime);
            yield return null;
        }

        if (panelHudBoss != null)
            panelHudBoss.SetActive(false);

        if (rutinaBanner != null)
        {
            StopCoroutine(rutinaBanner);
            rutinaBanner = null;
        }
        if (textoBannerAviso != null)
            textoBannerAviso.gameObject.SetActive(false);

        // Abrir pantalla final "¡JUEGO COMPLETADO!"
        if (metaFinalNivel4 == null)
            metaFinalNivel4 = FindAnyObjectByType<MetaNivel>();

        if (metaFinalNivel4 != null)
        {
            metaFinalNivel4.tituloPersonalizado =
                "★ ¡JUEGO COMPLETADO! ★\n<size=15><color=#F5E6C8>¡Wine Monster sellado en la Botella!</color></size>";
            GameObject playerObj = playerTransform != null ? playerTransform.gameObject : null;
            metaFinalNivel4.CompletarNivel(playerObj);
        }
    }

    // =========================================================================
    // UTILIDADES VISUALES Y HUD
    // =========================================================================
    public void MostrarMensajeBanner(string mensaje, float duracion = 3.2f)
    {
        if (textoBannerAviso == null) return;
        if (rutinaBanner != null) StopCoroutine(rutinaBanner);
        rutinaBanner = StartCoroutine(MostrarBannerRoutine(mensaje, duracion));
    }

    private IEnumerator MostrarBannerRoutine(string mensaje, float duracion)
    {
        textoBannerAviso.gameObject.SetActive(true);
        textoBannerAviso.text = mensaje;
        yield return new WaitForSeconds(duracion);
        textoBannerAviso.gameObject.SetActive(false);
        rutinaBanner = null;
    }

    private void ActualizarHUD()
    {
        if (barraVidaBoss != null)
        {
            barraVidaBoss.maxValue = maxHealth;
            barraVidaBoss.value = currentHealth;
        }

        if (textoVidaBoss != null)
        {
            textoVidaBoss.text = $"WINE MONSTER  —  {currentHealth} / {maxHealth} HP";
        }

        if (textoFaseBoss != null)
        {
            if (EstaDerrotado)
                textoFaseBoss.text = "SELLADO EN LA BOTELLA";
            else if (enFase2)
                textoFaseBoss.text = "FASE II: FURIA CARMESÍ";
            else
                textoFaseBoss.text = "FASE I: MAREA DE LA CAVA";
        }

        if (textoEsbirrosCorazon != null)
        {
            textoEsbirrosCorazon.text = $"Uvas: {esbirrosDerrotadosContador}/{esbirrosParaCorazon} → ♥";
        }
    }

    private IEnumerator FlashGolpeRoutine()
    {
        if (sr == null) yield break;
        sr.color = new Color(1f, 0.55f, 0.55f, 1f);
        yield return new WaitForSeconds(0.08f);
        sr.color = colorBase;
        rutinaFlash = null;
    }

    private void AnimarSecuencia(Sprite[] frames, float fps)
    {
        if (sr == null || frames == null || frames.Length == 0) return;
        animTimer += Time.deltaTime;
        float step = 1f / Mathf.Max(1f, fps);
        if (animTimer >= step)
        {
            animTimer -= step;
            animFrame = (animFrame + 1) % frames.Length;
        }
        Sprite s = frames[animFrame % frames.Length];
        if (s != null) sr.sprite = s;
    }

    private void ActualizarVisualDireccion(float dirX)
    {
        if (sr == null) return;
        // Los sprites originales miran hacia la DERECHA -> flipX cuando mira a la IZQUIERDA
        sr.flipX = dirX < 0f;
    }

    private void BuscarJugador()
    {
        GameObject ch = GameObject.Find("Character");
        if (ch != null)
        {
            playerTransform = ch.transform;
            return;
        }
        MovimientoJugador mov = FindAnyObjectByType<MovimientoJugador>();
        if (mov != null) playerTransform = mov.transform;
    }

    private void LimpiarReferenciasEsbirrosNulos()
    {
        for (int i = esbirrosVivos.Count - 1; i >= 0; i--)
        {
            if (esbirrosVivos[i] == null)
                esbirrosVivos.RemoveAt(i);
        }
    }

    private void ReproducirSonido(AudioClip clip, float volumen)
    {
        if (clip == null) return;
        if (audioSource != null)
            audioSource.PlayOneShot(clip, volumen);
        else
            CorazonVida.ReproducirSonidoPersistente(clip, transform.position, volumen);
    }
}
