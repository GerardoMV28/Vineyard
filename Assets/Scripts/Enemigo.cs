using System.Collections;
using UnityEngine;

public class Enemigo : MonoBehaviour, IDañable
{
    [Header("Estadísticas del Enemigo")]
    [Tooltip("Puntos de vida máxima")]
    public float vidaMaxima = 50f;
    public float vidaActual;

    [Header("Efectos al ser golpeado")]
    [Tooltip("Color al recibir un golpe")]
    public Color colorGolpe = Color.red;

    [Tooltip("Tiempo que dura el parpadeo de daño")]
    public float duracionFlash = 0.12f;

    [Header("Opcional")]
    [Tooltip("Prefab o efecto de partículas al morir (opcional)")]
    public GameObject efectoMuerte;

    private SpriteRenderer spriteRenderer;
    private Color colorOriginal;
    private Rigidbody2D rb;
    private bool estaMuerto = false;

    void Awake()
    {
        vidaActual = vidaMaxima;
        spriteRenderer = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();

        if (spriteRenderer != null)
        {
            colorOriginal = spriteRenderer.color;
        }
    }

    public void RecibirDaño(float cantidad, Vector2 direccionEmpuje)
    {
        if (estaMuerto) return;

        vidaActual -= cantidad;
        Debug.Log($"{gameObject.name} recibió {cantidad} de daño. Vida restante: {vidaActual}/{vidaMaxima}");

        // Aplicar retroceso físico si tiene Rigidbody2D
        if (rb != null && direccionEmpuje != Vector2.zero)
        {
            rb.linearVelocity = Vector2.zero; // Reinicia la velocidad previa
            rb.AddForce(direccionEmpuje, ForceMode2D.Impulse);
        }

        // Parpadeo visual
        if (spriteRenderer != null && gameObject.activeInHierarchy)
        {
            StartCoroutine(EfectoParpadeo());
        }

        if (vidaActual <= 0)
        {
            Morir();
        }
    }

    private IEnumerator EfectoParpadeo()
    {
        spriteRenderer.color = colorGolpe;
        yield return new WaitForSeconds(duracionFlash);
        if (spriteRenderer != null)
        {
            spriteRenderer.color = colorOriginal;
        }
    }

    protected virtual void Morir()
    {
        if (estaMuerto) return;
        estaMuerto = true;

        Debug.Log($"{gameObject.name} ha sido derrotado.");

        // Desactivar colisiones para que no bloquee al jugador
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.enabled = false;
        }

        // Instanciar efecto si existe
        if (efectoMuerte != null)
        {
            Instantiate(efectoMuerte, transform.position, Quaternion.identity);
        }

        // Destruir el objeto tras una fracción de segundo
        Destroy(gameObject, 0.1f);
    }
}
