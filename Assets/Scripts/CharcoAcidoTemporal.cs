using UnityEngine;

/// <summary>
/// Charco temporal de ácido burbujeante que deja una uva Nebbiolo en el suelo al explotar.
/// Usa los frames de charco (nebbiolo_5, 9, 12), inflige daño ácido al jugador y se evapora tras unos segundos.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(BoxCollider2D))]
public class CharcoAcidoTemporal : MonoBehaviour
{
    [Header("Daño y Duración")]
    public float danoPorContacto = 15f;
    public float intervaloDano = 0.85f;
    public float impulsoReboteVertical = 8.5f;
    public float duracionSegundos = 6.5f;

    [Header("Animación del Charco (nebbiolo_5, 9, 12)")]
    public Sprite[] spritesCharco;
    public float fpsAnimacion = 5f;
    public AudioClip sonidoSalpicadura;

    private SpriteRenderer sr;
    private float tiempoVida = 0f;
    private float timerAnim = 0f;
    private float proximoDano = 0f;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        BoxCollider2D col = GetComponent<BoxCollider2D>();
        if (col != null)
        {
            col.isTrigger = true;
        }
    }

    private void Update()
    {
        tiempoVida += Time.deltaTime;

        if (spritesCharco != null && spritesCharco.Length > 0 && sr != null)
        {
            timerAnim += Time.deltaTime * fpsAnimacion;
            int idx = Mathf.FloorToInt(timerAnim) % spritesCharco.Length;
            if (spritesCharco[idx] != null)
                sr.sprite = spritesCharco[idx];
        }

        // Desvanecimiento suave en el último segundo antes de desaparecer
        if (tiempoVida >= duracionSegundos - 1.0f && sr != null)
        {
            float alpha = Mathf.Clamp01(duracionSegundos - tiempoVida);
            sr.color = new Color(1f, 1f, 1f, alpha);
        }

        if (tiempoVida >= duracionSegundos)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        ProcesarContacto(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        ProcesarContacto(other);
    }

    private void ProcesarContacto(Collider2D other)
    {
        if (other == null) return;
        if (!other.CompareTag("Player") && other.GetComponent<MovimientoJugador>() == null)
            return;

        if (Time.time < proximoDano)
            return;

        proximoDano = Time.time + intervaloDano;

        HealthSystem hs = other.GetComponent<HealthSystem>();
        if (hs == null) hs = other.gameObject.AddComponent<HealthSystem>();
        hs.TakeDamageFromPosition(danoPorContacto, transform.position);

        Rigidbody2D rb = other.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x * 0.75f, impulsoReboteVertical);
        }

        if (sonidoSalpicadura != null)
        {
            CorazonVida.ReproducirSonidoPersistente(sonidoSalpicadura, transform.position, 0.7f);
        }

        EnemyDeathFeedback.SpawnRuntimeGrapeJuiceSplash(
            other.transform.position,
            10,
            new Color(0.42f, 0.96f, 0.16f, 1f),
            new Color(0.75f, 1.0f, 0.30f, 1f)
        );
    }
}
