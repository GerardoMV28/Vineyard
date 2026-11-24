using UnityEngine;

/// <summary>
/// Proyectil de chorro de vino tinto disparado por el Jefe Final (Wine Monster) en su Fase 2.
/// Daña al jugador al impactar (incluyendo sobre plataformas elevadas) y salpica vino carmesí.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class ChorroVinoProyectil : MonoBehaviour
{
    public float danoJugador = 20f;
    public float tiempoVida = 3.2f;
    public float fuerzaEmpuje = 5.5f;

    private Rigidbody2D rb;
    private bool yaImpacto = false;

    public static ChorroVinoProyectil Crear(Vector3 posicion, Vector2 velocidad, float dano = 20f)
    {
        GameObject obj = new GameObject("ChorroVinoProyectil");
        obj.transform.position = posicion;

        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite = CrearSpriteGotaVino();
        sr.sortingOrder = 6;
        sr.color = Color.white;

        Rigidbody2D rb = obj.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 0.45f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.linearVelocity = velocidad;

        CircleCollider2D col = obj.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.34f;

        ChorroVinoProyectil proj = obj.AddComponent<ChorroVinoProyectil>();
        proj.danoJugador = dano;
        return proj;
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        Destroy(gameObject, tiempoVida);
    }

    private void Update()
    {
        if (rb != null && rb.linearVelocity.sqrMagnitude > 0.1f)
        {
            float angle = Mathf.Atan2(rb.linearVelocity.y, rb.linearVelocity.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (yaImpacto || other == null) return;

        // Ignorar al jefe, enemigos, corchos y otros chorros
        if (other.GetComponent<JefeWineMonster>() != null ||
            other.CompareTag("Enemy") ||
            other.GetComponent<MerlotGrapeEnemy>() != null ||
            other.GetComponent<EnemigoCabSau>() != null ||
            other.GetComponent<EnemigoNebbiolo>() != null ||
            other.GetComponent<CorkProjectile>() != null ||
            other.GetComponent<ChorroVinoProyectil>() != null)
        {
            return;
        }

        // Impacto contra el jugador
        if (other.CompareTag("Player") || other.GetComponent<MovimientoJugador>() != null)
        {
            yaImpacto = true;
            HealthSystem hs = other.GetComponent<HealthSystem>();
            if (hs != null)
            {
                hs.TakeDamage(danoJugador);
            }

            Rigidbody2D playerRb = other.GetComponent<Rigidbody2D>();
            if (playerRb != null && rb != null)
            {
                float dirX = rb.linearVelocity.x >= 0f ? 1f : -1f;
                playerRb.linearVelocity = new Vector2(dirX * fuerzaEmpuje, Mathf.Max(playerRb.linearVelocity.y, 4.2f));
            }

            SalpicarYDestruir();
            return;
        }

        // Impacto contra suelo o muro sólido
        int sueloLayer = LayerMask.NameToLayer("Suelo");
        if (!other.isTrigger && (other.gameObject.layer == sueloLayer || other.CompareTag("Ground") || other.CompareTag("Obstacle")))
        {
            yaImpacto = true;
            SalpicarYDestruir();
        }
    }

    private void SalpicarYDestruir()
    {
        EnemyDeathFeedback.SpawnRuntimeGrapeJuiceSplash(
            transform.position,
            10,
            new Color(0.65f, 0.04f, 0.16f, 1f),
            new Color(0.95f, 0.18f, 0.30f, 1f)
        );
        Destroy(gameObject);
    }

    private static Sprite cachedWineDropSprite;

    private static Sprite CrearSpriteGotaVino()
    {
        if (cachedWineDropSprite != null) return cachedWineDropSprite;

        const int w = 24;
        const int h = 14;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;

        Color clear = new Color(0, 0, 0, 0);
        Color outline = new Color(0.24f, 0.02f, 0.08f, 1f);
        Color darkWine = new Color(0.55f, 0.04f, 0.16f, 1f);
        Color brightWine = new Color(0.88f, 0.14f, 0.28f, 1f);
        Color shine = new Color(1.0f, 0.65f, 0.72f, 1f);

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                // Forma de chorro alargado hacia la derecha
                float nx = (x - 14f) / 9f;
                float ny = (y - 6.5f) / 5.2f;
                float dist = nx * nx + ny * ny;

                // Cola ondulada a la izquierda
                bool inTail = x >= 2 && x <= 14 && Mathf.Abs(y - 6.5f) <= (x * 0.28f + 0.8f);
                if (dist <= 1.0f || inTail)
                {
                    if (dist > 0.72f && !inTail)
                        tex.SetPixel(x, y, outline);
                    else if (x >= 15 && y >= 8 && y <= 10 && x <= 19)
                        tex.SetPixel(x, y, shine);
                    else if (y >= 6)
                        tex.SetPixel(x, y, brightWine);
                    else
                        tex.SetPixel(x, y, darkWine);
                }
                else
                {
                    tex.SetPixel(x, y, clear);
                }
            }
        }

        tex.Apply();
        cachedWineDropSprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 20f);
        return cachedWineDropSprite;
    }
}
