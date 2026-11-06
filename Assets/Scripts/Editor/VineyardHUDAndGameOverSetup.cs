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
    private const string SplashPrefabPath = "Assets/Prefabs/GrapeJuiceSplash.prefab";
    private const string MerlotPrefabPath = "Assets/Prefabs/MerlotGrape.prefab";
    private const string SeedPrefabPath = "Assets/Prefabs/GrapeSeed.prefab";

    // Ruta de la imagen de Game Over subida por el usuario
    private const string UploadedGameOverImagePath =
        @"C:\Users\marim\.gemini\antigravity\brain\d5bf5fd1-8896-404c-8613-58c9e72f9423\.user_uploaded\media_1793999725614_665cd2a4.jpg";

    static VineyardHUDAndGameOverSetup()
    {
        EditorApplication.delayCall += RunAutoSetupIfNeeded;
    }

    private static void RunAutoSetupIfNeeded()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        bool needsGameOverArt = AssetDatabase.LoadAssetAtPath<Sprite>(GameOverBgSpritePath) == null ||
                                AssetDatabase.LoadAssetAtPath<Sprite>(GameOverBtnSpritePath) == null;
        bool needsCanvasSlider = GameObject.Find("PlayerHealthSlider") == null;
        bool needsGameOver = Object.FindFirstObjectByType<GameOverManager>() == null;
        bool needsSplashPrefab = AssetDatabase.LoadAssetAtPath<ParticleSystem>(SplashPrefabPath) == null;

        if (needsGameOverArt || needsCanvasSlider || needsGameOver || needsSplashPrefab)
        {
            SetupCompleteHUDAndFeedback(silent: true);
        }
    }

    [MenuItem("Herramientas Vineyard/Configurar Barra 100 HP, Salpicadura de Uva y Game Over")]
    public static void SetupFromMenu()
    {
        SetupCompleteHUDAndFeedback(silent: false);
        EditorUtility.DisplayDialog(
            "Vineyard - Pantalla de Game Over y UI Listas",
            "¡Se configuró la nueva pantalla de Game Over con tu diseño!\n\n" +
            "• Fondo y marco con enredaderas y uva derrotada (Assets/UI/GameOverScreen.png)\n" +
            "• Botón interactivo 'JUGAR DE NUEVO' recortado de tu diseño y conectado a RestartCurrentLevel()\n" +
            "• Barra de Vida (100 HP) en la esquina superior izquierda\n" +
            "• Efecto de salpicadura de jugo de uva al derrotar enemigos.",
            "¡Excelente!"
        );
    }

    public static void SetupCompleteHUDAndFeedback(bool silent)
    {
        EnsureDirectories();

        Sprite heartSprite = CreateOrLoadPixelHeartSprite();
        Sprite barBgSprite = CreateOrLoadPixelBarBgSprite();
        Sprite barFillSprite = CreateOrLoadPixelBarFillSprite();
        ImportCustomGameOverSprites(out Sprite gameOverBgSprite, out Sprite gameOverBtnSprite);

        ParticleSystem splashPrefab = CreateOrUpdateJuiceSplashPrefab();
        UpdateMerlotAndSeedPrefabs(splashPrefab);

        CreateOrUpdateCanvasAndManagers(
            heartSprite,
            barBgSprite,
            barFillSprite,
            gameOverBgSprite,
            gameOverBtnSprite,
            out Slider healthSlider,
            out Image fillImage,
            out Text hpLabelText,
            out GameOverManager gameOverManager
        );

        // Configurar el Jugador con HealthSystem (100 HP)
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

            EditorUtility.SetDirty(hs);
            EditorUtility.SetDirty(playerObj);
        }

        // Asegurar que todas las uvas en escena tengan EnemyDeathFeedback con el Prefab asignado
        MerlotGrapeEnemy[] sceneGrapes = Object.FindObjectsByType<MerlotGrapeEnemy>(FindObjectsSortMode.None);
        foreach (MerlotGrapeEnemy grape in sceneGrapes)
        {
            EnemyDeathFeedback feedback = grape.GetComponent<EnemyDeathFeedback>();
            if (feedback == null)
                feedback = grape.gameObject.AddComponent<EnemyDeathFeedback>();

            feedback.juiceSplashPrefab = splashPrefab;
            EditorUtility.SetDirty(feedback);
        }

        if (!Application.isPlaying)
        {
            EditorSceneManager.MarkAllScenesDirty();
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static void EnsureDirectories()
    {
        if (!AssetDatabase.IsValidFolder(UIFolder))
            AssetDatabase.CreateFolder("Assets", "UI");
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            AssetDatabase.CreateFolder("Assets", "Prefabs");
    }

    /// <summary>
    /// Importa la pantalla de Game Over subida por el usuario y separa el botón "JUGAR DE NUEVO"
    /// como su propio Sprite interactivo para que brille y reaccione al hacer clic.
    /// </summary>
    private static void ImportCustomGameOverSprites(out Sprite bgSprite, out Sprite btnSprite)
    {
        if (File.Exists(UploadedGameOverImagePath))
        {
            byte[] rawBytes = File.ReadAllBytes(UploadedGameOverImagePath);
            Texture2D fullTex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            fullTex.LoadImage(rawBytes);

            int w = fullTex.width;   // 1024
            int h = fullTex.height;  // 572

            // Coordenadas del botón "JUGAR DE NUEVO" en coordenadas de Texture2D (origen abajo-izquierda):
            // X: 328..696 (ancho 368), Y: 128..196 (alto 68)
            int btnX = Mathf.RoundToInt(w * 0.3203f);
            int btnW = Mathf.RoundToInt(w * 0.3594f);
            int btnY = Mathf.RoundToInt(h * 0.2238f);
            int btnH = Mathf.RoundToInt(h * 0.1189f);

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
            Object.DestroyImmediate(btnTex);

            // 2. En la imagen de fondo, rellenar el hueco detrás del botón con el color morado oscuro interior (#211226)
            // para que cuando el botón interactivo se mueva/escale no se vea duplicado atrás
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
            Object.DestroyImmediate(fullTex);

            AssetDatabase.ImportAsset(GameOverBgSpritePath, ImportAssetOptions.ForceUpdate);
            AssetDatabase.ImportAsset(GameOverBtnSpritePath, ImportAssetOptions.ForceUpdate);

            ConfigureHighResUISprite(GameOverBgSpritePath);
            ConfigureHighResUISprite(GameOverBtnSpritePath);
        }

        bgSprite = AssetDatabase.LoadAssetAtPath<Sprite>(GameOverBgSpritePath);
        btnSprite = AssetDatabase.LoadAssetAtPath<Sprite>(GameOverBtnSpritePath);
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

    private static void UpdateMerlotAndSeedPrefabs(ParticleSystem splashPrefab)
    {
        GameObject merlotPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MerlotPrefabPath);
        if (merlotPrefab != null)
        {
            EnemyDeathFeedback edf = merlotPrefab.GetComponent<EnemyDeathFeedback>();
            if (edf == null)
                edf = merlotPrefab.AddComponent<EnemyDeathFeedback>();

            edf.juiceSplashPrefab = splashPrefab;
            PrefabUtility.SavePrefabAsset(merlotPrefab);
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
        out Slider healthSlider,
        out Image fillImage,
        out Text hpLabelText,
        out GameOverManager gameOverManager)
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
        hpLabelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        hpLabelText.text = "HP: 100/100";
        hpLabelText.fontSize = 13;
        hpLabelText.fontStyle = FontStyle.Bold;
        hpLabelText.color = new Color(0.98f, 0.90f, 0.74f, 1f);
        Shadow textShadow = hpTextObj.AddComponent<Shadow>();
        textShadow.effectColor = new Color(0.12f, 0.04f, 0.08f, 0.95f);
        textShadow.effectDistance = new Vector2(1.5f, -1.5f);

        // ====================================================================
        // 2. PANTALLA DE GAME OVER CON LA IMAGEN SUBIDA Y BOTÓN FUNCIONAL
        // ====================================================================
        GameObject goPanel = new GameObject("GameOverPanel");
        goPanel.transform.SetParent(canvasObj.transform, false);
        RectTransform goPanelRect = goPanel.AddComponent<RectTransform>();
        goPanelRect.anchorMin = Vector2.zero;
        goPanelRect.anchorMax = Vector2.one;
        goPanelRect.offsetMin = Vector2.zero;
        goPanelRect.offsetMax = Vector2.zero;

        // Fondo base oscuro (#252229 igual al borde de tu imagen para cubrir cualquier relación de aspecto)
        Image backdropImg = goPanel.AddComponent<Image>();
        backdropImg.color = new Color(0.145f, 0.133f, 0.161f, 0.98f);
        CanvasGroup cg = goPanel.AddComponent<CanvasGroup>();

        // Contenedor con AspectRatioFitter 1024:572 para que el marco y el botón coincidan al milímetro
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
        fitter.aspectRatio = 1024f / 572f;

        // Botón interactivo "JUGAR DE NUEVO" ubicado exactamente en las coordenadas del diseño (32.03%..67.97% X, 22.38%..34.27% Y)
        GameObject btnObj = new GameObject("RestartButton");
        btnObj.transform.SetParent(artContainer.transform, false);
        RectTransform btnRect = btnObj.AddComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(0.3203f, 0.2238f);
        btnRect.anchorMax = new Vector2(0.6797f, 0.3427f);
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

        // Iniciar oculto hasta que la vida del jugador llegue a 0
        goPanel.SetActive(false);

        EditorUtility.SetDirty(canvasObj);
        EditorUtility.SetDirty(gomObj);
    }
}
#endif
