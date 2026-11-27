using UnityEngine;
using UnityEngine.InputSystem;

public class MovimientoJugador : MonoBehaviour
{
    [Header("Ajustes de Movimiento")]
    public float velocidadMovimiento = 8f;
    public float fuerzaSalto = 14.5f;

    [Header("Sensación de Salto y Fluidez (Plataformas)")]
    [Tooltip("Escala de gravedad aplicada al Rigidbody2D del jugador para un salto firme y sin flotar")]
    public float escalaGravedad = 2.5f;

    [Tooltip("Margen de gracia (segundos) para poder saltar justo al salir de un borde o colina")]
    public float tiempoCoyote = 0.13f;

    [Tooltip("Registra la tecla de salto si se pulsa justo antes de tocar el suelo")]
    public float tiempoBufferSalto = 0.14f;

    [Tooltip("Gravedad extra al caer para un salto más ágil y controlado")]
    public float multiplicadorCaida = 1.55f;

    [Tooltip("Gravedad extra al soltar el botón de salto temprano para controlar saltos cortos")]
    public float multiplicadorSaltoCorto = 2.6f;

    [Tooltip("Permite saltar con Espacio y también con W / Flecha Arriba")]
    public bool permitirSaltoConWYFlechaArriba = true;

    [Tooltip("Aplica automáticamente un PhysicsMaterial2D sin fricción para evitar atorarse en paredes y bordes")]
    public bool aplicarMaterialSinFriccion = true;

    [Header("Detección de Suelo")]
    public Transform comprobadorSuelo;
    public float radioComprobacion = 0.26f;
    public LayerMask capaSuelo;

    private Rigidbody2D rb;
    private Collider2D col;
    private SpriteRenderer spriteRenderer;
    private Animator animator;

    private float movimientoHorizontal;
    private bool enElSuelo;
    private bool manteniendoSalto;
    private float contadorCoyote;
    private float contadorBufferSalto;
    private float tiempoUltimoSalto = -1f;

    private static PhysicsMaterial2D cachedZeroFrictionMat;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();

        ConfigurarFisicasAntiAtoro();
    }

    void Start()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (col == null) col = GetComponent<Collider2D>();
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        if (animator == null) animator = GetComponent<Animator>();

        // Asegurar que capaSuelo incluya la capa "Suelo" si no fue asignada en el Inspector
        if (capaSuelo.value == 0)
        {
            int sueloLayer = LayerMask.NameToLayer("Suelo");
            capaSuelo = sueloLayer >= 0 ? (1 << sueloLayer) : ~0;
        }
    }

    private void ConfigurarFisicasAntiAtoro()
    {
        if (aplicarMaterialSinFriccion)
        {
            if (cachedZeroFrictionMat == null)
            {
                cachedZeroFrictionMat = new PhysicsMaterial2D("JugadorSinFriccion")
                {
                    friction = 0f,
                    bounciness = 0f
                };
            }

            if (col != null)
            {
                col.sharedMaterial = cachedZeroFrictionMat;
            }
            if (rb != null)
            {
                rb.sharedMaterial = cachedZeroFrictionMat;
            }
        }

        if (rb != null)
        {
            if (escalaGravedad > 0f)
                rb.gravityScale = escalaGravedad;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        }
    }

    void Update()
    {
        if (Time.timeScale <= 0f || Keyboard.current == null)
        {
            movimientoHorizontal = 0f;
            return;
        }

        movimientoHorizontal = 0f;

        if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)
        {
            movimientoHorizontal = -1f;
        }
        else if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed)
        {
            movimientoHorizontal = 1f;
        }

        // Girar el sprite
        if (spriteRenderer != null)
        {
            if (movimientoHorizontal > 0f)
                spriteRenderer.flipX = false;
            else if (movimientoHorizontal < 0f)
                spriteRenderer.flipX = true;
        }

        // Pasar el valor de movimiento al Animator
        if (animator != null)
        {
            animator.SetFloat("Velocidad", Mathf.Abs(movimientoHorizontal));
        }

        // Detección amplia de suelo (cubre todo el ancho de los pies + pendientes + bordes)
        enElSuelo = ComprobarSiEstaEnSuelo();

        if (enElSuelo && (Time.time - tiempoUltimoSalto) > 0.08f)
        {
            contadorCoyote = tiempoCoyote;
        }
        else
        {
            contadorCoyote -= Time.deltaTime;
        }

        // Buffer de salto para que nunca se "trague" una pulsación al aterrizar
        bool saltoPulsado = Keyboard.current.spaceKey.wasPressedThisFrame ||
                            (permitirSaltoConWYFlechaArriba &&
                             (Keyboard.current.wKey.wasPressedThisFrame || Keyboard.current.upArrowKey.wasPressedThisFrame));

        manteniendoSalto = Keyboard.current.spaceKey.isPressed ||
                           (permitirSaltoConWYFlechaArriba &&
                            (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed));

        if (saltoPulsado)
        {
            contadorBufferSalto = tiempoBufferSalto;
        }
        else
        {
            contadorBufferSalto -= Time.deltaTime;
        }

        // Ejecutar salto si hay buffer activo y estamos en suelo (o dentro del coyote time)
        if (contadorBufferSalto > 0f && contadorCoyote > 0f && rb != null)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, fuerzaSalto);
            contadorCoyote = 0f;
            contadorBufferSalto = 0f;
            tiempoUltimoSalto = Time.time;
        }
    }

    void FixedUpdate()
    {
        if (rb == null) return;

        float vy = rb.linearVelocity.y;

        // Caída más ágil y salto variable (soltar la tecla temprano frena el ascenso)
        if (vy < -0.1f && multiplicadorCaida > 1f)
        {
            vy += Physics2D.gravity.y * rb.gravityScale * (multiplicadorCaida - 1f) * Time.fixedDeltaTime;
        }
        else if (vy > 0.1f && !manteniendoSalto && multiplicadorSaltoCorto > 1f)
        {
            vy += Physics2D.gravity.y * rb.gravityScale * (multiplicadorSaltoCorto - 1f) * Time.fixedDeltaTime;
        }

        rb.linearVelocity = new Vector2(movimientoHorizontal * velocidadMovimiento, vy);
    }

    private bool ComprobarSiEstaEnSuelo()
    {
        int mask = capaSuelo.value != 0 ? capaSuelo.value : ~0;

        // 1. Sensor principal: caja ancha bajo los pies basada en los bounds reales del Collider2D
        if (col != null)
        {
            Bounds b = col.bounds;
            Vector2 boxCenter = new Vector2(b.center.x, b.min.y - 0.05f);
            Vector2 boxSize = new Vector2(Mathf.Max(0.45f, b.size.x * 0.86f), 0.24f);

            Collider2D[] hits = Physics2D.OverlapBoxAll(boxCenter, boxSize, 0f, mask);
            for (int i = 0; i < hits.Length; i++)
            {
                if (EsSuperficieValidaParaSaltar(hits[i]))
                    return true;
            }
        }

        // 2. Respaldo usando comprobadorSuelo si está asignado
        if (comprobadorSuelo != null)
        {
            Collider2D[] circleHits = Physics2D.OverlapCircleAll(
                comprobadorSuelo.position,
                Mathf.Max(0.24f, radioComprobacion),
                mask
            );
            for (int i = 0; i < circleHits.Length; i++)
            {
                if (EsSuperficieValidaParaSaltar(circleHits[i]))
                    return true;
            }
        }

        return false;
    }

    private bool EsSuperficieValidaParaSaltar(Collider2D hit)
    {
        if (hit == null || hit == col || hit.isTrigger)
            return false;

        if (hit.gameObject == gameObject)
            return false;

        return true;
    }

    private void OnDrawGizmosSelected()
    {
        Collider2D c = col != null ? col : GetComponent<Collider2D>();
        if (c != null)
        {
            Bounds b = c.bounds;
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(
                new Vector3(b.center.x, b.min.y - 0.05f, 0f),
                new Vector3(Mathf.Max(0.45f, b.size.x * 0.86f), 0.24f, 0.1f)
            );
        }

        if (comprobadorSuelo != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(comprobadorSuelo.position, Mathf.Max(0.24f, radioComprobacion));
        }
    }
}
