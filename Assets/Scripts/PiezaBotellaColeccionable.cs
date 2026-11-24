using UnityEngine;

/// <summary>
/// Pedazo coleccionable de la Botella Sagrada en el Nivel 3 (La Cava).
/// Flota suavemente con brillo dorado-esmeralda; al recogerlo se ensambla automáticamente
/// en el HUD de GestorBotellaNivel3 hasta formar la botella completa (5/5).
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Collider2D))]
public class PiezaBotellaColeccionable : MonoBehaviour
{
    [Header("Identificación del Pedazo (0 = Base ... 4 = Cuello y Sello)")]
    [Range(0, 4)]
    public int indicePieza = 0;

    public string nombrePieza = "Pedazo de Botella";

    [Header("Animación de Flotación")]
    public float amplitudFlotacion = 0.22f;
    public float velocidadFlotacion = 2.8f;

    private Vector3 posicionBase;
    private SpriteRenderer sr;
    private bool recogida = false;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
        posicionBase = transform.position;
    }

    private void Start()
    {
        posicionBase = transform.position;
    }

    private void Update()
    {
        if (recogida) return;

        float offsetY = Mathf.Sin(Time.time * velocidadFlotacion + indicePieza * 1.1f) * amplitudFlotacion;
        transform.position = new Vector3(posicionBase.x, posicionBase.y + offsetY, posicionBase.z);

        // Destello esmeralda/dorado suave para que destaque en la cava oscura
        if (sr != null)
        {
            float glow = 0.88f + Mathf.Sin(Time.time * 4.5f + indicePieza) * 0.12f;
            sr.color = new Color(glow, 1f, glow, 1f);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (recogida || other == null) return;
        if (!other.CompareTag("Player") && other.GetComponent<MovimientoJugador>() == null)
            return;

        recogida = true;

        GestorBotellaNivel3 gestor = GestorBotellaNivel3.Instancia ?? FindAnyObjectByType<GestorBotellaNivel3>();
        if (gestor != null)
        {
            gestor.RegistrarPieza(indicePieza, nombrePieza);
        }

        // Destello de partículas esmeralda y dorado al recoger el pedazo
        EnemyDeathFeedback.SpawnRuntimeGrapeJuiceSplash(
            transform.position,
            18,
            new Color(0.28f, 0.96f, 0.52f, 1f),
            new Color(1.0f, 0.88f, 0.30f, 1f)
        );

        Destroy(gameObject);
    }
}
