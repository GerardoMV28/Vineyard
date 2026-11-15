using UnityEngine;

/// <summary>
/// Zona peligrosa de líquido ácido / mosto fermentado para los charcos del Nivel 2.
/// Al entrar en contacto con el jugador le inflige daño, genera una salpicadura ácida
/// y le aplica un impulso hacia arriba para que pueda salir del charco.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class ZonaAcido : MonoBehaviour
{
    [Header("Daño y Rebote del Charco de Ácido")]
    [Tooltip("Daño que recibe el jugador al caer en el líquido ácido/fermentado")]
    public float danoPorContacto = 18f;

    [Tooltip("Tiempo mínimo entre ticks de daño si permanece en el charco")]
    public float intervaloDano = 0.85f;

    [Tooltip("Impulso vertical al tocar el ácido para permitir escapar del charco")]
    public float impulsoReboteVertical = 9.8f;

    [Header("Audio Opcional")]
    public AudioClip sonidoSalpicaduraAcido;

    [Range(0f, 1f)]
    public float volumenAudio = 0.75f;

    private float proximoDanoTiempo = 0f;

    private void Awake()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.isTrigger = true;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        ProcesarContactoAcido(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        ProcesarContactoAcido(other);
    }

    private void ProcesarContactoAcido(Collider2D other)
    {
        if (other == null) return;

        if (other.CompareTag("Player") || other.GetComponent<MovimientoJugador>() != null)
        {
            if (Time.time < proximoDanoTiempo)
                return;

            proximoDanoTiempo = Time.time + intervaloDano;

            HealthSystem hs = other.GetComponent<HealthSystem>();
            if (hs == null)
                hs = other.gameObject.AddComponent<HealthSystem>();

            hs.TakeDamageFromPosition(danoPorContacto, transform.position);

            Rigidbody2D rb = other.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x * 0.7f, impulsoReboteVertical);
            }

            if (sonidoSalpicaduraAcido != null)
            {
                CorazonVida.ReproducirSonidoPersistente(sonidoSalpicaduraAcido, other.transform.position, volumenAudio);
            }

            EnemyDeathFeedback.SpawnRuntimeGrapeJuiceSplash(
                other.transform.position,
                16,
                new Color(0.45f, 0.95f, 0.18f, 1f),
                new Color(0.75f, 1.0f, 0.25f, 1f)
            );
            return;
        }

        // Si un enemigo cae al ácido, también recibe daño
        EnemigoCabSau cabSau = other.GetComponent<EnemigoCabSau>();
        if (cabSau != null)
        {
            cabSau.TakeDamage(99);
        }
    }

    private void OnDrawGizmos()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            Gizmos.color = new Color(0.45f, 0.95f, 0.15f, 0.35f);
            Gizmos.DrawCube(col.bounds.center, col.bounds.size);
            Gizmos.color = new Color(0.55f, 1.0f, 0.20f, 0.85f);
            Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);
        }
    }
}
