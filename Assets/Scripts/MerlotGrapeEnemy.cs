using System.Collections;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Collider2D))]
public class MerlotGrapeEnemy : MonoBehaviour
{
    [Header("Salud de la Uva Merlot")]
    [Tooltip("Cantidad de corchazos necesarios para derrotar a la uva")]
    public int maxHealth = 3;
    public int currentHealth;

    [Header("Detección y Disparo de Semillas")]
    [Tooltip("Distancia a la que detecta al jugador para escupirle semillas")]
    public float detectionRange = 9.0f;

    [Tooltip("Tiempo entre cada escupitajo de semilla (segundos)")]
    public float attackInterval = 2.2f;

    [Tooltip("Prefab de la semilla que lanza por la boca")]
    public GameObject seedPrefab;

    [Tooltip("Punto de la boca desde donde sale la semilla")]
    public Transform mouthPoint;

    [Tooltip("Desplazamiento de la boca respecto al centro de la uva")]
    public Vector2 mouthOffset = new Vector2(0.42f, 0.04f);

    [Header("Movimiento / Patrulla")]
    [Tooltip("Si está activo, camina un poquito de lado a lado mientras no ataca")]
    public bool enableSmallPatrol = true;
    public float patrolSpeed = 1.2f;
    public float patrolDistance = 1.2f;

    [Header("Sprites de Animación (Merlot 1 al 10)")]
    [Tooltip("Frames de reposo/movimiento (merlot_1, merlot_2, merlot_3)")]
    public Sprite[] idleSprites;

    [Tooltip("Frames de ataque: inflar cachetes, abrir boca y escupir (merlot_9, merlot_10, merlot_8, merlot_4, merlot_6)")]
    public Sprite[] attackSprites;

    [Tooltip("Frames de daño al recibir un corchazo (merlot_5, merlot_7)")]
    public Sprite[] hurtSprites;

    [Tooltip("Velocidad de cuadros por segundo en reposo")]
    public float idleFrameRate = 4.5f;

    private SpriteRenderer spriteRenderer;
    private Rigidbody2D rb;
    private Animator animator;
    private Transform playerTransform;

    private Vector3 startPos;
    private Vector3 baseScale;
    private float facingDirection = -1f; // -1 mira a la izquierda, 1 mira a la derecha
    private float nextAttackTime = 0f;
    private float idleTimer = 0f;
    private int currentIdleFrame = 0;

    private bool isAttacking = false;
    private bool isHurt = false;
    private bool isDead = false;

    private void Awake()
    {
        currentHealth = maxHealth;
        spriteRenderer = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        baseScale = transform.localScale;
        startPos = transform.position;
    }

    private void Start()
    {
        startPos = transform.position;

        // Buscar al jugador automáticamente
        MovimientoJugador playerMove = FindFirstObjectByType<MovimientoJugador>();
        if (playerMove != null)
        {
            playerTransform = playerMove.transform;
            if (playerMove.GetComponent<PlayerHealth>() == null)
            {
                playerMove.gameObject.AddComponent<PlayerHealth>();
            }
        }

        // Crear MouthPoint automáticamente si no existe
        if (mouthPoint == null)
        {
            Transform existing = transform.Find("MouthPoint");
            if (existing != null)
            {
                mouthPoint = existing;
            }
            else
            {
                GameObject mp = new GameObject("MouthPoint");
                mp.transform.SetParent(transform);
                mp.transform.localPosition = new Vector3(mouthOffset.x, mouthOffset.y, 0f);
                mouthPoint = mp.transform;
            }
        }

        // Desfase aleatorio para que varias uvas en el mapa no disparen exactamente al mismo milisegundo
        nextAttackTime = Time.time + Random.Range(0.4f, 1.3f);
    }

    private void Update()
    {
        if (isDead) return;

        if (playerTransform == null)
        {
            MovimientoJugador pm = FindFirstObjectByType<MovimientoJugador>();
            if (pm != null) playerTransform = pm.transform;
        }

        float distToPlayer = float.MaxValue;
        if (playerTransform != null)
        {
            distToPlayer = Vector2.Distance(transform.position, playerTransform.position);

            // Mirar siempre hacia el jugador cuando está cerca
            if (distToPlayer <= detectionRange * 1.25f)
            {
                float dx = playerTransform.position.x - transform.position.x;
                if (Mathf.Abs(dx) > 0.1f)
                {
                    facingDirection = Mathf.Sign(dx);
                }
            }
        }

        UpdateFacingAndMouth();

        if (isHurt || isAttacking)
            return;

        // Si el jugador está dentro del rango de detección y se cumplió el cooldown, ¡escupir semilla!
        if (distToPlayer <= detectionRange && Time.time >= nextAttackTime)
        {
            StartCoroutine(AttackSpitSeedRoutine());
            return;
        }

        // Patrulla corta opcional cuando no está disparando
        if (enableSmallPatrol && distToPlayer > detectionRange * 0.7f)
        {
            float offset = Mathf.Sin((Time.time + startPos.x) * patrolSpeed) * patrolDistance;
            float targetX = startPos.x + offset;
            float moveDir = targetX - transform.position.x;
            if (Mathf.Abs(moveDir) > 0.01f && distToPlayer > detectionRange)
            {
                facingDirection = Mathf.Sign(moveDir);
            }

            if (rb != null && rb.bodyType == RigidbodyType2D.Dynamic)
            {
                rb.linearVelocity = new Vector2(Mathf.Clamp(moveDir * 4f, -patrolSpeed, patrolSpeed), rb.linearVelocity.y);
            }
            else
            {
                transform.position = new Vector3(targetX, transform.position.y, transform.position.z);
            }
        }
        else if (rb != null && rb.bodyType == RigidbodyType2D.Dynamic)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        }

        AnimateIdle();
    }

    private void UpdateFacingAndMouth()
    {
        // Los sprites originales de Merlot miran hacia la DERECHA (la boca apunta a la derecha)
        // Por lo tanto: flipX = false cuando mira a la derecha (1), flipX = true cuando mira a la izquierda (-1)
        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = facingDirection < 0f;
        }

        if (mouthPoint != null)
        {
            mouthPoint.localPosition = new Vector3(
                Mathf.Abs(mouthOffset.x) * facingDirection,
                mouthOffset.y,
                0f
            );
        }
    }

    private void AnimateIdle()
    {
        if (idleSprites == null || idleSprites.Length == 0 || spriteRenderer == null)
            return;

        idleTimer += Time.deltaTime * idleFrameRate;
        currentIdleFrame = Mathf.FloorToInt(idleTimer) % idleSprites.Length;
        if (idleSprites[currentIdleFrame] != null)
        {
            spriteRenderer.sprite = idleSprites[currentIdleFrame];
        }
    }

    private IEnumerator AttackSpitSeedRoutine()
    {
        isAttacking = true;
        if (rb != null && rb.bodyType == RigidbodyType2D.Dynamic)
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

        // Fase 1: Inflar cachetes (merlot_9 y merlot_10)
        if (attackSprites != null && attackSprites.Length > 0 && attackSprites[0] != null)
            spriteRenderer.sprite = attackSprites[0];

        // Efecto visual de inflarse
        transform.localScale = new Vector3(baseScale.x * 1.14f, baseScale.y * 1.08f, baseScale.z);
        yield return new WaitForSeconds(0.18f);

        if (!isHurt && !isDead)
        {
            if (attackSprites != null && attackSprites.Length > 1 && attackSprites[1] != null)
                spriteRenderer.sprite = attackSprites[1];

            transform.localScale = new Vector3(baseScale.x * 1.22f, baseScale.y * 0.94f, baseScale.z);
            yield return new WaitForSeconds(0.14f);
        }

        // Fase 2: Abrir la boca con colmillos (merlot_8) y ESCUPIR LA SEMILLA
        if (!isHurt && !isDead)
        {
            if (attackSprites != null && attackSprites.Length > 2 && attackSprites[2] != null)
                spriteRenderer.sprite = attackSprites[2];

            transform.localScale = new Vector3(baseScale.x * 0.90f, baseScale.y * 1.12f, baseScale.z);
            SpawnSeedFromMouth();
            yield return new WaitForSeconds(0.14f);
        }

        // Fase 3: Continuación del escupitajo (merlot_4 y merlot_6)
        if (!isHurt && !isDead)
        {
            if (attackSprites != null && attackSprites.Length > 3 && attackSprites[3] != null)
                spriteRenderer.sprite = attackSprites[3];

            transform.localScale = baseScale;
            yield return new WaitForSeconds(0.12f);

            if (attackSprites != null && attackSprites.Length > 4 && attackSprites[4] != null)
                spriteRenderer.sprite = attackSprites[4];

            yield return new WaitForSeconds(0.10f);
        }

        transform.localScale = baseScale;
        nextAttackTime = Time.time + attackInterval;
        isAttacking = false;
    }

    private void SpawnSeedFromMouth()
    {
        Vector3 spawnPos = mouthPoint != null
            ? mouthPoint.position
            : transform.position + new Vector3(facingDirection * 0.42f, 0.04f, 0f);

        GameObject seedObj;
        if (seedPrefab != null)
        {
            seedObj = Instantiate(seedPrefab, spawnPos, Quaternion.identity);
        }
        else
        {
            seedObj = CreateRuntimeFallbackSeed(spawnPos);
        }

        GrapeSeedProjectile seedProj = seedObj.GetComponent<GrapeSeedProjectile>();
        if (seedProj == null)
            seedProj = seedObj.AddComponent<GrapeSeedProjectile>();

        // Apuntar horizontalmente hacia el jugador (con un ligero ajuste si el jugador está a distinta altura)
        Vector2 shootDir = new Vector2(facingDirection, 0f);
        if (playerTransform != null)
        {
            Vector2 toPlayer = playerTransform.position - spawnPos;
            // Limitar el ángulo vertical para que se sienta como un disparo de plataformas 2D clásico
            toPlayer.y = Mathf.Clamp(toPlayer.y * 0.35f, -1.5f, 1.5f);
            if (Mathf.Sign(toPlayer.x) == facingDirection && toPlayer.sqrMagnitude > 0.01f)
            {
                shootDir = toPlayer.normalized;
            }
        }

        seedProj.Launch(shootDir);
    }

    /// <summary>
    /// Llamado automáticamente cuando el corcho del jugador impacta a la uva.
    /// </summary>
    public void TakeDamage(int damageAmount)
    {
        if (isDead) return;

        currentHealth -= damageAmount;
        Debug.Log($"[MerlotGrapeEnemy] ¡Corchazo recibido en {name}! Vida: {currentHealth}/{maxHealth}");

        if (currentHealth <= 0)
        {
            StartCoroutine(DieRoutine());
        }
        else
        {
            StopAllCoroutines();
            isAttacking = false;
            StartCoroutine(HurtReactionRoutine());
        }
    }

    public void RecibirDano(int damageAmount)
    {
        TakeDamage(damageAmount);
    }

    private IEnumerator HurtReactionRoutine()
    {
        isHurt = true;

        // Mostrar sprite de dolor (merlot_5 o merlot_7)
        if (hurtSprites != null && hurtSprites.Length > 0)
        {
            Sprite hurtSprite = hurtSprites[Random.Range(0, hurtSprites.Length)];
            if (hurtSprite != null)
                spriteRenderer.sprite = hurtSprite;
        }

        spriteRenderer.color = new Color(1f, 0.55f, 0.55f, 1f);
        transform.localScale = new Vector3(baseScale.x * 0.82f, baseScale.y * 1.22f, baseScale.z);

        yield return new WaitForSeconds(0.12f);

        if (hurtSprites != null && hurtSprites.Length > 1 && hurtSprites[1] != null)
            spriteRenderer.sprite = hurtSprites[1];

        spriteRenderer.color = Color.white;
        transform.localScale = baseScale;

        yield return new WaitForSeconds(0.10f);

        isHurt = false;
    }

    private IEnumerator DieRoutine()
    {
        isDead = true;
        isAttacking = false;
        isHurt = false;

        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Kinematic;
        }

        // Mostrar cara de derrota (merlot_7)
        if (hurtSprites != null && hurtSprites.Length > 1 && hurtSprites[1] != null)
            spriteRenderer.sprite = hurtSprites[1];
        else if (hurtSprites != null && hurtSprites.Length > 0 && hurtSprites[0] != null)
            spriteRenderer.sprite = hurtSprites[0];

        // Efecto de inflarse y explotar "¡POP!"
        Vector3 popScale = baseScale * 1.35f;
        float t = 0f;
        while (t < 0.12f)
        {
            t += Time.deltaTime;
            transform.localScale = Vector3.Lerp(baseScale, popScale, t / 0.12f);
            spriteRenderer.color = Color.Lerp(Color.white, new Color(0.85f, 0.35f, 1f, 1f), t / 0.12f);
            yield return null;
        }

        t = 0f;
        while (t < 0.10f)
        {
            t += Time.deltaTime;
            transform.localScale = Vector3.Lerp(popScale, Vector3.zero, t / 0.10f);
            yield return null;
        }

        Destroy(gameObject);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (isDead) return;

        if (collision.gameObject.CompareTag("Player") || collision.gameObject.GetComponent<MovimientoJugador>() != null)
        {
            PlayerHealth ph = collision.gameObject.GetComponent<PlayerHealth>();
            if (ph == null)
                ph = collision.gameObject.AddComponent<PlayerHealth>();

            ph.TakeDamageFromPosition(1, transform.position);
        }
    }

    private void OnGUI()
    {
        // Mostrar pequeña barra de vida flotante encima de la uva cuando ha recibido daño
        if (isDead || currentHealth >= maxHealth || Camera.main == null)
            return;

        Vector3 worldPos = transform.position + new Vector3(0f, 1.05f, 0f);
        Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPos);
        if (screenPos.z <= 0f) return;

        float guiX = screenPos.x - 22f;
        float guiY = Screen.height - screenPos.y - 8f;

        GUI.color = new Color(0.15f, 0.05f, 0.2f, 0.85f);
        GUI.DrawTexture(new Rect(guiX - 2, guiY - 2, 48, 9), Texture2D.whiteTexture);

        float fill = Mathf.Clamp01((float)currentHealth / maxHealth);
        GUI.color = new Color(0.68f, 0.18f, 0.88f, 1f); // Morado uva Merlot
        GUI.DrawTexture(new Rect(guiX, guiY, 44 * fill, 5), Texture2D.whiteTexture);
        GUI.color = Color.white;
    }

    private static Sprite cachedSeedSprite;

    private GameObject CreateRuntimeFallbackSeed(Vector3 pos)
    {
        GameObject seed = new GameObject("GrapeSeed");
        seed.transform.position = pos;

        SpriteRenderer sr = seed.AddComponent<SpriteRenderer>();
        sr.sprite = GetFallbackSeedSprite();
        sr.sortingOrder = spriteRenderer != null ? spriteRenderer.sortingOrder + 1 : 5;

        Rigidbody2D rb2d = seed.AddComponent<Rigidbody2D>();
        rb2d.gravityScale = 0f;
        rb2d.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        CircleCollider2D col = seed.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.14f;

        return seed;
    }

    private static Sprite GetFallbackSeedSprite()
    {
        if (cachedSeedSprite != null) return cachedSeedSprite;

        int w = 14, h = 10;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        Color clear = new Color(0, 0, 0, 0);
        Color outline = new Color(0.12f, 0.06f, 0.04f, 1f);
        Color seedBrown = new Color(0.58f, 0.36f, 0.20f, 1f);
        Color seedHighlight = new Color(0.78f, 0.56f, 0.36f, 1f);

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float dx = (x - w * 0.5f) / (w * 0.42f);
                float dy = (y - h * 0.5f) / (h * 0.42f);
                float d = dx * dx + dy * dy;
                if (d > 1.0f) tex.SetPixel(x, y, clear);
                else if (d > 0.65f) tex.SetPixel(x, y, outline);
                else if (y > h * 0.55f && x > w * 0.4f) tex.SetPixel(x, y, seedHighlight);
                else tex.SetPixel(x, y, seedBrown);
            }
        }
        tex.Apply();
        cachedSeedSprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 48f);
        return cachedSeedSprite;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.65f, 0.15f, 0.85f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, detectionRange);
    }
}
