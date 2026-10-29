using UnityEngine;
using UnityEngine.InputSystem; // Importante para el nuevo Input System

public class MovimientoJugador : MonoBehaviour
{
    [Header("Ajustes de Movimiento")]
    public float velocidadMovimiento = 5f;
    public float fuerzaSalto = 7f;

    [Header("Detección de Suelo")]
    public Transform comprobadorSuelo;
    public float radioComprobacion = 0.2f;
    public LayerMask capaSuelo;

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private float movimientoHorizontal;
    private bool enElSuelo;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        // Controles de teclado con el nuevo Input System
        movimientoHorizontal = 0f;

        if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)
        {
            movimientoHorizontal = -1f;
        }
        else if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed)
        {
            movimientoHorizontal = 1f;
        }

        // Voltear el sprite según la dirección
        if (movimientoHorizontal > 0)
        {
            spriteRenderer.flipX = false;
        }
        else if (movimientoHorizontal < 0)
        {
            spriteRenderer.flipX = true;
        }

        // Detectar contacto con el suelo
        if (comprobadorSuelo != null)
        {
            enElSuelo = Physics2D.OverlapCircle(comprobadorSuelo.position, radioComprobacion, capaSuelo);
        }

        // Salto con la barra espaciadora
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