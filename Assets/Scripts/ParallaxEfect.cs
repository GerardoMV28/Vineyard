using UnityEngine;

public class ParallaxEfecto : MonoBehaviour
{
    private float longitud, posicionInicial;
    public GameObject camara;
    
    [Header("Velocidad del Parallax (0 = Fijo, 1 = Sigue a la cámara)")]
    [Range(0f, 1f)]
    public float efectoParallax;

    void Start()
    {
        if (camara == null)
            camara = Camera.main.gameObject;

        posicionInicial = transform.position.x;
        longitud = GetComponent<SpriteRenderer>().bounds.size.x;
    }

    void Update()
    {
        // Cuánto se ha movido el fondo para el bucle infinito
        float temp = (camara.transform.position.x * (1 - efectoParallax));
        // Cuánto se mueve la capa según la velocidad asignada
        float distancia = (camara.transform.position.x * efectoParallax);

        transform.position = new Vector3(posicionInicial + distancia, transform.position.y, transform.position.z);

        // Bucle infinito: Reposiciona la imagen cuando la cámara la sobrepasa
        if (temp > posicionInicial + longitud) 
            posicionInicial += longitud;
        else if (temp < posicionInicial - longitud) 
            posicionInicial -= longitud;
    }
}