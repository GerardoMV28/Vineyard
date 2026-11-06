using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class CorkProjectile : MonoBehaviour
{
    [Header("Configuración de Lanzamiento y Alcance")]
    [Tooltip("Velocidad inicial a la que sale lanzado el corcho")]
    public float speed = 10f;

    [Tooltip("Distancia máxima que recorre el corcho antes de caer/desaparecer (alcance corto realista)")]
    public float maxDistance = 4.2f;

    [Tooltip("Tiempo máximo de vida de respaldo (en segundos)")]
    public float lifeTime = 3f;

    [Tooltip("Daño que causa al impactar contra un enemigo")]
    public int damage = 1;

    [Header("Tamaño y Arco Realista")]
    [Tooltip("Escala visual del corcho (más pequeño para que sea proporcional al personaje)")]
    public float corkScale = 0.45f;

    [Tooltip("Pequeño impulso inicial hacia arriba y caída suave al final del lanzamiento")]
    public bool useRealisticThrowArc = true;

    [Tooltip("Altura del pequeño arco al lanzarlo")]
    public float throwArcForce = 1.6f;

    [Header("Animación en Vuelo")]
    [Tooltip("Velocidad de rotación del corcho en el aire (grados por segundo)")]
    public float spinSpeed = 620f;

    [Tooltip("Efecto 'Pop' de estiramiento al salir disparado")]
    public bool usePopAnimation = true;

    [Header("Efectos de Impacto")]
    [Tooltip("Prefab opcional de partículas al chocar")]
    public GameObject hitEffectPrefab;

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private float direction = 1f;
    private Vector3 baseScale;
    private Vector3 startPosition;
    private bool hasHit = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        // Aplicar el tamaño pequeño proporcional al personaje
        baseScale = new Vector3(corkScale, corkScale, 1f);
        transform.localScale = baseScale;

        startPosition = transform.position;

        rb.gravityScale = 0f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
    }

    private void Start()
    {
        startPosition = transform.position;
        // Respaldo de autodestrucción a los 3 segundos
        Destroy(gameObject, lifeTime);
    }

    /// <summary>
    /// Inicializa el lanzamiento del corcho en la dirección indicada (1 = derecha, -1 = izquierda).
    /// </summary>
    public void Launch(float dir)
    {
        direction = Mathf.Sign(dir);
        if (direction == 0f) direction = 1f;

        startPosition = transform.position;
        baseScale = new Vector3(corkScale, corkScale, 1f);
        transform.localScale = baseScale;

        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = direction < 0f;
        }

        float initialVy = useRealisticThrowArc ? throwArcForce : 0f;
        rb.linearVelocity = new Vector2(direction * speed, initialVy);

        if (usePopAnimation && gameObject.activeInHierarchy)
        {
            StartCoroutine(PopScaleRoutine());
        }
    }

    private void Update()
    {
        if (hasHit) return;

        // 1. Calcular qué porcentaje de la distancia máxima ha recorrido (0.0 a 1.0)
        float traveledDistance = Vector2.Distance(
            new Vector2(startPosition.x, startPosition.y),
            new Vector2(transform.position.x, transform.position.y)
        );
        float progress = Mathf.Clamp01(traveledDistance / Mathf.Max(0.5f, maxDistance));

        // 2. Movimiento con sensación de lanzamiento real (pierde un poco de fuerza y cae suavemente al final)
        if (rb != null)
        {
            // Desacelera ligeramente en el último 30% del recorrido
            float speedFactor = Mathf.Lerp(1f, 0.55f, progress * progress);
            float currentVx = direction * speed * speedFactor;

            // Arco suave: sube un poquito al inicio y cae al acercarse al límite de distancia
            float currentVy = useRealisticThrowArc
                ? Mathf.Lerp(throwArcForce, -throwArcForce * 2.2f, progress)
                : 0f;

            rb.linearVelocity = new Vector2(currentVx, currentVy);
        }

        // 3. Giro continuo en el aire
        transform.Rotate(0f, 0f, -direction * spinSpeed * Time.deltaTime);

        // 4. Al llegar al límite de distancia (maxDistance), desaparecer suavemente
        if (traveledDistance >= maxDistance)
        {
            hasHit = true;
            StartCoroutine(FadeOutAtMaxRange());
        }
    }

    private IEnumerator PopScaleRoutine()
    {
        Vector3 stretched = new Vector3(baseScale.x * 1.35f, baseScale.y * 0.7f, baseScale.z);
        Vector3 oversized = new Vector3(baseScale.x * 0.92f, baseScale.y * 1.15f, baseScale.z);

        transform.localScale = stretched;

        float t = 0f;
        float halfDuration = 0.05f;

        while (t < halfDuration)
        {
            t += Time.deltaTime;
            transform.localScale = Vector3.Lerp(stretched, oversized, t / halfDuration);
            yield return null;
        }

        t = 0f;
        while (t < halfDuration)
        {
            t += Time.deltaTime;
            transform.localScale = Vector3.Lerp(oversized, baseScale, t / halfDuration);
            yield return null;
        }

        transform.localScale = baseScale;
    }

    private IEnumerator FadeOutAtMaxRange()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        Vector3 startScale = transform.localScale;
        Color startColor = spriteRenderer != null ? spriteRenderer.color : Color.white;

        float duration = 0.1f;
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            float norm = t / duration;
            transform.localScale = Vector3.Lerp(startScale, Vector3.zero, norm);
            if (spriteRenderer != null)
            {
                spriteRenderer.color = new Color(startColor.r, startColor.g, startColor.b, 1f - norm);
            }
            yield return null;
        }

        Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        HandleImpact(other.gameObject, other.isTrigger);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        HandleImpact(collision.gameObject, false);
    }

    private void HandleImpact(GameObject other, bool isTriggerCollider)
    {
        if (hasHit) return;

        // Ignorar al propio jugador o proyectiles
        if (other.CompareTag("Player") || other.GetComponent<MovimientoJugador>() != null)
            return;

        if (other.GetComponent<CorkProjectile>() != null)
            return;

        bool isEnemy = other.CompareTag("Enemy") || other.GetComponent<MerlotGrapeEnemy>() != null;
        bool isObstacle = other.CompareTag("Obstacle") || other.CompareTag("Ground") ||
                          other.layer == LayerMask.NameToLayer("Suelo");

        if (isTriggerCollider && !isEnemy && !isObstacle)
            return;

        if (isEnemy)
        {
            Debug.Log($"[CorkProjectile] ¡Corchazo a enemigo: {other.name}! (Daño: {damage})");
            other.SendMessage("TakeDamage", damage, SendMessageOptions.DontRequireReceiver);
            other.SendMessage("RecibirDano", damage, SendMessageOptions.DontRequireReceiver);
            ImpactAndDestroy();
        }
        else if (isObstacle || !isTriggerCollider)
        {
            ImpactAndDestroy();
        }
    }

    private void ImpactAndDestroy()
    {
        hasHit = true;

        if (hitEffectPrefab != null)
        {
            Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);
        }

        StartCoroutine(ImpactSquashAndDestroy());
    }

    private IEnumerator ImpactSquashAndDestroy()
    {
        if (rb != null)
            rb.linearVelocity = Vector2.zero;

        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
            col.enabled = false;

        Vector3 hitScale = new Vector3(baseScale.x * 0.5f, baseScale.y * 1.35f, baseScale.z);
        transform.localScale = hitScale;

        float duration = 0.06f;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            transform.localScale = Vector3.Lerp(hitScale, Vector3.zero, t / duration);
            yield return null;
        }

        Destroy(gameObject);
    }
}
