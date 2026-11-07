using UnityEngine;

/// <summary>
/// Permite que el emisor de lluvia ácida siga suavemente a la cámara principal
/// para que la lluvia cubra la pantalla en cualquier parte del nivel.
/// </summary>
public class LluviaSeguirCamara : MonoBehaviour
{
    [Header("Objetivo")]
    [Tooltip("Cámara a la que seguirá la lluvia. Si se deja vacío, tomará Camera.main automáticamente.")]
    public Transform camaraObjetivo;

    [Header("Posición")]
    [Tooltip("Distancia por encima de la cámara donde se posicionará el emisor.")]
    public float alturaSobreCamara = 8f;

    [Tooltip("Posición en Z relativa (debe estar entre la cámara y el fondo para ser visible).")]
    public float posicionZ = -1f;

    void Start()
    {
        if (camaraObjetivo == null && Camera.main != null)
        {
            camaraObjetivo = Camera.main.transform;
        }
    }

    void LateUpdate()
    {
        if (camaraObjetivo == null && Camera.main != null)
        {
            camaraObjetivo = Camera.main.transform;
        }

        if (camaraObjetivo != null)
        {
            transform.position = new Vector3(
                camaraObjetivo.position.x,
                camaraObjetivo.position.y + alturaSobreCamara,
                posicionZ
            );
        }
    }
}
