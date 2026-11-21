using UnityEngine;

/// <summary>
/// Aplica un efecto parallax suave y continuo a un grupo de paneles de fondo,
/// sin cortes ni saltos bruscos al moverse la cámara.
/// </summary>
public class ParallaxSuaveNivel1 : MonoBehaviour
{
    [Tooltip("Cámara principal (se detecta automáticamente si se deja vacío)")]
    public Transform camara;

    [Range(0f, 0.9f)]
    [Tooltip("Qué tanto acompaña el fondo a la cámara en X (0 = fijo al mundo, 0.3 = profundidad lejana suave)")]
    public float factorParallaxX = 0.28f;

    [Range(0f, 0.9f)]
    [Tooltip("Qué tanto acompaña el fondo a la cámara en Y")]
    public float factorParallaxY = 0.14f;

    private Vector3 posicionInicialFondo;
    private Vector3 posicionInicialCamara;
    private bool inicializado = false;

    private void Start()
    {
        Inicializar();
    }

    private void Inicializar()
    {
        if (camara == null && Camera.main != null)
            camara = Camera.main.transform;

        if (camara == null)
            return;

        posicionInicialFondo = transform.position;
        posicionInicialCamara = camara.position;
        inicializado = true;
    }

    private void LateUpdate()
    {
        if (!inicializado)
        {
            Inicializar();
            if (!inicializado) return;
        }

        float deltaX = camara.position.x - posicionInicialCamara.x;
        float deltaY = camara.position.y - posicionInicialCamara.y;

        transform.position = new Vector3(
            posicionInicialFondo.x + deltaX * factorParallaxX,
            posicionInicialFondo.y + deltaY * factorParallaxY,
            posicionInicialFondo.z
        );
    }
}
