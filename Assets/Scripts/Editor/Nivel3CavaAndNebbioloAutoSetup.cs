#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

/// <summary>
/// Construye por completo el Nivel 3 ("La Cava Subterránea", Assets/Scenes/Nivel3.unity):
/// 1. Importa los 14 sprites de la nueva uva explosiva Nebbiolo (PPU=26) y crea Assets/Prefabs/EnemigoNebbiolo.prefab.
/// 2. Genera la Botella Sagrada completa (32x64 pixel art) y sus 5 pedazos exactos ensamblables + HUD visual de botella.
/// 3. Configura el fondo pixel-art de Cava Subterránea (FondoNivel3_CavaSubterranea.jpg) en 6 paneles continuos.
/// 4. Crea Assets/Scenes/Nivel3.unity con 5 bóvedas simétricas de alta dificultad (X = 0..194),
///    5 pedazos de botella protegidos por combinaciones de Nebbiolo, Cab. Sau y Merlot,
///    5 fosos de ácido, 3 corazones curativos y el Altar Final (MetaNivel3) bloqueado hasta armar los 5/5 pedazos.
/// 5. Registra Nivel1 -> Nivel2 -> Nivel3 en Build Settings.
/// </summary>
[InitializeOnLoad]
public static class Nivel3CavaAndNebbioloAutoSetup
{
    private const string SetupPrefKey = "Vineyard_Nivel3_Cava_Nebbiolo_V2";
    private const int CurrentVersion = 2;

    private const string Nivel1ScenePath = "Assets/Scenes/Nivel1.unity";
    private const string Nivel2ScenePath = "Assets/Scenes/Nivel2.unity";
    private const string Nivel3ScenePath = "Assets/Scenes/Nivel3.unity";

    private const string NewBgAssetPath = "Assets/Sprites/Fondos/FondoNivel3_CavaSubterranea.jpg";
    private const string NebbioloSpriteFolder = "Assets/Sprites/Enemigos/Nebbiolo";
    private const string NebbioloPrefabPath = "Assets/Prefabs/EnemigoNebbiolo.prefab";
    private const string CabSauPrefabPath = "Assets/Prefabs/EnemigoCabSau.prefab";
    private const string MerlotPrefabPath = "Assets/Prefabs/EnemigoUva.prefab";
    private const string CorazonPrefabPath = "Assets/Prefabs/CorazonVida.prefab";
    private const string MetaPrefabPath = "Assets/Prefabs/MetaNivel.prefab";

    private const string BottleSpriteFolder = "Assets/Sprites/Items/BotellaNivel3";
    private const string BottleCompletePath = "Assets/Sprites/Items/BotellaNivel3/BotellaCompleta.png";
    private const string BottleAuraPath = "Assets/Sprites/Items/BotellaNivel3/AuraAltarBotella.png";
    private const string CheckpointOffPath = "Assets/Sprites/Items/BotellaNivel3/FarolCheckpoint_Apagado.png";
    private const string CheckpointOnPath = "Assets/Sprites/Items/BotellaNivel3/FarolCheckpoint_Encendido.png";

    private const string BarrelSpritePath = "Assets/Sprites/Nivel2Props/BarrilVino.png";
    private const string CrateSpritePath = "Assets/Sprites/Nivel2Props/CajaMadera.png";
    private const string ArchSpritePath = "Assets/Sprites/Nivel2Props/ArcoPiedraBodega.png";
    private const string ZeroFrictionMatPath = "Assets/Settings/JugadorSinFriccion.physicsMaterial2D";

    private const string SfxNebbioloSwellPath = "Assets/Audio/SFX_NebbioloHinchar.wav";
    private const string SfxNebbioloBoomPath = "Assets/Audio/SFX_NebbioloExplosion.wav";
    private const string SfxBottlePiecePath = "Assets/Audio/SFX_PiezaBotella.wav";
    private const string SfxBottleCompletePath = "Assets/Audio/SFX_BotellaCompleta.wav";
    private const string SfxBottleLockedPath = "Assets/Audio/SFX_BotellaBloqueada.wav";
    private const string SfxCheckpointPath = "Assets/Audio/SFX_CheckpointCava.wav";
    private const string SfxAcidSplashPath = "Assets/Audio/SFX_AcidoBurbuja.wav";

    static Nivel3CavaAndNebbioloAutoSetup()
    {
        EditorApplication.delayCall += RunOnceIfNeeded;
    }

    private static void RunOnceIfNeeded()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        bool needsVersion = EditorPrefs.GetInt(SetupPrefKey, 0) < CurrentVersion;
        bool needsScene = !File.Exists(Nivel3ScenePath);
        bool needsPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(NebbioloPrefabPath) == null;

        if ((needsVersion || needsScene || needsPrefab) && File.Exists(Nivel2ScenePath))
        {
            BuildNivel3AndNebbiolo(silent: true, keepNivel3Open: true);
        }
    }

    [MenuItem("Herramientas Vineyard/Construir Nivel 3 (La Cava, Botella, Checkpoints y Nebbiolo)")]
    public static void BuildFromMenu()
    {
        BuildNivel3AndNebbiolo(silent: false, keepNivel3Open: true);
        EditorUtility.DisplayDialog(
            "Vineyard - Nivel 3 (La Cava Subterránea) Listo",
            "¡Se construyó Assets/Scenes/Nivel3.unity ampliado con Checkpoints!\n\n" +
            "• Mapa extenso de 7 Bóvedas de Cava (X = 0 a 278, más largo que Nivel 1 y Nivel 2).\n" +
            "• 3 Checkpoints (Faroles de la Cava en X = 75.5, X = 155 y X = 234): guardan tu reaparición y los pedazos de botella reunidos + curan +25 HP.\n" +
            "• Nueva Uva Nebbiolo (Assets/Prefabs/EnemigoNebbiolo.prefab): se hincha al acercarte, explota lanzando 6 gotas de ácido y deja charco ácido burbujeante.\n" +
            "• 5 Pedazos ensamblables de la Botella repartidos en las 7 bóvedas + Altar Final (MetaNivel3 en X = 272.5).",
            "¡A explorar la Cava!"
        );
    }

    [MenuItem("Herramientas Vineyard/Abrir Escena Nivel 3 (La Cava Subterránea)")]
    public static void OpenNivel3Menu()
    {
        if (!File.Exists(Nivel3ScenePath))
        {
            BuildNivel3AndNebbiolo(silent: true, keepNivel3Open: true);
            return;
        }
        EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
        EditorSceneManager.OpenScene(Nivel3ScenePath, OpenSceneMode.Single);
    }

    public static void BuildNivel3AndNebbiolo(bool silent, bool keepNivel3Open)
    {
        EnsureDirectories();

        // 1. Generar SFX para Nebbiolo, piezas de la botella y checkpoints
        GenerateNivel3Audio(
            out AudioClip sfxSwell,
            out AudioClip sfxBoom,
            out AudioClip sfxPiece,
            out AudioClip sfxComplete,
            out AudioClip sfxLocked,
            out AudioClip sfxCheckpoint,
            out AudioClip sfxAcid
        );

        // 2. Configurar los 14 sprites de Nebbiolo (PPU=26) y crear su Prefab
        Sprite[] nebbioloSprites = ConfigureNebbioloSprites();
        GameObject nebbioloPrefab = CreateOrUpdateNebbioloPrefab(nebbioloSprites, sfxSwell, sfxBoom);

        // 3. Generar la Botella Sagrada completa + 5 pedazos exactos ensamblables + Faroles Checkpoint
        GenerateBottleAndPiecesSprites(
            out Sprite completeBottleSprite,
            out Sprite auraSprite,
            out Sprite[] hudPieceSprites,
            out Sprite[] worldPieceSprites
        );
        GenerateCheckpointLanternSprites(out Sprite checkpointOffSprite, out Sprite checkpointOnSprite);

        // 4. Configurar el fondo pixel-art de Cava Subterránea
        Sprite cavaBgSprite = ConfigureCavaBackgroundSprite();

        // 5. Construir la escena Assets/Scenes/Nivel3.unity (7 Bóvedas, X = 0..278, 3 Checkpoints)
        ConstructNivel3Scene(
            nebbioloPrefab,
            cavaBgSprite,
            completeBottleSprite,
            auraSprite,
            hudPieceSprites,
            worldPieceSprites,
            checkpointOffSprite,
            checkpointOnSprite,
            sfxPiece,
            sfxComplete,
            sfxLocked,
            sfxCheckpoint,
            sfxAcid,
            keepNivel3Open
        );

        // 6. Registrar Nivel1, Nivel2 y Nivel3 en Build Settings
        RegisterAllThreeScenesInBuildSettings();

        EditorPrefs.SetInt(SetupPrefKey, CurrentVersion);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        if (!silent)
        {
            Debug.Log("[Vineyard] Nivel 3 (La Cava), Botella de 5 piezas y EnemigoNebbiolo creados correctamente.");
        }
    }

    private static void EnsureDirectories()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Sprites")) AssetDatabase.CreateFolder("Assets", "Sprites");
        if (!AssetDatabase.IsValidFolder("Assets/Sprites/Enemigos")) AssetDatabase.CreateFolder("Assets/Sprites", "Enemigos");
        if (!AssetDatabase.IsValidFolder(NebbioloSpriteFolder)) AssetDatabase.CreateFolder("Assets/Sprites/Enemigos", "Nebbiolo");
        if (!AssetDatabase.IsValidFolder("Assets/Sprites/Items")) AssetDatabase.CreateFolder("Assets/Sprites", "Items");
        if (!AssetDatabase.IsValidFolder(BottleSpriteFolder)) AssetDatabase.CreateFolder("Assets/Sprites/Items", "BotellaNivel3");
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs")) AssetDatabase.CreateFolder("Assets", "Prefabs");
        if (!AssetDatabase.IsValidFolder("Assets/Audio")) AssetDatabase.CreateFolder("Assets", "Audio");
    }

    private static Sprite[] ConfigureNebbioloSprites()
    {
        Sprite[] sprites = new Sprite[14];
        for (int i = 1; i <= 14; i++)
        {
            string path = $"{NebbioloSpriteFolder}/nebbiolo_{i}.png";
            if (!File.Exists(path)) continue;

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            TextureImporter imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp != null)
            {
                imp.textureType = TextureImporterType.Sprite;
                imp.spriteImportMode = SpriteImportMode.Single;
                imp.spritePixelsPerUnit = 26f; // Misma escala limpia que Merlot y Cab. Sau (~1.85 unidades)
                imp.filterMode = FilterMode.Point;
                imp.textureCompression = TextureImporterCompression.Uncompressed;
                imp.alphaIsTransparency = true;
                imp.mipmapEnabled = false;
                imp.SaveAndReimport();
            }
            sprites[i - 1] = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
        return sprites;
    }

    private static GameObject CreateOrUpdateNebbioloPrefab(Sprite[] s, AudioClip sfxSwell, AudioClip sfxBoom)
    {
        GameObject temp = new GameObject("EnemigoNebbiolo");
        try { temp.tag = "Enemy"; } catch { }

        SpriteRenderer sr = temp.AddComponent<SpriteRenderer>();
        sr.sprite = (s != null && s.Length > 0) ? s[0] : null;
        sr.sortingOrder = 3;

        Rigidbody2D rb = temp.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 2.5f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        BoxCollider2D col = temp.AddComponent<BoxCollider2D>();
        col.size = new Vector2(1.14f, 1.24f);
        col.offset = new Vector2(0f, -0.05f);

        AudioSource audioSrc = temp.AddComponent<AudioSource>();
        audioSrc.playOnAwake = false;
        audioSrc.spatialBlend = 0f;

        EnemigoNebbiolo neb = temp.AddComponent<EnemigoNebbiolo>();
        neb.maxHealth = 3;
        neb.currentHealth = 3;
        neb.puntosAlDerrotar = 175;
        neb.rangoDetonacion = 4.4f;
        neb.diferenciaAlturaMaxima = 2.8f;
        neb.tiempoHinchadoAntesDeExplotar = 1.15f;
        neb.radioExplosion = 3.0f;
        neb.danoExplosion = 26f;
        neb.cantidadGotasAcido = 6;
        neb.danoPorGotaAcido = 16f;
        neb.duracionCharcoAcido = 6.5f;
        neb.patrullarSuave = true;
        neb.velocidadPatrulla = 1.1f;
        neb.distanciaPatrulla = 1.4f;
        neb.audioSource = audioSrc;
        neb.sonidoHinchando = sfxSwell;
        neb.sonidoExplosionAcido = sfxBoom;
        neb.sonidoGolpe = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX_ImpactoUva.wav");

        // Secuencia exacta verificada visualmente de los 14 sprites de Nebbiolo:
        // Idle: 1 | Hinchando: 10 -> 4 -> 14 -> 6 -> 11 | Explosión: 8 -> 7 -> 3 -> 13 -> 2 | Charco: 5 -> 9 -> 12
        if (s != null && s.Length >= 14)
        {
            neb.spriteIdle = s[0];
            neb.spritesHinchando = new Sprite[] { s[9], s[3], s[13], s[5], s[10] };
            neb.spritesExplosion = new Sprite[] { s[7], s[6], s[2], s[12], s[1] };
            neb.spritesCharcoAcido = new Sprite[] { s[4], s[8], s[11] };
        }

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(temp, NebbioloPrefabPath);
        UnityEngine.Object.DestroyImmediate(temp);
        return prefab;
    }

    private static Sprite ConfigureCavaBackgroundSprite()
    {
        if (!File.Exists(NewBgAssetPath)) return null;

        AssetDatabase.ImportAsset(NewBgAssetPath, ImportAssetOptions.ForceSynchronousImport);
        TextureImporter imp = AssetImporter.GetAtPath(NewBgAssetPath) as TextureImporter;
        if (imp != null)
        {
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.spritePixelsPerUnit = 34f; // 1376x768 -> ~40.47 x 22.59 unidades
            imp.filterMode = FilterMode.Point;
            imp.maxTextureSize = 2048;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.mipmapEnabled = false;
            imp.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(NewBgAssetPath);
    }

    // =========================================================================
    // GENERACIÓN DE LA BOTELLA SAGRADA COMPLETA (32x64) Y SUS 5 PEDAZOS ENSAMBLABLES
    // =========================================================================
    private static void GenerateBottleAndPiecesSprites(
        out Sprite completeBottleSprite,
        out Sprite auraSprite,
        out Sprite[] hudPieceSprites,
        out Sprite[] worldPieceSprites)
    {
        int w = 32, h = 64;
        Texture2D master = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Color clear = new Color(0f, 0f, 0f, 0f);
        Color outline = new Color(0.05f, 0.08f, 0.06f, 1f);
        Color glassDark = new Color(0.08f, 0.30f, 0.16f, 1f);
        Color glassMid = new Color(0.14f, 0.52f, 0.26f, 1f);
        Color glassBright = new Color(0.32f, 0.78f, 0.42f, 1f);
        Color glassShine = new Color(0.75f, 0.98f, 0.78f, 1f);
        Color labelCream = new Color(0.95f, 0.88f, 0.70f, 1f);
        Color labelBorder = new Color(0.84f, 0.64f, 0.22f, 1f);
        Color wineRuby = new Color(0.58f, 0.10f, 0.28f, 1f);
        Color waxSeal = new Color(0.74f, 0.12f, 0.20f, 1f);
        Color corkGold = new Color(0.92f, 0.74f, 0.36f, 1f);

        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                master.SetPixel(x, y, clear);

        // Dibujar silueta elegante de botella vintage de cava (y = 3..60)
        for (int y = 3; y <= 60; y++)
        {
            float halfW;
            if (y <= 36) halfW = 9.5f;                              // Cuerpo principal
            else if (y <= 45) halfW = Mathf.Lerp(9.5f, 3.6f, (y - 36f) / 9f); // Hombros curvos
            else if (y <= 55) halfW = 3.6f;                         // Cuello
            else if (y <= 57) halfW = 4.4f;                         // Labio del cuello
            else halfW = 3.2f;                                      // Corcho dorado superior

            float cx = 15.5f;
            for (int x = 0; x < w; x++)
            {
                float dist = Mathf.Abs(x - cx);
                if (dist > halfW + 0.5f) continue;

                bool isBorder = dist >= halfW - 0.65f || y == 3 || y == 60;
                if (isBorder)
                {
                    master.SetPixel(x, y, outline);
                    continue;
                }

                // Corcho dorado en la punta (y = 58..59)
                if (y >= 58)
                {
                    master.SetPixel(x, y, (x < 15) ? corkGold : labelBorder);
                }
                // Sello de cera borgoña en el cuello (y = 52..57)
                else if (y >= 52)
                {
                    master.SetPixel(x, y, (x == 14) ? glassShine : waxSeal);
                }
                // Etiqueta central del viñedo (y = 18..33)
                else if (y >= 18 && y <= 33 && dist <= 7.2f)
                {
                    bool frame = (y == 18 || y == 33 || dist >= 6.2f);
                    bool crest = (y >= 22 && y <= 29 && dist <= 3.0f);
                    if (frame) master.SetPixel(x, y, labelBorder);
                    else if (crest) master.SetPixel(x, y, (y == 25 || y == 26) ? labelBorder : wineRuby);
                    else master.SetPixel(x, y, labelCream);
                }
                // Vidrio esmeralda con reflejos
                else
                {
                    if (x >= cx - halfW + 2f && x <= cx - halfW + 3.4f && y > 6 && y < 50)
                        master.SetPixel(x, y, glassShine);
                    else if (x < cx - 1f)
                        master.SetPixel(x, y, glassBright);
                    else if (x > cx + halfW - 3f)
                        master.SetPixel(x, y, glassDark);
                    else
                        master.SetPixel(x, y, glassMid);
                }
            }
        }
        master.Apply();

        File.WriteAllBytes(BottleCompletePath, master.EncodeToPNG());
        AssetDatabase.ImportAsset(BottleCompletePath, ImportAssetOptions.ForceSynchronousImport);
        ConfigurePixelSprite(BottleCompletePath, 28f);
        completeBottleSprite = AssetDatabase.LoadAssetAtPath<Sprite>(BottleCompletePath);

        // Dividir la botella maestra en 5 franjas con corte diagonal/zig-zag de vidrio roto
        // que al superponerse reconstruyen EXACTAMENTE el 100% de los píxeles de BotellaCompleta!
        int[] baseCuts = new int[] { 0, 14, 24, 34, 46, 64 };
        hudPieceSprites = new Sprite[5];
        worldPieceSprites = new Sprite[5];

        for (int p = 0; p < 5; p++)
        {
            Texture2D hudTex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    hudTex.SetPixel(x, y, clear);

            int minX = w, maxX = 0, minY = h, maxY = 0;

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    Color c = master.GetPixel(x, y);
                    if (c.a <= 0.05f) continue;

                    // Corte ligeramente irregular de vidrio roto entre piezas (misma función para piezas adyacentes -> encaje perfecto!)
                    int cutLow = baseCuts[p] + ((p == 0) ? 0 : GetGlassFractureOffset(p, x));
                    int cutHigh = baseCuts[p + 1] + ((p == 4) ? 0 : GetGlassFractureOffset(p + 1, x));

                    if (y >= cutLow && y < cutHigh)
                    {
                        hudTex.SetPixel(x, y, c);
                        if (x < minX) minX = x;
                        if (x > maxX) maxX = x;
                        if (y < minY) minY = y;
                        if (y > maxY) maxY = y;
                    }
                }
            }
            hudTex.Apply();

            string hudPath = $"{BottleSpriteFolder}/PiezaBotella_{p + 1}.png";
            File.WriteAllBytes(hudPath, hudTex.EncodeToPNG());
            AssetDatabase.ImportAsset(hudPath, ImportAssetOptions.ForceSynchronousImport);
            ConfigurePixelSprite(hudPath, 32f);
            hudPieceSprites[p] = AssetDatabase.LoadAssetAtPath<Sprite>(hudPath);

            // Crear versión recortada con brillo dorado para recoger en el escenario
            int pad = 3;
            int cw = Mathf.Max(8, (maxX - minX + 1) + pad * 2);
            int ch = Mathf.Max(8, (maxY - minY + 1) + pad * 2);
            Texture2D worldTex = new Texture2D(cw, ch, TextureFormat.RGBA32, false);
            Color glowGold = new Color(1f, 0.94f, 0.45f, 0.65f);
            for (int y = 0; y < ch; y++)
                for (int x = 0; x < cw; x++)
                    worldTex.SetPixel(x, y, clear);

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    Color c = hudTex.GetPixel(x, y);
                    if (c.a > 0.1f)
                    {
                        int wx = (x - minX) + pad;
                        int wy = (y - minY) + pad;
                        worldTex.SetPixel(wx, wy, c);
                    }
                }
            }

            // Halo dorado sutil de 1px alrededor del fragmento para máxima visibilidad en la cava
            Texture2D copy = new Texture2D(cw, ch, TextureFormat.RGBA32, false);
            copy.SetPixels(worldTex.GetPixels());
            for (int y = 1; y < ch - 1; y++)
            {
                for (int x = 1; x < cw - 1; x++)
                {
                    if (copy.GetPixel(x, y).a > 0.1f) continue;
                    bool adj = copy.GetPixel(x - 1, y).a > 0.5f || copy.GetPixel(x + 1, y).a > 0.5f ||
                               copy.GetPixel(x, y - 1).a > 0.5f || copy.GetPixel(x, y + 1).a > 0.5f;
                    if (adj) worldTex.SetPixel(x, y, glowGold);
                }
            }
            worldTex.Apply();

            string worldPath = $"{BottleSpriteFolder}/PiezaMundo_{p + 1}.png";
            File.WriteAllBytes(worldPath, worldTex.EncodeToPNG());
            AssetDatabase.ImportAsset(worldPath, ImportAssetOptions.ForceSynchronousImport);
            ConfigurePixelSprite(worldPath, 18f); // Tamaño bien visible en el mundo (~1.2-1.5 unidades)
            worldPieceSprites[p] = AssetDatabase.LoadAssetAtPath<Sprite>(worldPath);

            UnityEngine.Object.DestroyImmediate(hudTex);
            UnityEngine.Object.DestroyImmediate(worldTex);
            UnityEngine.Object.DestroyImmediate(copy);
        }

        UnityEngine.Object.DestroyImmediate(master);

        // Aura suave circular para el Altar Final
        int aw = 64, ah = 64;
        Texture2D auraTex = new Texture2D(aw, ah, TextureFormat.RGBA32, false);
        for (int y = 0; y < ah; y++)
        {
            for (int x = 0; x < aw; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(31.5f, 31.5f)) / 31.5f;
                float a = Mathf.Pow(Mathf.Clamp01(1f - d), 1.8f);
                auraTex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }
        auraTex.Apply();
        File.WriteAllBytes(BottleAuraPath, auraTex.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(auraTex);
        AssetDatabase.ImportAsset(BottleAuraPath, ImportAssetOptions.ForceSynchronousImport);
        ConfigurePixelSprite(BottleAuraPath, 20f);
        auraSprite = AssetDatabase.LoadAssetAtPath<Sprite>(BottleAuraPath);
    }

    private static void GenerateCheckpointLanternSprites(out Sprite offSprite, out Sprite onSprite)
    {
        DrawCheckpointLanternPng(CheckpointOffPath, lit: false);
        DrawCheckpointLanternPng(CheckpointOnPath, lit: true);

        AssetDatabase.ImportAsset(CheckpointOffPath, ImportAssetOptions.ForceSynchronousImport);
        ConfigurePixelSprite(CheckpointOffPath, 16f);
        offSprite = AssetDatabase.LoadAssetAtPath<Sprite>(CheckpointOffPath);

        AssetDatabase.ImportAsset(CheckpointOnPath, ImportAssetOptions.ForceSynchronousImport);
        ConfigurePixelSprite(CheckpointOnPath, 16f);
        onSprite = AssetDatabase.LoadAssetAtPath<Sprite>(CheckpointOnPath);
    }

    private static void DrawCheckpointLanternPng(string path, bool lit)
    {
        int w = 24, h = 44;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Color clear = new Color(0f, 0f, 0f, 0f);
        Color outline = new Color(0.06f, 0.06f, 0.09f, 1f);
        Color ironDark = new Color(0.18f, 0.19f, 0.24f, 1f);
        Color ironMid = new Color(0.30f, 0.32f, 0.39f, 1f);
        Color ironLight = new Color(0.46f, 0.48f, 0.56f, 1f);
        Color stoneBase = new Color(0.28f, 0.28f, 0.34f, 1f);
        Color glassOff = new Color(0.16f, 0.24f, 0.26f, 0.88f);
        Color flameOuter = new Color(0.25f, 0.96f, 0.48f, 1f);
        Color flameMid = new Color(0.85f, 1.0f, 0.42f, 1f);
        Color flameCore = new Color(1.0f, 0.99f, 0.85f, 1f);

        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                tex.SetPixel(x, y, clear);

        // Pedestal de piedra en la base (y = 0..5, x = 5..18)
        for (int y = 0; y <= 5; y++)
        {
            for (int x = 5; x <= 18; x++)
            {
                bool edge = (y == 0 || y == 5 || x == 5 || x == 18);
                tex.SetPixel(x, y, edge ? outline : (y == 4 ? ironLight : stoneBase));
            }
        }

        // Poste de hierro forjado (y = 6..25, x = 10..13)
        for (int y = 6; y <= 25; y++)
        {
            tex.SetPixel(9, y, outline);
            tex.SetPixel(10, y, ironLight);
            tex.SetPixel(11, y, ironMid);
            tex.SetPixel(12, y, ironDark);
            tex.SetPixel(13, y, ironDark);
            tex.SetPixel(14, y, outline);
        }

        // Base y techo del farol (y = 26..27 y y = 38..41)
        for (int x = 6; x <= 17; x++)
        {
            tex.SetPixel(x, 26, outline);
            tex.SetPixel(x, 27, ironMid);
            tex.SetPixel(x, 38, ironMid);
            tex.SetPixel(x, 39, outline);
        }
        for (int x = 8; x <= 15; x++) { tex.SetPixel(x, 40, ironDark); tex.SetPixel(x, 41, outline); }
        tex.SetPixel(11, 42, ironLight); tex.SetPixel(12, 42, ironLight); tex.SetPixel(11, 43, outline); tex.SetPixel(12, 43, outline);

        // Cuerpo de cristal del farol (y = 28..37, x = 7..16)
        for (int y = 28; y <= 37; y++)
        {
            for (int x = 7; x <= 16; x++)
            {
                if (x == 7 || x == 16)
                {
                    tex.SetPixel(x, y, outline);
                }
                else if (x == 8 || x == 15)
                {
                    tex.SetPixel(x, y, ironMid);
                }
                else if (!lit)
                {
                    tex.SetPixel(x, y, (x == 10 && y >= 31 && y <= 35) ? ironLight : glassOff);
                }
                else
                {
                    float dx = Mathf.Abs(x - 11.5f);
                    float dy = Mathf.Abs(y - 32.5f);
                    float d = Mathf.Sqrt(dx * dx + dy * dy * 0.7f);
                    if (d < 1.8f) tex.SetPixel(x, y, flameCore);
                    else if (d < 3.1f) tex.SetPixel(x, y, flameMid);
                    else tex.SetPixel(x, y, flameOuter);
                }
            }
        }

        tex.Apply();
        File.WriteAllBytes(path, tex.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(tex);
    }

    private static int GetGlassFractureOffset(int cutIndex, int x)
    {
        // Pequeño patrón zig-zag determinista de vidrio quebrado (±2 px)
        int wave = Mathf.RoundToInt(Mathf.Sin((x + cutIndex * 7) * 0.45f) * 1.6f);
        return wave;
    }

    private static void ConfigurePixelSprite(string path, float ppu)
    {
        TextureImporter imp = AssetImporter.GetAtPath(path) as TextureImporter;
        if (imp == null) return;
        imp.textureType = TextureImporterType.Sprite;
        imp.spriteImportMode = SpriteImportMode.Single;
        imp.spritePixelsPerUnit = ppu;
        imp.filterMode = FilterMode.Point;
        imp.textureCompression = TextureImporterCompression.Uncompressed;
        imp.alphaIsTransparency = true;
        imp.mipmapEnabled = false;
        imp.SaveAndReimport();
    }

    // =========================================================================
    // CONSTRUCCIÓN DE LA ESCENA ASSETS/SCENES/NIVEL3.UNITY (LA CAVA SUBTERRÁNEA)
    // =========================================================================
    private static void ConstructNivel3Scene(
        GameObject nebbioloPrefab,
        Sprite cavaBgSprite,
        Sprite completeBottleSprite,
        Sprite auraSprite,
        Sprite[] hudPieceSprites,
        Sprite[] worldPieceSprites,
        Sprite checkpointOffSprite,
        Sprite checkpointOnSprite,
        AudioClip sfxPiece,
        AudioClip sfxComplete,
        AudioClip sfxLocked,
        AudioClip sfxCheckpoint,
        AudioClip sfxAcid,
        bool keepNivel3Open)
    {
        if (!File.Exists(Nivel2ScenePath))
        {
            Debug.LogWarning("[Nivel3Setup] No se encontró Assets/Scenes/Nivel2.unity como base.");
            return;
        }

        string previousScenePath = EditorSceneManager.GetActiveScene().path;
        if (EditorSceneManager.GetActiveScene().isDirty)
        {
            EditorSceneManager.SaveOpenScenes();
        }

        if (File.Exists(Nivel3ScenePath))
        {
            AssetDatabase.DeleteAsset(Nivel3ScenePath);
        }
        AssetDatabase.CopyAsset(Nivel2ScenePath, Nivel3ScenePath);
        AssetDatabase.ImportAsset(Nivel3ScenePath, ImportAssetOptions.ForceSynchronousImport);

        Scene scene3 = EditorSceneManager.OpenScene(Nivel3ScenePath, OpenSceneMode.Single);

        // 1. Limpieza completa de objetos y Tilemaps del Nivel 2 manteniendo Character, Cámara y Canvas
        CleanBaseSceneForNivel3(out Tilemap tilePiso, out Tilemap structuresMap, out Tilemap elementsMap);

        // 2. Configurar al jugador con el movimiento fluido y sin fricción
        ConfigureSmoothPlayerInNivel3();

        // 3. Fondo subterráneo de la Cava (8 paneles espejados de X=-30 a X=295)
        if (cavaBgSprite != null)
        {
            BuildCavaBackground(cavaBgSprite);
        }

        // 4. Construir las 7 Bóvedas simétricas de la Cava (X = 0..278) + 8 Fosos de Ácido + Escaleras + Props
        BuildSevenCellarVaults(tilePiso, structuresMap, elementsMap, sfxAcid);

        // 5. Crear el HUD de ensamblado de la Botella (5/5) + GestorBotellaNivel3 + Altar Final (MetaNivel3 en X=272.5)
        BuildBottleHUDAndFinalAltar(
            completeBottleSprite,
            auraSprite,
            hudPieceSprites,
            sfxPiece,
            sfxComplete,
            sfxLocked
        );

        // 6. Colocar los 3 Checkpoints (Faroles de la Cava) a lo largo del recorrido largo
        SpawnCheckpointsInNivel3(checkpointOffSprite, checkpointOnSprite, auraSprite, sfxCheckpoint);

        // 7. Colocar los 5 Pedazos de la Botella + 4 Corazones Curativos
        SpawnFiveBottlePiecesAndHearts(worldPieceSprites);

        // 8. Colocar los enemigos del Nivel 3 (Nebbiolo explosivas + Cab. Sau + Merlot en las 7 Bóvedas)
        SpawnNivel3HardEnemies(nebbioloPrefab);

        EditorSceneManager.MarkSceneDirty(scene3);
        EditorSceneManager.SaveScene(scene3);

        if (!keepNivel3Open && !string.IsNullOrEmpty(previousScenePath) && File.Exists(previousScenePath))
        {
            EditorSceneManager.OpenScene(previousScenePath, OpenSceneMode.Single);
        }
    }

    private static void CleanBaseSceneForNivel3(
        out Tilemap tilePiso,
        out Tilemap structuresMap,
        out Tilemap elementsMap)
    {
        // Quitar Opener si existiera
        TitleScreenOpener opener = UnityEngine.Object.FindAnyObjectByType<TitleScreenOpener>();
        if (opener != null)
        {
            if (opener.openerRootPanel != null) UnityEngine.Object.DestroyImmediate(opener.openerRootPanel);
            UnityEngine.Object.DestroyImmediate(opener.gameObject);
        }

        // Desactivar LluviaAcida (estamos bajo tierra en una cava cerrada)
        GameObject lluvia = GameObject.Find("LluviaAcida");
        if (lluvia != null) lluvia.SetActive(false);

        // Vaciar todos los Tilemaps
        GameObject tilePisoObj = GameObject.Find("TilePiso");
        GameObject structuresObj = GameObject.Find("Structures");
        GameObject elementsObj = GameObject.Find("Elements");
        tilePiso = tilePisoObj != null ? tilePisoObj.GetComponent<Tilemap>() : null;
        structuresMap = structuresObj != null ? structuresObj.GetComponent<Tilemap>() : null;
        elementsMap = elementsObj != null ? elementsObj.GetComponent<Tilemap>() : null;

        Tilemap[] allMaps = UnityEngine.Object.FindObjectsByType<Tilemap>(FindObjectsSortMode.None);
        for (int i = 0; i < allMaps.Length; i++)
        {
            allMaps[i].ClearAllTiles();
            allMaps[i].CompressBounds();
            allMaps[i].RefreshAllTiles();
        }

        int sueloLayer = LayerMask.NameToLayer("Suelo");
        if (sueloLayer < 0) sueloLayer = 6;
        if (tilePisoObj != null) tilePisoObj.layer = sueloLayer;

        // Borrar contenedores del Nivel 2 y fondos antiguos
        string[] toDelete = new string[]
        {
            "Enemigos_Nivel1", "Enemigos_Nivel2", "Enemigos_Nivel3_Cava",
            "ItemsCurativos_Nivel1", "ItemsCurativos_Nivel2", "ItemsCurativos_Nivel3",
            "InstalacionesBodega_Nivel2", "ExpansionZona4_Nivel2", "CavaSubterranea_Nivel3",
            "FondosBodega_Nivel2", "FondosCava_Nivel3",
            "fondo1", "fondo (1)", "fondo (2)", "fondo1 (3)", "fondo1 (4)",
            "PiezasBotella_Nivel3", "GestorBotellaNivel3", "Checkpoints_Nivel3"
        };
        for (int i = 0; i < toDelete.Length; i++)
        {
            GameObject g = GameObject.Find(toDelete[i]);
            if (g != null) UnityEngine.Object.DestroyImmediate(g);
        }

        // Borrar cualquier enemigo/corazón suelto
        foreach (var e in UnityEngine.Object.FindObjectsByType<EnemigoCabSau>(FindObjectsSortMode.None))
            UnityEngine.Object.DestroyImmediate(e.gameObject);
        foreach (var e in UnityEngine.Object.FindObjectsByType<MerlotGrapeEnemy>(FindObjectsSortMode.None))
            UnityEngine.Object.DestroyImmediate(e.gameObject);
        foreach (var e in UnityEngine.Object.FindObjectsByType<EnemigoNebbiolo>(FindObjectsSortMode.None))
            UnityEngine.Object.DestroyImmediate(e.gameObject);
        foreach (var h in UnityEngine.Object.FindObjectsByType<CorazonVida>(FindObjectsSortMode.None))
            UnityEngine.Object.DestroyImmediate(h.gameObject);

        // Eliminar HUD anterior de botella si se re-ejecuta
        GameObject oldHud = GameObject.Find("BotellaHUDPanel");
        if (oldHud != null) UnityEngine.Object.DestroyImmediate(oldHud);
        GameObject oldToast = GameObject.Find("AvisoBotellaCavaText");
        if (oldToast != null) UnityEngine.Object.DestroyImmediate(oldToast);

        // Posicionar Character y cámaras al inicio de la Cava (X = 3.0, Y = 1.4)
        GameObject character = GameObject.Find("Character");
        if (character != null)
        {
            character.transform.position = new Vector3(3.0f, 1.4f, 0f);
            EditorUtility.SetDirty(character.transform);
        }
        if (Camera.main != null)
        {
            Camera.main.transform.position = new Vector3(3.0f, 3.2f, -10f);
            EditorUtility.SetDirty(Camera.main.transform);
        }
        GameObject cm = GameObject.Find("CinemachineCamera");
        if (cm != null)
        {
            cm.transform.position = new Vector3(3.0f, 3.2f, -10f);
            EditorUtility.SetDirty(cm.transform);
        }

        // Ajustar límites del mapa para X = 0..278 (mapa más largo que Nivel 1 y Nivel 2)
        GameObject limIzq = GameObject.Find("LimiteIzquierda");
        if (limIzq != null)
        {
            limIzq.transform.position = new Vector3(-0.6f, 6f, 0f);
            BoxCollider2D c = limIzq.GetComponent<BoxCollider2D>();
            if (c != null) { c.offset = Vector2.zero; c.size = new Vector2(1.2f, 40f); }
        }
        GameObject limDer = GameObject.Find("LimiteDerecha");
        if (limDer != null)
        {
            limDer.transform.position = new Vector3(277.5f, 6f, 0f);
            BoxCollider2D c = limDer.GetComponent<BoxCollider2D>();
            if (c != null) { c.offset = Vector2.zero; c.size = new Vector2(1.5f, 40f); }
        }
        GameObject limAbajo = GameObject.Find("LimiteAbajo");
        if (limAbajo != null)
        {
            limAbajo.transform.position = new Vector3(138f, -8.5f, 0f);
            BoxCollider2D c = limAbajo.GetComponent<BoxCollider2D>();
            if (c != null) { c.offset = Vector2.zero; c.size = new Vector2(330f, 1.5f); }
        }
        GameObject limArriba = GameObject.Find("LimiteArriba");
        if (limArriba != null)
        {
            limArriba.transform.position = new Vector3(138f, 20f, 0f);
            BoxCollider2D c = limArriba.GetComponent<BoxCollider2D>();
            if (c != null) { c.offset = Vector2.zero; c.size = new Vector2(330f, 1.5f); }
        }
    }

    private static void ConfigureSmoothPlayerInNivel3()
    {
        GameObject character = GameObject.Find("Character");
        if (character == null) return;

        PhysicsMaterial2D zeroMat = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(ZeroFrictionMatPath);

        BoxCollider2D box = character.GetComponent<BoxCollider2D>();
        if (box != null)
        {
            box.size = new Vector2(1.04f, 1.72f);
            box.offset = new Vector2(0f, -0.08f);
            box.edgeRadius = 0.08f;
            if (zeroMat != null) box.sharedMaterial = zeroMat;
        }

        Rigidbody2D rb = character.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            if (zeroMat != null) rb.sharedMaterial = zeroMat;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        }

        Transform puntoSuelo = character.transform.Find("PuntoSuelo");
        if (puntoSuelo != null) puntoSuelo.localPosition = new Vector3(0f, -1.00f, 0f);

        MovimientoJugador mov = character.GetComponent<MovimientoJugador>();
        if (mov != null)
        {
            int sueloLayer = LayerMask.NameToLayer("Suelo");
            if (sueloLayer < 0) sueloLayer = 6;
            mov.velocidadMovimiento = 8f;
            mov.fuerzaSalto = 11.4f;
            mov.tiempoCoyote = 0.13f;
            mov.tiempoBufferSalto = 0.14f;
            mov.multiplicadorCaida = 1.35f;
            mov.capaSuelo = 1 << sueloLayer;
            if (puntoSuelo != null) mov.comprobadorSuelo = puntoSuelo;
        }
    }

    private static void BuildCavaBackground(Sprite bgSprite)
    {
        GameObject container = new GameObject("FondosCava_Nivel3");
        float panelW = bgSprite.bounds.size.x;
        if (panelW < 1f) panelW = 40.47f;
        float stepX = panelW - 0.08f;
        float startX = -10f;
        float centerY = 7.2f;

        // 8 paneles continuos espejados cubren de X = -30 a X = 294
        for (int i = 0; i < 8; i++)
        {
            GameObject panel = new GameObject($"FondoCava_{i + 1}");
            panel.transform.SetParent(container.transform);
            panel.transform.position = new Vector3(startX + i * stepX, centerY, 0f);
            panel.transform.localScale = new Vector3((i % 2 == 0) ? 1f : -1f, 1f, 1f);

            SpriteRenderer sr = panel.AddComponent<SpriteRenderer>();
            sr.sprite = bgSprite;
            sr.sortingOrder = -10;
            sr.color = new Color(0.88f, 0.88f, 0.94f, 1f);
        }
    }

    private static void BuildSevenCellarVaults(
        Tilemap tilePiso,
        Tilemap structuresMap,
        Tilemap elementsMap,
        AudioClip sfxAcid)
    {
        GameObject root = new GameObject("CavaSubterranea_Nivel3");
        int sueloLayer = LayerMask.NameToLayer("Suelo");
        if (sueloLayer < 0) sueloLayer = 6;

        // Tiles de piedra oscura de cava (reutilizando los tiles más limpios de Nivel2)
        TileBase[] stoneTop = LoadTiles(
            "Assets/Tiles/Nivel2/tileset-ok-lvl2_12.asset",
            "Assets/Tiles/Nivel2/tileset-ok-lvl2_13.asset",
            "Assets/Tiles/Nivel2/tileset-ok-lvl2_14.asset",
            "Assets/Tiles/Nivel2/tileset-ok-lvl2_15.asset"
        );
        TileBase[] stoneMid = LoadTiles(
            "Assets/Tiles/Nivel2/tileset-ok-lvl2_28.asset",
            "Assets/Tiles/Nivel2/tileset-ok-lvl2_29.asset",
            "Assets/Tiles/Nivel2/tileset-ok-lvl2_30.asset",
            "Assets/Tiles/Nivel2/tileset-ok-lvl2_31.asset"
        );
        TileBase[] stoneDeep = LoadTiles(
            "Assets/Tiles/Nivel2/tileset-ok-lvl2_44.asset",
            "Assets/Tiles/Nivel2/tileset-ok-lvl2_45.asset",
            "Assets/Tiles/Nivel2/tileset-ok-lvl2_46.asset",
            "Assets/Tiles/Nivel2/tileset-ok-lvl2_47.asset"
        );
        TileBase[] platTop = LoadTiles(
            "Assets/Tiles/Nivel2/tileset-ok-lvl2_98.asset",
            "Assets/Tiles/Nivel2/tileset-ok-lvl2_99.asset"
        );
        TileBase[] platBot = LoadTiles(
            "Assets/Tiles/Nivel2/tileset-ok-lvl2_114.asset",
            "Assets/Tiles/Nivel2/tileset-ok-lvl2_115.asset"
        );

        TileBase fallbackTop = LoadTile("Assets/Tiles/Nivel2/Tile_PiedraGrisSuperficie.asset");
        TileBase fallbackDeep = LoadTile("Assets/Tiles/Nivel2/Tile_PiedraGrisProfunda.asset");
        TileBase tileWood = LoadTile("Assets/Tiles/Nivel2/Tile_PasarelaMadera.asset") ?? fallbackTop;
        TileBase tileLadder = LoadTile("Assets/Tiles/Nivel2/Tile_EscaleraMano.asset");
        TileBase tileAcidSurf = LoadTile("Assets/Tiles/Nivel2/Tile_AcidoSuperficie.asset");
        TileBase tileAcidDeep = LoadTile("Assets/Tiles/Nivel2/Tile_AcidoProfundo.asset");

        Sprite barrelSprite = AssetDatabase.LoadAssetAtPath<Sprite>(BarrelSpritePath);
        Sprite crateSprite = AssetDatabase.LoadAssetAtPath<Sprite>(CrateSpritePath);
        Sprite archSprite = AssetDatabase.LoadAssetAtPath<Sprite>(ArchSpritePath);

        // Muro de contención izquierdo (X = -3..-1) y derecho (X = 276..279)
        PaintStoneBlock(tilePiso, -3, -1, -6, 12, stoneTop, stoneMid, stoneDeep, fallbackTop, fallbackDeep);
        PaintStoneBlock(tilePiso, 276, 279, -6, 12, stoneTop, stoneMid, stoneDeep, fallbackTop, fallbackDeep);

        // =====================================================================
        // BÓVEDA 1 (X = 0..38): DESCENSO A LA CAVA — Pieza #1 (Base de la Botella)
        // =====================================================================
        PaintStoneBlock(tilePiso, 0, 38, -6, -1, stoneTop, stoneMid, stoneDeep, fallbackTop, fallbackDeep);
        PaintPlatform(tilePiso, 10, 16, 3, 4, platTop, platBot, tileWood);   // Balcón inicial
        PaintPlatform(tilePiso, 22, 34, 3, 4, platTop, platBot, tileWood);   // Balcón de Bóveda 1
        PaintPlatform(tilePiso, 25, 31, 7, 8, platTop, platBot, tileWood);   // Nicho superior de Pieza #1

        CreateLadder(elementsMap, root.transform, "Escalera_C1_Balcon", 22, 0, 4, tileLadder);
        if (structuresMap != null)
        {
            PaintCellarArch4x4(structuresMap, 3, 0);
            PaintCellarArch4x4(structuresMap, 26, 0);
        }
        if (barrelSprite != null) CreateBarrelStack(root.transform, "Barriles_C1_Entrada", 7.0f, 0f, barrelSprite, sueloLayer);
        if (crateSprite != null) CreateCrateStack(root.transform, "Cajas_C1_Salida", 36.5f, 0f, crateSprite, sueloLayer);

        // =====================================================================
        // BÓVEDA 2 (X = 39..76): FOSOS DE AÑEJAMIENTO ÁCIDO — Pieza #2 (Cuerpo Inferior)
        // =====================================================================
        PaintStoneBlock(tilePiso, 39, 41, -6, -1, stoneTop, stoneMid, stoneDeep, fallbackTop, fallbackDeep);
        // Foso Ácido 1 (X = 42..49)
        PaintStoneBlock(tilePiso, 42, 49, -6, -5, stoneTop, stoneMid, stoneDeep, fallbackTop, fallbackDeep);
        PaintLine(tilePiso, 44, 47, 2, tileWood);
        CreateAcidPit(elementsMap, root.transform, "FosoAcidoCava_1", 42, 49, tileAcidSurf, tileAcidDeep, sfxAcid);

        // Isla Central (X = 50..62) con doble nivel para Pieza #2
        PaintStoneBlock(tilePiso, 50, 62, -6, -1, stoneTop, stoneMid, stoneDeep, fallbackTop, fallbackDeep);
        PaintPlatform(tilePiso, 52, 60, 3, 4, platTop, platBot, tileWood);
        PaintPlatform(tilePiso, 53, 59, 7, 8, platTop, platBot, tileWood);
        CreateLadder(elementsMap, root.transform, "Escalera_C2_Isla", 52, 0, 4, tileLadder);

        // Foso Ácido 2 (X = 63..70)
        PaintStoneBlock(tilePiso, 63, 70, -6, -5, stoneTop, stoneMid, stoneDeep, fallbackTop, fallbackDeep);
        PaintLine(tilePiso, 64, 66, 2, tileWood);
        PaintLine(tilePiso, 68, 70, 4, tileWood);
        CreateAcidPit(elementsMap, root.transform, "FosoAcidoCava_2", 63, 70, tileAcidSurf, tileAcidDeep, sfxAcid);

        // Terraza de salida de Bóveda 2 (X = 71..76)
        PaintStoneBlock(tilePiso, 71, 76, -6, -1, stoneTop, stoneMid, stoneDeep, fallbackTop, fallbackDeep);
        if (structuresMap != null)
        {
            PaintCellarArch4x4(structuresMap, 54, 0);
            PaintCellarArch4x4(structuresMap, 72, 0);
        }

        // =====================================================================
        // BÓVEDA 3 (X = 77..116): GALERÍA DE BARRICAS DE DOS NIVELES — Pieza #3 (Etiqueta)
        // =====================================================================
        PaintStoneBlock(tilePiso, 77, 116, -6, -1, stoneTop, stoneMid, stoneDeep, fallbackTop, fallbackDeep);
        PaintPlatform(tilePiso, 81, 92, 3, 4, platTop, platBot, tileWood);   // Ala izquierda Piso 2
        PaintPlatform(tilePiso, 100, 111, 3, 4, platTop, platBot, tileWood); // Ala derecha Piso 2
        PaintPlatform(tilePiso, 91, 101, 7, 8, platTop, platBot, tileWood);  // Puente central de Pieza #3

        CreateLadder(elementsMap, root.transform, "Escalera_C3_Izq", 81, 0, 4, tileLadder);
        CreateLadder(elementsMap, root.transform, "Escalera_C3_Der", 111, 0, 4, tileLadder);

        if (structuresMap != null)
        {
            PaintCellarArch4x4(structuresMap, 84, 0);
            PaintCellarArch4x4(structuresMap, 94, 0);
            PaintCellarArch4x4(structuresMap, 104, 0);
        }
        if (barrelSprite != null) CreateBarrelStack(root.transform, "Barriles_C3_Centro", 96.0f, 0f, barrelSprite, sueloLayer);
        if (crateSprite != null)
        {
            CreateCrateStack(root.transform, "Cajas_C3_Izq", 78.5f, 0f, crateSprite, sueloLayer);
            CreateCrateStack(root.transform, "Cajas_C3_Der", 114.0f, 0f, crateSprite, sueloLayer);
        }

        // =====================================================================
        // BÓVEDA 4 (X = 117..156): EL ABISMO DE FERMENTACIÓN — Pieza #4 (Hombros de Vidrio)
        // 3 Fosos Ácidos encadenados con islas de Nebbiolo y francotiradores Merlot
        // =====================================================================
        PaintStoneBlock(tilePiso, 117, 119, -6, -1, stoneTop, stoneMid, stoneDeep, fallbackTop, fallbackDeep);

        // Foso Ácido 3 (X = 120..126)
        PaintStoneBlock(tilePiso, 120, 126, -6, -5, stoneTop, stoneMid, stoneDeep, fallbackTop, fallbackDeep);
        PaintLine(tilePiso, 122, 124, 2, tileWood);
        CreateAcidPit(elementsMap, root.transform, "FosoAcidoCava_3", 120, 126, tileAcidSurf, tileAcidDeep, sfxAcid);

        // Pilar Isla 1 (X = 127..131) + Balcón alto
        PaintStoneBlock(tilePiso, 127, 131, -6, -1, stoneTop, stoneMid, stoneDeep, fallbackTop, fallbackDeep);
        PaintLine(tilePiso, 128, 131, 5, tileWood);

        // Foso Ácido 4 (X = 132..138) con doble ruta (puente bajo Y=2 + bóveda alta de Pieza #4 en Y=8)
        PaintStoneBlock(tilePiso, 132, 138, -6, -5, stoneTop, stoneMid, stoneDeep, fallbackTop, fallbackDeep);
        PaintLine(tilePiso, 134, 136, 2, tileWood);
        PaintPlatform(tilePiso, 133, 138, 7, 8, platTop, platBot, tileWood);
        CreateAcidPit(elementsMap, root.transform, "FosoAcidoCava_4", 132, 138, tileAcidSurf, tileAcidDeep, sfxAcid);

        // Pilar Isla 2 (X = 139..143) + Balcón alto
        PaintStoneBlock(tilePiso, 139, 143, -6, -1, stoneTop, stoneMid, stoneDeep, fallbackTop, fallbackDeep);
        PaintLine(tilePiso, 139, 142, 5, tileWood);

        // Foso Ácido 5 (X = 144..150)
        PaintStoneBlock(tilePiso, 144, 150, -6, -5, stoneTop, stoneMid, stoneDeep, fallbackTop, fallbackDeep);
        PaintLine(tilePiso, 146, 148, 3, tileWood);
        CreateAcidPit(elementsMap, root.transform, "FosoAcidoCava_5", 144, 150, tileAcidSurf, tileAcidDeep, sfxAcid);

        // Aterrizaje salida Bóveda 4 (X = 151..156)
        PaintStoneBlock(tilePiso, 151, 156, -6, -1, stoneTop, stoneMid, stoneDeep, fallbackTop, fallbackDeep);
        if (structuresMap != null) PaintCellarArch4x4(structuresMap, 152, 0);

        // =====================================================================
        // BÓVEDA 5 (X = 157..198): CATACUMBAS DE RESERVA (NUEVA!) — Pieza #4 (Hombros de Vidrio)
        // Gran sala de 2 pisos dividida por un foso profundo de ácido con puente elevado y escaleras dobles
        // =====================================================================
        PaintStoneBlock(tilePiso, 157, 171, -6, -1, stoneTop, stoneMid, stoneDeep, fallbackTop, fallbackDeep);
        PaintPlatform(tilePiso, 160, 170, 3, 4, platTop, platBot, tileWood);
        CreateLadder(elementsMap, root.transform, "Escalera_C5_Izq", 160, 0, 4, tileLadder);

        // Foso Ácido 6 (X = 172..182) con doble puente (bajo Y=2 + galería alta de Pieza #4 en Y=8)
        PaintStoneBlock(tilePiso, 172, 182, -6, -5, stoneTop, stoneMid, stoneDeep, fallbackTop, fallbackDeep);
        PaintLine(tilePiso, 174, 180, 2, tileWood);
        PaintPlatform(tilePiso, 173, 181, 7, 8, platTop, platBot, tileWood);
        CreateAcidPit(elementsMap, root.transform, "FosoAcidoCava_6", 172, 182, tileAcidSurf, tileAcidDeep, sfxAcid);

        // Ala derecha de Catacumbas (X = 183..198) con balcón y 3er Corazón
        PaintStoneBlock(tilePiso, 183, 198, -6, -1, stoneTop, stoneMid, stoneDeep, fallbackTop, fallbackDeep);
        PaintPlatform(tilePiso, 184, 194, 3, 4, platTop, platBot, tileWood);
        CreateLadder(elementsMap, root.transform, "Escalera_C5_Der", 194, 0, 4, tileLadder);

        if (structuresMap != null)
        {
            PaintCellarArch4x4(structuresMap, 162, 0);
            PaintCellarArch4x4(structuresMap, 186, 0);
        }
        if (barrelSprite != null) CreateBarrelStack(root.transform, "Barriles_C5_Catacumbas", 166.0f, 0f, barrelSprite, sueloLayer);
        if (crateSprite != null) CreateCrateStack(root.transform, "Cajas_C5_Salida", 196.0f, 0f, crateSprite, sueloLayer);

        // =====================================================================
        // BÓVEDA 6 (X = 199..236): PASILLO DE LAS CUBAS TÓXICAS (NUEVA!)
        // Doble foso ancho con isla de emboscada + antesala del Checkpoint #3 (X = 234)
        // =====================================================================
        PaintStoneBlock(tilePiso, 199, 201, -6, -1, stoneTop, stoneMid, stoneDeep, fallbackTop, fallbackDeep);

        // Foso Ácido 7 (X = 202..210)
        PaintStoneBlock(tilePiso, 202, 210, -6, -5, stoneTop, stoneMid, stoneDeep, fallbackTop, fallbackDeep);
        PaintLine(tilePiso, 204, 208, 3, tileWood);
        CreateAcidPit(elementsMap, root.transform, "FosoAcidoCava_7", 202, 210, tileAcidSurf, tileAcidDeep, sfxAcid);

        // Isla Central de Cubas (X = 211..220) con plataforma superior
        PaintStoneBlock(tilePiso, 211, 220, -6, -1, stoneTop, stoneMid, stoneDeep, fallbackTop, fallbackDeep);
        PaintPlatform(tilePiso, 213, 218, 4, 5, platTop, platBot, tileWood);

        // Foso Ácido 8 (X = 221..229)
        PaintStoneBlock(tilePiso, 221, 229, -6, -5, stoneTop, stoneMid, stoneDeep, fallbackTop, fallbackDeep);
        PaintLine(tilePiso, 222, 224, 2, tileWood);
        PaintLine(tilePiso, 226, 228, 4, tileWood);
        CreateAcidPit(elementsMap, root.transform, "FosoAcidoCava_8", 221, 229, tileAcidSurf, tileAcidDeep, sfxAcid);

        // Antesala del Santuario (X = 230..236) donde descansa el Checkpoint #3 (X = 234)
        PaintStoneBlock(tilePiso, 230, 236, -6, -1, stoneTop, stoneMid, stoneDeep, fallbackTop, fallbackDeep);
        if (structuresMap != null)
        {
            PaintCellarArch4x4(structuresMap, 214, 0);
            PaintCellarArch4x4(structuresMap, 232, 0);
        }

        // =====================================================================
        // BÓVEDA 7 (X = 237..276): SANTUARIO DEL ALTAR FINAL — Pieza #5 + MetaNivel3
        // =====================================================================
        PaintStoneBlock(tilePiso, 237, 276, -6, -1, stoneTop, stoneMid, stoneDeep, fallbackTop, fallbackDeep);
        PaintPlatform(tilePiso, 240, 258, 3, 4, platTop, platBot, tileWood); // Galería superior del Santuario
        PaintPlatform(tilePiso, 246, 254, 7, 8, platTop, platBot, tileWood); // Balcón de Pieza #5
        PaintLine(tilePiso, 261, 263, 4, tileWood);                          // Puesto elevado de francotirador

        CreateLadder(elementsMap, root.transform, "Escalera_C7_Izq", 240, 0, 4, tileLadder);
        CreateLadder(elementsMap, root.transform, "Escalera_C7_Der", 258, 0, 4, tileLadder);

        if (structuresMap != null)
        {
            PaintCellarArch4x4(structuresMap, 243, 0);
            PaintCellarArch4x4(structuresMap, 251, 0);
            PaintCellarArch4x4(structuresMap, 271, 0);
        }
        if (barrelSprite != null) CreateBarrelStack(root.transform, "Barriles_C7_Santuario", 238.5f, 0f, barrelSprite, sueloLayer);
        if (crateSprite != null) CreateCrateStack(root.transform, "Cajas_C7_AntesAltar", 262.0f, 0f, crateSprite, sueloLayer);

        // Arcos de piedra monumentales a lo largo de las 7 Bóvedas y enmarcando el Altar Final
        if (archSprite != null)
        {
            float[] archXs = new float[] { 4.5f, 56.0f, 96.0f, 165.0f, 215.5f, 250.0f, 272.5f };
            for (int i = 0; i < archXs.Length; i++)
            {
                GameObject arch = new GameObject($"ArcoCava_{i + 1}");
                arch.transform.SetParent(root.transform);
                arch.transform.position = new Vector3(archXs[i], 1.45f, 0f);
                arch.transform.localScale = new Vector3(1.55f, 1.55f, 1f);
                SpriteRenderer sr = arch.AddComponent<SpriteRenderer>();
                sr.sprite = archSprite;
                sr.sortingOrder = -1;
            }
        }

        RefreshTilemapAndColliders(tilePiso);
        if (structuresMap != null) RefreshTilemapAndColliders(structuresMap);
        if (elementsMap != null) RefreshTilemapAndColliders(elementsMap);
        Physics2D.SyncTransforms();
    }

    private static GestorBotellaNivel3 BuildBottleHUDAndFinalAltar(
        Sprite completeBottleSprite,
        Sprite auraSprite,
        Sprite[] hudPieceSprites,
        AudioClip sfxPiece,
        AudioClip sfxComplete,
        AudioClip sfxLocked)
    {
        GameObject gestorObj = new GameObject("GestorBotellaNivel3");
        AudioSource audio = gestorObj.AddComponent<AudioSource>();
        audio.playOnAwake = false;
        audio.spatialBlend = 0f;

        GestorBotellaNivel3 gestor = gestorObj.AddComponent<GestorBotellaNivel3>();
        gestor.totalPiezas = 5;
        gestor.piezasRecogidas = new bool[5];
        gestor.imagenesPiezasHUD = new Image[5];
        gestor.audioSource = audio;
        gestor.sonidoPiezaRecogida = sfxPiece;
        gestor.sonidoBotellaCompleta = sfxComplete;
        gestor.sonidoFaltanPiezas = sfxLocked;

        // 1. Construir Widget HUD en esquina superior derecha del VineyardCanvas
        Canvas canvas = UnityEngine.Object.FindAnyObjectByType<Canvas>();
        if (canvas != null)
        {
            GameObject hudPanel = new GameObject("BotellaHUDPanel");
            hudPanel.transform.SetParent(canvas.transform, false);
            RectTransform panelRt = hudPanel.AddComponent<RectTransform>();
            panelRt.anchorMin = new Vector2(1f, 1f);
            panelRt.anchorMax = new Vector2(1f, 1f);
            panelRt.pivot = new Vector2(1f, 1f);
            panelRt.anchoredPosition = new Vector2(-18f, -18f);
            panelRt.sizeDelta = new Vector2(210f, 126f);

            Image panelBg = hudPanel.AddComponent<Image>();
            panelBg.color = new Color(0.08f, 0.06f, 0.14f, 0.84f);

            Outline outline = hudPanel.AddComponent<Outline>();
            outline.effectColor = new Color(0.78f, 0.62f, 0.24f, 0.92f);
            outline.effectDistance = new Vector2(2f, -2f);

            // Brillo dorado detrás de la botella cuando se completa 5/5
            GameObject glowObj = new GameObject("BrilloBotellaCompleta");
            glowObj.transform.SetParent(hudPanel.transform, false);
            RectTransform glowRt = glowObj.AddComponent<RectTransform>();
            glowRt.anchorMin = new Vector2(0f, 0.5f);
            glowRt.anchorMax = new Vector2(0f, 0.5f);
            glowRt.pivot = new Vector2(0.5f, 0.5f);
            glowRt.anchoredPosition = new Vector2(40f, 0f);
            glowRt.sizeDelta = new Vector2(78f, 114f);
            Image glowImg = glowObj.AddComponent<Image>();
            glowImg.sprite = auraSprite;
            glowImg.color = new Color(0.35f, 1.0f, 0.55f, 0.70f);
            glowImg.enabled = false;
            gestor.imagenBrilloCompletoHUD = glowImg;

            // Las 5 imágenes exactas superpuestas en el mismo RectTransform -> forman la botella completa al encenderse!
            for (int i = 0; i < 5; i++)
            {
                GameObject sliceObj = new GameObject($"BotellaHUD_Pieza_{i + 1}");
                sliceObj.transform.SetParent(hudPanel.transform, false);
                RectTransform sRt = sliceObj.AddComponent<RectTransform>();
                sRt.anchorMin = new Vector2(0f, 0.5f);
                sRt.anchorMax = new Vector2(0f, 0.5f);
                sRt.pivot = new Vector2(0.5f, 0.5f);
                sRt.anchoredPosition = new Vector2(40f, 0f);
                sRt.sizeDelta = new Vector2(54f, 108f);

                Image img = sliceObj.AddComponent<Image>();
                img.sprite = (hudPieceSprites != null && i < hudPieceSprites.Length) ? hudPieceSprites[i] : completeBottleSprite;
                img.preserveAspect = true;
                img.color = new Color(0.08f, 0.12f, 0.10f, 0.42f);
                gestor.imagenesPiezasHUD[i] = img;
            }

            // Texto contador "BOTELLA: 0/5"
            Font uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            GameObject countObj = new GameObject("ContadorBotellaText");
            countObj.transform.SetParent(hudPanel.transform, false);
            RectTransform cRt = countObj.AddComponent<RectTransform>();
            cRt.anchorMin = new Vector2(0f, 0f);
            cRt.anchorMax = new Vector2(1f, 1f);
            cRt.offsetMin = new Vector2(74f, 12f);
            cRt.offsetMax = new Vector2(-10f, -12f);

            Text countTxt = countObj.AddComponent<Text>();
            countTxt.font = uiFont;
            countTxt.fontSize = 18;
            countTxt.fontStyle = FontStyle.Bold;
            countTxt.alignment = TextAnchor.MiddleLeft;
            countTxt.color = new Color(0.85f, 0.98f, 0.88f, 1f);
            countTxt.text = "BOTELLA: 0/5";
            Shadow sh = countObj.AddComponent<Shadow>();
            sh.effectColor = new Color(0f, 0f, 0f, 0.85f);
            gestor.textoContadorBotella = countTxt;

            // Texto flotante superior-centro para avisos de piezas / Altar bloqueado
            GameObject toastObj = new GameObject("AvisoBotellaCavaText");
            toastObj.transform.SetParent(canvas.transform, false);
            RectTransform tRt = toastObj.AddComponent<RectTransform>();
            tRt.anchorMin = new Vector2(0.5f, 1f);
            tRt.anchorMax = new Vector2(0.5f, 1f);
            tRt.pivot = new Vector2(0.5f, 1f);
            tRt.anchoredPosition = new Vector2(0f, -26f);
            tRt.sizeDelta = new Vector2(740f, 52f);

            Text toastTxt = toastObj.AddComponent<Text>();
            toastTxt.font = uiFont;
            toastTxt.fontSize = 21;
            toastTxt.fontStyle = FontStyle.Bold;
            toastTxt.alignment = TextAnchor.MiddleCenter;
            toastTxt.color = new Color(1f, 0.96f, 0.78f, 1f);
            toastTxt.text = "";
            Shadow tSh = toastObj.AddComponent<Shadow>();
            tSh.effectColor = new Color(0f, 0f, 0f, 0.95f);
            tSh.effectDistance = new Vector2(2f, -2f);
            gestor.textoAvisoFlotante = toastTxt;
        }

        // 2. Configurar MetaNivel3 en el Altar Final (X = 272.5, Y = 1.0) con la botella holográfica/completa encima
        MetaNivel meta = UnityEngine.Object.FindAnyObjectByType<MetaNivel>();
        if (meta == null)
        {
            GameObject goalPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MetaPrefabPath);
            if (goalPrefab != null)
            {
                GameObject goalObj = (GameObject)PrefabUtility.InstantiatePrefab(goalPrefab);
                meta = goalObj.GetComponent<MetaNivel>();
            }
        }

        if (meta != null)
        {
            meta.gameObject.name = "MetaNivel3_AltarBotella";
            meta.tituloPersonalizado = "¡Nivel 3 Completado!";
            meta.puntosPorUva = 150;
            meta.multiplicadorBonoVida = 6f;
            meta.transform.position = new Vector3(272.5f, 1.0f, 0f);

            if (meta.textoTitulo != null)
            {
                meta.textoTitulo.text = "¡Nivel 3 Completado!";
                EditorUtility.SetDirty(meta.textoTitulo);
            }
            if (meta.botonSiguienteNivel != null)
            {
                UnityEventTools.RemovePersistentListener(meta.botonSiguienteNivel.onClick, meta.CargarSiguienteNivel);
                UnityEventTools.AddPersistentListener(meta.botonSiguienteNivel.onClick, meta.CargarSiguienteNivel);
                EditorUtility.SetDirty(meta.botonSiguienteNivel);
            }

            // Aura y Botella Armada flotando sobre el Altar
            GameObject auraObj = new GameObject("AuraAltarBotella");
            auraObj.transform.SetParent(meta.transform);
            auraObj.transform.localPosition = new Vector3(0f, 1.65f, 0f);
            auraObj.transform.localScale = new Vector3(1.1f, 1.1f, 1f);
            SpriteRenderer auraSr = auraObj.AddComponent<SpriteRenderer>();
            auraSr.sprite = auraSprite;
            auraSr.sortingOrder = 2;
            auraSr.color = new Color(0.55f, 0.25f, 0.75f, 0.28f);
            gestor.auraAltar = auraSr;

            GameObject bottleDisplay = new GameObject("BotellaArmadaAltar");
            bottleDisplay.transform.SetParent(meta.transform);
            bottleDisplay.transform.localPosition = new Vector3(0f, 1.65f, 0f);
            bottleDisplay.transform.localScale = new Vector3(1.15f, 1.15f, 1f);
            SpriteRenderer bSr = bottleDisplay.AddComponent<SpriteRenderer>();
            bSr.sprite = completeBottleSprite;
            bSr.sortingOrder = 4;
            bSr.color = new Color(0.15f, 0.25f, 0.20f, 0.35f);
            gestor.botellaArmadaEnAltar = bSr;

            EditorUtility.SetDirty(meta);
        }

        return gestor;
    }

    private static void SpawnCheckpointsInNivel3(
        Sprite offSprite,
        Sprite onSprite,
        Sprite auraSprite,
        AudioClip sfxCheckpoint)
    {
        GameObject cpRoot = new GameObject("Checkpoints_Nivel3");

        (Vector3 pos, string zoneName)[] checkpoints = new (Vector3, string)[]
        {
            (new Vector3(75.5f, 1.38f, 0f),  "Bóveda de Añejamiento"),
            (new Vector3(155.0f, 1.38f, 0f), "Salida del Abismo"),
            (new Vector3(234.0f, 1.38f, 0f), "Antesala del Santuario")
        };

        for (int i = 0; i < checkpoints.Length; i++)
        {
            GameObject cpObj = new GameObject($"Checkpoint_{i + 1}_{checkpoints[i].zoneName.Replace(" ", "")}");
            cpObj.transform.SetParent(cpRoot.transform);
            cpObj.transform.position = checkpoints[i].pos;

            SpriteRenderer sr = cpObj.AddComponent<SpriteRenderer>();
            sr.sprite = offSprite;
            sr.sortingOrder = 2;

            BoxCollider2D col = cpObj.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(1.8f, 3.2f);
            col.offset = new Vector2(0f, 0.2f);

            // Halo brillante alrededor del farol (se enciende al activar el checkpoint)
            GameObject haloObj = new GameObject("HaloLuzCheckpoint");
            haloObj.transform.SetParent(cpObj.transform);
            haloObj.transform.localPosition = new Vector3(0f, 0.65f, 0f);
            haloObj.transform.localScale = new Vector3(0.95f, 0.95f, 1f);
            SpriteRenderer haloSr = haloObj.AddComponent<SpriteRenderer>();
            haloSr.sprite = auraSprite;
            haloSr.sortingOrder = 1;
            haloSr.color = new Color(0.42f, 1.0f, 0.58f, 0.65f);
            haloSr.enabled = false;

            CheckpointCava cp = cpObj.AddComponent<CheckpointCava>();
            cp.indiceCheckpoint = i;
            cp.nombreZona = checkpoints[i].zoneName;
            cp.spriteApagado = offSprite;
            cp.spriteEncendido = onSprite;
            cp.haloLuz = haloSr;
            cp.curacionAlActivar = 25f;
            cp.sonidoActivar = sfxCheckpoint;
        }
    }

    private static void SpawnFiveBottlePiecesAndHearts(Sprite[] worldPieceSprites)
    {
        GameObject piecesRoot = new GameObject("PiezasBotella_Nivel3");

        (Vector3 pos, string name)[] pieceLocations = new (Vector3, string)[]
        {
            (new Vector3(28.5f, 10.2f, 0f),  "Base de la Botella"),        // Bóveda 1
            (new Vector3(56.5f, 10.2f, 0f),  "Cuerpo Inferior"),           // Bóveda 2
            (new Vector3(135.5f, 10.2f, 0f), "Etiqueta de la Cava"),       // Bóveda 4 (Abismo)
            (new Vector3(177.0f, 10.2f, 0f), "Hombros de Vidrio"),         // Bóveda 5 (Catacumbas)
            (new Vector3(250.5f, 10.2f, 0f), "Cuello y Sello Dorado")      // Bóveda 7 (Santuario Final)
        };

        for (int i = 0; i < pieceLocations.Length; i++)
        {
            GameObject pObj = new GameObject($"PiezaBotella_{i + 1}_{pieceLocations[i].name.Replace(" ", "")}");
            pObj.transform.SetParent(piecesRoot.transform);
            pObj.transform.position = pieceLocations[i].pos;

            SpriteRenderer sr = pObj.AddComponent<SpriteRenderer>();
            sr.sprite = (worldPieceSprites != null && i < worldPieceSprites.Length) ? worldPieceSprites[i] : null;
            sr.sortingOrder = 5;

            CircleCollider2D col = pObj.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.72f;

            PiezaBotellaColeccionable pieza = pObj.AddComponent<PiezaBotellaColeccionable>();
            pieza.indicePieza = i;
            pieza.nombrePieza = pieceLocations[i].name;
        }

        // 4 Corazones Curativos bien repartidos a lo largo de las 7 Bóvedas
        GameObject heartPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CorazonPrefabPath);
        if (heartPrefab != null)
        {
            GameObject heartsRoot = new GameObject("ItemsCurativos_Nivel3");
            Vector3[] heartSpots = new Vector3[]
            {
                new Vector3(73.0f, 1.55f, 0f),   // Salida Bóveda 2 (antes del Checkpoint 1)
                new Vector3(96.5f, 10.2f, 0f),   // Puente alto de Bóveda 3 (recompensa por subir)
                new Vector3(190.5f, 6.55f, 0f),  // Balcón derecho de Bóveda 5 (Catacumbas)
                new Vector3(242.5f, 6.55f, 0f)   // Galería izquierda del Santuario Final (Bóveda 7)
            };
            for (int i = 0; i < heartSpots.Length; i++)
            {
                GameObject h = (GameObject)PrefabUtility.InstantiatePrefab(heartPrefab);
                h.name = $"CorazonVida_{i + 1}_Cava";
                h.transform.SetParent(heartsRoot.transform);
                h.transform.position = heartSpots[i];
            }
        }
    }

    private static void SpawnNivel3HardEnemies(GameObject nebbioloPrefab)
    {
        GameObject container = new GameObject("Enemigos_Nivel3_Cava");
        GameObject cabSauPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CabSauPrefabPath);
        GameObject merlotPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MerlotPrefabPath);

        int sueloLayer = LayerMask.NameToLayer("Suelo");
        int sueloMask = sueloLayer >= 0 ? (1 << sueloLayer) : (1 << 6);

        // 1. 14 Uvas Nebbiolo Explosivas protegiendo pasillos clave y los 5 pedazos de botella
        if (nebbioloPrefab != null)
        {
            (Vector3 pos, float patrol, string label)[] nebSpots = new (Vector3, float, string)[]
            {
                (new Vector3(15.5f, 0.95f, 0f), 1.4f, "Nebbiolo_1_EntradaCava"),
                (new Vector3(26.5f, 5.95f, 0f), 1.2f, "Nebbiolo_2_GuardiaPieza1"),
                (new Vector3(55.0f, 5.95f, 0f), 1.3f, "Nebbiolo_3_GuardiaPieza2"),
                (new Vector3(71.5f, 0.95f, 0f), 0.9f, "Nebbiolo_4_SalidaFosos"),
                (new Vector3(86.0f, 5.95f, 0f), 1.5f, "Nebbiolo_5_GaleriaIzq"),
                (new Vector3(106.0f, 5.95f, 0f), 1.5f, "Nebbiolo_6_GaleriaDer"),
                (new Vector3(129.0f, 0.95f, 0f), 0.9f, "Nebbiolo_7_AbismoIsla1"),
                (new Vector3(141.0f, 0.95f, 0f), 0.9f, "Nebbiolo_8_AbismoIsla2"),
                (new Vector3(165.0f, 5.95f, 0f), 1.3f, "Nebbiolo_9_CatacumbasIzq"),
                (new Vector3(188.0f, 5.95f, 0f), 1.3f, "Nebbiolo_10_CatacumbasDer"),
                (new Vector3(215.5f, 0.95f, 0f), 1.4f, "Nebbiolo_11_IslaCubas"),
                (new Vector3(247.5f, 5.95f, 0f), 1.4f, "Nebbiolo_12_SantuarioIzq"),
                (new Vector3(254.5f, 5.95f, 0f), 1.4f, "Nebbiolo_13_SantuarioDer")
            };

            for (int i = 0; i < nebSpots.Length; i++)
            {
                GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(nebbioloPrefab);
                inst.name = nebSpots[i].label;
                inst.transform.SetParent(container.transform);
                inst.transform.position = nebSpots[i].pos;
                EnemigoNebbiolo n = inst.GetComponent<EnemigoNebbiolo>();
                if (n != null) { n.distanciaPatrulla = nebSpots[i].patrol; EditorUtility.SetDirty(n); }
            }
        }

        // 2. 9 Uvas Cab. Sau rápidas patrullando plantas bajas y pasarelas anchas
        if (cabSauPrefab != null)
        {
            (Vector3 pos, float patrol, string label)[] cabSpots = new (Vector3, float, string)[]
            {
                (new Vector3(28.0f, 0.95f, 0f),  5.0f, "CabSau_1_Boveda1"),
                (new Vector3(56.0f, 0.95f, 0f),  4.0f, "CabSau_2_IslaCentral"),
                (new Vector3(87.0f, 0.95f, 0f),  5.2f, "CabSau_3_GaleriaBajaIzq"),
                (new Vector3(105.0f, 0.95f, 0f), 5.2f, "CabSau_4_GaleriaBajaDer"),
                (new Vector3(163.5f, 0.95f, 0f), 4.2f, "CabSau_5_CatacumbasIzq"),
                (new Vector3(189.5f, 0.95f, 0f), 4.5f, "CabSau_6_CatacumbasDer"),
                (new Vector3(249.0f, 0.95f, 0f), 5.5f, "CabSau_7_SantuarioBajo1"),
                (new Vector3(259.5f, 0.95f, 0f), 4.2f, "CabSau_8_SantuarioBajo2")
            };

            for (int i = 0; i < cabSpots.Length; i++)
            {
                GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(cabSauPrefab);
                inst.name = cabSpots[i].label;
                inst.transform.SetParent(container.transform);
                inst.transform.position = cabSpots[i].pos;
                EnemigoCabSau c = inst.GetComponent<EnemigoCabSau>();
                if (c != null)
                {
                    c.distanciaPatrulla = cabSpots[i].patrol;
                    c.capaSuelo = sueloMask;
                    c.velocidadSprint = 5.9f; // Más agresivas en Nivel 3
                    EditorUtility.SetDirty(c);
                }
            }
        }

        // 3. 10 Uvas Merlot (3 golpes mínimos) cubriendo desde puntos elevados y custodiando piezas
        if (merlotPrefab != null)
        {
            (Vector3 pos, string label)[] merlotSpots = new (Vector3, string)[]
            {
                (new Vector3(32.5f, 5.85f, 0f),  "Merlot_1_BalconBoveda1"),
                (new Vector3(59.0f, 5.85f, 0f),  "Merlot_2_IslaAltaBoveda2"),
                (new Vector3(91.0f, 5.85f, 0f),  "Merlot_3_GaleriaCentroIzq"),
                (new Vector3(101.0f, 5.85f, 0f), "Merlot_4_GaleriaCentroDer"),
                (new Vector3(129.5f, 6.85f, 0f), "Merlot_5_AbismoTorre1"),
                (new Vector3(140.5f, 6.85f, 0f), "Merlot_6_AbismoTorre2"),
                (new Vector3(177.0f, 3.85f, 0f), "Merlot_7_PuenteCatacumbas"),
                (new Vector3(215.5f, 6.85f, 0f), "Merlot_8_TorreCubasToxicas"),
                (new Vector3(250.5f, 9.85f, 0f), "Merlot_9_BalconPiezaFinal"),
                (new Vector3(262.0f, 5.85f, 0f), "Merlot_10_VigiaAltarFinal")
            };

            for (int i = 0; i < merlotSpots.Length; i++)
            {
                GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(merlotPrefab);
                inst.name = merlotSpots[i].label;
                inst.transform.SetParent(container.transform);
                inst.transform.position = merlotSpots[i].pos;
            }
        }
    }

    private static void RegisterAllThreeScenesInBuildSettings()
    {
        List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>();
        if (File.Exists(Nivel1ScenePath)) scenes.Add(new EditorBuildSettingsScene(Nivel1ScenePath, true));
        if (File.Exists(Nivel2ScenePath)) scenes.Add(new EditorBuildSettingsScene(Nivel2ScenePath, true));
        if (File.Exists(Nivel3ScenePath)) scenes.Add(new EditorBuildSettingsScene(Nivel3ScenePath, true));

        foreach (var existing in EditorBuildSettings.scenes)
        {
            if (existing.path != Nivel1ScenePath &&
                existing.path != Nivel2ScenePath &&
                existing.path != Nivel3ScenePath &&
                File.Exists(existing.path))
            {
                scenes.Add(existing);
            }
        }

        EditorBuildSettings.scenes = scenes.ToArray();
    }

    // =========================================================================
    // UTILIDADES DE TILEMAP, PROPS, ÁCIDO Y AUDIO SINTETIZADO
    // =========================================================================
    private static TileBase LoadTile(string path) => AssetDatabase.LoadAssetAtPath<TileBase>(path);
    private static TileBase[] LoadTiles(params string[] paths)
    {
        TileBase[] a = new TileBase[paths.Length];
        for (int i = 0; i < paths.Length; i++) a[i] = LoadTile(paths[i]);
        return a;
    }
    private static TileBase Pick(TileBase[] a, int idx, TileBase fb)
    {
        if (a != null && a.Length > 0) { TileBase t = a[Mathf.Abs(idx) % a.Length]; if (t != null) return t; }
        return fb;
    }

    private static void PaintStoneBlock(
        Tilemap map, int x0, int x1, int y0, int y1,
        TileBase[] top, TileBase[] mid, TileBase[] deep, TileBase fbTop, TileBase fbDeep)
    {
        for (int x = x0; x <= x1; x++)
        {
            for (int y = y0; y <= y1; y++)
            {
                TileBase t = (y == y1) ? Pick(top, x, fbTop)
                           : (Mathf.Abs(y1 - y) % 2 == 1) ? Pick(mid, x, fbDeep)
                           : Pick(deep, x, fbDeep);
                if (t != null) map.SetTile(new Vector3Int(x, y, 0), t);
            }
        }
    }

    private static void PaintPlatform(Tilemap map, int x0, int x1, int yBot, int yTop, TileBase[] top, TileBase[] bot, TileBase fb)
    {
        for (int x = x0; x <= x1; x++)
        {
            map.SetTile(new Vector3Int(x, yTop, 0), Pick(top, x, fb));
            if (yBot < yTop) map.SetTile(new Vector3Int(x, yBot, 0), Pick(bot, x, fb));
        }
    }

    private static void PaintLine(Tilemap map, int x0, int x1, int y, TileBase tile)
    {
        for (int x = x0; x <= x1; x++) map.SetTile(new Vector3Int(x, y, 0), tile);
    }

    private static void PaintCellarArch4x4(Tilemap structuresMap, int xStart, int yStart)
    {
        int[][] ids = new int[][]
        {
            new int[] { 124, 125, 126, 127 },
            new int[] { 108, 109, 110, 111 },
            new int[] { 92,  93,  94,  95  },
            new int[] { 76,  77,  78,  79  }
        };
        for (int r = 0; r < ids.Length; r++)
            for (int c = 0; c < ids[r].Length; c++)
            {
                TileBase t = LoadTile($"Assets/Tiles/Nivel2/tileset-ok-lvl2_{ids[r][c]}.asset");
                if (t != null) structuresMap.SetTile(new Vector3Int(xStart + c, yStart + r, 0), t);
            }
    }

    private static void CreateAcidPit(Tilemap elementsMap, Transform parent, string name, int x0, int x1, TileBase surf, TileBase deep, AudioClip clip)
    {
        if (elementsMap != null)
        {
            for (int x = x0; x <= x1; x++)
            {
                if (deep != null)
                {
                    elementsMap.SetTile(new Vector3Int(x, -4, 0), deep);
                    elementsMap.SetTile(new Vector3Int(x, -3, 0), deep);
                }
                if (surf != null) elementsMap.SetTile(new Vector3Int(x, -2, 0), surf);
            }
        }

        int width = x1 - x0 + 1;
        GameObject pool = new GameObject(name);
        pool.transform.SetParent(parent);
        pool.transform.position = new Vector3(x0 + width * 0.5f, -2.5f, 0f);

        BoxCollider2D col = pool.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(width, 2.2f);

        ZonaAcido zona = pool.AddComponent<ZonaAcido>();
        zona.danoPorContacto = 20f;
        zona.intervaloDano = 0.8f;
        zona.impulsoReboteVertical = 10.8f;
        zona.sonidoSalpicaduraAcido = clip;

        GameObject vfx = new GameObject("BurbujasAcido_VFX");
        vfx.transform.SetParent(pool.transform);
        vfx.transform.localPosition = new Vector3(0f, 0.9f, 0f);
        ParticleSystem ps = vfx.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, 1.5f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.4f, 1.0f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.16f);
        main.gravityModifier = -0.15f;
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.45f, 0.98f, 0.18f, 0.85f), new Color(0.82f, 1f, 0.32f, 0.9f));
        var em = ps.emission; em.rateOverTime = 12f;
        var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(width * 0.9f, 0.25f, 0.1f);
        var psr = vfx.GetComponent<ParticleSystemRenderer>();
        if (psr != null) { psr.material = EnemyDeathFeedback.GetPixelParticleMaterial(); psr.sortingOrder = 5; }
    }

    private static void CreateLadder(Tilemap elementsMap, Transform parent, string name, int xCell, int yBot, int yTop, TileBase tileLadder)
    {
        if (elementsMap != null && tileLadder != null)
            for (int y = yBot; y <= yTop; y++)
                elementsMap.SetTile(new Vector3Int(xCell, y, 0), tileLadder);

        float h = (yTop - yBot + 1);
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent);
        obj.transform.position = new Vector3(xCell + 0.5f, yBot + h * 0.5f, 0f);
        BoxCollider2D col = obj.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(0.95f, h + 0.5f);
        EscaleraMano esc = obj.AddComponent<EscaleraMano>();
        esc.velocidadSubida = 5.8f;
    }

    private static void CreateBarrelStack(Transform parent, string name, float cx, float gy, Sprite spr, int layer)
    {
        GameObject g = new GameObject(name);
        g.transform.SetParent(parent);
        Vector3[] p = new Vector3[] { new Vector3(cx - 0.48f, gy + 0.52f, 0), new Vector3(cx + 0.48f, gy + 0.52f, 0), new Vector3(cx, gy + 1.48f, 0) };
        for (int i = 0; i < p.Length; i++) CreateSolidProp(g.transform, $"Barril_{i + 1}", p[i], spr, new Vector2(0.88f, 0.98f), layer);
    }

    private static void CreateCrateStack(Transform parent, string name, float cx, float gy, Sprite spr, int layer)
    {
        GameObject g = new GameObject(name);
        g.transform.SetParent(parent);
        Vector3[] p = new Vector3[] { new Vector3(cx - 0.48f, gy + 0.48f, 0), new Vector3(cx + 0.48f, gy + 0.48f, 0), new Vector3(cx + 0.48f, gy + 1.42f, 0) };
        for (int i = 0; i < p.Length; i++) CreateSolidProp(g.transform, $"Caja_{i + 1}", p[i], spr, new Vector2(0.92f, 0.92f), layer);
    }

    private static void CreateSolidProp(Transform parent, string name, Vector3 pos, Sprite spr, Vector2 size, int layer)
    {
        GameObject o = new GameObject(name);
        o.transform.SetParent(parent);
        o.transform.position = pos;
        o.layer = layer;
        SpriteRenderer sr = o.AddComponent<SpriteRenderer>();
        sr.sprite = spr;
        sr.sortingOrder = 2;
        BoxCollider2D c = o.AddComponent<BoxCollider2D>();
        c.size = size;
    }

    private static void RefreshTilemapAndColliders(Tilemap map)
    {
        map.CompressBounds();
        map.RefreshAllTiles();
        TilemapCollider2D tc = map.GetComponent<TilemapCollider2D>();
        if (tc != null) { tc.ProcessTilemapChanges(); EditorUtility.SetDirty(tc); }
        CompositeCollider2D cc = map.GetComponent<CompositeCollider2D>();
        if (cc != null) { cc.offsetDistance = 0.005f; cc.GenerateGeometry(); EditorUtility.SetDirty(cc); }
        EditorUtility.SetDirty(map);
    }

    private static void GenerateNivel3Audio(
        out AudioClip swell, out AudioClip boom, out AudioClip piece,
        out AudioClip complete, out AudioClip locked, out AudioClip checkpoint, out AudioClip acid)
    {
        if (!File.Exists(SfxNebbioloSwellPath))
        {
            WriteWav(SfxNebbioloSwellPath, 0.55f, (t, d) =>
            {
                float f = Mathf.Lerp(140f, 520f, t / d);
                float bub = Mathf.Sin(2f * Mathf.PI * 24f * t) * 45f;
                return Mathf.Sin(2f * Mathf.PI * (f + bub) * t) * Mathf.Clamp01(t / 0.1f) * (1f - 0.2f * (t / d)) * 0.55f;
            });
            AssetDatabase.ImportAsset(SfxNebbioloSwellPath, ImportAssetOptions.ForceSynchronousImport);
        }

        if (!File.Exists(SfxNebbioloBoomPath))
        {
            WriteWav(SfxNebbioloBoomPath, 0.42f, (t, d) =>
            {
                float env = Mathf.Pow(1f - (t / d), 1.6f);
                float punch = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(210f, 48f, t / d) * t);
                float fizz = Mathf.Sin(t * 2600f + Mathf.Sin(t * 320f) * 8f) * 0.45f;
                return (punch * 0.7f + fizz * 0.35f) * env * 0.8f;
            });
            AssetDatabase.ImportAsset(SfxNebbioloBoomPath, ImportAssetOptions.ForceSynchronousImport);
        }

        if (!File.Exists(SfxBottlePiecePath))
        {
            WriteWav(SfxBottlePiecePath, 0.36f, (t, d) =>
            {
                float env = 1f - (t / d);
                float f = t < 0.12f ? 784f : 1174f;
                return (Mathf.Sin(2f * Mathf.PI * f * t) * 0.65f + Mathf.Sin(2f * Mathf.PI * f * 2f * t) * 0.25f) * env * 0.65f;
            });
            AssetDatabase.ImportAsset(SfxBottlePiecePath, ImportAssetOptions.ForceSynchronousImport);
        }

        if (!File.Exists(SfxBottleCompletePath))
        {
            WriteWav(SfxBottleCompletePath, 0.75f, (t, d) =>
            {
                float[] notes = new float[] { 523.25f, 659.25f, 783.99f, 1046.5f };
                int idx = Mathf.Min(notes.Length - 1, Mathf.FloorToInt((t / d) * notes.Length));
                float env = 1f - (t / d) * 0.7f;
                return (Mathf.Sin(2f * Mathf.PI * notes[idx] * t) * 0.65f + Mathf.Sin(2f * Mathf.PI * notes[idx] * 1.5f * t) * 0.25f) * env * 0.7f;
            });
            AssetDatabase.ImportAsset(SfxBottleCompletePath, ImportAssetOptions.ForceSynchronousImport);
        }

        if (!File.Exists(SfxBottleLockedPath))
        {
            WriteWav(SfxBottleLockedPath, 0.28f, (t, d) =>
            {
                float env = 1f - (t / d);
                float f = t < 0.14f ? 190f : 135f;
                return Mathf.Sin(2f * Mathf.PI * f * t) * env * 0.65f;
            });
            AssetDatabase.ImportAsset(SfxBottleLockedPath, ImportAssetOptions.ForceSynchronousImport);
        }

        if (!File.Exists(SfxCheckpointPath))
        {
            WriteWav(SfxCheckpointPath, 0.52f, (t, d) =>
            {
                float[] notes = new float[] { 440f, 554.37f, 659.25f, 880f };
                int idx = Mathf.Min(notes.Length - 1, Mathf.FloorToInt((t / d) * notes.Length));
                float env = 1f - (t / d) * 0.75f;
                return Mathf.Sin(2f * Mathf.PI * notes[idx] * t) * env * 0.65f;
            });
            AssetDatabase.ImportAsset(SfxCheckpointPath, ImportAssetOptions.ForceSynchronousImport);
        }

        swell = AssetDatabase.LoadAssetAtPath<AudioClip>(SfxNebbioloSwellPath);
        boom = AssetDatabase.LoadAssetAtPath<AudioClip>(SfxNebbioloBoomPath);
        piece = AssetDatabase.LoadAssetAtPath<AudioClip>(SfxBottlePiecePath);
        complete = AssetDatabase.LoadAssetAtPath<AudioClip>(SfxBottleCompletePath);
        locked = AssetDatabase.LoadAssetAtPath<AudioClip>(SfxBottleLockedPath);
        checkpoint = AssetDatabase.LoadAssetAtPath<AudioClip>(SfxCheckpointPath);
        acid = AssetDatabase.LoadAssetAtPath<AudioClip>(SfxAcidSplashPath);
    }

    private static void WriteWav(string path, float durationSec, Func<float, float, float> fn)
    {
        int sr = 44100;
        int n = Mathf.Max(1, Mathf.RoundToInt(sr * durationSec));
        byte[] pcm = new byte[n * 2];
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / sr;
            short s = (short)Mathf.RoundToInt(Mathf.Clamp(fn(t, durationSec), -1f, 1f) * 32767f);
            pcm[i * 2] = (byte)(s & 0xff);
            pcm[i * 2 + 1] = (byte)((s >> 8) & 0xff);
        }
        using (FileStream fs = new FileStream(path, FileMode.Create, FileAccess.Write))
        using (BinaryWriter bw = new BinaryWriter(fs))
        {
            bw.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
            bw.Write(36 + pcm.Length);
            bw.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));
            bw.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
            bw.Write(16); bw.Write((short)1); bw.Write((short)1);
            bw.Write(sr); bw.Write(sr * 2); bw.Write((short)2); bw.Write((short)16);
            bw.Write(System.Text.Encoding.ASCII.GetBytes("data"));
            bw.Write(pcm.Length); bw.Write(pcm);
        }
    }
}
#endif
