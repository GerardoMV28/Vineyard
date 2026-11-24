using System.Collections;
using UnityEngine;

/// <summary>
/// Enemigo explosivo "Nebbiolo" (racimo morado oscuro hinchable con ácido tóxico) para el Nivel 3 (La Cava).
/// - Patrulla suavemente su zona o vigila piezas de botella.
/// - Cuando el jugador se acerca dentro de rangoDetonacion, se hincha agresivamente con humo verde ácido
///   (spritesHinchando: nebbiolo_10, 4, 14, 6, 11) y EXPLOTA:
///   1) Inflige daño en área (radioExplosion) si el jugador sigue cerca.
///   2) Lanza múltiples gotas de ácido en arco parabólico (GotaAcidoProyectil) para dañar al jugador.
///   3) Deja un charco de ácido burbujeante temporal en el suelo (CharcoAcidoTemporal con nebbiolo_5, 9, 12).
/// - También puede ser derrotada desde lejos con 3 impactos de corcho antes de explotar.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(AudioSource))]
public class EnemigoNebbiolo : MonoBehaviour
{
    [Header("Salud y Puntuación")]
    public int maxHealth = 3;
    public int currentHealth = 3;
    public int puntosAlDerrotar = 175;

    [Header("Detección y Detonación por Cercanía")]
    [Tooltip("Distancia a la que detecta al jugador y comienza a hincharse para explotar")]
    public float rangoDetonacion = 4.4f;

    [Tooltip("Diferencia máxima de altura (Y) para iniciar la detonación")]
    public float diferenciaAlturaMaxima = 2.8f;

    [Tooltip("Tiempo total que tarda en hincharse antes de explotar (da margen justo para reaccionar)")]
    public float tiempoHinchadoAntesDeExplotar = 1.15f;

    [Tooltip("Radio de daño directo de la explosión ácida")]
    public float radioExplosion = 3.0f;

    [Tooltip("Daño directo de la explosión si el jugador está dentro del radio")]
    public float danoExplosion = 26f;

    [Header("Proyectiles de Ácido y Charco Residual")]
    [Tooltip("Cantidad de gotas ácidas lanzadas en arco al explotar")]
    public int cantidadGotasAcido = 6;

    [Tooltip("Daño de cada gota ácida proyectil")]
    public float danoPorGotaAcido = 16f;

    [Tooltip("Duración del charco de ácido burbujeante dejado en el suelo")]
    public float duracionCharcoAcido = 6.5f;

    [Header("Patrulla Suave")]
    public bool patrullarSuave = true;
    public float velocidadPatrulla = 1.15f;
    public float distanciaPatrulla = 1.6f;

    [Header("Sprites de Animación (Nebbiolo 1 al 14)")]
    [Tooltip("Frame en reposo (nebbiolo_1)")]
    public Sprite spriteIdle;

    [Tooltip("Secuencia de hinchado agresivo (nebbiolo_10, 4, 14, 6, 11)")]
    public Sprite[] spritesHinchando;

    [Tooltip("Secuencia de estallido ácido (nebbiolo_8, 7, 3, 13, 2)")]
    public Sprite[] spritesExplosion;

    [Tooltip("Secuencia del charco ácido burbujeante en el suelo (nebbiolo_5, 9, 12)")]
    public Sprite[] spritesCharcoAcido;

    [Header("Audio (SFX de Nebbiolo)")]
    public AudioSource audioSource;
    public AudioClip sonidoHinchando;
    public AudioClip sonidoExplosionAcido;
    public AudioClip sonidoGolpe;
    [Range(0f, 1f)]
    public float volumenAudio = 0.9f;

    private SpriteRenderer sr;
    private Rigidbody2D rb;
    private Collider2D col;
    private Transform playerTransform;

    private Vector3 posicionInicial;
    private Vector3 escalaBase;
    private bool estaHinchando = false;
    private bool estaExplotando = false;
    private bool estaMuerto = false;
    private bool estaHerido = false;

    private void Awake()
    {
        currentHealth = maxHealth;
        sr = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();

        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;

        if (rb != null)
        {
            rb.gravityScale = 2.5f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }

        escalaBase = transform.localScale;
        posicionInicial = transform.position;
    }

    private void Start()
    {
        posicionInicial = transform.position;
        if (spriteIdle != null && sr != null)
            sr.sprite = spriteIdle;
        BuscarJugador();
    }

    private void BuscarJugador()
    {
        if (playerTransform != null) return;
        MovimientoJugador mov = FindAnyObjectByType<MovimientoJugador>();
        if (mov != null)
        {
            playerTransform = mov.transform;
            return;
        }
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) playerTransform = p.transform;
    }

    private void Update()
    {
        if (estaMuerto || estaExplotando)
            return;

        if (playerTransform == null)
            BuscarJugador();

        if (!estaHinchando && playerTransform != null)
        {
            float dx = playerTransform.position.x - transform.position.x;
            float dy = Mathf.Abs(playerTransform.position.y - transform.position.y);

            if (sr != null && Mathf.Abs(dx) > 0.15f)
                sr.flipX = dx < 0f;

            // Si el jugador se acerca lo suficiente, ¡comienza a hincharse para explotar!
            if (Mathf.Abs(dx) <= rangoDetonacion && dy <= diferenciaAlturaMaxima)
            {
                StartCoroutine(SecuenciaHincharYExplotar());
                return;
            }
        }

        if (!estaHinchando && !estaHerido)
        {
            // Respiración leve amenazante
            float breath = 1f + Mathf.Sin(Time.time * 4.5f + posicionInicial.x) * 0.05f;
            transform.localScale = new Vector3(escalaBase.x * breath, escalaBase.y * (2f - breath), escalaBase.z);

            if (patrullarSuave && rb != null && rb.bodyType == RigidbodyType2D.Dynamic)
            {
                float targetX = posicionInicial.x + Mathf.Sin((Time.time + posicionInicial.x) * velocidadPatrulla) * distanciaPatrulla;
                float diff = targetX - transform.position.x;
                rb.linearVelocity = new Vector2(Mathf.Clamp(diff * 3.5f, -velocidadPatrulla, velocidadPatrulla), rb.linearVelocity.y);
            }
        }
    }

    private IEnumerator SecuenciaHincharYExplotar()
    {
        estaHinchando = true;
        if (rb != null)
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

        if (sonidoHinchando != null && audioSource != null)
            audioSource.PlayOneShot(sonidoHinchando, volumenAudio);

        int totalFrames = (spritesHinchando != null && spritesHinchando.Length > 0) ? spritesHinchando.Length : 5;
        float elapsed = 0f;

        while (elapsed < tiempoHinchadoAntesDeExplotar && !estaMuerto && !estaExplotando)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / Mathf.Max(0.15f, tiempoHinchadoAntesDeExplotar));

            if (spritesHinchando != null && spritesHinchando.Length > 0 && sr != null)
            {
                int idx = Mathf.Min(spritesHinchando.Length - 1, Mathf.FloorToInt(progress * totalFrames));
                if (spritesHinchando[idx] != null)
                    sr.sprite = spritesHinchando[idx];
            }

            // Se hincha progresivamente y vibra con energía ácida verde
            float swell = Mathf.Lerp(1.0f, 1.42f, progress);
            float wobble = Mathf.Sin(elapsed * 32f) * 0.07f * progress;
            transform.localScale = new Vector3(escalaBase.x * (swell + wobble), escalaBase.y * (swell - wobble), escalaBase.z);

            if (sr != null)
            {
                bool flash = Mathf.FloorToInt(elapsed * (8f + progress * 16f)) % 2 == 0;
                sr.color = flash
                    ? Color.Lerp(Color.white, new Color(0.62f, 1.0f, 0.35f, 1f), progress)
                    : Color.white;
            }

            yield return null;
        }

        if (!estaMuerto && !estaExplotando)
        {
            yield return StartCoroutine(ExplotarAcidoRoutine(explosionCompleta: true));
        }
    }

    public void TakeDamage(int damageAmount)
    {
        if (estaMuerto || estaExplotando) return;

        currentHealth -= Mathf.Max(1, damageAmount);
        Debug.Log($"[EnemigoNebbiolo] {name} recibió daño. Vida: {currentHealth}/{maxHealth}");

        if (currentHealth <= 0)
        {
            StopAllCoroutines();
            // Si ya estaba hinchándose al morir, detona por completo; si la matan de lejos tranquila, libera solo 2 gotas
            StartCoroutine(ExplotarAcidoRoutine(explosionCompleta: estaHinchando));
        }
        else
        {
            if (sonidoGolpe != null && audioSource != null)
                audioSource.PlayOneShot(sonidoGolpe, volumenAudio);

            if (!estaHinchando)
                StartCoroutine(FlashHeridaRoutine());
        }
    }

    public void RecibirDano(int damageAmount)
    {
        TakeDamage(damageAmount);
    }

    private IEnumerator FlashHeridaRoutine()
    {
        estaHerido = true;
        if (sr != null) sr.color = new Color(1f, 0.5f, 0.6f, 1f);
        transform.localScale = new Vector3(escalaBase.x * 0.85f, escalaBase.y * 1.18f, escalaBase.z);
        yield return new WaitForSeconds(0.12f);
        if (sr != null && !estaExplotando) sr.color = Color.white;
        if (!estaHinchando && !estaExplotando) transform.localScale = escalaBase;
        estaHerido = false;
    }

    private IEnumerator ExplotarAcidoRoutine(bool explosionCompleta)
    {
        estaExplotando = true;
        estaMuerto = true;

        MetaNivel.RegistrarUvaEliminada(puntosAlDerrotar);

        if (col != null) col.enabled = false;
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Kinematic;
        }

        if (sonidoExplosionAcido != null)
        {
            CorazonVida.ReproducirSonidoPersistente(sonidoExplosionAcido, transform.position, volumenAudio);
        }

        // 1. Daño en área al jugador si está dentro del radio de explosión
        if (explosionCompleta && playerTransform != null)
        {
            float dist = Vector2.Distance(transform.position, playerTransform.position);
            if (dist <= radioExplosion)
            {
                HealthSystem hs = playerTransform.GetComponent<HealthSystem>();
                if (hs == null) hs = playerTransform.gameObject.AddComponent<HealthSystem>();
                hs.TakeDamageFromPosition(danoExplosion, transform.position);
            }
        }

        // 2. Lanzar proyectiles de ácido en arco parabólico hacia ambos lados
        int numGotas = explosionCompleta ? Mathf.Max(4, cantidadGotasAcido) : 2;
        LanzarAbanicoDeAcido(numGotas);

        // 3. Crear charco ácido burbujeante temporal en el suelo donde explotó
        if (explosionCompleta)
        {
            CrearCharcoAcidoEnSuelo();
        }

        // 4. Salpicadura de partículas morado + verde ácido
        EnemyDeathFeedback.SpawnRuntimeGrapeJuiceSplash(
            transform.position,
            explosionCompleta ? 32 : 20,
            new Color(0.45f, 0.98f, 0.16f, 1f),
            new Color(0.42f, 0.08f, 0.62f, 1f)
        );

        // 5. Reproducir los frames de estallido (nebbiolo_8, 7, 3, 13, 2)
        if (sr != null) sr.color = Color.white;
        transform.localScale = escalaBase * (explosionCompleta ? 1.35f : 1.15f);

        if (spritesExplosion != null && spritesExplosion.Length > 0 && sr != null)
        {
            for (int i = 0; i < spritesExplosion.Length; i++)
            {
                if (spritesExplosion[i] != null)
                    sr.sprite = spritesExplosion[i];
                yield return new WaitForSeconds(0.065f);
            }
        }
        else
        {
            yield return new WaitForSeconds(0.18f);
        }

        Destroy(gameObject);
    }

    private void LanzarAbanicoDeAcido(int cantidad)
    {
        Vector3 origen = transform.position + new Vector3(0f, 0.25f, 0f);
        Sprite dropSprite = GotaAcidoProyectil.ObtenerSpriteGotaAcido();

        for (int i = 0; i < cantidad; i++)
        {
            // Distribuir en un abanico amplio desde 25° hasta 155° (arcos altos y medios a izquierda y derecha)
            float t = (cantidad == 1) ? 0.5f : (float)i / (cantidad - 1);
            float angleDeg = Mathf.Lerp(28f, 152f, t);
            float angleRad = angleDeg * Mathf.Deg2Rad;
            float speed = (i % 2 == 0) ? 8.2f : 6.4f;

            Vector2 vel = new Vector2(Mathf.Cos(angleRad) * speed, Mathf.Sin(angleRad) * speed + 2.4f);

            GameObject drop = new GameObject("GotaAcido_Nebbiolo");
            drop.transform.position = origen;

            SpriteRenderer dropSr = drop.AddComponent<SpriteRenderer>();
            dropSr.sprite = dropSprite;
            dropSr.sortingOrder = 6;

            Rigidbody2D dropRb = drop.AddComponent<Rigidbody2D>();
            dropRb.gravityScale = 2.4f;
            dropRb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            CircleCollider2D dropCol = drop.AddComponent<CircleCollider2D>();
            dropCol.isTrigger = true;
            dropCol.radius = 0.20f;

            GotaAcidoProyectil proj = drop.AddComponent<GotaAcidoProyectil>();
            proj.danoJugador = danoPorGotaAcido;
            proj.Lanzar(vel);
        }
    }

    private void CrearCharcoAcidoEnSuelo()
    {
        Vector3 puddlePos = transform.position + new Vector3(0f, -0.35f, 0f);
        int sueloMask = LayerMask.GetMask("Suelo");
        RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.down, 4.5f, sueloMask != 0 ? sueloMask : ~0);
        if (hit.collider != null && hit.collider != col && !hit.collider.isTrigger)
        {
            puddlePos = new Vector3(hit.point.x, hit.point.y + 0.55f, 0f);
        }

        GameObject puddle = new GameObject("CharcoAcido_Nebbiolo");
        puddle.transform.position = puddlePos;
        puddle.transform.localScale = new Vector3(1.25f, 1.15f, 1f);

        SpriteRenderer pSr = puddle.AddComponent<SpriteRenderer>();
        pSr.sortingOrder = 4;
        if (spritesCharcoAcido != null && spritesCharcoAcido.Length > 0)
            pSr.sprite = spritesCharcoAcido[0];

        BoxCollider2D pCol = puddle.AddComponent<BoxCollider2D>();
        pCol.isTrigger = true;
        pCol.size = new Vector2(1.45f, 0.55f);
        pCol.offset = new Vector2(0f, -0.38f);

        CharcoAcidoTemporal charco = puddle.AddComponent<CharcoAcidoTemporal>();
        charco.danoPorContacto = 15f;
        charco.duracionSegundos = duracionCharcoAcido;
        charco.spritesCharco = spritesCharcoAcido;
        charco.sonidoSalpicadura = sonidoExplosionAcido;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (estaMuerto || estaExplotando) return;
        if (collision.gameObject.CompareTag("Player") || collision.gameObject.GetComponent<MovimientoJugador>() != null)
        {
            // Al tocarla directamente se dispara su explosión de inmediato tras un breve destello
            if (!estaHinchando)
            {
                tiempoHinchadoAntesDeExplotar = Mathf.Min(tiempoHinchadoAntesDeExplotar, 0.45f);
                StartCoroutine(SecuenciaHincharYExplotar());
            }
        }
    }

    private void OnGUI()
    {
        if (estaMuerto || currentHealth >= maxHealth || Camera.main == null)
            return;

        Vector3 worldPos = transform.position + new Vector3(0f, 1.35f, 0f);
        Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPos);
        if (screenPos.z <= 0f) return;

        float guiX = screenPos.x - 24f;
        float guiY = Screen.height - screenPos.y - 8f;

        GUI.color = new Color(0.10f, 0.04f, 0.18f, 0.88f);
        GUI.DrawTexture(new Rect(guiX - 2, guiY - 2, 52, 9), Texture2D.whiteTexture);

        float fill = Mathf.Clamp01((float)currentHealth / maxHealth);
        GUI.color = new Color(0.55f, 0.96f, 0.20f, 1f); // Verde ácido Nebbiolo
        GUI.DrawTexture(new Rect(guiX, guiY, 48 * fill, 5), Texture2D.whiteTexture);
        GUI.color = Color.white;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.55f, 1f, 0.2f, 0.45f);
        Gizmos.DrawWireSphere(transform.position, rangoDetonacion);
        Gizmos.color = new Color(0.95f, 0.25f, 0.15f, 0.65f);
        Gizmos.DrawWireSphere(transform.position, radioExplosion);
    }
}
