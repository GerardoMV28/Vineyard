using UnityEngine;

public class EnemyDeathFeedback : MonoBehaviour
{
    [Header("Configuración de Salpicadura de Jugo de Uva")]
    [Tooltip("Prefab de ParticleSystem para la salpicadura al morir (opcional, si está vacío se genera automáticamente)")]
    public ParticleSystem juiceSplashPrefab;

    [Tooltip("Cantidad de gotas de jugo de uva que salpican al explotar")]
    public int dropletCount = 24;

    [Tooltip("Color principal del jugo (Morado Merlot)")]
    public Color primaryJuiceColor = new Color(0.58f, 0.12f, 0.68f, 1f);

    [Tooltip("Color secundario del jugo (Vino Tinto / Magenta)")]
    public Color secondaryJuiceColor = new Color(0.85f, 0.22f, 0.48f, 1f);

    private static Material cachedPixelParticleMat;

    /// <summary>
    /// Dispara el efecto de salpicadura de jugo de uva en la posición del enemigo antes de destruirlo.
    /// </summary>
    public void PlayDeathFeedback()
    {
        PlayDeathFeedbackAt(transform.position);
    }

    public void PlayDeathFeedbackAt(Vector3 position)
    {
        if (juiceSplashPrefab != null)
        {
            ParticleSystem instance = Instantiate(juiceSplashPrefab, position, Quaternion.identity);
            instance.Play();
            Destroy(instance.gameObject, 2.0f);
        }
        else
        {
            SpawnRuntimeGrapeJuiceSplash(position, dropletCount, primaryJuiceColor, secondaryJuiceColor);
        }
    }

    /// <summary>
    /// Crea un ParticleSystem 2D estilo Pixel Art de salpicadura de jugo de uva Merlot.
    /// </summary>
    public static GameObject SpawnRuntimeGrapeJuiceSplash(
        Vector3 position,
        int count = 24,
        Color? colorA = null,
        Color? colorB = null)
    {
        Color col1 = colorA ?? new Color(0.58f, 0.12f, 0.68f, 1f);
        Color col2 = colorB ?? new Color(0.85f, 0.22f, 0.48f, 1f);

        GameObject splashObj = new GameObject("GrapeJuiceSplash_VFX");
        splashObj.transform.position = position;

        ParticleSystem ps = splashObj.AddComponent<ParticleSystem>();
        ParticleSystemRenderer psRenderer = splashObj.GetComponent<ParticleSystemRenderer>();

        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.duration = 0.6f;
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.32f, 0.65f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(2.2f, 6.2f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.10f, 0.24f);
        main.gravityModifier = 1.65f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 60;
        main.startColor = new ParticleSystem.MinMaxGradient(col1, col2);

        var emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = 0f;
        emission.SetBursts(new ParticleSystem.Burst[]
        {
            new ParticleSystem.Burst(0f, (short)count)
        });

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.22f;

        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve shrinkCurve = new AnimationCurve(
            new Keyframe(0f, 1f),
            new Keyframe(0.7f, 0.85f),
            new Keyframe(1f, 0f)
        );
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, shrinkCurve);

        if (psRenderer != null)
        {
            psRenderer.material = GetPixelParticleMaterial();
            psRenderer.sortingOrder = 10;
        }

        ps.Play();
        Destroy(splashObj, 1.5f);
        return splashObj;
    }

    public static Material GetPixelParticleMaterial()
    {
        if (cachedPixelParticleMat != null)
            return cachedPixelParticleMat;

        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null)
            shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");

        cachedPixelParticleMat = new Material(shader);

        // Textura cuadrada 8x8 con borde redondeado pixel art para gota de jugo
        int size = 8;
        Texture2D dropTex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        dropTex.filterMode = FilterMode.Point;
        Color clear = new Color(0, 0, 0, 0);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                bool corner = (x == 0 || x == size - 1) && (y == 0 || y == size - 1);
                if (corner)
                    dropTex.SetPixel(x, y, clear);
                else if (x >= 2 && x <= 3 && y >= 4 && y <= 5)
                    dropTex.SetPixel(x, y, Color.white); // Brillo de la gota
                else
                    dropTex.SetPixel(x, y, new Color(0.9f, 0.85f, 0.95f, 1f));
            }
        }
        dropTex.Apply();
        cachedPixelParticleMat.mainTexture = dropTex;
        return cachedPixelParticleMat;
    }
}
