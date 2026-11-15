using UnityEngine;

[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(AudioSource))]
public class CorazonVida : MonoBehaviour
{
    [Header("Configuración de Curación")]
    [Tooltip("Cantidad de puntos de vida (HP) que regenera al jugador sin exceder su vida máxima")]
    public float cantidadCuracion = 35f;

    [Header("Audio (SFX de Curación)")]
    [Tooltip("Componente AudioSource del ítem (se asigna automáticamente)")]
    public AudioSource audioSource;

    [Tooltip("Sonido opcional al recoger el corazón (asigna tu AudioClip en el Inspector)")]
    public AudioClip sonidoCuracion;

    [Range(0f, 1f)]
    [Tooltip("Volumen del sonido de curación")]
    public float volumenCuracion = 0.9f;

    [Header("Animación Visual en el Mapa")]
    [Tooltip("Hacer que el corazón flote suavemente arriba y abajo")]
    public bool animacionFlotar = true;
    public float amplitudFlotar = 0.14f;
    public float velocidadFlotar = 2.8f;

    private Vector3 posicionInicial;
    private Vector3 escalaInicial;
    private bool yaRecogido = false;

    private void Awake()
    {
        posicionInicial = transform.position;
        escalaInicial = transform.localScale;

        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.isTrigger = true;
        }

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
    }

    private void Start()
    {
        posicionInicial = transform.position;
    }

    private void Update()
    {
        if (yaRecogido || !animacionFlotar)
            return;

        float offsetY = Mathf.Sin((Time.time + posicionInicial.x) * velocidadFlotar) * amplitudFlotar;
        transform.position = new Vector3(posicionInicial.x, posicionInicial.y + offsetY, posicionInicial.z);

        float pulso = 1f + Mathf.Sin(Time.time * velocidadFlotar * 1.5f) * 0.06f;
        transform.localScale = escalaInicial * pulso;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        IntentarCurarJugador(other.gameObject);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        IntentarCurarJugador(collision.gameObject);
    }

    private void IntentarCurarJugador(GameObject obj)
    {
        if (yaRecogido || obj == null)
            return;

        if (!obj.CompareTag("Player") && obj.GetComponent<MovimientoJugador>() == null)
            return;

        HealthSystem healthSystem = obj.GetComponent<HealthSystem>();
        if (healthSystem == null)
            healthSystem = FindFirstObjectByType<HealthSystem>();

        if (healthSystem != null)
        {
            // Regenera la vida sin exceder maxHealth (controlado por Mathf.Clamp en HealthSystem.Heal)
            healthSystem.Heal(cantidadCuracion);
            Debug.Log($"[CorazonVida] ¡Corazón recogido! +{cantidadCuracion} HP -> Salud actual: {healthSystem.currentHealth}/{healthSystem.maxHealth}");
        }

        yaRecogido = true;

        // Reproducir sonido de curación en un objeto temporal para que no se corte al destruir el corazón
        if (sonidoCuracion != null)
        {
            ReproducirSonidoPersistente(sonidoCuracion, transform.position, volumenCuracion);
        }

        CrearEfectoCuracion(transform.position);
        Destroy(gameObject);
    }

    public static void ReproducirSonidoPersistente(AudioClip clip, Vector3 pos, float volumen)
    {
        if (clip == null) return;

        GameObject tempAudio = new GameObject($"SFX_{clip.name}");
        tempAudio.transform.position = pos;
        AudioSource src = tempAudio.AddComponent<AudioSource>();
        src.clip = clip;
        src.volume = volumen;
        src.spatialBlend = 0f; // 2D para que se escuche claro en todo momento
        src.Play();
        Destroy(tempAudio, clip.length + 0.1f);
    }

    private void CrearEfectoCuracion(Vector3 pos)
    {
        GameObject fxObj = new GameObject("EfectoCuracionCorazon");
        fxObj.transform.position = pos;

        ParticleSystem ps = fxObj.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.duration = 0.45f;
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.25f, 0.5f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 3.8f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.10f, 0.20f);
        main.gravityModifier = -0.4f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(1f, 0.25f, 0.35f, 1f),
            new Color(1f, 0.75f, 0.82f, 1f)
        );

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 16) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.2f;

        ParticleSystemRenderer psr = fxObj.GetComponent<ParticleSystemRenderer>();
        if (psr != null)
        {
            psr.sortingOrder = 12;
        }

        ps.Play();
        Destroy(fxObj, 1.0f);
    }
}
