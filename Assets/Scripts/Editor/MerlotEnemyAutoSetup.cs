#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class MerlotEnemyAutoSetup
{
    private const string MerlotSpriteFolder = "Assets/Sprites/Enemigos/Merlot";
    private const string MerlotAnimFolder = "Assets/Animations/Merlot";
    private const string SeedSpritePath = "Assets/Sprites/Enemigos/Merlot/GrapeSeed.png";
    private const string SeedPrefabPath = "Assets/Prefabs/GrapeSeed.prefab";
    private const string MerlotPrefabPath = "Assets/Prefabs/MerlotGrape.prefab";

    // Ruta donde se extrajo tu archivo Merlotgrapes.zip
    private const string ExtractedZipObjectsDir =
        @"C:\Users\marim\.gemini\antigravity\brain\d5bf5fd1-8896-404c-8613-58c9e72f9423\scratch\Merlotgrapes\objects";

    static MerlotEnemyAutoSetup()
    {
        EditorApplication.delayCall += RunIfNeeded;
    }

    private static void RunIfNeeded()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        bool needsMerlotPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MerlotPrefabPath) == null;
        bool needsSeedPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SeedPrefabPath) == null;
        bool needsFirstSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{MerlotSpriteFolder}/merlot_1.png") == null;

        if (needsMerlotPrefab || needsSeedPrefab || needsFirstSprite)
        {
            SetupMerlotEnemiesAndScene(silent: true);
        }
        else
        {
            if (UnityEngine.Object.FindFirstObjectByType<MerlotGrapeEnemy>() == null)
            {
                SpawnGrapesInScene();
            }
        }
    }

    [MenuItem("Herramientas Vineyard/Colocar y Configurar Uvas Merlot en el Mapa")]
    public static void SetupMenu()
    {
        SetupMerlotEnemiesAndScene(silent: false);
        EditorUtility.DisplayDialog(
            "Vineyard - Uvas Merlot Listas",
            "¡Se han configurado las Uvas Merlot con éxito!\n\n" +
            "• 10 Sprites de animación importados en Assets/Sprites/Enemigos/Merlot/\n" +
            "• Sprite y Prefab de la Semilla (Assets/Prefabs/GrapeSeed.prefab)\n" +
            "• Animaciones Idle, Attack y Hurt creadas en Assets/Animations/Merlot/\n" +
            "• Prefab MerlotGrape (Assets/Prefabs/MerlotGrape.prefab)\n" +
            "• Uvas Merlot colocadas en el mapa listas para lanzar semillas y recibir corchazos.",
            "¡A jugar!"
        );
    }

    public static void SetupMerlotEnemiesAndScene(bool silent)
    {
        EnsureFolders();

        Sprite[] merlotSprites = ImportMerlotSpritesAndExtractSeed(out Sprite seedSprite);
        CreateMerlotAnimationClips(merlotSprites);

        GameObject seedPrefab = CreateOrUpdateSeedPrefab(seedSprite);
        GameObject merlotPrefab = CreateOrUpdateMerlotPrefab(merlotSprites, seedPrefab);

        // Asegurar que el Jugador tenga PlayerHealth
        MovimientoJugador player = UnityEngine.Object.FindFirstObjectByType<MovimientoJugador>();
        if (player != null && player.GetComponent<PlayerHealth>() == null)
        {
            player.gameObject.AddComponent<PlayerHealth>();
            EditorUtility.SetDirty(player.gameObject);
        }

        SpawnGrapesInScene(merlotPrefab);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static void EnsureFolders()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Sprites"))
            AssetDatabase.CreateFolder("Assets", "Sprites");
        if (!AssetDatabase.IsValidFolder("Assets/Sprites/Enemigos"))
            AssetDatabase.CreateFolder("Assets/Sprites", "Enemigos");
        if (!AssetDatabase.IsValidFolder(MerlotSpriteFolder))
            AssetDatabase.CreateFolder("Assets/Sprites/Enemigos", "Merlot");

        if (!AssetDatabase.IsValidFolder("Assets/Animations"))
            AssetDatabase.CreateFolder("Assets", "Animations");
        if (!AssetDatabase.IsValidFolder(MerlotAnimFolder))
            AssetDatabase.CreateFolder("Assets/Animations", "Merlot");

        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            AssetDatabase.CreateFolder("Assets", "Prefabs");
    }

    private static string GetExtractedSourcePngPath(int idx)
    {
        string suffix = (idx == 1) ? "" : $"_{idx}";
        return Path.Combine(
            ExtractedZipObjectsDir,
            $"help_me_to_create_a_16x16_pixe{suffix}",
            "rotations",
            $"help_me_to_create_a_16x16_pixe{suffix}.png"
        );
    }

    private static Sprite[] ImportMerlotSpritesAndExtractSeed(out Sprite seedSprite)
    {
        Sprite[] result = new Sprite[10];

        for (int i = 0; i < 10; i++)
        {
            int idx = i + 1;
            string destPath = $"{MerlotSpriteFolder}/merlot_{idx}.png";
            string srcPath = GetExtractedSourcePngPath(idx);

            if (File.Exists(srcPath))
            {
                byte[] rawBytes = File.ReadAllBytes(srcPath);
                Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                tex.LoadImage(rawBytes);

                // Del frame 4 extraemos la semilla original a GrapeSeed.png
                if (idx == 4)
                {
                    ExtractSeedTexture(tex);
                }

                // En los frames 4, 6 y 8 borramos la semilla pintada a la derecha (x >= 38)
                // para que no quede una semilla congelada junto a la uva mientras dispara la semilla real
                if (idx == 4 || idx == 6 || idx == 8)
                {
                    for (int y = 0; y < tex.height; y++)
                    {
                        for (int x = 38; x < tex.width; x++)
                        {
                            tex.SetPixel(x, y, new Color(0, 0, 0, 0));
                        }
                    }
                    tex.Apply();
                }

                File.WriteAllBytes(destPath, tex.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(tex);
            }

            if (File.Exists(destPath))
            {
                AssetDatabase.ImportAsset(destPath, ImportAssetOptions.ForceUpdate);
                ConfigurePixelSpriteImporter(destPath, pixelsPerUnit: 26f);
                result[i] = AssetDatabase.LoadAssetAtPath<Sprite>(destPath);
            }
        }

        if (File.Exists(SeedSpritePath))
        {
            AssetDatabase.ImportAsset(SeedSpritePath, ImportAssetOptions.ForceUpdate);
            ConfigurePixelSpriteImporter(SeedSpritePath, pixelsPerUnit: 32f);
        }

        seedSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SeedSpritePath);
        return result;
    }

    private static void ExtractSeedTexture(Texture2D frame4Tex)
    {
        int sw = 14, sh = 12;
        Texture2D seedTex = new Texture2D(sw, sh, TextureFormat.RGBA32, false);
        Color clear = new Color(0, 0, 0, 0);

        for (int y = 0; y < sh; y++)
        {
            for (int x = 0; x < sw; x++)
            {
                seedTex.SetPixel(x, y, clear);
            }
        }

        int copiedPixels = 0;
        for (int y = 0; y < sh; y++)
        {
            for (int x = 0; x < 10; x++)
            {
                int srcX = 38 + x;
                int srcY = 16 + y;
                if (srcX < frame4Tex.width && srcY < frame4Tex.height)
                {
                    Color c = frame4Tex.GetPixel(srcX, srcY);
                    if (c.a > 0.1f)
                    {
                        seedTex.SetPixel(x + 2, y, c);
                        copiedPixels++;
                    }
                }
            }
        }

        if (copiedPixels < 10)
        {
            Color outline = new Color(0.10f, 0.05f, 0.03f, 1f);
            Color brown = new Color(0.56f, 0.35f, 0.20f, 1f);
            Color light = new Color(0.76f, 0.54f, 0.34f, 1f);
            for (int y = 2; y < sh - 2; y++)
            {
                for (int x = 2; x < sw - 2; x++)
                {
                    bool edge = (x == 2 || x == sw - 3 || y == 2 || y == sh - 3);
                    seedTex.SetPixel(x, y, edge ? outline : (y > sh / 2 ? light : brown));
                }
            }
        }

        seedTex.Apply();
        File.WriteAllBytes(SeedSpritePath, seedTex.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(seedTex);
    }

    private static void ConfigurePixelSpriteImporter(string assetPath, float pixelsPerUnit)
    {
        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.spritePixelsPerUnit = pixelsPerUnit;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
        }
    }

    private static void CreateMerlotAnimationClips(Sprite[] s)
    {
        if (s == null || s.Length < 10 || s[0] == null) return;

        // 1. Merlot_Idle.anim (frames 1, 2, 3)
        CreateSpriteAnimClip(
            $"{MerlotAnimFolder}/Merlot_Idle.anim",
            new Sprite[] { s[0], s[1], s[2], s[1] },
            new float[] { 0f, 0.22f, 0.44f, 0.66f },
            loop: true
        );

        // 2. Merlot_Attack.anim (inflar cachetes: 9 -> 10 -> abrir boca con dientes: 8 -> escupir: 4 -> 6)
        CreateSpriteAnimClip(
            $"{MerlotAnimFolder}/Merlot_Attack.anim",
            new Sprite[] { s[8], s[9], s[7], s[3], s[5] },
            new float[] { 0f, 0.16f, 0.30f, 0.44f, 0.56f },
            loop: false
        );

        // 3. Merlot_Hurt.anim (frames 5 y 7)
        CreateSpriteAnimClip(
            $"{MerlotAnimFolder}/Merlot_Hurt.anim",
            new Sprite[] { s[4], s[6] },
            new float[] { 0f, 0.15f },
            loop: false
        );
    }

    private static void CreateSpriteAnimClip(string path, Sprite[] sprites, float[] times, bool loop)
    {
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        bool isNew = false;
        if (clip == null)
        {
            clip = new AnimationClip();
            isNew = true;
        }

        EditorCurveBinding binding = new EditorCurveBinding
        {
            type = typeof(SpriteRenderer),
            path = "",
            propertyName = "m_Sprite"
        };

        ObjectReferenceKeyframe[] keys = new ObjectReferenceKeyframe[sprites.Length];
        for (int i = 0; i < sprites.Length; i++)
        {
            keys[i] = new ObjectReferenceKeyframe
            {
                time = times[i],
                value = sprites[i]
            };
        }

        AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);

        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        if (isNew)
            AssetDatabase.CreateAsset(clip, path);
        else
            EditorUtility.SetDirty(clip);
    }

    private static GameObject CreateOrUpdateSeedPrefab(Sprite seedSprite)
    {
        GameObject temp = new GameObject("GrapeSeed");

        SpriteRenderer sr = temp.AddComponent<SpriteRenderer>();
        sr.sprite = seedSprite;
        sr.sortingOrder = 6;

        Rigidbody2D rb = temp.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 0f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        CircleCollider2D col = temp.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.15f;

        GrapeSeedProjectile proj = temp.AddComponent<GrapeSeedProjectile>();
        proj.speed = 7.5f;
        proj.maxDistance = 7.5f;
        proj.damage = 1;
        proj.seedScale = 0.85f;

        temp.transform.localScale = new Vector3(0.85f, 0.85f, 1f);

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(temp, SeedPrefabPath);
        UnityEngine.Object.DestroyImmediate(temp);
        return prefab;
    }

    private static GameObject CreateOrUpdateMerlotPrefab(Sprite[] s, GameObject seedPrefab)
    {
        GameObject temp = new GameObject("MerlotGrape");
        temp.tag = "Enemy";

        SpriteRenderer sr = temp.AddComponent<SpriteRenderer>();
        sr.sprite = s[0];
        sr.sortingOrder = 2;

        Rigidbody2D rb = temp.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 1.5f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        BoxCollider2D col = temp.AddComponent<BoxCollider2D>();
        col.size = new Vector2(1.05f, 1.35f);
        col.offset = new Vector2(0f, -0.05f);

        GameObject mouth = new GameObject("MouthPoint");
        mouth.transform.SetParent(temp.transform);
        mouth.transform.localPosition = new Vector3(0.42f, 0.04f, 0f);

        MerlotGrapeEnemy enemy = temp.AddComponent<MerlotGrapeEnemy>();
        enemy.maxHealth = 3;
        enemy.detectionRange = 8.5f;
        enemy.attackInterval = 2.2f;
        enemy.seedPrefab = seedPrefab;
        enemy.mouthPoint = mouth.transform;
        enemy.idleSprites = new Sprite[] { s[0], s[1], s[2] };
        enemy.attackSprites = new Sprite[] { s[8], s[9], s[7], s[3], s[5] };
        enemy.hurtSprites = new Sprite[] { s[4], s[6] };

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(temp, MerlotPrefabPath);
        UnityEngine.Object.DestroyImmediate(temp);
        return prefab;
    }

    private static void SpawnGrapesInScene(GameObject merlotPrefabOverride = null)
    {
        GameObject merlotPrefab = merlotPrefabOverride != null
            ? merlotPrefabOverride
            : AssetDatabase.LoadAssetAtPath<GameObject>(MerlotPrefabPath);

        if (merlotPrefab == null) return;

        GameObject container = GameObject.Find("Enemigos_UvasMerlot");
        if (container != null)
        {
            UnityEngine.Object.DestroyImmediate(container);
        }

        container = new GameObject("Enemigos_UvasMerlot");

        Vector3 playerPos = new Vector3(-4.26f, 7.08f, 0f);
        MovimientoJugador player = UnityEngine.Object.FindFirstObjectByType<MovimientoJugador>();
        if (player != null)
        {
            playerPos = player.transform.position;
        }

        Vector2[] relativeOffsets = new Vector2[]
        {
            new Vector2(6.5f, 0.2f),   // 1ra Uva: cerca del inicio para probar el combate de inmediato
            new Vector2(14.5f, 0.2f),  // 2da Uva: más adelante en el camino
            new Vector2(23.0f, 0.2f),  // 3ra Uva: zona media
            new Vector2(32.5f, 0.2f),  // 4ta Uva: más adelante
            new Vector2(-11.0f, 0.2f)  // 5ta Uva: hacia el lado izquierdo del viñedo
        };

        int sueloMask = LayerMask.GetMask("Suelo");

        for (int i = 0; i < relativeOffsets.Length; i++)
        {
            Vector3 targetPos = new Vector3(
                playerPos.x + relativeOffsets[i].x,
                playerPos.y + relativeOffsets[i].y,
                0f
            );

            Vector2 rayOrigin = new Vector2(targetPos.x, playerPos.y + 8f);
            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.down, 25f, sueloMask != 0 ? sueloMask : ~0);
            if (hit.collider != null && hit.collider.gameObject != (player != null ? player.gameObject : null))
            {
                targetPos.y = hit.point.y + 0.75f;
            }

            GameObject grapeInstance = (GameObject)PrefabUtility.InstantiatePrefab(merlotPrefab);
            grapeInstance.name = $"UvaMerlot_{i + 1}";
            grapeInstance.transform.SetParent(container.transform);
            grapeInstance.transform.position = targetPos;
        }

        EditorSceneManager.MarkSceneDirty(container.scene);
        Debug.Log("[Vineyard] ¡Se colocaron 5 Uvas Merlot en el mapa listas para el combate!");
    }
}
#endif
