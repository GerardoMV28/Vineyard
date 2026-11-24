using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Proyectil especial "¡SUPER CORCHAZO DE RESERVA!" del jugador.
/// - Vuela recto a gran velocidad y largo alcance (ideal desde las plataformas elevadas contra el Wine Monster).
/// - Atraviesa y derrota al instante a uvas normales/invocadas en su camino.
/// - Al impactar al Jefe Final (JefeWineMonster), explota infligiendo 350 HP de daño masivo.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class SuperCorchazoProyectil : MonoBehaviour
{
    public float velocidad = 18.5f;
    public float distanciaMaxima = 17.5f;
    public int danoJefe = 350;
    public int danoEnemigoNormal = 10;

    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private float direccion = 1f;
    private Vector3 posicionInicio;
    private bool impactoFinal = false;
    private float trailTimer = 0f;
    private readonly HashSet<int> enemigosGolpeados = new HashSet<int>();

    public static SuperCorchazoProyectil CrearYLanzar(Vector3 posicion, float dir, int danoAlJefe = 350)
    {
        GameObject obj = new GameObject("SuperCorchazo_Proyectil");
        obj.transform.position = posicion;
        obj.transform.localScale = new Vector3(1.15f, 1.15f, 1f);

        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite = ObtenerSpriteSuperCorcho();
        sr.sortingOrder = 7;

        Rigidbody2D rb = obj.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 0f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        BoxCollider2D col = obj.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(1.25f, 0.72f);

        SuperCorchazoProyectil proj = obj.AddComponent<SuperCorchazoProyectil>();
        proj.danoJefe = danoAlJefe;
        proj.Lanzar(dir);
        return proj;
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        posicionInicio = transform.position;
    }

    public void Lanzar(float dir)
    {
        direccion = dir >= 0f ? 1f : -1f;
        posicionInicio = transform.position;
        if (sr != null) sr.flipX = direccion < 0f;
        if (rb != null) rb.linearVelocity = new Vector2(direccion * velocidad, 0f);
        Destroy(gameObject, 2.2f);
    }

    private void Update()
    {
        if (impactoFinal) return;

        // Pulso dorado vibrante en vuelo
        float pulse = 1.12f + Mathf.Sin(Time.time * 38f) * 0.10f;
        transform.localScale = new Vector3(pulse, pulse, 1f);

        trailTimer -= Time.deltaTime;
        if (trailTimer <= 0f)
        {
            trailTimer = 0.045f;
            EnemyDeathFeedback.SpawnRuntimeGrapeJuiceSplash(
                transform.position - new Vector3(direccion * 0.45f, 0f, 0f),
                4,
                new Color(1.0f, 0.86f, 0.22f, 1f),
                new Color(0.35f, 1.0f, 0.65f, 1f)
            );
        }

        if (Vector2.Distance(posicionInicio, transform.position) >= distanciaMaxima)
        {
            ExplotarYDestruir();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (impactoFinal || other == null) return;

        GameObject go = other.gameObject;

        // Ignorar al jugador y otros proyectiles aliados
        if (go.CompareTag("Player") ||
            go.GetComponent<MovimientoJugador>() != null ||
            go.GetComponent<CorkProjectile>() != null ||
            go.GetComponent<SuperCorchazoProyectil>() != null)
        {
            return;
        }

        // 1. Impacto masivo contra el Jefe Wine Monster (detiene y explota!)
        JefeWineMonster boss = go.GetComponent<JefeWineMonster>();
        if (boss != null)
        {
            impactoFinal = true;
            boss.RecibirSuperCorchazo(danoJefe);
            ExplotarYDestruir(granExplosion: true);
            return;
        }

        // 2. Atraviesa y derrota al instante a cualquier uva normal o invocada en el camino
        MerlotGrapeEnemy merlot = go.GetComponent<MerlotGrapeEnemy>();
        EnemigoCabSau cabSau = go.GetComponent<EnemigoCabSau>();
        EnemigoNebbiolo nebbiolo = go.GetComponent<EnemigoNebbiolo>();

        if (merlot != null || cabSau != null || nebbiolo != null)
        {
            int id = go.GetInstanceID();
            if (!enemigosGolpeados.Contains(id))
            {
                enemigosGolpeados.Add(id);
                if (merlot != null) merlot.TakeDamage(danoEnemigoNormal);
                else if (cabSau != null) cabSau.TakeDamage(danoEnemigoNormal);
                else if (nebbiolo != null) nebbiolo.TakeDamage(danoEnemigoNormal);

                EnemyDeathFeedback.SpawnRuntimeGrapeJuiceSplash(
                    go.transform.position,
                    14,
                    new Color(1.0f, 0.88f, 0.28f, 1f),
                    new Color(0.45f, 1.0f, 0.65f, 1f)
                );
            }
            return; // ¡Continúa atravesando hacia el Jefe!
        }

        // 3. Impacto contra muro o suelo sólido
        int sueloLayer = LayerMask.NameToLayer("Suelo");
        if (!other.isTrigger && (go.layer == sueloLayer || go.CompareTag("Ground") || go.CompareTag("Obstacle")))
        {
            impactoFinal = true;
            ExplotarYDestruir();
        }
    }

    private void ExplotarYDestruir(bool granExplosion = false)
    {
        impactoFinal = true;
        EnemyDeathFeedback.SpawnRuntimeGrapeJuiceSplash(
            transform.position,
            granExplosion ? 30 : 12,
            new Color(1.0f, 0.85f, 0.20f, 1f),
            granExplosion ? new Color(1.0f, 0.30f, 0.35f, 1f) : new Color(0.40f, 1.0f, 0.65f, 1f)
        );
        Destroy(gameObject);
    }

    private static Sprite cachedSuperCorkSprite;

    private static Sprite ObtenerSpriteSuperCorcho()
    {
        if (cachedSuperCorkSprite != null) return cachedSuperCorkSprite;

        const int w = 32;
        const int h = 18;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;

        Color clear = new Color(0, 0, 0, 0);
        Color aura = new Color(0.40f, 1.0f, 0.68f, 0.82f);
        Color outline = new Color(0.28f, 0.14f, 0.02f, 1f);
        Color goldDark = new Color(0.84f, 0.56f, 0.10f, 1f);
        Color goldMid = new Color(1.0f, 0.82f, 0.24f, 1f);
        Color goldLight = new Color(1.0f, 0.96f, 0.72f, 1f);
        Color rubyTip = new Color(0.95f, 0.20f, 0.36f, 1f);

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                bool inCore = (x >= 6 && x <= 27 && y >= 4 && y <= 13);
                bool inAura = (x >= 2 && x <= 29 && y >= 2 && y <= 15) &&
                              ((x >= 4 && y >= 3 && y <= 14) || (y >= 6 && y <= 11));

                if (inCore)
                {
                    bool border = (x == 6 || x == 27 || y == 4 || y == 13);
                    if (border)
                        tex.SetPixel(x, y, outline);
                    else if (x >= 22)
                        tex.SetPixel(x, y, (y >= 9 && y <= 11) ? goldLight : rubyTip);
                    else if (y >= 10)
                        tex.SetPixel(x, y, goldLight);
                    else if (y <= 6)
                        tex.SetPixel(x, y, goldDark);
                    else
                        tex.SetPixel(x, y, goldMid);
                }
                else if (inAura)
                {
                    // Estela luminosa dorada-esmeralda alrededor del Super Corcho
                    bool spark = ((x + y) % 2 == 0) || (x >= 26 && y >= 6 && y <= 11);
                    tex.SetPixel(x, y, spark ? aura : new Color(1.0f, 0.90f, 0.35f, 0.45f));
                }
                else
                {
                    tex.SetPixel(x, y, clear);
                }
            }
        }

        tex.Apply();
        cachedSuperCorkSprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 22f);
        return cachedSuperCorkSprite;
    }
}
