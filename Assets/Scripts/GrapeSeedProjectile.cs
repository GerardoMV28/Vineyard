using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class GrapeSeedProjectile : MonoBehaviour
{
    [Header("Configuración de la Semilla")]
    [Tooltip("Velocidad a la que viaja la semilla escupida por la uva")]
    public float speed = 7.5f;

    [Tooltip("Distancia máxima que recorre la semilla antes de desaparecer")]
    public float maxDistance = 7.5f;

    [Tooltip("Tiempo máximo de vida en segundos")]
    public float lifeTime = 3.5f;

    [Tooltip("Puntos de vida (HP) que resta al impactar al jugador (sobre base de 100 HP)")]
    public int damage = 20;

    [Header("Tamaño y Animación")]
    [Tooltip("Escala de la semilla")]
    public float seedScale = 0.75f;

    [Tooltip("Velocidad de rotación suave en vuelo")]
    public float spinSpeed = 240f;

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Vector2 moveDirection = Vector2.left;
    private Vector3 startPosition;
    private Vector3 baseScale;
    private bool hasHit = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        baseScale = new Vector3(seedScale, seedScale, 1f);
        transform.localScale = baseScale;
        startPosition = transform.position;

        rb.gravityScale = 0f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
    }

    private void Start()
    {
        startPosition = transform.position;
        Destroy(gameObject, lifeTime);
    }

    /// <summary>
    /// Lanza la semilla hacia un vector de dirección (por ejemplo, hacia el jugador).
    /// </summary>
    public void Launch(Vector2 dir)
    {
        if (dir.sqrMagnitude < 0.001f)
            dir = Vector2.left;

        moveDirection = dir.normalized;
        startPosition = transform.position;
        baseScale = new Vector3(seedScale, seedScale, 1f);
        transform.localScale = baseScale;

        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = moveDirection.x < 0f;
        }

        rb.linearVelocity = moveDirection * speed;
    }

    private void Update()
    {
        if (hasHit) return;

        if (rb != null)
        {
            rb.linearVelocity = moveDirection * speed;
        }

        transform.Rotate(0f, 0f, -Mathf.Sign(moveDirection.x) * spinSpeed * Time.deltaTime);

        float dist = Vector2.Distance(startPosition, transform.position);
        if (dist >= maxDistance)
        {
            hasHit = true;
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        HandleHit(other.gameObject, other.isTrigger);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        HandleHit(collision.gameObject, false);
    }

    private void HandleHit(GameObject other, bool isTrigger)
    {
        if (hasHit) return;

        // Ignorar colisión con las propias uvas enemigas o con otras semillas
        if (other.CompareTag("Enemy") || other.GetComponent<MerlotGrapeEnemy>() != null || other.GetComponent<GrapeSeedProjectile>() != null)
            return;

        // Si choca con un corcho del jugador, ambos pueden chocar o ignorarse (dejamos que el corcho pase a matar la uva)
        if (other.GetComponent<CorkProjectile>() != null)
            return;

        // Si impacta al Jugador
        HealthSystem healthSystem = other.GetComponent<HealthSystem>();
        MovimientoJugador playerMove = other.GetComponent<MovimientoJugador>();
        if (other.CompareTag("Player") || healthSystem != null || playerMove != null)
        {
            if (healthSystem == null && playerMove != null)
            {
                healthSystem = playerMove.gameObject.GetComponent<HealthSystem>();
                if (healthSystem == null)
                    healthSystem = playerMove.gameObject.AddComponent<HealthSystem>();
            }

            if (healthSystem != null)
            {
                healthSystem.TakeDamageFromPosition(damage, transform.position);
            }

            ImpactAndDestroy();
            return;
        }

        // Si choca contra el suelo u obstáculo
        bool isGroundOrObstacle = other.CompareTag("Obstacle") || other.CompareTag("Ground") ||
                                  other.layer == LayerMask.NameToLayer("Suelo");
        if (isGroundOrObstacle || !isTrigger)
        {
            ImpactAndDestroy();
        }
    }

    private void ImpactAndDestroy()
    {
        hasHit = true;
        StartCoroutine(ShrinkAndDestroy());
    }

    private IEnumerator ShrinkAndDestroy()
    {
        if (rb != null)
            rb.linearVelocity = Vector2.zero;

        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
            col.enabled = false;

        Vector3 s0 = transform.localScale;
        float t = 0f;
        float dur = 0.06f;
        while (t < dur)
        {
            t += Time.deltaTime;
            transform.localScale = Vector3.Lerp(s0, Vector3.zero, t / dur);
            yield return null;
        }

        Destroy(gameObject);
    }
}
