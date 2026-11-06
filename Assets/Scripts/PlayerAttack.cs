using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerAttack : MonoBehaviour
{
    [Header("Referencias del Disparo")]
    [Tooltip("Prefab del corcho que se instanciará al disparar")]
    public GameObject corkPrefab;

    [Tooltip("Punto desde donde sale el corcho (hijo del Player)")]
    public Transform firePoint;

    [Header("Configuración de Ataque")]
    [Tooltip("Tiempo de espera entre disparos en segundos")]
    public float fireCooldown = 0.35f;

    [Tooltip("Permitir disparar con Clic Izquierdo o tecla F/E")]
    public bool shootWithClickOrKey = true;

    [Header("Desplazamiento del FirePoint")]
    [Tooltip("Distancia horizontal desde el centro del jugador hacia donde mira")]
    public Vector2 firePointOffset = new Vector2(0.55f, 0.08f);

    [Header("Animación al Atacar")]
    [Tooltip("Referencia al Animator del jugador (se busca automáticamente)")]
    public Animator animator;

    [Tooltip("Nombre del parámetro Trigger en el Animator")]
    public string attackTriggerName = "Attack";

    [Tooltip("Activar animación de retroceso, inclinación y destello de descorche al atacar")]
    public bool useProceduralAttackAnimation = true;

    private SpriteRenderer spriteRenderer;
    private float nextFireTime = 0f;
    private float facingDirection = 1f;
    private Vector3 originalScale;
    private Coroutine attackAnimCoroutine;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (animator == null)
            animator = GetComponent<Animator>();

        originalScale = transform.localScale;
    }

    private void Start()
    {
        // Si el usuario aún no creó el objeto hijo FirePoint, lo creamos automáticamente
        if (firePoint == null)
        {
            Transform existing = transform.Find("FirePoint");
            if (existing != null)
            {
                firePoint = existing;
            }
            else
            {
                GameObject fp = new GameObject("FirePoint");
                fp.transform.SetParent(transform);
                fp.transform.localPosition = new Vector3(firePointOffset.x, firePointOffset.y, 0f);
                firePoint = fp.transform;
            }
        }
        else
        {
            firePointOffset = new Vector2(Mathf.Abs(firePoint.localPosition.x), firePoint.localPosition.y);
            if (firePointOffset.x < 0.05f)
                firePointOffset.x = 0.8f;
        }
    }

    private void Update()
    {
        UpdateFacingDirection();
        UpdateFirePointPosition();

        if (Time.time >= nextFireTime && IsAttackPressed())
        {
            Shoot();
            nextFireTime = Time.time + fireCooldown;
        }
    }

    private void UpdateFacingDirection()
    {
        // Sincronizado con MovimientoJugador.cs que usa spriteRenderer.flipX
        if (spriteRenderer != null)
        {
            facingDirection = spriteRenderer.flipX ? -1f : 1f;
        }
        else if (Mathf.Abs(transform.localScale.x) > 0.001f)
        {
            facingDirection = Mathf.Sign(transform.localScale.x);
        }
    }

    private void UpdateFirePointPosition()
    {
        if (firePoint != null)
        {
            // Coloca automáticamente el FirePoint a la derecha o izquierda según hacia dónde mire el jugador
            firePoint.localPosition = new Vector3(
                Mathf.Abs(firePointOffset.x) * facingDirection,
                firePointOffset.y,
                0f
            );
        }
    }

    private bool IsAttackPressed()
    {
        bool pressed = false;

        // Soporte para el nuevo Input System (usado en MovimientoJugador.cs)
        if (shootWithClickOrKey)
        {
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                pressed = true;

            if (Keyboard.current != null &&
                (Keyboard.current.fKey.wasPressedThisFrame ||
                 Keyboard.current.eKey.wasPressedThisFrame ||
                 Keyboard.current.jKey.wasPressedThisFrame ||
                 Keyboard.current.enterKey.wasPressedThisFrame))
            {
                pressed = true;
            }
        }
        return pressed;
    }

    private void Shoot()
    {
        // 1. Disparar animación en el Animator (si tiene el parámetro Attack)
        if (animator != null && HasParameter(animator, attackTriggerName))
        {
            animator.SetTrigger(attackTriggerName);
        }

        // 2. Ejecutar animación procedural de ataque (Squash & Stretch + inclinación + pop visual)
        if (useProceduralAttackAnimation)
        {
            if (attackAnimCoroutine != null)
                StopCoroutine(attackAnimCoroutine);

            attackAnimCoroutine = StartCoroutine(PlayAttackMotionRoutine());
        }

        // 3. Instanciar y lanzar el proyectil de corcho
        Vector3 spawnPos = firePoint != null
            ? firePoint.position
            : transform.position + new Vector3(facingDirection * 0.8f, 0.1f, 0f);

        GameObject corkInstance;
        if (corkPrefab != null)
        {
            corkInstance = Instantiate(corkPrefab, spawnPos, Quaternion.identity);
        }
        else
        {
            // Respaldo automático si olvidaste arrastrar el Prefab en el Inspector
            corkInstance = CreateFallbackCork(spawnPos);
        }

        CorkProjectile projectile = corkInstance.GetComponent<CorkProjectile>();
        if (projectile == null)
            projectile = corkInstance.AddComponent<CorkProjectile>();

        projectile.Launch(facingDirection);
    }

    private IEnumerator PlayAttackMotionRoutine()
    {
        // Fase 1: Preparación / retroceso rápido al descorchar (0.06s)
        Vector3 recoilScale = new Vector3(
            originalScale.x * 1.18f,
            originalScale.y * 0.86f,
            originalScale.z
        );
        Quaternion recoilRot = Quaternion.Euler(0f, 0f, facingDirection * 7f);

        transform.localScale = recoilScale;
        transform.localRotation = recoilRot;

        float elapsed = 0f;
        float recoilDuration = 0.08f;

        while (elapsed < recoilDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / recoilDuration;
            transform.localScale = Vector3.Lerp(recoilScale, originalScale, t);
            transform.localRotation = Quaternion.Lerp(recoilRot, Quaternion.identity, t);
            yield return null;
        }

        transform.localScale = originalScale;
        transform.localRotation = Quaternion.identity;
    }

    private bool HasParameter(Animator anim, string paramName)
    {
        foreach (AnimatorControllerParameter param in anim.parameters)
        {
            if (param.name == paramName)
                return true;
        }
        return false;
    }

    /// <summary>
    /// Crea un corcho funcional con sprite en tiempo de ejecución por si no se asignó el Prefab aún.
    /// </summary>
    private GameObject CreateFallbackCork(Vector3 position)
    {
        GameObject cork = new GameObject("Cork_Proyectil");
        cork.transform.position = position;

        SpriteRenderer sr = cork.AddComponent<SpriteRenderer>();
        sr.sprite = GenerateRuntimeCorkSprite();
        if (spriteRenderer != null)
        {
            sr.sortingLayerID = spriteRenderer.sortingLayerID;
            sr.sortingOrder = spriteRenderer.sortingOrder + 1;
        }

        Rigidbody2D rb2d = cork.AddComponent<Rigidbody2D>();
        rb2d.gravityScale = 0f;
        rb2d.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        BoxCollider2D col = cork.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(0.45f, 0.25f);

        return cork;
    }

    private static Sprite cachedCorkSprite;

    private static Sprite GenerateRuntimeCorkSprite()
    {
        if (cachedCorkSprite != null)
            return cachedCorkSprite;

        int w = 24, h = 14;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;

        Color clear = new Color(0, 0, 0, 0);
        Color outline = new Color(0.18f, 0.10f, 0.05f, 1f);
        Color corkBase = new Color(0.82f, 0.62f, 0.38f, 1f);
        Color corkLight = new Color(0.91f, 0.74f, 0.50f, 1f);
        Color corkDark = new Color(0.65f, 0.45f, 0.24f, 1f);
        Color wineStain = new Color(0.55f, 0.12f, 0.22f, 1f);

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                bool inside = (x >= 2 && x <= w - 3 && y >= 2 && y <= h - 3);
                if (!inside)
                {
                    tex.SetPixel(x, y, clear);
                    continue;
                }

                bool isBorder = (x == 2 || x == w - 3 || y == 2 || y == h - 3);
                if (isBorder)
                {
                    tex.SetPixel(x, y, outline);
                }
                else if (x >= w - 7)
                {
                    // Punta manchada de vino tinto
                    tex.SetPixel(x, y, wineStain);
                }
                else if (y >= h - 5)
                {
                    tex.SetPixel(x, y, corkLight);
                }
                else if (y <= 4 || (x + y) % 5 == 0)
                {
                    tex.SetPixel(x, y, corkDark);
                }
                else
                {
                    tex.SetPixel(x, y, corkBase);
                }
            }
        }

        tex.Apply();
        cachedCorkSprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 96f);
        return cachedCorkSprite;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.9f, 0.6f, 0.2f, 0.9f);
        Vector3 pos = firePoint != null
            ? firePoint.position
            : transform.position + new Vector3(firePointOffset.x, firePointOffset.y, 0f);
        Gizmos.DrawWireSphere(pos, 0.15f);
    }
}
