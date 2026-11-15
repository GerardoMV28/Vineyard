using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Permite al jugador ("Player") subir y bajar por las escaleras de mano de las instalaciones
/// de la bodega en el Nivel 2 usando W / Flecha Arriba / Espacio.
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public class EscaleraMano : MonoBehaviour
{
    [Tooltip("Velocidad vertical al subir por la escalera de mano")]
    public float velocidadSubida = 5.5f;

    private void Awake()
    {
        BoxCollider2D col = GetComponent<BoxCollider2D>();
        if (col != null)
        {
            col.isTrigger = true;
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (other == null) return;
        if (!other.CompareTag("Player") && other.GetComponent<MovimientoJugador>() == null)
            return;

        if (Keyboard.current == null) return;

        Rigidbody2D rb = other.GetComponent<Rigidbody2D>();
        if (rb == null) return;

        bool subir = Keyboard.current.wKey.isPressed ||
                     Keyboard.current.upArrowKey.isPressed ||
                     Keyboard.current.spaceKey.isPressed;

        bool bajar = Keyboard.current.sKey.isPressed ||
                     Keyboard.current.downArrowKey.isPressed;

        if (subir)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, velocidadSubida);
        }
        else if (bajar)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, -velocidadSubida * 0.75f);
        }
    }
}
