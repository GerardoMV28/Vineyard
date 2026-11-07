using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(SpriteRenderer))]
public class AtaqueJugador : MonoBehaviour
{
    [Header("Configuración de Ataque")]
    [Tooltip("Cantidad de daño que inflige cada ataque")]
    public float danio = 25f;

    [Tooltip("Tiempo de espera mínimo entre cada ataque (en segundos)")]
    public float tiempoEntreAtaques = 0.4f;

    [Tooltip("Radio del área de impacto del golpe")]
    public float radioAtaque = 0.8f;

    [Tooltip("Distancia hacia el frente del jugador donde se genera el impacto")]
    public float distanciaFrente = 0.8f;

    [Tooltip("Ajuste vertical de la altura del golpe")]
    public float offsetVertical = 0.0f;

    [Tooltip("Fuerza con la que el golpe empuja a los enemigos")]
    public float fuerzaEmpuje = 4f;

    [Header("Detección de Objetivos")]
    [Tooltip("Capa (Layer) asignada a los enemigos o monstruos")]
    public LayerMask capaEnemigos;

    [Header("Opcional")]
    [Tooltip("Si se asigna, el golpe se originará en este punto en vez de calcularse automáticamente")]
    public Transform puntoAtaquePersonalizado;

    private SpriteRenderer spriteRenderer;
    private Animator animator;
    private float tiempoSiguienteAtaque = 0f;
    private bool tieneParametroAtaque = false;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();

        // Verificar si el Animator tiene el trigger "Atacar" para evitar advertencias en consola
        if (animator != null)
        {
            foreach (AnimatorControllerParameter param in animator.parameters)
            {
                if (param.type == AnimatorControllerParameterType.Trigger && param.name == "Atacar")
                {
                    tieneParametroAtaque = true;
                    break;
                }
            }
        }
    }

    void Update()
    {
        if (Time.time >= tiempoSiguienteAtaque)
        {
            // Ataque con Clic Izquierdo del ratón o con la tecla J / F del teclado
            bool clickMouse = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
            bool teclaJ = Keyboard.current != null && Keyboard.current.jKey.wasPressedThisFrame;
            bool teclaF = Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame;

            if (clickMouse || teclaJ || teclaF)
            {
                Atacar();
                tiempoSiguienteAtaque = Time.time + tiempoEntreAtaques;
            }
        }
    }

    private void Atacar()
    {
        // Activar la animación de ataque si existe en el Animator
        if (animator != null && tieneParametroAtaque)
        {
            animator.SetTrigger("Atacar");
        }

        Vector2 centroAtaque = ObtenerPosicionAtaque();

        // Detectar todos los colisionadores en el área de impacto
        Collider2D[] objetosGolpeados = Physics2D.OverlapCircleAll(centroAtaque, radioAtaque, capaEnemigos);

        Vector2 direccionEmpuje = (spriteRenderer.flipX ? Vector2.left : Vector2.right) * fuerzaEmpuje;

        foreach (Collider2D col in objetosGolpeados)
        {
            // Intentar aplicar daño a través de la interfaz IDañable
            IDañable dañado = col.GetComponent<IDañable>();
            if (dañado != null)
            {
                dañado.RecibirDaño(danio, direccionEmpuje);
            }
            else
            {
                // Alternativa directa si tiene el script Enemigo
                Enemigo enemigo = col.GetComponent<Enemigo>();
                if (enemigo != null)
                {
                    enemigo.RecibirDaño(danio, direccionEmpuje);
                }
            }
        }
    }

    public Vector2 ObtenerPosicionAtaque()
    {
        if (puntoAtaquePersonalizado != null)
        {
            return puntoAtaquePersonalizado.position;
        }

        // Si el sprite está volteado a la izquierda (flipX), golpea hacia la izquierda (-1), si no a la derecha (+1)
        float direccion = (spriteRenderer != null && spriteRenderer.flipX) ? -1f : 1f;
        return (Vector2)transform.position + new Vector2(direccion * distanciaFrente, offsetVertical);
    }

    private void OnDrawGizmosSelected()
    {
        // Dibuja en la escena el rango del ataque para poder ajustarlo visualmente
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(ObtenerPosicionAtaque(), radioAtaque);
    }
}
