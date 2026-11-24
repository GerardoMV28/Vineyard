using UnityEngine;

/// <summary>
/// Proyectil de gota ácida lanzado en arco parabólico cuando explota una uva Nebbiolo.
/// Daña al jugador al contacto y salpica al chocar contra suelo o paredes.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class GotaAcidoProyectil : MonoBehaviour
{
    [Header("Configuración del Ácido")]
    public float danoJugador = 16f;
    public float tiempoVidaMaximo = 3.2f;
    public float gravedadAcido = 2.4f;

    private Rigidbody2D rb;
    private bool yaImpacto = false;
    private static Sprite cachedAcidDropSprite;

    public static Sprite ObtenerSpriteGotaAcido()
    {
        if (cachedAcidDropSprite != null) return cachedAcidDropSprite;

        int w = 12, h = 12;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        Color clear = new Color(0f, 0f, 0f, 0f);
        Color outline = new Color(0.10f, 0.24f, 0.05f, 1f);
        Color acidDeep = new Color(0.28f, 0.72f, 0.10f, 1f);
        Color acidBright = new Color(0.62f, 0.98f, 0.18f, 1f);
        Color acidGlow = new Color(0.88f, 1.0f, 0.45f, 1f);

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float dx = (x - 5.5f) / 4.8f;
                float dy = (y - 5.5f) / 4.8f;
                float d = dx * dx + dy * dy;
                if (d > 1.0f) tex.SetPixel(x, y, clear);
                else if (d > 0.68f) tex.SetPixel(x, y, outline);
                else if (x >= 6 && y >= 6) tex.SetPixel(x, y, acidGlow);
                else if (y >= 4) tex.SetPixel(x, y, acidBright);
                else tex.SetPixel(x, y, acidDeep);
            }
        }
        tex.Apply();
        cachedAcidDropSprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 26f);
        return cachedAcidDropSprite;
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.gravityScale = gravedadAcido;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }

        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
    }

    private void Start()
    {
        Destroy(gameObject, tiempoVidaMaximo);
    }

    public void Lanzar(Vector2 velocidadInicial)
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.gravityScale = gravedadAcido;
            rb.linearVelocity = velocidadInicial;
        }
    }

    private void Update()
    {
        if (rb != null && rb.linearVelocity.sqrMagnitude > 0.1f)
        {
            float ang = Mathf.Atan2(rb.linearVelocity.y, rb.linearVelocity.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, ang);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (yaImpacto || other == null) return;

        // Ignorar enemigos, otras gotas, corchos y triggers decorativos
        if (other.GetComponent<EnemigoNebbiolo>() != null ||
            other.GetComponent<EnemigoCabSau>() != null ||
            other.GetComponent<MerlotGrapeEnemy>() != null ||
            other.GetComponent<GotaAcidoProyectil>() != null ||
            other.GetComponent<CorkProjectile>() != null ||
            other.GetComponent<CharcoAcidoTemporal>() != null)
        {
            return;
        }

        if (other.CompareTag("Player") || other.GetComponent<MovimientoJugador>() != null)
        {
            yaImpacto = true;
            HealthSystem hs = other.GetComponent<HealthSystem>();
            if (hs == null) hs = other.gameObject.AddComponent<HealthSystem>();
            hs.TakeDamageFromPosition(danoJugador, transform.position);

            SalpicarYDestruir();
            return;
        }

        // Choque contra suelo o paredes sólidas
        bool esSuelo = other.gameObject.layer == LayerMask.NameToLayer("Suelo") ||
                       other.CompareTag("Ground") ||
                       other.CompareTag("Obstacle") ||
                       !other.isTrigger;

        if (esSuelo)
        {
            yaImpacto = true;
            SalpicarYDestruir();
        }
    }

    private void SalpicarYDestruir()
    {
        EnemyDeathFeedback.SpawnRuntimeGrapeJuiceSplash(
            transform.position,
            8,
            new Color(0.45f, 0.96f, 0.16f, 1f),
            new Color(0.78f, 1.0f, 0.30f, 1f)
        );
        Destroy(gameObject);
    }
}
