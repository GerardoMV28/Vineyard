#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

[InitializeOnLoad]
public static class VineyardHUDAndGameOverSetup
{
    private const string UIFolder = "Assets/UI";
    private const string HeartSpritePath = "Assets/UI/PixelHeart.png";
    private const string BarBgSpritePath = "Assets/UI/PixelBarBg.png";
    private const string BarFillSpritePath = "Assets/UI/PixelBarFill.png";
    private const string GameOverBgSpritePath = "Assets/UI/GameOverScreen.png";
    private const string GameOverBtnSpritePath = "Assets/UI/JugarDeNuevoBtn.png";
    private const string LevelCompleteBgSpritePath = "Assets/UI/LevelCompleteFrame.png";
    private const string LevelCompleteBtnSpritePath = "Assets/UI/LevelCompleteBtnBg.png";
    private const string SplashPrefabPath = "Assets/Prefabs/GrapeJuiceSplash.prefab";
    private const string MerlotPrefabPath = "Assets/Prefabs/MerlotGrape.prefab";
    private const string EnemigoUvaPrefabPath = "Assets/Prefabs/EnemigoUva.prefab";
    private const string SeedPrefabPath = "Assets/Prefabs/GrapeSeed.prefab";
    private const string CorazonSpritePath = "Assets/Sprites/Items/CorazonMapa.png";
    private const string CorazonPrefabPath = "Assets/Prefabs/CorazonVida.prefab";
    private const string MetaSpritePath = "Assets/Sprites/Items/MetaBandera.png";
    private const string MetaPrefabPath = "Assets/Prefabs/MetaNivel.prefab";

    private const string SfxCorkPath = "Assets/Audio/SFX_LanzarCorcho.wav";
    private const string SfxSeedPath = "Assets/Audio/SFX_DisparoSemilla.wav";
    private const string SfxGrapeHitPath = "Assets/Audio/SFX_ImpactoUva.wav";
    private const string SfxHealPath = "Assets/Audio/SFX_CurarCorazon.wav";
    private const string SfxGoalPath = "Assets/Audio/SFX_MetaCompletada.wav";

    // Recursos HD para la Pantalla de Carga / Opener ("Grapefall - START GAME")
    private const string OpenerBgSpritePath = "Assets/UI/Panatalla-Opener-Vineyard.jpg";
    private const string OpenerStartBtnSpritePath = "Assets/UI/StartGameBtnSlice.png";
    private const string OpenerLanternGlowSpritePath = "Assets/UI/LanternWarmGlow.png";
    private const string BgmChillOpenerPath = "Assets/Audio/BGM_VineyardChillOpener.wav";
    private const string SfxStartGamePath = "Assets/Audio/SFX_StartGameCinematic.wav";

    private const string ExtractedOpenerJpgPath =
        @"C:\Users\marim\.gemini\antigravity\brain\d5bf5fd1-8896-404c-8613-58c9e72f9423\scratch\opener\Panatalla-Opener-Vineyard.jpg";

    private const string SetupVersionPrefKey = "Vineyard_GameOverPopup_Version";
    private const int CurrentSetupVersion = 5;

    // Ruta de la imagen emergente de Game Over sin fondo subida por el usuario
    private const string UploadedGameOverImagePath =
        @"C:\Users\marim\.gemini\antigravity\brain\d5bf5fd1-8896-404c-8613-58c9e72f9423\.user_uploaded\media_1794005582059_98d3ef04.png";

    static VineyardHUDAndGameOverSetup()
    {
        EditorApplication.delayCall += RunAutoSetupIfNeeded;
    }

    private static void RunAutoSetupIfNeeded()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        string activeSceneName = EditorSceneManager.GetActiveScene().name;
        if (!string.IsNullOrEmpty(activeSceneName) &&
            (activeSceneName.Contains("Nivel2") || activeSceneName.Contains("Nivel3") || activeSceneName.Contains("Nivel4")))
            return;

        bool needsVersionUpdate = EditorPrefs.GetInt(SetupVersionPrefKey, 0) < CurrentSetupVersion;
        bool needsGameOverArt = AssetDatabase.LoadAssetAtPath<Sprite>(GameOverBgSpritePath) == null ||
                                AssetDatabase.LoadAssetAtPath<Sprite>(GameOverBtnSpritePath) == null ||
                                AssetDatabase.LoadAssetAtPath<Sprite>(LevelCompleteBgSpritePath) == null ||
                                AssetDatabase.LoadAssetAtPath<Sprite>(OpenerBgSpritePath) == null;
        bool needsCanvasSlider = GameObject.Find("PlayerHealthSlider") == null;
        bool needsGameOver = Object.FindFirstObjectByType<GameOverManager>() == null;
        bool needsOpener = Object.FindFirstObjectByType<TitleScreenOpener>() == null;
        bool needsMeta = Object.FindFirstObjectByType<MetaNivel>() == null;
        bool needsCorazones = Object.FindFirstObjectByType<CorazonVida>() == null;
        bool needsSplashPrefab = AssetDatabase.LoadAssetAtPath<ParticleSystem>(SplashPrefabPath) == null;

        if (needsVersionUpdate || needsGameOverArt || needsCanvasSlider || needsGameOver || needsOpener || needsMeta || needsCorazones || needsSplashPrefab)
        {
            SetupCompleteHUDAndFeedback(silent: true);
        }
    }

    [MenuItem("Herramientas Vineyard/Completar Nivel 1 y Pantalla Opener HD (Grapefall)")]
    public static void SetupFromMenu()
    {
        SetupCompleteHUDAndFeedback(silent: false);
        EditorUtility.DisplayDialog(
            "Vineyard - Pantalla Opener HD y Nivel 1 Listos",
            "¡Se configuró la Pantalla Opener HD ('Grapefall') y el Nivel 1!\n\n" +
            "• Ilustración Opener en máxima calidad HD (2752x1536 sin compresión, MaxSize 4096)\n" +
            "• Melodía Chill de viñedo en bucle mientras estás en la pantalla inicial\n" +
            "• Botón 'START GAME' con sonido propio, cinemática de acercamiento al farol y transición tenue hacia el Nivel 1\n" +
            "• 8 uvas enemigas, 2 corazones curativos y meta con pantalla de victoria.",
            "¡A jugar!"
        );
    }

    public static void SetupCompleteHUDAndFeedback(bool silent)
    {
        EnsureDirectories();
        EnsureDefaultAudioClipsExist(
            out AudioClip sfxCork,
            out AudioClip sfxSeed,
            out AudioClip sfxGrapeHit,
            out AudioClip sfxHeal,
            out AudioClip sfxGoal
        );

        CreateOrLoadOpenerAudioClips(
            out AudioClip bgmChillOpener,
            out AudioClip sfxStartGame
        );

        Sprite heartSprite = CreateOrLoadPixelHeartSprite();
        Sprite barBgSprite = CreateOrLoadPixelBarBgSprite();
        Sprite barFillSprite = CreateOrLoadPixelBarFillSprite();
        ImportCustomGameOverSprites(
            out Sprite gameOverBgSprite,
            out Sprite gameOverBtnSprite,
            out Sprite levelCompleteBgSprite,
            out Sprite levelCompleteBtnSprite
        );
        ImportOrCreateOpenerHDSprites(
            out Sprite openerBgSprite,
            out Sprite openerStartBtnSprite,
            out Sprite lanternGlowSprite
        );

        ParticleSystem splashPrefab = CreateOrUpdateJuiceSplashPrefab();

        // Actualizar prefabs y distribución de EnemigoUva en el Nivel 1
        MerlotEnemyAutoSetup.SetupMerlotEnemiesAndScene(silent: true);
        UpdateMerlotAndSeedPrefabs(splashPrefab, sfxSeed, sfxGrapeHit);

        CreateOrUpdateCanvasAndManagers(
            heartSprite,
            barBgSprite,
            barFillSprite,
            gameOverBgSprite,
            gameOverBtnSprite,
            levelCompleteBgSprite,
            levelCompleteBtnSprite,
            out Slider healthSlider,
            out Image fillImage,
            out Text hpLabelText,
            out GameOverManager gameOverManager,
            out GameObject levelCompletePanel,
            out CanvasGroup levelCompleteCg,
            out Text lcTitleText,
            out Text lcBreakdownText,
            out Text lcTotalScoreText,
            out Button nextLevelBtn
        );

        // Crear e integrar la Pantalla de Carga / Opener HD con su música chill, cinemática y transición tenue
        CreateOrUpdateTitleScreenOpener(
            openerBgSprite,
            openerStartBtnSprite,
            lanternGlowSprite,
            bgmChillOpener,
            sfxStartGame
        );

        // Colocar Corazones Curativos y Meta del Nivel 1
        CreateAndSpawnHealingHearts(sfxHeal);
        CreateAndSpawnLevelGoal(
            sfxGoal,
            levelCompletePanel,
            levelCompleteCg,
            lcTitleText,
            lcBreakdownText,
            lcTotalScoreText,
            nextLevelBtn
        );

        // Configurar el Jugador con HealthSystem (100 HP) y Audio de disparo de corcho
        MovimientoJugador playerMove = Object.FindFirstObjectByType<MovimientoJugador>();
        if (playerMove != null)
        {
            GameObject playerObj = playerMove.gameObject;

            HealthSystem hs = playerObj.GetComponent<HealthSystem>();
            if (hs == null)
                hs = playerObj.AddComponent<HealthSystem>();

            hs.maxHealth = 100f;
            hs.currentHealth = 100f;
            hs.healthSlider = healthSlider;
            hs.fillImage = fillImage;
            hs.hpLabelText = hpLabelText;
            hs.gameOverManager = gameOverManager;

            AudioSource playerAudio = playerObj.GetComponent<AudioSource>();
            if (playerAudio == null)
                playerAudio = playerObj.AddComponent<AudioSource>();
            playerAudio.playOnAwake = false;
            playerAudio.spatialBlend = 0f;

            PlayerAttack pa = playerObj.GetComponent<PlayerAttack>();
            if (pa != null)
            {
                pa.audioSource = playerAudio;
                if (pa.sonidoLanzarCorcho == null)
                    pa.sonidoLanzarCorcho = sfxCork;
                EditorUtility.SetDirty(pa);
            }

            EditorUtility.SetDirty(hs);
            EditorUtility.SetDirty(playerObj);
        }

        // Asegurar que todas las uvas en escena tengan EnemyDeathFeedback y AudioClips asignados
        MerlotGrapeEnemy[] sceneGrapes = Object.FindObjectsByType<MerlotGrapeEnemy>(FindObjectsSortMode.None);
        foreach (MerlotGrapeEnemy grape in sceneGrapes)
        {
            EnemyDeathFeedback feedback = grape.GetComponent<EnemyDeathFeedback>();
            if (feedback == null)
                feedback = grape.gameObject.AddComponent<EnemyDeathFeedback>();

            feedback.juiceSplashPrefab = splashPrefab;

            AudioSource grapeAudio = grape.GetComponent<AudioSource>();
            if (grapeAudio == null)
                grapeAudio = grape.gameObject.AddComponent<AudioSource>();
            grapeAudio.playOnAwake = false;
            grapeAudio.spatialBlend = 0f;

            grape.audioSource = grapeAudio;
            if (grape.sonidoDisparoSemilla == null)
                grape.sonidoDisparoSemilla = sfxSeed;
            if (grape.sonidoImpactoOMuerte == null)
                grape.sonidoImpactoOMuerte = sfxGrapeHit;

            EditorUtility.SetDirty(grape);
            EditorUtility.SetDirty(feedback);
        }

        if (!Application.isPlaying)
        {
            EditorSceneManager.MarkAllScenesDirty();
        }

        EditorPrefs.SetInt(SetupVersionPrefKey, CurrentSetupVersion);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static void EnsureDirectories()
    {
        if (!AssetDatabase.IsValidFolder(UIFolder))
            AssetDatabase.CreateFolder("Assets", "UI");
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            AssetDatabase.CreateFolder("Assets", "Prefabs");
        if (!AssetDatabase.IsValidFolder("Assets/Audio"))
            AssetDatabase.CreateFolder("Assets", "Audio");
        if (!AssetDatabase.IsValidFolder("Assets/Sprites"))
            AssetDatabase.CreateFolder("Assets", "Sprites");
        if (!AssetDatabase.IsValidFolder("Assets/Sprites/Items"))
            AssetDatabase.CreateFolder("Assets/Sprites", "Items");
    }

    /// <summary>
    /// Importa la ventana emergente de Game Over sin fondo subida por el usuario y genera también
    /// el marco a juego para la ventana emergente de "¡Nivel 1 Completado!".
    /// </summary>
    private static void ImportCustomGameOverSprites(
        out Sprite bgSprite,
        out Sprite btnSprite,
        out Sprite levelCompleteBgSprite,
        out Sprite levelCompleteBtnSprite)
    {
        if (File.Exists(UploadedGameOverImagePath))
        {
            byte[] rawBytes = File.ReadAllBytes(UploadedGameOverImagePath);
            Texture2D fullTex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            fullTex.LoadImage(rawBytes);

            int w = fullTex.width;   // 669
            int h = fullTex.height;  // 373

            // Limpiar pequeños restos blancos entre los huecos de las enredaderas exteriores
            int frameMinX = Mathf.RoundToInt(w * 0.26f);
            int frameMaxX = Mathf.RoundToInt(w * 0.74f);
            int frameMinY = Mathf.RoundToInt(h * 0.14f);
            int frameMaxY = Mathf.RoundToInt(h * 0.85f);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    bool outsideInnerPanel = x < frameMinX || x > frameMaxX || y < frameMinY || y > frameMaxY;
                    if (!outsideInnerPanel)
                        continue;

                    Color c = fullTex.GetPixel(x, y);
                    if (c.a > 0f)
                    {
                        float maxChannel = Mathf.Max(c.r, Mathf.Max(c.g, c.b));
                        float minChannel = Mathf.Min(c.r, Mathf.Min(c.g, c.b));
                        if (minChannel > 0.58f && (maxChannel - minChannel) < 0.14f)
                        {
                            fullTex.SetPixel(x, y, new Color(0f, 0f, 0f, 0f));
                        }
                    }
                }
            }

            // Coordenadas del botón "JUGAR DE NUEVO" en Texture2D (origen abajo-izquierda, 669x373):
            // X: 213..453 (ancho 240), Y: 84..129 (alto 45)
            int btnX = Mathf.RoundToInt(w * 0.3184f);
            int btnW = Mathf.RoundToInt(w * 0.3587f);
            int btnY = Mathf.RoundToInt(h * 0.2252f);
            int btnH = Mathf.RoundToInt(h * 0.1206f);

            // 1. Extraer el sprite del botón "JUGAR DE NUEVO"
            Texture2D btnTex = new Texture2D(btnW, btnH, TextureFormat.RGBA32, false);
            for (int y = 0; y < btnH; y++)
            {
                for (int x = 0; x < btnW; x++)
                {
                    Color c = fullTex.GetPixel(btnX + x, btnY + y);
                    btnTex.SetPixel(x, y, c);
                }
            }
            btnTex.Apply();
            File.WriteAllBytes(GameOverBtnSpritePath, btnTex.EncodeToPNG());

            // 1b. Crear versión limpia del botón (con marco dorado y fondo vino tinto sin letras) para "SIGUIENTE NIVEL"
            Color btnInnerBurgundy = btnTex.GetPixel(16, btnH / 2);
            for (int y = 8; y < btnH - 8; y++)
            {
                for (int x = 14; x < btnW - 14; x++)
                {
                    btnTex.SetPixel(x, y, btnInnerBurgundy);
                }
            }
            btnTex.Apply();
            File.WriteAllBytes(LevelCompleteBtnSpritePath, btnTex.EncodeToPNG());
            Object.DestroyImmediate(btnTex);

            // 2. En la imagen del cartel emergente de Game Over, rellenar el hueco detrás del botón
            Color innerPanelColor = fullTex.GetPixel(w / 2, Mathf.RoundToInt(h * 0.38f));
            for (int y = btnY; y < btnY + btnH; y++)
            {
                for (int x = btnX; x < btnX + btnW; x++)
                {
                    fullTex.SetPixel(x, y, innerPanelColor);
                }
            }
            fullTex.Apply();
            File.WriteAllBytes(GameOverBgSpritePath, fullTex.EncodeToPNG());

            // 3. Crear el marco emergente limpio (con las mismas enredaderas y marco de madera) para "¡Nivel 1 Completado!"
            int cleanMinX = Mathf.RoundToInt(w * 0.278f);
            int cleanMaxX = Mathf.RoundToInt(w * 0.722f);
            int cleanMinY = Mathf.RoundToInt(h * 0.175f);
            int cleanMaxY = Mathf.RoundToInt(h * 0.815f);
            for (int y = cleanMinY; y <= cleanMaxY; y++)
            {
                for (int x = cleanMinX; x <= cleanMaxX; x++)
                {
                    fullTex.SetPixel(x, y, innerPanelColor);
                }
            }
            fullTex.Apply();
            File.WriteAllBytes(LevelCompleteBgSpritePath, fullTex.EncodeToPNG());
            Object.DestroyImmediate(fullTex);

            AssetDatabase.ImportAsset(GameOverBgSpritePath, ImportAssetOptions.ForceUpdate);
            AssetDatabase.ImportAsset(GameOverBtnSpritePath, ImportAssetOptions.ForceUpdate);
            AssetDatabase.ImportAsset(LevelCompleteBgSpritePath, ImportAssetOptions.ForceUpdate);
            AssetDatabase.ImportAsset(LevelCompleteBtnSpritePath, ImportAssetOptions.ForceUpdate);

            ConfigureHighResUISprite(GameOverBgSpritePath);
            ConfigureHighResUISprite(GameOverBtnSpritePath);
            ConfigureHighResUISprite(LevelCompleteBgSpritePath);
            ConfigureHighResUISprite(LevelCompleteBtnSpritePath);
        }

        bgSprite = AssetDatabase.LoadAssetAtPath<Sprite>(GameOverBgSpritePath);
        btnSprite = AssetDatabase.LoadAssetAtPath<Sprite>(GameOverBtnSpritePath);
        levelCompleteBgSprite = AssetDatabase.LoadAssetAtPath<Sprite>(LevelCompleteBgSpritePath);
        levelCompleteBtnSprite = AssetDatabase.LoadAssetAtPath<Sprite>(LevelCompleteBtnSpritePath);
    }

    private static void ConfigureHighResUISprite(string path)
    {
        TextureImporter imp = AssetImporter.GetAtPath(path) as TextureImporter;
        if (imp != null)
        {
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.filterMode = FilterMode.Bilinear;
            imp.maxTextureSize = 2048;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.alphaIsTransparency = true;
            imp.SaveAndReimport();
        }
    }

    private static Sprite CreateOrLoadPixelHeartSprite()
    {
        if (!File.Exists(HeartSpritePath))
        {
            string[] pattern = new string[]
            {
                "....WW....WW....",
                "...WWWW..WWWW...",
                "..WWWRRWWWRRWW..",
                ".WWRRRRRRRRRRWW.",
                ".WWRRWHRRRRRRWW.",
                ".WWRWHRRRRRRRRW.",
                ".WWRRRRRRRRRRRW.",
                "..WWRRRRRRRRRWW.",
                "..WWRRRRRRRRWW..",
                "...WWRRRRRRWW...",
                "....WWRRRRWW....",
                ".....WWRRWW.....",
                "......WWWW......",
                ".......WW.......",
                "................",
                "................"
            };

            int w = 16, h = 16;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color clear = new Color(0, 0, 0, 0);
            Color whiteBorder = new Color(0.97f, 0.96f, 0.96f, 1f);
            Color redFill = new Color(0.91f, 0.18f, 0.18f, 1f);
            Color highlight = new Color(1f, 0.78f, 0.78f, 1f);

            for (int y = 0; y < h; y++)
            {
                string row = pattern[h - 1 - y];
                for (int x = 0; x < w; x++)
                {
                    char c = row[x];
                    if (c == 'W') tex.SetPixel(x, y, whiteBorder);
                    else if (c == 'R') tex.SetPixel(x, y, redFill);
                    else if (c == 'H') tex.SetPixel(x, y, highlight);
                    else tex.SetPixel(x, y, clear);
                }
            }

            tex.Apply();
            File.WriteAllBytes(HeartSpritePath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(HeartSpritePath, ImportAssetOptions.ForceUpdate);
        }

        ConfigurePixelArtUISprite(HeartSpritePath);
        return AssetDatabase.LoadAssetAtPath<Sprite>(HeartSpritePath);
    }

    private static Sprite CreateOrLoadPixelBarBgSprite()
    {
        if (!File.Exists(BarBgSpritePath))
        {
            int w = 64, h = 16;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color clear = new Color(0, 0, 0, 0);
            Color outerBorder = new Color(0.95f, 0.94f, 0.96f, 1f);
            Color innerShadow = new Color(0.55f, 0.52f, 0.58f, 1f);
            Color darkTrack = new Color(0.18f, 0.11f, 0.16f, 0.95f);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    bool cornerCut =
                        ((x < 2 || x >= w - 2) && (y < 3 || y >= h - 3)) ||
                        ((x < 4 || x >= w - 4) && (y == 0 || y == h - 1));

                    if (cornerCut)
                        tex.SetPixel(x, y, clear);
                    else if (y <= 1 || y >= h - 2 || x <= 3 || x >= w - 4)
                        tex.SetPixel(x, y, outerBorder);
                    else if (y == h - 3 || x == 4 || x == w - 5)
                        tex.SetPixel(x, y, innerShadow);
                    else
                        tex.SetPixel(x, y, darkTrack);
                }
            }

            tex.Apply();
            File.WriteAllBytes(BarBgSpritePath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(BarBgSpritePath, ImportAssetOptions.ForceUpdate);
        }

        ConfigurePixelArtUISprite(BarBgSpritePath);
        return AssetDatabase.LoadAssetAtPath<Sprite>(BarBgSpritePath);
    }

    private static Sprite CreateOrLoadPixelBarFillSprite()
    {
        if (!File.Exists(BarFillSpritePath))
        {
            int w = 32, h = 10;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    if (y >= h - 3)
                        tex.SetPixel(x, y, new Color(1f, 1f, 1f, 1f));
                    else if (y <= 2)
                        tex.SetPixel(x, y, new Color(0.78f, 0.78f, 0.78f, 1f));
                    else
                        tex.SetPixel(x, y, new Color(0.92f, 0.92f, 0.92f, 1f));
                }
            }

            tex.Apply();
            File.WriteAllBytes(BarFillSpritePath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(BarFillSpritePath, ImportAssetOptions.ForceUpdate);
        }

        ConfigurePixelArtUISprite(BarFillSpritePath);
        return AssetDatabase.LoadAssetAtPath<Sprite>(BarFillSpritePath);
    }

    private static void ConfigurePixelArtUISprite(string path)
    {
        TextureImporter imp = AssetImporter.GetAtPath(path) as TextureImporter;
        if (imp != null)
        {
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.filterMode = FilterMode.Point;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.alphaIsTransparency = true;
            imp.SaveAndReimport();
        }
    }

    private static void ConfigureSpriteImporter(string path, float pixelsPerUnit)
    {
        TextureImporter imp = AssetImporter.GetAtPath(path) as TextureImporter;
        if (imp != null)
        {
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.spritePixelsPerUnit = pixelsPerUnit;
            imp.filterMode = FilterMode.Point;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.alphaIsTransparency = true;
            imp.SaveAndReimport();
        }
    }

    private static ParticleSystem CreateOrUpdateJuiceSplashPrefab()
    {
        GameObject temp = new GameObject("GrapeJuiceSplash");
        ParticleSystem ps = temp.AddComponent<ParticleSystem>();
        ParticleSystemRenderer psr = temp.GetComponent<ParticleSystemRenderer>();

        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.duration = 0.6f;
        main.loop = false;
        main.playOnAwake = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.32f, 0.65f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(2.2f, 6.2f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.25f);
        main.gravityModifier = 1.65f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(0.58f, 0.12f, 0.68f, 1f),
            new Color(0.85f, 0.22f, 0.48f, 1f)
        );

        var emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = 0f;
        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 24) });

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.22f;

        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(
            1f,
            new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(0.7f, 0.85f), new Keyframe(1f, 0f))
        );

        if (psr != null)
        {
            psr.sortingOrder = 10;
        }

        GameObject prefabObj = PrefabUtility.SaveAsPrefabAsset(temp, SplashPrefabPath);
        Object.DestroyImmediate(temp);
        return prefabObj != null ? prefabObj.GetComponent<ParticleSystem>() : null;
    }

    private static void UpdateMerlotAndSeedPrefabs(
        ParticleSystem splashPrefab,
        AudioClip seedShootClip,
        AudioClip grapeHitClip)
    {
        string[] enemyPrefabPaths = { MerlotPrefabPath, EnemigoUvaPrefabPath };
        foreach (string path in enemyPrefabPaths)
        {
            GameObject enemyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (enemyPrefab != null)
            {
                EnemyDeathFeedback edf = enemyPrefab.GetComponent<EnemyDeathFeedback>();
                if (edf == null)
                    edf = enemyPrefab.AddComponent<EnemyDeathFeedback>();

                edf.juiceSplashPrefab = splashPrefab;

                AudioSource audioSrc = enemyPrefab.GetComponent<AudioSource>();
                if (audioSrc == null)
                    audioSrc = enemyPrefab.AddComponent<AudioSource>();
                audioSrc.playOnAwake = false;
                audioSrc.spatialBlend = 0f;

                MerlotGrapeEnemy grapeScript = enemyPrefab.GetComponent<MerlotGrapeEnemy>();
                if (grapeScript != null)
                {
                    grapeScript.audioSource = audioSrc;
                    if (grapeScript.sonidoDisparoSemilla == null)
                        grapeScript.sonidoDisparoSemilla = seedShootClip;
                    if (grapeScript.sonidoImpactoOMuerte == null)
                        grapeScript.sonidoImpactoOMuerte = grapeHitClip;
                }

                PrefabUtility.SavePrefabAsset(enemyPrefab);
            }
        }

        // También actualizar las instancias ya colocadas en la escena
        MerlotGrapeEnemy[] sceneEnemies = Object.FindObjectsByType<MerlotGrapeEnemy>(FindObjectsSortMode.None);
        foreach (MerlotGrapeEnemy enemy in sceneEnemies)
        {
            EnemyDeathFeedback edf = enemy.GetComponent<EnemyDeathFeedback>();
            if (edf == null)
                edf = enemy.gameObject.AddComponent<EnemyDeathFeedback>();
            edf.juiceSplashPrefab = splashPrefab;

            AudioSource audioSrc = enemy.GetComponent<AudioSource>();
            if (audioSrc == null)
                audioSrc = enemy.gameObject.AddComponent<AudioSource>();
            audioSrc.playOnAwake = false;
            audioSrc.spatialBlend = 0f;

            enemy.audioSource = audioSrc;
            if (enemy.sonidoDisparoSemilla == null)
                enemy.sonidoDisparoSemilla = seedShootClip;
            if (enemy.sonidoImpactoOMuerte == null)
                enemy.sonidoImpactoOMuerte = grapeHitClip;
            EditorUtility.SetDirty(enemy.gameObject);
        }

        GameObject seedPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SeedPrefabPath);
        if (seedPrefab != null)
        {
            GrapeSeedProjectile gsp = seedPrefab.GetComponent<GrapeSeedProjectile>();
            if (gsp != null)
            {
                gsp.damage = 20;
                PrefabUtility.SavePrefabAsset(seedPrefab);
            }
        }
    }

    private static void CreateOrUpdateCanvasAndManagers(
        Sprite heartSprite,
        Sprite barBgSprite,
        Sprite barFillSprite,
        Sprite gameOverBgSprite,
        Sprite gameOverBtnSprite,
        Sprite levelCompleteBgSprite,
        Sprite levelCompleteBtnSprite,
        out Slider healthSlider,
        out Image fillImage,
        out Text hpLabelText,
        out GameOverManager gameOverManager,
        out GameObject levelCompletePanel,
        out CanvasGroup levelCompleteCg,
        out Text levelCompleteTitleText,
        out Text levelCompleteDetailsText,
        out Text levelCompleteTotalScoreText,
        out Button nextLevelButton)
    {
        if (Object.FindFirstObjectByType<EventSystem>() == null)
        {
            GameObject es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<InputSystemUIInputModule>();
        }

        GameObject canvasObj = GameObject.Find("VineyardCanvas");
        if (canvasObj != null)
        {
            Object.DestroyImmediate(canvasObj);
        }

        canvasObj = new GameObject("VineyardCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1024, 572);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObj.AddComponent<GraphicRaycaster>();

        Font defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // ====================================================================
        // 1. BARRA DE VIDA EN LA ESQUINA SUPERIOR IZQUIERDA (100 HP)
        // ====================================================================
        GameObject hudContainer = new GameObject("HealthBarHUD");
        hudContainer.transform.SetParent(canvasObj.transform, false);
        RectTransform hudRect = hudContainer.AddComponent<RectTransform>();
        hudRect.anchorMin = new Vector2(0f, 1f);
        hudRect.anchorMax = new Vector2(0f, 1f);
        hudRect.pivot = new Vector2(0f, 1f);
        hudRect.anchoredPosition = new Vector2(20f, -18f);
        hudRect.sizeDelta = new Vector2(300f, 50f);

        GameObject heartObj = new GameObject("PixelHeartIcon");
        heartObj.transform.SetParent(hudContainer.transform, false);
        RectTransform heartRect = heartObj.AddComponent<RectTransform>();
        heartRect.anchorMin = new Vector2(0f, 0.5f);
        heartRect.anchorMax = new Vector2(0f, 0.5f);
        heartRect.pivot = new Vector2(0f, 0.5f);
        heartRect.anchoredPosition = new Vector2(0f, 0f);
        heartRect.sizeDelta = new Vector2(38f, 38f);
        Image heartImg = heartObj.AddComponent<Image>();
        heartImg.sprite = heartSprite;
        heartImg.preserveAspect = true;

        GameObject sliderObj = new GameObject("PlayerHealthSlider");
        sliderObj.transform.SetParent(hudContainer.transform, false);
        RectTransform sliderRect = sliderObj.AddComponent<RectTransform>();
        sliderRect.anchorMin = new Vector2(0f, 0.5f);
        sliderRect.anchorMax = new Vector2(0f, 0.5f);
        sliderRect.pivot = new Vector2(0f, 0.5f);
        sliderRect.anchoredPosition = new Vector2(44f, -4f);
        sliderRect.sizeDelta = new Vector2(220f, 24f);

        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(sliderObj.transform, false);
        RectTransform bgRect = bgObj.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;
        Image bgImg = bgObj.AddComponent<Image>();
        bgImg.sprite = barBgSprite;

        GameObject fillAreaObj = new GameObject("Fill Area");
        fillAreaObj.transform.SetParent(sliderObj.transform, false);
        RectTransform fillAreaRect = fillAreaObj.AddComponent<RectTransform>();
        fillAreaRect.anchorMin = Vector2.zero;
        fillAreaRect.anchorMax = Vector2.one;
        fillAreaRect.offsetMin = new Vector2(12f, 4f);
        fillAreaRect.offsetMax = new Vector2(-12f, -4f);

        GameObject fillObj = new GameObject("Fill");
        fillObj.transform.SetParent(fillAreaObj.transform, false);
        RectTransform fillRect = fillObj.AddComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        fillImage = fillObj.AddComponent<Image>();
        fillImage.sprite = barFillSprite;
        fillImage.color = new Color(0.88f, 0.18f, 0.22f, 1f);

        healthSlider = sliderObj.AddComponent<Slider>();
        healthSlider.interactable = false;
        healthSlider.transition = Selectable.Transition.None;
        healthSlider.fillRect = fillRect;
        healthSlider.direction = Slider.Direction.LeftToRight;
        healthSlider.minValue = 0f;
        healthSlider.maxValue = 100f;
        healthSlider.value = 100f;

        GameObject hpTextObj = new GameObject("HPLabelText");
        hpTextObj.transform.SetParent(hudContainer.transform, false);
        RectTransform hpTextRect = hpTextObj.AddComponent<RectTransform>();
        hpTextRect.anchorMin = new Vector2(0f, 1f);
        hpTextRect.anchorMax = new Vector2(0f, 1f);
        hpTextRect.pivot = new Vector2(0f, 1f);
        hpTextRect.anchoredPosition = new Vector2(48f, 2f);
        hpTextRect.sizeDelta = new Vector2(200f, 20f);

        hpLabelText = hpTextObj.AddComponent<Text>();
        hpLabelText.font = defaultFont;
        hpLabelText.text = "HP: 100/100";
        hpLabelText.fontSize = 13;
        hpLabelText.fontStyle = FontStyle.Bold;
        hpLabelText.color = new Color(0.98f, 0.90f, 0.74f, 1f);
        Shadow textShadow = hpTextObj.AddComponent<Shadow>();
        textShadow.effectColor = new Color(0.12f, 0.04f, 0.08f, 0.95f);
        textShadow.effectDistance = new Vector2(1.5f, -1.5f);

        // ====================================================================
        // 2. VENTANA EMERGENTE DE GAME OVER SIN FONDO Y BOTÓN FUNCIONAL
        // ====================================================================
        GameObject goPanel = new GameObject("GameOverPanel");
        goPanel.transform.SetParent(canvasObj.transform, false);
        RectTransform goPanelRect = goPanel.AddComponent<RectTransform>();
        goPanelRect.anchorMin = Vector2.zero;
        goPanelRect.anchorMax = Vector2.one;
        goPanelRect.offsetMin = Vector2.zero;
        goPanelRect.offsetMax = Vector2.zero;

        Image backdropImg = goPanel.AddComponent<Image>();
        backdropImg.color = new Color(0f, 0f, 0f, 0f);
        CanvasGroup cg = goPanel.AddComponent<CanvasGroup>();

        GameObject artContainer = new GameObject("GameOverArtwork");
        artContainer.transform.SetParent(goPanel.transform, false);
        RectTransform artRect = artContainer.AddComponent<RectTransform>();
        artRect.anchorMin = Vector2.zero;
        artRect.anchorMax = Vector2.one;
        artRect.offsetMin = Vector2.zero;
        artRect.offsetMax = Vector2.zero;

        Image artImg = artContainer.AddComponent<Image>();
        artImg.sprite = gameOverBgSprite;
        artImg.color = Color.white;

        AspectRatioFitter fitter = artContainer.AddComponent<AspectRatioFitter>();
        fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        fitter.aspectRatio = 669f / 373f;

        GameObject btnObj = new GameObject("RestartButton");
        btnObj.transform.SetParent(artContainer.transform, false);
        RectTransform btnRect = btnObj.AddComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(0.3184f, 0.2252f);
        btnRect.anchorMax = new Vector2(0.6771f, 0.3458f);
        btnRect.offsetMin = Vector2.zero;
        btnRect.offsetMax = Vector2.zero;
        btnRect.pivot = new Vector2(0.5f, 0.5f);

        Image btnImg = btnObj.AddComponent<Image>();
        btnImg.sprite = gameOverBtnSprite;
        btnImg.color = Color.white;

        Button restartBtn = btnObj.AddComponent<Button>();
        restartBtn.targetGraphic = btnImg;
        ColorBlock cb = restartBtn.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(1f, 0.92f, 0.82f, 1f);
        cb.pressedColor = new Color(0.75f, 0.65f, 0.65f, 1f);
        cb.selectedColor = Color.white;
        restartBtn.colors = cb;

        // ====================================================================
        // 3. OBJETO GAME OVER MANAGER Y CONEXIÓN ONCLICK DEL BOTÓN
        // ====================================================================
        GameObject gomObj = GameObject.Find("GameOverManager");
        if (gomObj != null)
            Object.DestroyImmediate(gomObj);

        gomObj = new GameObject("GameOverManager");
        gameOverManager = gomObj.AddComponent<GameOverManager>();
        gameOverManager.gameOverPanel = goPanel;
        gameOverManager.restartButton = restartBtn;
        gameOverManager.panelCanvasGroup = cg;

        UnityEventTools.AddPersistentListener(restartBtn.onClick, gameOverManager.RestartCurrentLevel);
        goPanel.SetActive(false);

        // ====================================================================
        // 4. PANTALLA EMERGENTE "¡NIVEL 1 COMPLETADO!" CON PUNTUACIÓN Y BOTÓN
        // ====================================================================
        levelCompletePanel = new GameObject("LevelCompletePanel");
        levelCompletePanel.transform.SetParent(canvasObj.transform, false);
        RectTransform lcPanelRect = levelCompletePanel.AddComponent<RectTransform>();
        lcPanelRect.anchorMin = Vector2.zero;
        lcPanelRect.anchorMax = Vector2.one;
        lcPanelRect.offsetMin = Vector2.zero;
        lcPanelRect.offsetMax = Vector2.zero;

        Image lcBackdrop = levelCompletePanel.AddComponent<Image>();
        lcBackdrop.color = new Color(0f, 0f, 0f, 0f);
        levelCompleteCg = levelCompletePanel.AddComponent<CanvasGroup>();

        // Marco emergente estilo viñedo (usando el mismo marco artesanal con parras de LevelCompleteFrame.png)
        GameObject lcArtObj = new GameObject("LevelCompleteArtwork");
        lcArtObj.transform.SetParent(levelCompletePanel.transform, false);
        RectTransform lcArtRect = lcArtObj.AddComponent<RectTransform>();
        lcArtRect.anchorMin = Vector2.zero;
        lcArtRect.anchorMax = Vector2.one;
        lcArtRect.offsetMin = Vector2.zero;
        lcArtRect.offsetMax = Vector2.zero;

        Image lcArtImg = lcArtObj.AddComponent<Image>();
        lcArtImg.sprite = levelCompleteBgSprite;
        lcArtImg.color = Color.white;

        AspectRatioFitter lcFitter = lcArtObj.AddComponent<AspectRatioFitter>();
        lcFitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        lcFitter.aspectRatio = 669f / 373f;

        // Título: "¡Nivel 1 Completado!"
        GameObject lcTitleObj = new GameObject("TitleText");
        lcTitleObj.transform.SetParent(lcArtObj.transform, false);
        RectTransform lcTitleRect = lcTitleObj.AddComponent<RectTransform>();
        lcTitleRect.anchorMin = new Vector2(0.18f, 0.64f);
        lcTitleRect.anchorMax = new Vector2(0.82f, 0.80f);
        lcTitleRect.offsetMin = Vector2.zero;
        lcTitleRect.offsetMax = Vector2.zero;
        levelCompleteTitleText = lcTitleObj.AddComponent<Text>();
        levelCompleteTitleText.font = defaultFont;
        levelCompleteTitleText.text = "¡Nivel 1 Completado!";
        levelCompleteTitleText.fontSize = 28;
        levelCompleteTitleText.fontStyle = FontStyle.Bold;
        levelCompleteTitleText.alignment = TextAnchor.MiddleCenter;
        levelCompleteTitleText.color = new Color(0.98f, 0.84f, 0.35f, 1f);
        Shadow titleShadow = lcTitleObj.AddComponent<Shadow>();
        titleShadow.effectColor = new Color(0.08f, 0.02f, 0.05f, 0.95f);
        titleShadow.effectDistance = new Vector2(2f, -2f);

        // Desglose de puntos (Uvas eliminadas + Bono de vida restante)
        GameObject lcDetailsObj = new GameObject("BreakdownText");
        lcDetailsObj.transform.SetParent(lcArtObj.transform, false);
        RectTransform lcDetailsRect = lcDetailsObj.AddComponent<RectTransform>();
        lcDetailsRect.anchorMin = new Vector2(0.18f, 0.45f);
        lcDetailsRect.anchorMax = new Vector2(0.82f, 0.63f);
        lcDetailsRect.offsetMin = Vector2.zero;
        lcDetailsRect.offsetMax = Vector2.zero;
        levelCompleteDetailsText = lcDetailsObj.AddComponent<Text>();
        levelCompleteDetailsText.font = defaultFont;
        levelCompleteDetailsText.text = "Uvas Eliminadas (0): +0 pts\nBono de Salud (100 HP): +500 pts";
        levelCompleteDetailsText.fontSize = 15;
        levelCompleteDetailsText.fontStyle = FontStyle.Bold;
        levelCompleteDetailsText.alignment = TextAnchor.MiddleCenter;
        levelCompleteDetailsText.color = new Color(0.94f, 0.88f, 0.82f, 1f);
        Shadow detailsShadow = lcDetailsObj.AddComponent<Shadow>();
        detailsShadow.effectColor = new Color(0.08f, 0.02f, 0.05f, 0.95f);
        detailsShadow.effectDistance = new Vector2(1.5f, -1.5f);

        // Puntuación Total
        GameObject lcTotalObj = new GameObject("TotalScoreText");
        lcTotalObj.transform.SetParent(lcArtObj.transform, false);
        RectTransform lcTotalRect = lcTotalObj.AddComponent<RectTransform>();
        lcTotalRect.anchorMin = new Vector2(0.18f, 0.35f);
        lcTotalRect.anchorMax = new Vector2(0.82f, 0.45f);
        lcTotalRect.offsetMin = Vector2.zero;
        lcTotalRect.offsetMax = Vector2.zero;
        levelCompleteTotalScoreText = lcTotalObj.AddComponent<Text>();
        levelCompleteTotalScoreText.font = defaultFont;
        levelCompleteTotalScoreText.text = "PUNTUACIÓN TOTAL: 500 PTS";
        levelCompleteTotalScoreText.fontSize = 20;
        levelCompleteTotalScoreText.fontStyle = FontStyle.Bold;
        levelCompleteTotalScoreText.alignment = TextAnchor.MiddleCenter;
        levelCompleteTotalScoreText.color = new Color(0.55f, 0.96f, 0.52f, 1f);
        Shadow totalShadow = lcTotalObj.AddComponent<Shadow>();
        totalShadow.effectColor = new Color(0.06f, 0.02f, 0.05f, 0.95f);
        totalShadow.effectDistance = new Vector2(2f, -2f);

        // Botón "Siguiente Nivel" con estilo vino tinto y borde dorado
        GameObject nextBtnObj = new GameObject("NextLevelButton");
        nextBtnObj.transform.SetParent(lcArtObj.transform, false);
        RectTransform nextBtnRect = nextBtnObj.AddComponent<RectTransform>();
        nextBtnRect.anchorMin = new Vector2(0.3184f, 0.215f);
        nextBtnRect.anchorMax = new Vector2(0.6771f, 0.335f);
        nextBtnRect.offsetMin = Vector2.zero;
        nextBtnRect.offsetMax = Vector2.zero;
        nextBtnRect.pivot = new Vector2(0.5f, 0.5f);

        Image nextBtnImg = nextBtnObj.AddComponent<Image>();
        nextBtnImg.sprite = levelCompleteBtnSprite;
        nextBtnImg.color = Color.white;

        nextLevelButton = nextBtnObj.AddComponent<Button>();
        nextLevelButton.targetGraphic = nextBtnImg;
        ColorBlock nextCb = nextLevelButton.colors;
        nextCb.normalColor = Color.white;
        nextCb.highlightedColor = new Color(1f, 0.90f, 0.80f, 1f);
        nextCb.pressedColor = new Color(0.75f, 0.65f, 0.65f, 1f);
        nextCb.selectedColor = Color.white;
        nextLevelButton.colors = nextCb;

        GameObject nextBtnTextObj = new GameObject("ButtonLabel");
        nextBtnTextObj.transform.SetParent(nextBtnObj.transform, false);
        RectTransform nextBtnTextRect = nextBtnTextObj.AddComponent<RectTransform>();
        nextBtnTextRect.anchorMin = Vector2.zero;
        nextBtnTextRect.anchorMax = Vector2.one;
        nextBtnTextRect.offsetMin = Vector2.zero;
        nextBtnTextRect.offsetMax = Vector2.zero;
        Text nextBtnText = nextBtnTextObj.AddComponent<Text>();
        nextBtnText.font = defaultFont;
        nextBtnText.text = "SIGUIENTE NIVEL";
        nextBtnText.fontSize = 15;
        nextBtnText.fontStyle = FontStyle.Bold;
        nextBtnText.alignment = TextAnchor.MiddleCenter;
        nextBtnText.color = Color.white;

        levelCompletePanel.SetActive(false);

        EditorUtility.SetDirty(canvasObj);
        EditorUtility.SetDirty(gomObj);
    }

    // ========================================================================
    // 5. CORAZONES CURATIVOS EN EL MAPA (PREFAB + INSTANCIAS EN NIVEL 1)
    // ========================================================================
    private static void CreateAndSpawnHealingHearts(AudioClip healClip)
    {
        Sprite heartWorldSprite = CreateOrLoadWorldHeartSprite();

        GameObject tempHeart = new GameObject("CorazonVida");
        SpriteRenderer sr = tempHeart.AddComponent<SpriteRenderer>();
        sr.sprite = heartWorldSprite;
        sr.sortingOrder = 5;

        CircleCollider2D col = tempHeart.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.42f;

        AudioSource audioSrc = tempHeart.AddComponent<AudioSource>();
        audioSrc.playOnAwake = false;
        audioSrc.spatialBlend = 0f;

        CorazonVida corazonScript = tempHeart.AddComponent<CorazonVida>();
        corazonScript.cantidadCuracion = 35f;
        corazonScript.audioSource = audioSrc;
        corazonScript.sonidoCuracion = healClip;
        corazonScript.volumenCuracion = 0.9f;

        GameObject heartPrefab = PrefabUtility.SaveAsPrefabAsset(tempHeart, CorazonPrefabPath);
        Object.DestroyImmediate(tempHeart);

        if (heartPrefab == null)
            return;

        GameObject oldContainer = GameObject.Find("ItemsCurativos_Nivel1");
        if (oldContainer != null)
            Object.DestroyImmediate(oldContainer);

        GameObject container = new GameObject("ItemsCurativos_Nivel1");

        // Dos corazones curativos en puntos clave a mitad del recorrido del Nivel 1
        Vector2[] heartZones = new Vector2[]
        {
            new Vector2(14.5f, 8.5f), // Zona intermedia tras los primeros combates
            new Vector2(61.5f, 12.5f) // Plataforma elevada a mitad/final del nivel
        };

        for (int i = 0; i < heartZones.Length; i++)
        {
            float spawnX = heartZones[i].x;
            float rayStartY = heartZones[i].y;
            float spawnY = rayStartY - 1.2f;

            RaycastHit2D[] hits = Physics2D.RaycastAll(new Vector2(spawnX, rayStartY), Vector2.down, 12f);
            foreach (RaycastHit2D hit in hits)
            {
                if (hit.collider != null && !hit.collider.isTrigger &&
                    hit.collider.GetComponent<MovimientoJugador>() == null &&
                    hit.collider.GetComponent<MerlotGrapeEnemy>() == null)
                {
                    spawnY = hit.point.y + 0.85f;
                    break;
                }
            }

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(heartPrefab);
            instance.name = $"CorazonVida_{i + 1}";
            instance.transform.SetParent(container.transform);
            instance.transform.position = new Vector3(spawnX, spawnY, 0f);
        }
    }

    private static Sprite CreateOrLoadWorldHeartSprite()
    {
        if (!File.Exists(CorazonSpritePath))
        {
            CreateOrLoadPixelHeartSprite();
            if (File.Exists(HeartSpritePath))
            {
                File.Copy(HeartSpritePath, CorazonSpritePath, overwrite: true);
                AssetDatabase.ImportAsset(CorazonSpritePath, ImportAssetOptions.ForceSynchronousImport);
            }
        }
        ConfigureSpriteImporter(CorazonSpritePath, 18f);
        return AssetDatabase.LoadAssetAtPath<Sprite>(CorazonSpritePath);
    }

    // ========================================================================
    // 6. META DEL NIVEL 1 Y CONEXIÓN CON LA PANTALLA EMERGENTE DE VICTORIA
    // ========================================================================
    private static void CreateAndSpawnLevelGoal(
        AudioClip victoryClip,
        GameObject levelCompletePanel,
        CanvasGroup levelCompleteCg,
        Text titleText,
        Text detailsText,
        Text totalScoreText,
        Button nextLevelButton)
    {
        Sprite flagSprite = CreateGoalFlagSprite();

        GameObject tempGoal = new GameObject("MetaNivel");
        SpriteRenderer sr = tempGoal.AddComponent<SpriteRenderer>();
        sr.sprite = flagSprite;
        sr.sortingOrder = 4;

        BoxCollider2D col = tempGoal.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(1.8f, 3.8f);
        col.offset = new Vector2(0f, 0.8f);

        AudioSource audioSrc = tempGoal.AddComponent<AudioSource>();
        audioSrc.playOnAwake = false;
        audioSrc.spatialBlend = 0f;

        MetaNivel metaScript = tempGoal.AddComponent<MetaNivel>();
        metaScript.puntosPorUva = 100;
        metaScript.multiplicadorBonoVida = 5f;
        metaScript.audioSource = audioSrc;
        metaScript.sonidoMetaCompletada = victoryClip;

        GameObject goalPrefab = PrefabUtility.SaveAsPrefabAsset(tempGoal, MetaPrefabPath);
        Object.DestroyImmediate(tempGoal);

        GameObject existingGoal = GameObject.Find("MetaNivel1");
        if (existingGoal != null)
            Object.DestroyImmediate(existingGoal);

        GameObject goalInstance = goalPrefab != null
            ? (GameObject)PrefabUtility.InstantiatePrefab(goalPrefab)
            : new GameObject("MetaNivel1");

        goalInstance.name = "MetaNivel1";

        // Ubicar la meta al final del recorrido del Nivel 1 (x = 111.5)
        float goalX = 111.5f;
        float goalY = 5.95f;
        RaycastHit2D[] hits = Physics2D.RaycastAll(new Vector2(goalX, 10f), Vector2.down, 12f);
        foreach (RaycastHit2D hit in hits)
        {
            if (hit.collider != null && !hit.collider.isTrigger &&
                hit.collider.GetComponent<MovimientoJugador>() == null &&
                hit.collider.GetComponent<MerlotGrapeEnemy>() == null)
            {
                goalY = hit.point.y + 0.95f;
                break;
            }
        }
        goalInstance.transform.position = new Vector3(goalX, goalY, 0f);

        MetaNivel instanceMeta = goalInstance.GetComponent<MetaNivel>();
        if (instanceMeta == null)
            instanceMeta = goalInstance.AddComponent<MetaNivel>();

        instanceMeta.panelNivelCompletado = levelCompletePanel;
        instanceMeta.panelCanvasGroup = levelCompleteCg;
        instanceMeta.textoTitulo = titleText;
        instanceMeta.textoDesglosePuntos = detailsText;
        instanceMeta.textoPuntuacionTotal = totalScoreText;
        instanceMeta.botonSiguienteNivel = nextLevelButton;
        instanceMeta.sonidoMetaCompletada = victoryClip;

        if (nextLevelButton != null)
        {
            UnityEventTools.AddPersistentListener(nextLevelButton.onClick, instanceMeta.CargarSiguienteNivel);
        }

        EditorUtility.SetDirty(goalInstance);
    }

    private static Sprite CreateGoalFlagSprite()
    {
        if (!File.Exists(MetaSpritePath))
        {
            int w = 32;
            int h = 48;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color clear = new Color(0, 0, 0, 0);
            Color woodDark = new Color(0.32f, 0.18f, 0.08f, 1f);
            Color woodLight = new Color(0.52f, 0.32f, 0.14f, 1f);
            Color gold = new Color(0.95f, 0.78f, 0.25f, 1f);
            Color wineBanner = new Color(0.62f, 0.10f, 0.22f, 1f);

            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    tex.SetPixel(x, y, clear);

            // Mástil de madera con remate dorado
            for (int y = 0; y < 44; y++)
            {
                tex.SetPixel(6, y, woodDark);
                tex.SetPixel(7, y, woodLight);
                tex.SetPixel(8, y, woodDark);
            }
            for (int y = 44; y < 47; y++)
                for (int x = 5; x <= 9; x++)
                    tex.SetPixel(x, y, gold);

            // Estandarte / bandera del viñedo
            for (int y = 26; y <= 42; y++)
            {
                int maxX = 28 - Mathf.Abs(y - 34) / 2;
                for (int x = 9; x <= maxX; x++)
                {
                    bool isBorder = (y == 26 || y == 42 || x == maxX);
                    tex.SetPixel(x, y, isBorder ? gold : wineBanner);
                }
            }

            tex.Apply();
            File.WriteAllBytes(MetaSpritePath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(MetaSpritePath, ImportAssetOptions.ForceSynchronousImport);
        }

        ConfigureSpriteImporter(MetaSpritePath, 24f);
        return AssetDatabase.LoadAssetAtPath<Sprite>(MetaSpritePath);
    }

    // ========================================================================
    // 7. GENERACIÓN DE AUDIOCLIPS WAV BASE (PARA QUE FUNCIONE DESDE EL PRIMER PLAY)
    // ========================================================================
    public static void EnsureDefaultAudioClipsExist(
        out AudioClip corkShootClip,
        out AudioClip seedShootClip,
        out AudioClip grapeHitClip,
        out AudioClip healClip,
        out AudioClip goalClip)
    {
        if (!AssetDatabase.IsValidFolder("Assets/Audio"))
            AssetDatabase.CreateFolder("Assets", "Audio");

        corkShootClip = CreateOrLoadToneWav(SfxCorkPath, 340f, 680f, 0.11f, 0.7f);
        seedShootClip = CreateOrLoadToneWav(SfxSeedPath, 520f, 260f, 0.09f, 0.55f);
        grapeHitClip = CreateOrLoadToneWav(SfxGrapeHitPath, 240f, 95f, 0.18f, 0.8f);
        healClip = CreateOrLoadToneWav(SfxHealPath, 440f, 880f, 0.25f, 0.7f);
        goalClip = CreateOrLoadToneWav(SfxGoalPath, 523.25f, 1046.5f, 0.45f, 0.75f);
    }

    private static AudioClip CreateOrLoadToneWav(string assetPath, float startFreq, float endFreq, float durationSec, float amplitude)
    {
        if (!File.Exists(assetPath))
        {
            WriteSynthesizedWav(assetPath, startFreq, endFreq, durationSec, amplitude);
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);
        }
        return AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath);
    }

    private static void WriteSynthesizedWav(string filePath, float startFreq, float endFreq, float durationSec, float amplitude)
    {
        int sampleRate = 44100;
        int numSamples = Mathf.Max(1, Mathf.RoundToInt(sampleRate * durationSec));
        short[] samples = new short[numSamples];

        float phase = 0f;
        for (int i = 0; i < numSamples; i++)
        {
            float t = (float)i / numSamples;
            float freq = Mathf.Lerp(startFreq, endFreq, t);
            float envelope = Mathf.Sin(t * Mathf.PI);
            phase += 2f * Mathf.PI * freq / sampleRate;
            float val = Mathf.Sin(phase) * amplitude * envelope;
            samples[i] = (short)Mathf.Clamp(val * 32767f, short.MinValue, short.MaxValue);
        }

        using (FileStream fs = new FileStream(filePath, FileMode.Create, FileAccess.Write))
        using (BinaryWriter bw = new BinaryWriter(fs))
        {
            int byteRate = sampleRate * 2;
            int dataSize = numSamples * 2;

            bw.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
            bw.Write(36 + dataSize);
            bw.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));
            bw.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
            bw.Write(16);
            bw.Write((short)1); // PCM
            bw.Write((short)1); // Mono
            bw.Write(sampleRate);
            bw.Write(byteRate);
            bw.Write((short)2);
            bw.Write((short)16);
            bw.Write(System.Text.Encoding.ASCII.GetBytes("data"));
            bw.Write(dataSize);

            for (int i = 0; i < numSamples; i++)
            {
                bw.Write(samples[i]);
            }
        }
    }

    // ========================================================================
    // 8. PANTALLA DE CARGA / OPENER EN MÁXIMA CALIDAD HD ("Grapefall - START GAME")
    // ========================================================================
    private static void ImportOrCreateOpenerHDSprites(
        out Sprite openerBgSprite,
        out Sprite openerStartBtnSprite,
        out Sprite lanternGlowSprite)
    {
        // 1. Copiar el archivo original Panatalla-Opener-Vineyard.jpg (2752x1536, ~2.87 MB) byte por byte sin bajar calidad
        if (File.Exists(ExtractedOpenerJpgPath))
        {
            bool needsCopy = !File.Exists(OpenerBgSpritePath);
            if (!needsCopy)
            {
                FileInfo srcInfo = new FileInfo(ExtractedOpenerJpgPath);
                FileInfo dstInfo = new FileInfo(OpenerBgSpritePath);
                if (srcInfo.Length != dstInfo.Length)
                    needsCopy = true;
            }

            if (needsCopy)
            {
                File.Copy(ExtractedOpenerJpgPath, OpenerBgSpritePath, overwrite: true);
                AssetDatabase.ImportAsset(OpenerBgSpritePath, ImportAssetOptions.ForceSynchronousImport);
            }
        }

        // Configurar el TextureImporter a MÁXIMA CALIDAD HD (MaxSize = 4096, Uncompressed, sin MipMaps)
        ConfigureUltraHDSpriteImporter(OpenerBgSpritePath, isReadable: true);
        openerBgSprite = AssetDatabase.LoadAssetAtPath<Sprite>(OpenerBgSpritePath);

        // 2. Extraer el recorte exacto de "-> START GAME" en resolución nativa 2752x1536 para animarlo al dar clic
        if (!File.Exists(OpenerStartBtnSpritePath))
        {
            Texture2D fullTex = AssetDatabase.LoadAssetAtPath<Texture2D>(OpenerBgSpritePath);
            if (fullTex != null)
            {
                int w = fullTex.width;   // 2752
                int h = fullTex.height;  // 1536

                // Coordenadas normalizadas del área "-> START GAME": X = 0.354..0.656, Y(Unity bottom-up) = 0.163..0.242
                int startX = Mathf.Clamp(Mathf.RoundToInt(w * 0.354f), 0, w - 1);
                int endX = Mathf.Clamp(Mathf.RoundToInt(w * 0.656f), startX + 1, w);
                int startY = Mathf.Clamp(Mathf.RoundToInt(h * 0.163f), 0, h - 1);
                int endY = Mathf.Clamp(Mathf.RoundToInt(h * 0.242f), startY + 1, h);

                int sliceW = endX - startX;
                int sliceH = endY - startY;

                Color[] pixels = fullTex.GetPixels(startX, startY, sliceW, sliceH);

                // Suavizar ligeramente los bordes del recorte para que al hacer pulso/escala se funda con el camino
                for (int py = 0; py < sliceH; py++)
                {
                    float edgeY = Mathf.Min(py / 10f, (sliceH - 1 - py) / 10f);
                    for (int px = 0; px < sliceW; px++)
                    {
                        float edgeX = Mathf.Min(px / 14f, (sliceW - 1 - px) / 14f);
                        float fade = Mathf.Clamp01(Mathf.Min(edgeX, edgeY));
                        int idx = py * sliceW + px;
                        Color c = pixels[idx];
                        c.a = fade;
                        pixels[idx] = c;
                    }
                }

                Texture2D btnSliceTex = new Texture2D(sliceW, sliceH, TextureFormat.RGBA32, false);
                btnSliceTex.SetPixels(pixels);
                btnSliceTex.Apply();

                File.WriteAllBytes(OpenerStartBtnSpritePath, btnSliceTex.EncodeToPNG());
                Object.DestroyImmediate(btnSliceTex);
                AssetDatabase.ImportAsset(OpenerStartBtnSpritePath, ImportAssetOptions.ForceSynchronousImport);
            }
        }

        ConfigureUltraHDSpriteImporter(OpenerStartBtnSpritePath, isReadable: false);
        openerStartBtnSprite = AssetDatabase.LoadAssetAtPath<Sprite>(OpenerStartBtnSpritePath);

        // 3. Crear sprite radial cálido para el brillo del farol del viñedo
        if (!File.Exists(OpenerLanternGlowSpritePath))
        {
            int size = 128;
            Texture2D glowTex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
            float maxDist = size * 0.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), center) / maxDist;
                    float a = Mathf.Pow(Mathf.Clamp01(1f - d), 2.2f);
                    glowTex.SetPixel(x, y, new Color(1f, 0.85f, 0.42f, a));
                }
            }

            glowTex.Apply();
            File.WriteAllBytes(OpenerLanternGlowSpritePath, glowTex.EncodeToPNG());
            Object.DestroyImmediate(glowTex);
            AssetDatabase.ImportAsset(OpenerLanternGlowSpritePath, ImportAssetOptions.ForceSynchronousImport);
        }

        ConfigureUltraHDSpriteImporter(OpenerLanternGlowSpritePath, isReadable: false);
        lanternGlowSprite = AssetDatabase.LoadAssetAtPath<Sprite>(OpenerLanternGlowSpritePath);
    }

    private static void ConfigureUltraHDSpriteImporter(string path, bool isReadable)
    {
        TextureImporter imp = AssetImporter.GetAtPath(path) as TextureImporter;
        if (imp != null)
        {
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.maxTextureSize = 4096; // Preserva los 2752x1536 píxeles completos sin reescalar a 2048
            imp.textureCompression = TextureImporterCompression.Uncompressed; // Cero pérdida de calidad
            imp.compressionQuality = 100;
            imp.mipmapEnabled = false; // Evita cualquier desenfoque por mipmaps
            imp.npotScale = TextureImporterNPOTScale.None;
            imp.filterMode = FilterMode.Bilinear;
            imp.alphaIsTransparency = true;
            imp.isReadable = isReadable;

            TextureImporterPlatformSettings defaultPlatform = imp.GetDefaultPlatformTextureSettings();
            defaultPlatform.maxTextureSize = 4096;
            defaultPlatform.textureCompression = TextureImporterCompression.Uncompressed;
            defaultPlatform.compressionQuality = 100;
            imp.SetPlatformTextureSettings(defaultPlatform);

            imp.SaveAndReimport();
        }
    }

    private static void CreateOrUpdateTitleScreenOpener(
        Sprite openerBgSprite,
        Sprite openerStartBtnSprite,
        Sprite lanternGlowSprite,
        AudioClip bgmChillClip,
        AudioClip sfxStartClip)
    {
        GameObject canvasObj = GameObject.Find("VineyardCanvas");
        if (canvasObj == null)
            return;

        Transform oldPanel = canvasObj.transform.Find("TitleScreenOpenerPanel");
        if (oldPanel != null)
            Object.DestroyImmediate(oldPanel.gameObject);

        // Panel raíz de la Pantalla Opener (al final del Canvas para cubrir toda la pantalla al iniciar)
        GameObject openerRoot = new GameObject("TitleScreenOpenerPanel");
        openerRoot.transform.SetParent(canvasObj.transform, false);
        openerRoot.transform.SetAsLastSibling();
        RectTransform rootRect = openerRoot.AddComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;

        // Contenedor de la ilustración HD con fondo oscuro de seguridad y máscara de recorte para el zoom cinemático
        GameObject artContainer = new GameObject("OpenerArtworkContainer");
        artContainer.transform.SetParent(openerRoot.transform, false);
        RectTransform artContRect = artContainer.AddComponent<RectTransform>();
        artContRect.anchorMin = Vector2.zero;
        artContRect.anchorMax = Vector2.one;
        artContRect.offsetMin = Vector2.zero;
        artContRect.offsetMax = Vector2.zero;
        Image bgDarkImg = artContainer.AddComponent<Image>();
        bgDarkImg.color = new Color(0.06f, 0.02f, 0.08f, 1f);
        artContainer.AddComponent<RectMask2D>();
        CanvasGroup artCg = artContainer.AddComponent<CanvasGroup>();

        // Ilustración "Grapefall" en 2752x1536 (Sin compresión)
        GameObject hdArtObj = new GameObject("OpenerHDArtwork");
        hdArtObj.transform.SetParent(artContainer.transform, false);
        RectTransform hdArtRect = hdArtObj.AddComponent<RectTransform>();
        hdArtRect.anchorMin = Vector2.zero;
        hdArtRect.anchorMax = Vector2.one;
        hdArtRect.offsetMin = Vector2.zero;
        hdArtRect.offsetMax = Vector2.zero;
        hdArtRect.pivot = new Vector2(0.54f, 0.44f); // Pivote orientado hacia el sendero y el farol para el zoom cinemático

        Image hdArtImg = hdArtObj.AddComponent<Image>();
        hdArtImg.sprite = openerBgSprite;
        hdArtImg.color = Color.white;

        AspectRatioFitter fitter = hdArtObj.AddComponent<AspectRatioFitter>();
        fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        fitter.aspectRatio = 2752f / 1536f;

        // Resplandor cálido sobre el farol colgante del viñedo (X: 52.5%..62.0%, Y: 40.5%..57.5%)
        GameObject lanternGlowObj = new GameObject("LanternWarmGlow");
        lanternGlowObj.transform.SetParent(hdArtObj.transform, false);
        RectTransform glowRect = lanternGlowObj.AddComponent<RectTransform>();
        glowRect.anchorMin = new Vector2(0.515f, 0.395f);
        glowRect.anchorMax = new Vector2(0.630f, 0.585f);
        glowRect.offsetMin = Vector2.zero;
        glowRect.offsetMax = Vector2.zero;
        Image glowImg = lanternGlowObj.AddComponent<Image>();
        glowImg.sprite = lanternGlowSprite;
        glowImg.color = new Color(1f, 0.78f, 0.32f, 0.28f);
        glowImg.raycastTarget = false;

        // Botón interactivo "-> START GAME" ubicado en las coordenadas exactas (X: 0.354..0.656, Y: 0.163..0.242)
        GameObject startBtnObj = new GameObject("StartGameButton");
        startBtnObj.transform.SetParent(hdArtObj.transform, false);
        RectTransform startBtnRect = startBtnObj.AddComponent<RectTransform>();
        startBtnRect.anchorMin = new Vector2(0.354f, 0.163f);
        startBtnRect.anchorMax = new Vector2(0.656f, 0.242f);
        startBtnRect.offsetMin = Vector2.zero;
        startBtnRect.offsetMax = Vector2.zero;
        startBtnRect.pivot = new Vector2(0.5f, 0.5f);

        Image startBtnImg = startBtnObj.AddComponent<Image>();
        startBtnImg.sprite = openerStartBtnSprite;
        startBtnImg.color = Color.white;

        Button startBtn = startBtnObj.AddComponent<Button>();
        startBtn.targetGraphic = startBtnImg;
        ColorBlock cb = startBtn.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(1f, 0.96f, 0.82f, 1f);
        cb.pressedColor = new Color(1f, 0.88f, 0.55f, 1f);
        cb.selectedColor = Color.white;
        startBtn.colors = cb;

        // Barras cinemáticas (Letterbox superior e inferior) que aparecen al dar clic en START GAME
        GameObject topBarObj = new GameObject("TopCinematicBar");
        topBarObj.transform.SetParent(openerRoot.transform, false);
        RectTransform topBarRect = topBarObj.AddComponent<RectTransform>();
        topBarRect.anchorMin = new Vector2(0f, 1f);
        topBarRect.anchorMax = new Vector2(1f, 1f);
        topBarRect.pivot = new Vector2(0.5f, 1f);
        topBarRect.anchoredPosition = Vector2.zero;
        topBarRect.sizeDelta = new Vector2(0f, 0f);
        Image topBarImg = topBarObj.AddComponent<Image>();
        topBarImg.color = new Color(0.04f, 0.01f, 0.05f, 0.95f);
        topBarImg.raycastTarget = false;

        GameObject bottomBarObj = new GameObject("BottomCinematicBar");
        bottomBarObj.transform.SetParent(openerRoot.transform, false);
        RectTransform bottomBarRect = bottomBarObj.AddComponent<RectTransform>();
        bottomBarRect.anchorMin = new Vector2(0f, 0f);
        bottomBarRect.anchorMax = new Vector2(1f, 0f);
        bottomBarRect.pivot = new Vector2(0.5f, 0f);
        bottomBarRect.anchoredPosition = Vector2.zero;
        bottomBarRect.sizeDelta = new Vector2(0f, 0f);
        Image bottomBarImg = bottomBarObj.AddComponent<Image>();
        bottomBarImg.color = new Color(0.04f, 0.01f, 0.05f, 0.95f);
        bottomBarImg.raycastTarget = false;

        // Velo suave (SoftTransitionVeil) para la transición tenue al comenzar el Nivel 1
        GameObject veilObj = new GameObject("SoftTransitionVeil");
        veilObj.transform.SetParent(openerRoot.transform, false);
        RectTransform veilRect = veilObj.AddComponent<RectTransform>();
        veilRect.anchorMin = Vector2.zero;
        veilRect.anchorMax = Vector2.one;
        veilRect.offsetMin = Vector2.zero;
        veilRect.offsetMax = Vector2.zero;
        Image veilImg = veilObj.AddComponent<Image>();
        veilImg.color = new Color(0.07f, 0.03f, 0.09f, 0f);
        veilImg.raycastTarget = false;

        // Objeto controlador TitleScreenOpenerManager con sus dos AudioSources (Música Chill + SFX Start Game)
        GameObject openerMgrObj = GameObject.Find("TitleScreenOpenerManager");
        if (openerMgrObj != null)
            Object.DestroyImmediate(openerMgrObj);

        openerMgrObj = new GameObject("TitleScreenOpenerManager");
        AudioSource musicSource = openerMgrObj.AddComponent<AudioSource>();
        musicSource.playOnAwake = false;
        musicSource.loop = true;
        musicSource.spatialBlend = 0f;
        musicSource.volume = 0.65f;
        musicSource.clip = bgmChillClip;

        AudioSource sfxSource = openerMgrObj.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;
        sfxSource.loop = false;
        sfxSource.spatialBlend = 0f;
        sfxSource.volume = 0.95f;

        TitleScreenOpener openerScript = openerMgrObj.AddComponent<TitleScreenOpener>();
        openerScript.openerRootPanel = openerRoot;
        openerScript.artworkCanvasGroup = artCg;
        openerScript.artworkRectTransform = hdArtRect;
        openerScript.startGameButton = startBtn;
        openerScript.startGameButtonImage = startBtnImg;
        openerScript.lanternGlowImage = glowImg;
        openerScript.softTransitionOverlay = veilImg;
        openerScript.topCinematicBar = topBarRect;
        openerScript.bottomCinematicBar = bottomBarRect;
        openerScript.musicAudioSource = musicSource;
        openerScript.sfxAudioSource = sfxSource;
        openerScript.musicaChillFondo = bgmChillClip;
        openerScript.sonidoStartGame = sfxStartClip;

        UnityEventTools.AddPersistentListener(startBtn.onClick, openerScript.AlPresionarStartGame);

        EditorUtility.SetDirty(openerRoot);
        EditorUtility.SetDirty(openerMgrObj);
    }

    private static void CreateOrLoadOpenerAudioClips(
        out AudioClip bgmChillClip,
        out AudioClip sfxStartClip)
    {
        if (!AssetDatabase.IsValidFolder("Assets/Audio"))
            AssetDatabase.CreateFolder("Assets", "Audio");

        if (!File.Exists(BgmChillOpenerPath))
        {
            WriteChillVineyardMusicWav(BgmChillOpenerPath);
            AssetDatabase.ImportAsset(BgmChillOpenerPath, ImportAssetOptions.ForceSynchronousImport);
        }

        if (!File.Exists(SfxStartGamePath))
        {
            WriteStartGameCinematicSfxWav(SfxStartGamePath);
            AssetDatabase.ImportAsset(SfxStartGamePath, ImportAssetOptions.ForceSynchronousImport);
        }

        bgmChillClip = AssetDatabase.LoadAssetAtPath<AudioClip>(BgmChillOpenerPath);
        sfxStartClip = AssetDatabase.LoadAssetAtPath<AudioClip>(SfxStartGamePath);
    }

    /// <summary>
    /// Sintetiza una melodía chill acústica/ambiental de 16 segundos en bucle ("Atardecer en el Viñedo")
    /// con progresión cálida Fmaj7 -> Em7 -> Dm7 -> Cmaj7, arpegio de arpa suave y pad crepuscular.
    /// </summary>
    private static void WriteChillVineyardMusicWav(string filePath)
    {
        int sampleRate = 44100;
        float duration = 16.0f; // 4 acordes de 4.0s cada uno (bucle perfecto)
        int numSamples = Mathf.RoundToInt(sampleRate * duration);
        float[] buffer = new float[numSamples];

        // Progresión chill cálida (Fmaj7 -> Em7 -> Dm7 -> Cmaj7/G)
        float[][] chords = new float[][]
        {
            new float[] { 174.61f, 220.00f, 261.63f, 329.63f }, // Fmaj7 (F3, A3, C4, E4)
            new float[] { 164.81f, 196.00f, 246.94f, 293.66f }, // Em7   (E3, G3, B3, D4)
            new float[] { 146.83f, 174.61f, 220.00f, 261.63f }, // Dm7   (D3, F3, A3, C4)
            new float[] { 130.81f, 196.00f, 246.94f, 329.63f }  // Cmaj7 (C3, G3, B3, E4)
        };

        // Notas de la melodía de arpa/guitarra acústica suave (32 notas de 0.5s cada una)
        float[] arpNotes = new float[]
        {
            // Compás 1 (Fmaj7)
            261.63f, 329.63f, 392.00f, 523.25f, 392.00f, 329.63f, 440.00f, 329.63f,
            // Compás 2 (Em7)
            246.94f, 293.66f, 392.00f, 493.88f, 392.00f, 293.66f, 329.63f, 246.94f,
            // Compás 3 (Dm7)
            220.00f, 261.63f, 349.23f, 440.00f, 523.25f, 440.00f, 349.23f, 261.63f,
            // Compás 4 (Cmaj7)
            196.00f, 261.63f, 329.63f, 392.00f, 493.88f, 392.00f, 329.63f, 293.66f
        };

        // 1. Pad cálido crepuscular de fondo
        for (int c = 0; c < 4; c++)
        {
            int startSample = Mathf.RoundToInt(c * 4.0f * sampleRate);
            int chordSamples = Mathf.RoundToInt(4.0f * sampleRate);
            float[] chord = chords[c];

            for (int i = 0; i < chordSamples && (startSample + i) < numSamples; i++)
            {
                float t = (float)i / sampleRate;
                float env = Mathf.Sin((float)i / chordSamples * Mathf.PI); // Entrada y salida suave

                float padVal = 0f;
                for (int n = 0; n < chord.Length; n++)
                {
                    float freq = chord[n];
                    // Onda sinusoidal cálida con ligero vibrato lento
                    float vibrato = 1f + Mathf.Sin(2f * Mathf.PI * 3.5f * t + n) * 0.0015f;
                    padVal += Mathf.Sin(2f * Mathf.PI * freq * vibrato * t) * 0.11f;
                    padVal += Mathf.Sin(2f * Mathf.PI * (freq * 0.5f) * t) * 0.05f; // Sub-bajo cálido
                }

                buffer[startSample + i] += padVal * env;
            }
        }

        // 2. Arpegio acústico / arpa chill con eco suave (delay)
        float noteStep = 0.5f;
        float noteDuration = 1.35f; // Deja cola armónica entre notas
        for (int k = 0; k < arpNotes.Length; k++)
        {
            float freq = arpNotes[k];
            int noteStart = Mathf.RoundToInt(k * noteStep * sampleRate);
            int noteLen = Mathf.RoundToInt(noteDuration * sampleRate);

            for (int i = 0; i < noteLen; i++)
            {
                int idx = (noteStart + i) % numSamples; // Envuelve al inicio para loop perfecto
                float t = (float)i / sampleRate;

                // Envolvente de cuerda pulsada suave (ataque gentil de 18ms + decaimiento exponencial)
                float attack = Mathf.Clamp01(t / 0.018f);
                float decay = Mathf.Exp(-3.4f * t);
                float env = attack * decay;

                // Timbre acústico cálido (fundamental + 2º y 3er armónico suaves que se apagan rápido)
                float fundamental = Mathf.Sin(2f * Mathf.PI * freq * t);
                float harm2 = Mathf.Sin(2f * Mathf.PI * (freq * 2f) * t) * 0.28f * Mathf.Exp(-6f * t);
                float harm3 = Mathf.Sin(2f * Mathf.PI * (freq * 3f) * t) * 0.08f * Mathf.Exp(-10f * t);

                float pluck = (fundamental + harm2 + harm3) * env * 0.24f;
                buffer[idx] += pluck;

                // Eco suave a 375ms para sensación espacial chill
                int echoIdx = (idx + Mathf.RoundToInt(0.375f * sampleRate)) % numSamples;
                buffer[echoIdx] += pluck * 0.32f;
            }
        }

        // Guardar como WAV PCM 16-bit
        using (FileStream fs = new FileStream(filePath, FileMode.Create, FileAccess.Write))
        using (BinaryWriter bw = new BinaryWriter(fs))
        {
            int byteRate = sampleRate * 2;
            int dataSize = numSamples * 2;

            bw.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
            bw.Write(36 + dataSize);
            bw.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));
            bw.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
            bw.Write(16);
            bw.Write((short)1);
            bw.Write((short)1);
            bw.Write(sampleRate);
            bw.Write(byteRate);
            bw.Write((short)2);
            bw.Write((short)16);
            bw.Write(System.Text.Encoding.ASCII.GetBytes("data"));
            bw.Write(dataSize);

            for (int i = 0; i < numSamples; i++)
            {
                short s = (short)Mathf.Clamp(buffer[i] * 32767f, short.MinValue, short.MaxValue);
                bw.Write(s);
            }
        }
    }

    /// <summary>
    /// Sintetiza el efecto de sonido de seleccionar "START GAME" (acorde luminoso ascendente + destello cálido).
    /// </summary>
    private static void WriteStartGameCinematicSfxWav(string filePath)
    {
        int sampleRate = 44100;
        float duration = 1.35f;
        int numSamples = Mathf.RoundToInt(sampleRate * duration);
        float[] buffer = new float[numSamples];

        // Arpegio ascendente rápido de confirmación (F4 -> A4 -> C5 -> E5 -> G5)
        float[] chimeNotes = { 349.23f, 440.00f, 523.25f, 659.25f, 783.99f };
        float[] startTimes = { 0.00f, 0.06f, 0.12f, 0.18f, 0.25f };

        for (int n = 0; n < chimeNotes.Length; n++)
        {
            float freq = chimeNotes[n];
            int startSample = Mathf.RoundToInt(startTimes[n] * sampleRate);

            for (int i = startSample; i < numSamples; i++)
            {
                float t = (float)(i - startSample) / sampleRate;
                float attack = Mathf.Clamp01(t / 0.008f);
                float decay = Mathf.Exp(-3.2f * t);
                float sig = (Mathf.Sin(2f * Mathf.PI * freq * t) +
                             0.35f * Mathf.Sin(2f * Mathf.PI * freq * 2f * t) * Mathf.Exp(-5f * t))
                            * attack * decay * 0.26f;
                buffer[i] += sig;
            }
        }

        using (FileStream fs = new FileStream(filePath, FileMode.Create, FileAccess.Write))
        using (BinaryWriter bw = new BinaryWriter(fs))
        {
            int byteRate = sampleRate * 2;
            int dataSize = numSamples * 2;

            bw.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
            bw.Write(36 + dataSize);
            bw.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));
            bw.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
            bw.Write(16);
            bw.Write((short)1);
            bw.Write((short)1);
            bw.Write(sampleRate);
            bw.Write(byteRate);
            bw.Write((short)2);
            bw.Write((short)16);
            bw.Write(System.Text.Encoding.ASCII.GetBytes("data"));
            bw.Write(dataSize);

            for (int i = 0; i < numSamples; i++)
            {
                short s = (short)Mathf.Clamp(buffer[i] * 32767f, short.MinValue, short.MaxValue);
                bw.Write(s);
            }
        }
    }
}
#endif
