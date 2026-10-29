using UnityEngine;
using UnityEngine.InputSystem;

public class MovimientoJugador : MonoBehaviour
{
    [Header("Ajustes de Movimiento")]
    public float velocidadMovimiento = 8f;
    public float fuerzaSalto = 10f;

    [Header("Detección de Suelo")]
    public Transform comprobadorSuelo;
    public float radioComprobacion = 0.2f;
    public LayerMask capaSuelo;

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Animator animator; // Componente Animator
    private float movimientoHorizontal;
    private bool enElSuelo;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>(); // Obtener referencia al Animator
    }

    void Update()
    {
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
        if (movimientoHorizontal > 0)
        {
            spriteRenderer.flipX = false;
        }
        else if (movimientoHorizontal < 0)
        {
            spriteRenderer.flipX = true;
        }

        // Pasar el valor de movimiento al Animator (Mathf.Abs vuelve el número positivo)
        if (animator != null)
        {
            animator.SetFloat("Velocidad", Mathf.Abs(movimientoHorizontal));
        }

        // Detectar suelo
        if (comprobadorSuelo != null)
        {
            enElSuelo = Physics2D.OverlapCircle(comprobadorSuelo.position, radioComprobacion, capaSuelo);
        }

        // Salto
        if (Keyboard.current.spaceKey.wasPressedThisFrame && enElSuelo)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, fuerzaSalto);
        }
    }

    void FixedUpdate()
    {
        rb.linearVelocity = new Vector2(movimientoHorizontal * velocidadMovimiento, rb.linearVelocity.y);
    }

    private void OnDrawGizmosSelected()
    {
        if (comprobadorSuelo != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(comprobadorSuelo.position, radioComprobacion);
        }
    }
}