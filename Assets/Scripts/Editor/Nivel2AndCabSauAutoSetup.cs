#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

[InitializeOnLoad]
public static class Nivel2AndCabSauAutoSetup
{
    private const string CabSauSpriteFolder = "Assets/Sprites/Enemigos/CabSau";
    private const string CabSauAnimFolder = "Assets/Animations/CabSau";
    private const string CabSauControllerPath = "Assets/Animations/CabSau/CabSauController.controller";
    private const string CabSauPrefabPath = "Assets/Prefabs/EnemigoCabSau.prefab";

    private const string PropsFolder = "Assets/Sprites/Nivel2Props";
    private const string TilesFolder = "Assets/Tiles/Nivel2";

    private const string BarrelSpritePath = "Assets/Sprites/Nivel2Props/BarrilVino.png";
    private const string CrateSpritePath = "Assets/Sprites/Nivel2Props/CajaMadera.png";
    private const string ArchSpritePath = "Assets/Sprites/Nivel2Props/ArcoPiedraBodega.png";
    private const string LadderSpritePath = "Assets/Sprites/Nivel2Props/EscaleraMadera.png";
    private const string AcidSurfaceSpritePath = "Assets/Sprites/Nivel2Props/CharcoAcidoSuperficie.png";
    private const string AcidDeepSpritePath = "Assets/Sprites/Nivel2Props/CharcoAcidoProfundo.png";
    private const string WoodPlatformSpritePath = "Assets/Sprites/Nivel2Props/PasarelaMadera.png";
    private const string StoneTopSpritePath = "Assets/Sprites/Nivel2Props/PiedraGrisSuperficie.png";
    private const string StoneDeepSpritePath = "Assets/Sprites/Nivel2Props/PiedraGrisProfunda.png";
    private const string BrickWallSpritePath = "Assets/Sprites/Nivel2Props/MuroLadrilloBodega.png";
    private const string StonePillarSpritePath = "Assets/Sprites/Nivel2Props/PilarPiedraBodega.png";

    private const string TileStoneTopPath = "Assets/Tiles/Nivel2/Tile_PiedraGrisSuperficie.asset";
    private const string TileStoneDeepPath = "Assets/Tiles/Nivel2/Tile_PiedraGrisProfunda.asset";
    private const string TileWoodWalkwayPath = "Assets/Tiles/Nivel2/Tile_PasarelaMadera.asset";
    private const string TileBrickWallPath = "Assets/Tiles/Nivel2/Tile_MuroLadrilloBodega.asset";
    private const string TileStonePillarPath = "Assets/Tiles/Nivel2/Tile_PilarPiedraBodega.asset";
    private const string TileLadderPath = "Assets/Tiles/Nivel2/Tile_EscaleraMano.asset";
    private const string TileAcidSurfacePath = "Assets/Tiles/Nivel2/Tile_AcidoSuperficie.asset";
    private const string TileAcidDeepPath = "Assets/Tiles/Nivel2/Tile_AcidoProfundo.asset";

    private const string SfxCabSauAlertPath = "Assets/Audio/SFX_CabSauAlerta.wav";
    private const string SfxCabSauHitPath = "Assets/Audio/SFX_CabSauGolpe.wav";
    private const string SfxCabSauDeathPath = "Assets/Audio/SFX_CabSauMuerte.wav";
    private const string SfxAcidSplashPath = "Assets/Audio/SFX_AcidoBurbuja.wav";

    private const string Nivel1ScenePath = "Assets/Scenes/nivel1nocompleto.unity";
    private const string Nivel2ScenePath = "Assets/Scenes/Nivel2.unity";
    private const string CorazonPrefabPath = "Assets/Prefabs/CorazonVida.prefab";
    private const string EnemigoUvaPrefabPath = "Assets/Prefabs/EnemigoUva.prefab";
    private const string MetaPrefabPath = "Assets/Prefabs/MetaNivel.prefab";

    private const string ExtractedCabSauObjectsDir =
        @"C:\Users\marim\.gemini\antigravity\brain\d5bf5fd1-8896-404c-8613-58c9e72f9423\scratch\cabsau\objects";

    private const string SetupVersionKey = "Vineyard_Nivel2_CabSau_Version";
    private const int CurrentVersion = 3;

    static Nivel2AndCabSauAutoSetup()
    {
        EditorApplication.delayCall += RunAutoSetupIfNeeded;
    }

    private static void RunAutoSetupIfNeeded()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        bool needsVersion = EditorPrefs.GetInt(SetupVersionKey, 0) < CurrentVersion;
        bool needsCabSauPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CabSauPrefabPath) == null;
        bool needsNivel2Scene = !File.Exists(Nivel2ScenePath);

        if (needsVersion || needsCabSauPrefab || needsNivel2Scene)
        {
            BuildCabSauAndNivel2(silent: true, openNivel2AtEnd: true);
        }
    }

    [MenuItem("Herramientas Vineyard/Construir Nivel 2 y Enemigo Cab. Sau")]
    public static void BuildFromMenu()
    {
        BuildCabSauAndNivel2(silent: false, openNivel2AtEnd: true);
        EditorUtility.DisplayDialog(
            "Vineyard - Nivel 2 (Instalaciones de la Bodega) Listo",
            "¡Se ha reconstruido por completo la escena Nivel2 desde cero siguiendo las 4 reglas arquitectónicas!\n\n" +
            "1. Limpieza Total: Tilemaps (TilePiso, Structures, Elements) vaciados y Enemigos_Nivel1 eliminado.\n" +
            "2. Terreno de Instalaciones (X = 0 a 120): Suelo de piedra gris cincelada, muros de ladrillo, pilares, arcos y pasarelas de madera (sin colinas de pasto).\n" +
            "3. Tres Zonas Conectadas:\n" +
            "   • Zona 1 (Patio de Entrada, X = 0 a 35): Suelo plano de piedra, plataformas en Y=3 y Y=6 con escaleras de mano y barriles apilados.\n" +
            "   • Zona 2 (Fosos de Fermentación, X = 35 a 80): 3 fosos hundidos con ácido verde + LluviaAcida, pasarelas elevadas, arcos y 1er Corazón en plataforma alta.\n" +
            "   • Zona 3 (Almacén Final, X = 80 a 120): Estructura de 2 pisos con cajas, pasarelas superiores, 2do Corazón y MetaNivel2 en X = 115.\n" +
            "4. Enemigos Nivel 2: 7 EnemigoCabSau con DetectorBorde apuntando a capa Suelo y 2 EnemigoUva en puntos elevados.",
            "¡Excelente!"
        );
    }

    [MenuItem("Herramientas Vineyard/Abrir Escena Nivel 2 (Instalaciones del Viñedo)")]
    public static void OpenNivel2Menu()
    {
        if (!File.Exists(Nivel2ScenePath))
        {
            BuildCabSauAndNivel2(silent: true, openNivel2AtEnd: true);
            return;
        }

        EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
        EditorSceneManager.OpenScene(Nivel2ScenePath, OpenSceneMode.Single);
    }

    [MenuItem("Herramientas Vineyard/Abrir Escena Nivel 1 (Entrada del Viñedo)")]
    public static void OpenNivel1Menu()
    {
        if (File.Exists(Nivel1ScenePath))
        {
            EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
            EditorSceneManager.OpenScene(Nivel1ScenePath, OpenSceneMode.Single);
        }
    }

    public static void BuildCabSauAndNivel2(bool silent, bool openNivel2AtEnd)
    {
        EnsureDirectories();

        // 1. Generar SFX para Cab. Sau y charcos de ácido
        GenerateCabSauAndAcidAudio(
            out AudioClip alertClip,
            out AudioClip hitClip,
            out AudioClip deathClip,
            out AudioClip acidClip
        );

        // 2. Importar los 13 sprites de Cab. Sau
        Sprite[] cabSauSprites = ImportCabSauSprites();

        // 3. Crear AnimatorController (con bool "EstaCorriendo" y trigger "Hurt") + AnimationClips
        AnimatorController controller = CreateCabSauAnimatorController(cabSauSprites);

        // 4. Crear Prefab EnemigoCabSau.prefab (con hijo DetectorBorde apuntando a capa Suelo)
        GameObject cabSauPrefab = CreateOrUpdateCabSauPrefab(
            cabSauSprites,
            controller,
            alertClip,
            hitClip,
            deathClip
        );

        // 5. Crear sprites y Tile assets 16x16 de las instalaciones de la bodega (piedra gris, ladrillo, madera, escaleras, barriles, cajas, arcos, ácido)
        CreateWinerySpritesAndTiles(
            out Sprite barrelSprite,
            out Sprite crateSprite,
            out Sprite archSprite,
            out Sprite ladderSprite,
            out Sprite acidSurfaceSprite,
            out Sprite woodPlatformSprite,
            out Tile tileStoneTop,
            out Tile tileStoneDeep,
            out Tile tileWoodWalkway,
            out Tile tileBrickWall,
            out Tile tileStonePillar,
            out Tile tileLadder,
            out Tile tileAcidSurface,
            out Tile tileAcidDeep
        );

        // 6. Construir la escena Assets/Scenes/Nivel2.unity limpiando el terreno de Nivel 1 y pintando las 3 Zonas nuevas (X = 0 a 120)
        ConstructNivel2Scene(
            cabSauPrefab,
            barrelSprite,
            crateSprite,
            archSprite,
            ladderSprite,
            acidSurfaceSprite,
            woodPlatformSprite,
            tileStoneTop,
            tileStoneDeep,
            tileWoodWalkway,
            tileBrickWall,
            tileStonePillar,
            tileLadder,
            tileAcidSurface,
            tileAcidDeep,
            acidClip,
            openNivel2AtEnd
        );

        // 7. Registrar Nivel 1 y Nivel 2 en Build Settings
        RegisterScenesInBuildSettings();

        EditorPrefs.SetInt(SetupVersionKey, CurrentVersion);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static void EnsureDirectories()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Sprites"))
            AssetDatabase.CreateFolder("Assets", "Sprites");
        if (!AssetDatabase.IsValidFolder("Assets/Sprites/Enemigos"))
            AssetDatabase.CreateFolder("Assets/Sprites", "Enemigos");
        if (!AssetDatabase.IsValidFolder(CabSauSpriteFolder))
            AssetDatabase.CreateFolder("Assets/Sprites/Enemigos", "CabSau");
        if (!AssetDatabase.IsValidFolder(PropsFolder))
            AssetDatabase.CreateFolder("Assets/Sprites", "Nivel2Props");

        if (!AssetDatabase.IsValidFolder("Assets/Tiles"))
            AssetDatabase.CreateFolder("Assets", "Tiles");
        if (!AssetDatabase.IsValidFolder(TilesFolder))
            AssetDatabase.CreateFolder("Assets/Tiles", "Nivel2");

        if (!AssetDatabase.IsValidFolder("Assets/Animations"))
            AssetDatabase.CreateFolder("Assets", "Animations");
        if (!AssetDatabase.IsValidFolder(CabSauAnimFolder))
            AssetDatabase.CreateFolder("Assets/Animations", "CabSau");

        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            AssetDatabase.CreateFolder("Assets", "Prefabs");
        if (!AssetDatabase.IsValidFolder("Assets/Audio"))
            AssetDatabase.CreateFolder("Assets", "Audio");
    }

    private static string GetExtractedCabSauPngPath(int idx)
    {
        string suffix = (idx == 1) ? "" : $"_{idx}";
        return Path.Combine(
            ExtractedCabSauObjectsDir,
            $"16x16_pixel_art_sprite_sheet_o{suffix}",
            "rotations",
            $"16x16_pixel_art_sprite_sheet_o{suffix}.png"
        );
    }

    private static Sprite[] ImportCabSauSprites()
    {
        Sprite[] sprites = new Sprite[13];

        for (int i = 1; i <= 13; i++)
        {
            string targetPath = $"{CabSauSpriteFolder}/cabsau_{i}.png";
            string sourcePath = GetExtractedCabSauPngPath(i);

            if (File.Exists(sourcePath))
            {
                File.Copy(sourcePath, targetPath, overwrite: true);
            }
            else if (!File.Exists(targetPath))
            {
                CreateFallbackCabSauTexture(targetPath, i);
            }

            AssetDatabase.ImportAsset(targetPath, ImportAssetOptions.ForceSynchronousImport);
            ConfigurePixelArtSprite(targetPath, 26f);
            sprites[i - 1] = AssetDatabase.LoadAssetAtPath<Sprite>(targetPath);
        }

        return sprites;
    }

    private static void ConfigurePixelArtSprite(string assetPath, float pixelsPerUnit)
    {
        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null) return;

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = pixelsPerUnit;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.SaveAndReimport();
    }

    private static AnimatorController CreateCabSauAnimatorController(Sprite[] sprites)
    {
        if (sprites == null || sprites.Length < 13)
            return null;

        Sprite[] patrolFrames = new Sprite[]
        {
            sprites[4], sprites[5], sprites[7], sprites[11], sprites[9], sprites[3]
        };
        AnimationClip patrolClip = CreateSpriteAnimationClip(
            $"{CabSauAnimFolder}/CabSau_Patrulla.anim",
            patrolFrames,
            frameRate: 7f,
            loop: true
        );

        Sprite[] runFrames = new Sprite[]
        {
            sprites[0], sprites[1], sprites[2], sprites[6], sprites[8], sprites[10], sprites[12]
        };
        AnimationClip runClip = CreateSpriteAnimationClip(
            $"{CabSauAnimFolder}/CabSau_Correr.anim",
            runFrames,
            frameRate: 14f,
            loop: true
        );

        Sprite[] hurtFrames = new Sprite[] { sprites[0], sprites[3] };
        AnimationClip hurtClip = CreateSpriteAnimationClip(
            $"{CabSauAnimFolder}/CabSau_Hurt.anim",
            hurtFrames,
            frameRate: 8f,
            loop: false
        );

        if (File.Exists(CabSauControllerPath))
        {
            AssetDatabase.DeleteAsset(CabSauControllerPath);
        }

        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(CabSauControllerPath);
        controller.AddParameter("EstaCorriendo", AnimatorControllerParameterType.Bool);
        controller.AddParameter("Hurt", AnimatorControllerParameterType.Trigger);

        AnimatorStateMachine rootSm = controller.layers[0].stateMachine;

        AnimatorState patrolState = rootSm.AddState("CabSau_Patrulla", new Vector3(260, 60, 0));
        patrolState.motion = patrolClip;
        rootSm.defaultState = patrolState;

        AnimatorState runState = rootSm.AddState("CabSau_Correr", new Vector3(260, 190, 0));
        runState.motion = runClip;

        AnimatorState hurtState = rootSm.AddState("CabSau_Hurt", new Vector3(520, 120, 0));
        hurtState.motion = hurtClip;

        AnimatorStateTransition toRun = patrolState.AddTransition(runState);
        toRun.hasExitTime = false;
        toRun.duration = 0f;
        toRun.AddCondition(AnimatorConditionMode.If, 0, "EstaCorriendo");

        AnimatorStateTransition toPatrol = runState.AddTransition(patrolState);
        toPatrol.hasExitTime = false;
        toPatrol.duration = 0f;
        toPatrol.AddCondition(AnimatorConditionMode.IfNot, 0, "EstaCorriendo");

        AnimatorStateTransition anyToHurt = rootSm.AddAnyStateTransition(hurtState);
        anyToHurt.hasExitTime = false;
        anyToHurt.duration = 0f;
        anyToHurt.AddCondition(AnimatorConditionMode.If, 0, "Hurt");

        AnimatorStateTransition hurtToPatrol = hurtState.AddTransition(patrolState);
        hurtToPatrol.hasExitTime = true;
        hurtToPatrol.exitTime = 1f;
        hurtToPatrol.duration = 0f;

        EditorUtility.SetDirty(controller);
        return controller;
    }

    private static AnimationClip CreateSpriteAnimationClip(string path, Sprite[] frames, float frameRate, bool loop)
    {
        AnimationClip clip = new AnimationClip { frameRate = frameRate };

        EditorCurveBinding spriteBinding = new EditorCurveBinding
        {
            type = typeof(SpriteRenderer),
            path = "",
            propertyName = "m_Sprite"
        };

        ObjectReferenceKeyframe[] keyFrames = new ObjectReferenceKeyframe[frames.Length];
        for (int i = 0; i < frames.Length; i++)
        {
            keyFrames[i] = new ObjectReferenceKeyframe
            {
                time = i / frameRate,
                value = frames[i]
            };
        }

        AnimationUtility.SetObjectReferenceCurve(clip, spriteBinding, keyFrames);

        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        if (File.Exists(path))
            AssetDatabase.DeleteAsset(path);

        AssetDatabase.CreateAsset(clip, path);
        return clip;
    }

    private static GameObject CreateOrUpdateCabSauPrefab(
        Sprite[] sprites,
        AnimatorController controller,
        AudioClip alertClip,
        AudioClip hitClip,
        AudioClip deathClip)
    {
        GameObject temp = new GameObject("EnemigoCabSau");

        try
        {
            temp.tag = "Enemy";
        }
        catch
        {
            // En caso de que el tag Enemy no exista
        }

        SpriteRenderer sr = temp.AddComponent<SpriteRenderer>();
        sr.sprite = (sprites != null && sprites.Length > 4) ? sprites[4] : null;
        sr.sortingOrder = 3;

        Rigidbody2D rb = temp.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 3f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        BoxCollider2D col = temp.AddComponent<BoxCollider2D>();
        col.size = new Vector2(1.18f, 1.26f);
        col.offset = new Vector2(0f, -0.06f);

        Animator anim = temp.AddComponent<Animator>();
        anim.runtimeAnimatorController = controller;

        AudioSource audioSrc = temp.AddComponent<AudioSource>();
        audioSrc.playOnAwake = false;
        audioSrc.spatialBlend = 0f;

        // Crear objeto hijo DetectorBorde apuntando hacia abajo a la capa Suelo
        GameObject detectorObj = new GameObject("DetectorBorde");
        detectorObj.transform.SetParent(temp.transform);
        detectorObj.transform.localPosition = new Vector3(0.72f, -0.28f, 0f);

        int sueloLayer = LayerMask.NameToLayer("Suelo");
        int sueloMask = sueloLayer >= 0 ? (1 << sueloLayer) : (1 << 6);

        EnemigoCabSau cabSau = temp.AddComponent<EnemigoCabSau>();
        cabSau.maxHealth = 3;
        cabSau.currentHealth = 3;
        cabSau.puntosAlDerrotar = 150;
        cabSau.velocidadPatrulla = 1.65f;
        cabSau.distanciaPatrulla = 3.8f;
        cabSau.detectorBorde = detectorObj.transform;
        cabSau.detectarBordes = true;
        cabSau.frenarEnBordesDuranteSprint = true;
        cabSau.distanciaSensorBordeX = 0.72f;
        cabSau.longitudRayoSuelo = 1.65f;
        cabSau.distanciaSensorPared = 0.78f;
        cabSau.capaSuelo = sueloMask;
        cabSau.rangoDeteccion = 6.8f;
        cabSau.diferenciaAlturaMaxima = 2.1f;
        cabSau.velocidadSprint = 5.4f;
        cabSau.parametroAnimatorCorriendo = "EstaCorriendo";
        cabSau.danoContacto = 22f;
        cabSau.tiempoEntreGolpes = 0.90f;
        cabSau.recibirDanoAlSaltarEncima = true;
        cabSau.danoPorSaltoEncima = 2;
        cabSau.fuerzaReboteJugador = 10.2f;
        cabSau.audioSource = audioSrc;
        cabSau.sonidoDetectarJugador = alertClip;
        cabSau.sonidoRecibirDano = hitClip;
        cabSau.sonidoMuerte = deathClip;

        if (sprites != null && sprites.Length >= 13)
        {
            cabSau.spritesPatrulla = new Sprite[]
            {
                sprites[4], sprites[5], sprites[7], sprites[11], sprites[9], sprites[3]
            };
            cabSau.spriteAlertaFuria = sprites[0];
            cabSau.spritesSprint = new Sprite[]
            {
                sprites[1], sprites[2], sprites[6], sprites[8], sprites[10], sprites[12]
            };
        }

        EnemyDeathFeedback feedback = temp.AddComponent<EnemyDeathFeedback>();
        feedback.dropletCount = 28;
        feedback.primaryJuiceColor = new Color(0.76f, 0.08f, 0.22f, 1f);   // Rojo Rubí Cabernet Sauvignon
        feedback.secondaryJuiceColor = new Color(0.95f, 0.24f, 0.36f, 1f); // Carmesí Brillante

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(temp, CabSauPrefabPath);
        UnityEngine.Object.DestroyImmediate(temp);
        return prefab;
    }

    // =========================================================================
    // CONSTRUCCIÓN DEL ESCENARIO DE NIVEL 2 (INSTALACIONES INTERIORES / PATIO DE PIEDRA DE LA BODEGA)
    // =========================================================================
    private static void ConstructNivel2Scene(
        GameObject cabSauPrefab,
        Sprite barrelSprite,
        Sprite crateSprite,
        Sprite archSprite,
        Sprite ladderSprite,
        Sprite acidSurfaceSprite,
        Sprite woodPlatformSprite,
        Tile tileStoneTop,
        Tile tileStoneDeep,
        Tile tileWoodWalkway,
        Tile tileBrickWall,
        Tile tileStonePillar,
        Tile tileLadder,
        Tile tileAcidSurface,
        Tile tileAcidDeep,
        AudioClip acidClip,
        bool keepNivel2Open)
    {
        if (!File.Exists(Nivel1ScenePath))
        {
            Debug.LogWarning($"[Nivel2AndCabSauAutoSetup] No se encontró {Nivel1ScenePath}.");
            return;
        }

        string previousScenePath = EditorSceneManager.GetActiveScene().path;
        if (EditorSceneManager.GetActiveScene().isDirty)
        {
            EditorSceneManager.SaveOpenScenes();
        }

        if (File.Exists(Nivel2ScenePath))
        {
            AssetDatabase.DeleteAsset(Nivel2ScenePath);
        }
        AssetDatabase.CopyAsset(Nivel1ScenePath, Nivel2ScenePath);
        AssetDatabase.ImportAsset(Nivel2ScenePath, ImportAssetOptions.ForceSynchronousImport);

        Scene scene2 = EditorSceneManager.OpenScene(Nivel2ScenePath, OpenSceneMode.Single);

        // ---------------------------------------------------------------------
        // REGLA 1: LIMPIEZA INICIAL
        // - Borrar todo el contenido pintado en los Tilemaps (TilePiso, Structures, Elements)
        // - Eliminar Enemigos_Nivel1 (e ItemsCurativos_Nivel1)
        // - Mantener intactos a Character, cámara, fondo1, Canvas y scripts de gestión
        // ---------------------------------------------------------------------
        PerformInitialCleanup(out Tilemap tilePiso, out Tilemap structuresMap, out Tilemap elementsMap);

        // ---------------------------------------------------------------------
        // REGLA 2 Y 3: TERRENO DE INSTALACIONES (SIN COLINAS DE PASTO, X = 0 a X = 120)
        // - Zona 1 (Patio de Entrada, X = 0 a 35)
        // - Zona 2 (Fosos de Fermentación, X = 35 a 80)
        // - Zona 3 (Almacén Final, X = 80 a 120)
        // ---------------------------------------------------------------------
        BuildWineryZonesAndTilemaps(
            tilePiso,
            structuresMap,
            elementsMap,
            barrelSprite,
            crateSprite,
            archSprite,
            ladderSprite,
            acidSurfaceSprite,
            woodPlatformSprite,
            tileStoneTop,
            tileStoneDeep,
            tileWoodWalkway,
            tileBrickWall,
            tileStonePillar,
            tileLadder,
            tileAcidSurface,
            tileAcidDeep,
            acidClip
        );

        // ---------------------------------------------------------------------
        // COLOCAR LOS 2 CORAZONES DE VIDA EN ZONA 2 (PLATAFORMA ALTA) Y ZONA 3 (PASARELA SUPERIOR)
        // Y CONFIGURAR METANIVEL2 EN X = 115
        // ---------------------------------------------------------------------
        SpawnNivel2HeartsAndGoal();

        // ---------------------------------------------------------------------
        // REGLA 4: ENEMIGOS EXCLUSIVOS DE NIVEL 2
        // - 7 instancias de EnemigoCabSau con DetectorBorde apuntando a la capa Suelo
        // - 2 instancias de EnemigoUva en puntos elevados
        // ---------------------------------------------------------------------
        SpawnNivel2ExclusiveEnemies(cabSauPrefab);

        EditorSceneManager.MarkSceneDirty(scene2);
        EditorSceneManager.SaveScene(scene2);

        if (!keepNivel2Open && !string.IsNullOrEmpty(previousScenePath) && previousScenePath != Nivel2ScenePath && File.Exists(previousScenePath))
        {
            EditorSceneManager.OpenScene(previousScenePath, OpenSceneMode.Single);
        }
    }

    private static void PerformInitialCleanup(
        out Tilemap tilePiso,
        out Tilemap structuresMap,
        out Tilemap elementsMap)
    {
        // 1. Quitar la pantalla Opener de inicio para que Nivel 2 arranque directamente en el escenario
        TitleScreenOpener opener = UnityEngine.Object.FindFirstObjectByType<TitleScreenOpener>();
        if (opener != null)
        {
            if (opener.openerRootPanel != null)
            {
                UnityEngine.Object.DestroyImmediate(opener.openerRootPanel);
            }
            UnityEngine.Object.DestroyImmediate(opener.gameObject);
        }

        // 2. Localizar y vaciar por completo TODOS los Tilemaps (TilePiso, Structures, Elements, ElementsNoCentral)
        GameObject tilePisoObj = GameObject.Find("TilePiso");
        GameObject structuresObj = GameObject.Find("Structures");
        GameObject elementsObj = GameObject.Find("Elements");

        tilePiso = tilePisoObj != null ? tilePisoObj.GetComponent<Tilemap>() : null;
        structuresMap = structuresObj != null ? structuresObj.GetComponent<Tilemap>() : null;
        elementsMap = elementsObj != null ? elementsObj.GetComponent<Tilemap>() : null;

        Tilemap[] allTilemaps = UnityEngine.Object.FindObjectsByType<Tilemap>(FindObjectsSortMode.None);
        for (int i = 0; i < allTilemaps.Length; i++)
        {
            if (allTilemaps[i] != null)
            {
                allTilemaps[i].ClearAllTiles();
                allTilemaps[i].CompressBounds();
                allTilemaps[i].RefreshAllTiles();
                EditorUtility.SetDirty(allTilemaps[i]);
            }
        }

        // Asegurar que TilePiso esté en la capa Suelo (Layer 6)
        int sueloLayer = LayerMask.NameToLayer("Suelo");
        if (sueloLayer < 0) sueloLayer = 6;
        if (tilePisoObj != null)
        {
            tilePisoObj.layer = sueloLayer;
        }

        // 3. Eliminar Enemigos_Nivel1, ItemsCurativos_Nivel1 y cualquier resto del Nivel 1
        string[] objectsToDelete = new string[]
        {
            "Enemigos_Nivel1",
            "Enemigos_UvasMerlot",
            "Enemigos_Nivel2",
            "Enemigos_Nivel2_Bodega",
            "ItemsCurativos_Nivel1",
            "Items_CorazonesVida",
            "ItemsCurativos_Nivel2",
            "InstalacionesBodega_Nivel2"
        };

        for (int i = 0; i < objectsToDelete.Length; i++)
        {
            GameObject obj = GameObject.Find(objectsToDelete[i]);
            if (obj != null)
            {
                UnityEngine.Object.DestroyImmediate(obj);
            }
        }

        // Eliminar cualquier uva o corazón suelto que hubiera quedado fuera del contenedor
        MerlotGrapeEnemy[] leftoverGrapes = UnityEngine.Object.FindObjectsByType<MerlotGrapeEnemy>(FindObjectsSortMode.None);
        for (int i = 0; i < leftoverGrapes.Length; i++)
        {
            if (leftoverGrapes[i] != null)
                UnityEngine.Object.DestroyImmediate(leftoverGrapes[i].gameObject);
        }

        CorazonVida[] leftoverHearts = UnityEngine.Object.FindObjectsByType<CorazonVida>(FindObjectsSortMode.None);
        for (int i = 0; i < leftoverHearts.Length; i++)
        {
            if (leftoverHearts[i] != null)
                UnityEngine.Object.DestroyImmediate(leftoverHearts[i].gameObject);
        }

        // 4. Posicionar al jugador (Character) y las cámaras al inicio de la Zona 1 (X = 2.5, Y = 1.25)
        GameObject character = GameObject.Find("Character");
        if (character != null)
        {
            character.transform.position = new Vector3(2.5f, 1.25f, 0f);
            EditorUtility.SetDirty(character.transform);
        }

        if (Camera.main != null)
        {
            Camera.main.transform.position = new Vector3(2.5f, 3.2f, -10f);
            EditorUtility.SetDirty(Camera.main.transform);
        }

        GameObject cmCam = GameObject.Find("CinemachineCamera");
        if (cmCam != null)
        {
            cmCam.transform.position = new Vector3(2.5f, 3.2f, -10f);
            EditorUtility.SetDirty(cmCam.transform);
        }

        // 5. Ajustar los límites del mapa para el nuevo recorrido (X = 0 a X = 120)
        GameObject limiteIzq = GameObject.Find("LimiteIzquierda");
        if (limiteIzq != null)
        {
            limiteIzq.transform.position = new Vector3(-0.5f, 0f, 0f);
            BoxCollider2D colIzq = limiteIzq.GetComponent<BoxCollider2D>();
            if (colIzq != null)
            {
                colIzq.offset = new Vector2(0f, 12f);
                colIzq.size = new Vector2(1f, 40f);
                EditorUtility.SetDirty(colIzq);
            }
            EditorUtility.SetDirty(limiteIzq.transform);
        }

        GameObject limiteAbajo = GameObject.Find("LimiteAbajo");
        if (limiteAbajo != null)
        {
            limiteAbajo.transform.position = new Vector3(60f, -8.5f, 0f);
            BoxCollider2D colAbajo = limiteAbajo.GetComponent<BoxCollider2D>();
            if (colAbajo != null)
            {
                colAbajo.offset = Vector2.zero;
                colAbajo.size = new Vector2(160f, 1.5f);
                EditorUtility.SetDirty(colAbajo);
            }
            EditorUtility.SetDirty(limiteAbajo.transform);
        }

        // Asegurar que los paneles de fondo (fondo1, etc.) cubran de X = -10 a X = 130
        string[] bgNames = new string[] { "fondo1", "fondo (1)", "fondo (2)", "fondo1 (3)", "fondo1 (4)" };
        float[] bgTargetX = new float[] { 5f, 35f, 65f, 95f, 122f };
        for (int i = 0; i < bgNames.Length; i++)
        {
            GameObject bg = GameObject.Find(bgNames[i]);
            if (bg != null)
            {
                bg.transform.position = new Vector3(bgTargetX[i], 6.5f, 0f);
                EditorUtility.SetDirty(bg.transform);
            }
        }
    }

    private static void BuildWineryZonesAndTilemaps(
        Tilemap tilePiso,
        Tilemap structuresMap,
        Tilemap elementsMap,
        Sprite barrelSprite,
        Sprite crateSprite,
        Sprite archSprite,
        Sprite ladderSprite,
        Sprite acidSurfaceSprite,
        Sprite woodPlatformSprite,
        Tile tileStoneTop,
        Tile tileStoneDeep,
        Tile tileWoodWalkway,
        Tile tileBrickWall,
        Tile tileStonePillar,
        Tile tileLadder,
        Tile tileAcidSurface,
        Tile tileAcidDeep,
        AudioClip acidClip)
    {
        GameObject root = new GameObject("InstalacionesBodega_Nivel2");
        int sueloLayer = LayerMask.NameToLayer("Suelo");
        if (sueloLayer < 0) sueloLayer = 6;

        // Intentar cargar también el tile de ladrillo/piedra original de la escena por si queremos alternar variaciones
        TileBase nativeBrickTile = LoadTileByGuid("fbd141dd64c146943ac4e3753e32c0f2") ?? tileBrickWall;
        TileBase nativeStoneBorderTile = LoadTileByGuid("4296d634b31561447ab748ab63061580") ?? tileStonePillar;

        // Muro de contención izquierdo del patio (X = -3 a -1)
        if (tilePiso != null)
        {
            PaintSolidStoneBlock(tilePiso, -3, -1, -4, 8, tileStoneTop, tileStoneDeep);
        }

        // =====================================================================
        // ZONA 1: PATIO DE ENTRADA (X = 0 a 35)
        // - Suelo plano de piedra (y = -4..-1, superficie en Y = 0.0)
        // - Muros de ladrillo de fondo en Structures (y = 0..5)
        // - Plataformas de madera a media altura (Y = 3 y Y = 6) conectadas con escaleras de mano
        // - Barriles apilados
        // =====================================================================
        if (tilePiso != null)
        {
            // Suelo plano de piedra de X = 0 a X = 35
            PaintSolidStoneBlock(tilePiso, 0, 35, -4, -1, tileStoneTop, tileStoneDeep);

            // Plataformas de madera a media altura en Y = 3 (X = 7..15 y X = 23..31)
            PaintHorizontalTileLine(tilePiso, 7, 15, 3, tileWoodWalkway);
            PaintHorizontalTileLine(tilePiso, 23, 31, 3, tileWoodWalkway);

            // Plataforma de madera superior en Y = 6 (X = 14..24)
            PaintHorizontalTileLine(tilePiso, 14, 24, 6, tileWoodWalkway);
        }

        if (structuresMap != null)
        {
            // Muros de ladrillo de fondo en el Patio de Entrada (y = 0..4) con pilares de piedra cada 7 bloques
            PaintWineryBackgroundWall(structuresMap, 0, 35, 0, 4, tileBrickWall, nativeBrickTile, tileStonePillar, pillarInterval: 7);
            // Muro posterior de soporte tras la plataforma Y = 6
            PaintWineryBackgroundWall(structuresMap, 14, 24, 5, 7, tileBrickWall, nativeBrickTile, tileStonePillar, pillarInterval: 5);
        }

        // Escaleras de mano conectando el suelo con Y = 3 y Y = 6 en Zona 1
        GameObject laddersParent = new GameObject("EscalerasDeMano_Nivel2");
        laddersParent.transform.SetParent(root.transform);

        CreateLadderColumn(elementsMap, laddersParent.transform, "Escalera_Z1_SubidaY3_Izq", xCell: 7, yBottom: 0, yTop: 3, tileLadder, ladderSprite);
        CreateLadderColumn(elementsMap, laddersParent.transform, "Escalera_Z1_SubidaY6_Izq", xCell: 14, yBottom: 4, yTop: 6, tileLadder, ladderSprite);
        CreateLadderColumn(elementsMap, laddersParent.transform, "Escalera_Z1_SubidaY6_Der", xCell: 24, yBottom: 4, yTop: 6, tileLadder, ladderSprite);
        CreateLadderColumn(elementsMap, laddersParent.transform, "Escalera_Z1_BajadaY3_Der", xCell: 31, yBottom: 0, yTop: 3, tileLadder, ladderSprite);

        // Barriles apilados en Zona 1
        GameObject propsParent = new GameObject("BarrilesYCajas_Nivel2");
        propsParent.transform.SetParent(root.transform);

        // Pirámide 1 de barriles apilados (X = 4.5 .. 5.5)
        CreateBarrelStack(propsParent.transform, "BarrilesApilados_Z1_Entrada", baseCenterX: 5.0f, groundY: 0.0f, barrelSprite, sueloLayer);
        // Pirámide 2 de barriles apilados en el centro del patio (X = 18.5 .. 19.5)
        CreateBarrelStack(propsParent.transform, "BarrilesApilados_Z1_Centro", baseCenterX: 19.0f, groundY: 0.0f, barrelSprite, sueloLayer);
        // Pirámide 3 de barriles apilados al final del Patio de Entrada (X = 33.0 .. 34.0)
        CreateBarrelStack(propsParent.transform, "BarrilesApilados_Z1_Salida", baseCenterX: 33.5f, groundY: 0.0f, barrelSprite, sueloLayer);

        // =====================================================================
        // ZONA 2: FOSOS DE FERMENTACIÓN (X = 35 a 80)
        // - 3 Fosos hundidos llenos de líquido ácido verde (X=38..46, X=53..62, X=69..76)
        // - LluviaAcida posicionada sobre la Zona 2 + tiles de ácido con daño (ZonaAcido.cs)
        // - Pasarelas de madera elevadas y arcos de piedra para saltar entre fosos
        // - 1 Corazón de vida en plataforma alta opcional (X = 55..60, Y = 6)
        // =====================================================================
        if (tilePiso != null)
        {
            // Muelles/pilares de piedra entre los 3 fosos hundidos
            PaintSolidStoneBlock(tilePiso, 35, 37, -4, -1, tileStoneTop, tileStoneDeep); // Borde inicio Zona 2
            PaintSolidStoneBlock(tilePiso, 47, 52, -4, -1, tileStoneTop, tileStoneDeep); // Pilar central 1
            PaintSolidStoneBlock(tilePiso, 63, 68, -4, -1, tileStoneTop, tileStoneDeep); // Pilar central 2
            PaintSolidStoneBlock(tilePiso, 77, 80, -4, -1, tileStoneTop, tileStoneDeep); // Borde salida hacia Zona 3

            // Fondo de piedra hundido de los 3 fosos de fermentación (y = -5..-4)
            PaintSolidStoneBlock(tilePiso, 38, 46, -5, -4, tileStoneTop, tileStoneDeep);
            PaintSolidStoneBlock(tilePiso, 53, 62, -5, -4, tileStoneTop, tileStoneDeep);
            PaintSolidStoneBlock(tilePiso, 69, 76, -5, -4, tileStoneTop, tileStoneDeep);

            // Pasarelas de madera elevadas sobre los fosos de ácido
            PaintHorizontalTileLine(tilePiso, 39, 45, 2, tileWoodWalkway); // Pasarela sobre Foso 1 (Y = 2)
            PaintHorizontalTileLine(tilePiso, 48, 51, 3, tileStoneTop);    // Plataforma de arco de piedra (Y = 3)
            PaintHorizontalTileLine(tilePiso, 54, 61, 3, tileWoodWalkway); // Pasarela ancha sobre Foso 2 (Y = 3)
            PaintHorizontalTileLine(tilePiso, 55, 60, 6, tileWoodWalkway); // Plataforma alta opcional del 1er Corazón (Y = 6)
            PaintHorizontalTileLine(tilePiso, 70, 75, 3, tileWoodWalkway); // Pasarela sobre Foso 3 (Y = 3)
        }

        if (structuresMap != null)
        {
            // Muros bajos y columnas de piedra detrás de los fosos de fermentación
            PaintWineryBackgroundWall(structuresMap, 35, 80, -3, 2, tileBrickWall, nativeBrickTile, tileStonePillar, pillarInterval: 6);
            PaintWineryBackgroundWall(structuresMap, 54, 61, 3, 5, tileBrickWall, nativeBrickTile, tileStonePillar, pillarInterval: 7);
        }

        // Escalera de mano hacia la plataforma alta opcional del 1er Corazón en Zona 2
        CreateLadderColumn(elementsMap, laddersParent.transform, "Escalera_Z2_PlataformaCorazon", xCell: 55, yBottom: 4, yTop: 6, tileLadder, ladderSprite);

        // Pintar tiles de ácido verde en Elements y crear los 3 triggers de daño ZonaAcido
        GameObject acidParent = new GameObject("FososAcidoFermentacion_Zona2");
        acidParent.transform.SetParent(root.transform);

        (int xStart, int xEnd, string name)[] fermentationPits = new (int, int, string)[]
        {
            (38, 46, "FosoFermentacion_1"),
            (53, 62, "FosoFermentacion_2"),
            (69, 76, "FosoFermentacion_3")
        };

        for (int i = 0; i < fermentationPits.Length; i++)
        {
            int x0 = fermentationPits[i].xStart;
            int x1 = fermentationPits[i].xEnd;

            if (elementsMap != null)
            {
                for (int x = x0; x <= x1; x++)
                {
                    elementsMap.SetTile(new Vector3Int(x, -3, 0), tileAcidDeep);
                    elementsMap.SetTile(new Vector3Int(x, -2, 0), tileAcidSurface);
                }
            }

            float width = (x1 - x0 + 1);
            float centerX = x0 + width * 0.5f;
            CreateAcidPitTriggerAndVFX(
                acidParent.transform,
                fermentationPits[i].name,
                new Vector2(centerX, -2.0f),
                widthTiles: x1 - x0 + 1,
                acidSurfaceSprite,
                acidClip
            );
        }

        // Posicionar el sistema LluviaAcida de la escena sobre los Fosos de Fermentación (Zona 2, X = 35..80)
        GameObject lluviaAcida = GameObject.Find("LluviaAcida");
        if (lluviaAcida != null)
        {
            lluviaAcida.transform.position = new Vector3(57.5f, 22f, 0f);
            EditorUtility.SetDirty(lluviaAcida.transform);
        }

        // =====================================================================
        // ZONA 3: ALMACÉN FINAL (X = 80 a 120)
        // - Estructura de dos pisos con suelo de piedra (X = 80..120, y = -4..-1)
        // - Segundo piso en Y = 4 (X = 83..94 y X = 98..109)
        // - Pasarela superior en Y = 7 (X = 92..102) con el 2do Corazón de Vida
        // - Cajas de madera, escaleras y MetaNivel2 en X = 115
        // =====================================================================
        if (tilePiso != null)
        {
            // Planta baja continua de piedra del Almacén Final (X = 80 a 120)
            PaintSolidStoneBlock(tilePiso, 80, 120, -4, -1, tileStoneTop, tileStoneDeep);

            // Muro de cierre derecho detrás de MetaNivel2 (X = 120..122)
            PaintSolidStoneBlock(tilePiso, 120, 122, -4, 9, tileStoneTop, tileStoneDeep);

            // Segundo piso del Almacén (Y = 4): dos alas amplias (X = 83..94 y X = 98..109)
            PaintHorizontalTileLine(tilePiso, 83, 94, 4, tileWoodWalkway);
            PaintHorizontalTileLine(tilePiso, 98, 109, 4, tileWoodWalkway);

            // Pasarela superior del Almacén (Y = 7): puente superior (X = 92..102) donde está el 2do Corazón
            PaintHorizontalTileLine(tilePiso, 92, 102, 7, tileWoodWalkway);
        }

        if (structuresMap != null)
        {
            // Muro interior de ladrillo de dos pisos del Almacén Final (X = 80..120, y = 0..8)
            PaintWineryBackgroundWall(structuresMap, 80, 119, 0, 8, tileBrickWall, nativeBrickTile, tileStonePillar, pillarInterval: 8);
        }

        // Escaleras de mano del Almacén Final (Zona 3)
        CreateLadderColumn(elementsMap, laddersParent.transform, "Escalera_Z3_SubidaPiso2_Izq", xCell: 83, yBottom: 0, yTop: 4, tileLadder, ladderSprite);
        CreateLadderColumn(elementsMap, laddersParent.transform, "Escalera_Z3_SubidaPasarelaY7", xCell: 93, yBottom: 5, yTop: 7, tileLadder, ladderSprite);
        CreateLadderColumn(elementsMap, laddersParent.transform, "Escalera_Z3_BajadaPiso2_Der", xCell: 109, yBottom: 0, yTop: 4, tileLadder, ladderSprite);

        // Cajas de madera (Cajas de vendimia) en la estructura de dos pisos de Zona 3
        CreateCrateCluster(propsParent.transform, "CajasMadera_Z3_Entrada", baseCenterX: 81.2f, groundY: 0.0f, crateSprite, sueloLayer);
        CreateCrateCluster(propsParent.transform, "CajasMadera_Z3_PlantaBajaCentro", baseCenterX: 96.0f, groundY: 0.0f, crateSprite, sueloLayer);
        CreateCrateCluster(propsParent.transform, "CajasMadera_Z3_SegundoPisoIzq", baseCenterX: 90.5f, groundY: 5.0f, crateSprite, sueloLayer);
        CreateCrateCluster(propsParent.transform, "CajasMadera_Z3_SegundoPisoDer", baseCenterX: 105.5f, groundY: 5.0f, crateSprite, sueloLayer);
        CreateBarrelStack(propsParent.transform, "BarrilesApilados_Z3_AntesDeMeta", baseCenterX: 111.5f, groundY: 0.0f, barrelSprite, sueloLayer);

        // Arcos de piedra arquitectónicos a lo largo de las 3 Zonas
        GameObject archesParent = new GameObject("ArcosDePiedra_Nivel2");
        archesParent.transform.SetParent(root.transform);

        (float x, float y)[] archSpots = new (float, float)[]
        {
            (3.0f, 1.4f),    // Arco de bienvenida en Zona 1
            (19.0f, 1.4f),   // Arco central en Zona 1
            (42.0f, 3.4f),   // Arco sobre pasarela del Foso 1 en Zona 2
            (49.5f, 1.4f),   // Arco del pilar central 1 en Zona 2
            (65.5f, 1.4f),   // Arco del pilar central 2 en Zona 2
            (72.5f, 4.4f),   // Arco sobre pasarela del Foso 3 en Zona 2
            (86.0f, 1.4f),   // Arco inferior del Almacén Final (Zona 3)
            (97.0f, 8.4f),   // Arco de la pasarela superior del 2do Corazón (Zona 3)
            (115.0f, 1.4f)   // Arco triunfal enmarcando MetaNivel2 en X = 115
        };

        for (int i = 0; i < archSpots.Length; i++)
        {
            GameObject arch = new GameObject($"ArcoPiedra_{i + 1}");
            arch.transform.SetParent(archesParent.transform);
            arch.transform.position = new Vector3(archSpots[i].x, archSpots[i].y, 0f);
            arch.transform.localScale = new Vector3(1.5f, 1.5f, 1f);
            SpriteRenderer sr = arch.AddComponent<SpriteRenderer>();
            sr.sprite = archSprite;
            sr.sortingOrder = -1;
        }

        // Refrescar todos los Tilemaps y regenerar la geometría de colisión de TilePiso
        if (tilePiso != null)
        {
            tilePiso.CompressBounds();
            tilePiso.RefreshAllTiles();
            TilemapCollider2D tmCol = tilePiso.GetComponent<TilemapCollider2D>();
            if (tmCol != null)
            {
                tmCol.ProcessTilemapChanges();
                EditorUtility.SetDirty(tmCol);
            }
            CompositeCollider2D compCol = tilePiso.GetComponent<CompositeCollider2D>();
            if (compCol != null)
            {
                compCol.GenerateGeometry();
                EditorUtility.SetDirty(compCol);
            }
            EditorUtility.SetDirty(tilePiso);
        }

        if (structuresMap != null)
        {
            structuresMap.CompressBounds();
            structuresMap.RefreshAllTiles();
            EditorUtility.SetDirty(structuresMap);
        }

        if (elementsMap != null)
        {
            elementsMap.CompressBounds();
            elementsMap.RefreshAllTiles();
            EditorUtility.SetDirty(elementsMap);
        }

        Physics2D.SyncTransforms();
    }

    private static TileBase LoadTileByGuid(string guid)
    {
        if (string.IsNullOrEmpty(guid)) return null;
        string path = AssetDatabase.GUIDToAssetPath(guid);
        if (string.IsNullOrEmpty(path)) return null;
        return AssetDatabase.LoadAssetAtPath<TileBase>(path);
    }

    private static void PaintSolidStoneBlock(
        Tilemap map,
        int xStart,
        int xEnd,
        int yBottom,
        int yTop,
        Tile topTile,
        Tile deepTile)
    {
        for (int x = xStart; x <= xEnd; x++)
        {
            for (int y = yBottom; y <= yTop; y++)
            {
                Tile t = (y == yTop) ? topTile : deepTile;
                map.SetTile(new Vector3Int(x, y, 0), t);
            }
        }
    }

    private static void PaintHorizontalTileLine(Tilemap map, int xStart, int xEnd, int y, TileBase tile)
    {
        for (int x = xStart; x <= xEnd; x++)
        {
            map.SetTile(new Vector3Int(x, y, 0), tile);
        }
    }

    private static void PaintWineryBackgroundWall(
        Tilemap structuresMap,
        int xStart,
        int xEnd,
        int yBottom,
        int yTop,
        TileBase brickA,
        TileBase brickB,
        TileBase pillarTile,
        int pillarInterval)
    {
        for (int x = xStart; x <= xEnd; x++)
        {
            bool isPillar = pillarInterval > 0 && ((x - xStart) % pillarInterval == 0);
            for (int y = yBottom; y <= yTop; y++)
            {
                TileBase chosen = isPillar
                    ? pillarTile
                    : (((x + y) % 3 == 0 && brickB != null) ? brickB : brickA);
                structuresMap.SetTile(new Vector3Int(x, y, 0), chosen);
            }
        }
    }

    private static void CreateLadderColumn(
        Tilemap elementsMap,
        Transform triggerParent,
        string ladderName,
        int xCell,
        int yBottom,
        int yTop,
        Tile tileLadder,
        Sprite ladderSprite)
    {
        for (int y = yBottom; y <= yTop; y++)
        {
            if (elementsMap != null && tileLadder != null)
            {
                elementsMap.SetTile(new Vector3Int(xCell, y, 0), tileLadder);
            }
        }

        // Objeto interactivo EscaleraMano con trigger para subir/bajar con W, Flecha Arriba o Espacio
        float height = (yTop - yBottom + 1);
        float centerY = yBottom + height * 0.5f;
        GameObject ladderObj = new GameObject(ladderName);
        ladderObj.transform.SetParent(triggerParent);
        ladderObj.transform.position = new Vector3(xCell + 0.5f, centerY, 0f);

        BoxCollider2D col = ladderObj.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(0.95f, height + 0.5f);

        EscaleraMano escalera = ladderObj.AddComponent<EscaleraMano>();
        escalera.velocidadSubida = 5.6f;
    }

    private static void CreateBarrelStack(
        Transform parent,
        string stackName,
        float baseCenterX,
        float groundY,
        Sprite barrelSprite,
        int sueloLayer)
    {
        GameObject group = new GameObject(stackName);
        group.transform.SetParent(parent);

        // 2 barriles en la base y 1 barril apilado arriba
        Vector3[] offsets = new Vector3[]
        {
            new Vector3(baseCenterX - 0.48f, groundY + 0.52f, 0f),
            new Vector3(baseCenterX + 0.48f, groundY + 0.52f, 0f),
            new Vector3(baseCenterX, groundY + 1.48f, 0f)
        };

        for (int i = 0; i < offsets.Length; i++)
        {
            CreateSolidProp(
                group.transform,
                $"Barril_{i + 1}",
                offsets[i],
                barrelSprite,
                new Vector2(0.88f, 0.98f),
                sueloLayer,
                sortingOrder: 2
            );
        }
    }

    private static void CreateCrateCluster(
        Transform parent,
        string clusterName,
        float baseCenterX,
        float groundY,
        Sprite crateSprite,
        int sueloLayer)
    {
        GameObject group = new GameObject(clusterName);
        group.transform.SetParent(parent);

        Vector3[] offsets = new Vector3[]
        {
            new Vector3(baseCenterX - 0.48f, groundY + 0.48f, 0f),
            new Vector3(baseCenterX + 0.48f, groundY + 0.48f, 0f),
            new Vector3(baseCenterX + 0.48f, groundY + 1.42f, 0f)
        };

        for (int i = 0; i < offsets.Length; i++)
        {
            CreateSolidProp(
                group.transform,
                $"CajaMadera_{i + 1}",
                offsets[i],
                crateSprite,
                new Vector2(0.92f, 0.92f),
                sueloLayer,
                sortingOrder: 2
            );
        }
    }

    private static void CreateAcidPitTriggerAndVFX(
        Transform parent,
        string name,
        Vector2 centerPos,
        int widthTiles,
        Sprite acidSprite,
        AudioClip acidClip)
    {
        GameObject poolObj = new GameObject(name);
        poolObj.transform.SetParent(parent);
        poolObj.transform.position = new Vector3(centerPos.x, centerPos.y, 0f);

        BoxCollider2D col = poolObj.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(widthTiles * 1.0f, 1.85f);
        col.offset = new Vector2(0f, 0.1f);

        ZonaAcido zonaAcido = poolObj.AddComponent<ZonaAcido>();
        zonaAcido.danoPorContacto = 18f;
        zonaAcido.intervaloDano = 0.85f;
        zonaAcido.impulsoReboteVertical = 10.5f;
        zonaAcido.sonidoSalpicaduraAcido = acidClip;

        // Sistema de partículas de burbujas ácidas verdes
        GameObject bubblesObj = new GameObject("BurbujasAcido_VFX");
        bubblesObj.transform.SetParent(poolObj.transform);
        bubblesObj.transform.localPosition = new Vector3(0f, 0.7f, 0f);

        ParticleSystem ps = bubblesObj.AddComponent<ParticleSystem>();
        ParticleSystemRenderer psr = bubblesObj.GetComponent<ParticleSystemRenderer>();
        var main = ps.main;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, 1.6f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.4f, 1.1f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.16f);
        main.gravityModifier = -0.15f;
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(0.45f, 0.98f, 0.18f, 0.85f),
            new Color(0.82f, 1.0f, 0.32f, 0.90f)
        );

        var emission = ps.emission;
        emission.rateOverTime = 12f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(widthTiles * 0.9f, 0.25f, 0.1f);

        if (psr != null)
        {
            psr.material = EnemyDeathFeedback.GetPixelParticleMaterial();
            psr.sortingOrder = 5;
        }
    }

    private static void CreateSolidProp(
        Transform parent,
        string name,
        Vector3 position,
        Sprite sprite,
        Vector2 colliderSize,
        int layer,
        int sortingOrder)
    {
        GameObject prop = new GameObject(name);
        prop.transform.SetParent(parent);
        prop.transform.position = position;
        prop.layer = layer;

        SpriteRenderer sr = prop.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = sortingOrder;

        BoxCollider2D col = prop.AddComponent<BoxCollider2D>();
        col.size = colliderSize;
    }

    private static void SpawnNivel2HeartsAndGoal()
    {
        GameObject heartPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CorazonPrefabPath);
        if (heartPrefab != null)
        {
            GameObject heartsContainer = new GameObject("ItemsCurativos_Nivel2");

            // Corazón 1: En la plataforma alta opcional de la Zona 2 (Fosos de Fermentación, X = 57.5, sobre plataforma Y = 6)
            GameObject heart1 = (GameObject)PrefabUtility.InstantiatePrefab(heartPrefab);
            heart1.name = "CorazonVida_1_Zona2";
            heart1.transform.SetParent(heartsContainer.transform);
            heart1.transform.position = new Vector3(57.5f, 7.85f, 0f);

            // Corazón 2: En la pasarela superior de la Zona 3 (Almacén Final, X = 97.0, sobre pasarela Y = 7) antes de llegar a MetaNivel2
            GameObject heart2 = (GameObject)PrefabUtility.InstantiatePrefab(heartPrefab);
            heart2.name = "CorazonVida_2_Zona3";
            heart2.transform.SetParent(heartsContainer.transform);
            heart2.transform.position = new Vector3(97.0f, 8.85f, 0f);
        }

        // Configurar MetaNivel2 exactamente en X = 115
        MetaNivel meta = UnityEngine.Object.FindFirstObjectByType<MetaNivel>();
        if (meta == null)
        {
            GameObject goalPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MetaPrefabPath);
            if (goalPrefab != null)
            {
                GameObject goalObj = (GameObject)PrefabUtility.InstantiatePrefab(goalPrefab);
                goalObj.name = "MetaNivel2";
                meta = goalObj.GetComponent<MetaNivel>();
            }
        }

        if (meta != null)
        {
            meta.gameObject.name = "MetaNivel2";
            meta.tituloPersonalizado = "¡Nivel 2 Completado!";
            meta.puntosPorUva = 125;
            meta.multiplicadorBonoVida = 5f;
            meta.transform.position = new Vector3(115.0f, 1.0f, 0f);

            if (meta.textoTitulo != null)
            {
                meta.textoTitulo.text = "¡Nivel 2 Completado!";
                EditorUtility.SetDirty(meta.textoTitulo);
            }

            if (meta.botonSiguienteNivel != null)
            {
                UnityEventTools.RemovePersistentListener(meta.botonSiguienteNivel.onClick, meta.CargarSiguienteNivel);
                UnityEventTools.AddPersistentListener(meta.botonSiguienteNivel.onClick, meta.CargarSiguienteNivel);
                EditorUtility.SetDirty(meta.botonSiguienteNivel);
            }

            EditorUtility.SetDirty(meta);
        }
    }

    private static void SpawnNivel2ExclusiveEnemies(GameObject cabSauPrefab)
    {
        GameObject container = new GameObject("Enemigos_Nivel2");

        if (cabSauPrefab == null)
            cabSauPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CabSauPrefabPath);

        int sueloLayer = LayerMask.NameToLayer("Suelo");
        int sueloMask = sueloLayer >= 0 ? (1 << sueloLayer) : (1 << 6);

        // 7 instancias de EnemigoCabSau patrullando en los tramos planos de piedra y pasarelas anchas
        // Cada uno con DetectorBorde apuntando a la capa Suelo y amplio espacio horizontal para correr
        (Vector3 pos, float patrolDist, string label)[] cabSauPlacements = new (Vector3, float, string)[]
        {
            // Zona 1 (Patio de Entrada, X = 0 a 35)
            (new Vector3(11.5f, 0.65f, 0f), 4.0f, "EnemigoCabSau_1_PatioEntrada"),
            (new Vector3(26.5f, 0.65f, 0f), 4.2f, "EnemigoCabSau_2_PatioEntrada"),
            (new Vector3(19.0f, 7.65f, 0f), 3.2f, "EnemigoCabSau_3_PasarelaAltaZ1"),

            // Zona 2 (Fosos de Fermentación, X = 35 a 80)
            (new Vector3(57.5f, 4.65f, 0f), 2.8f, "EnemigoCabSau_4_PasarelaFermentacion"),

            // Zona 3 (Almacén Final, X = 80 a 120)
            (new Vector3(88.0f, 0.65f, 0f), 4.5f, "EnemigoCabSau_5_AlmacenPlantaBaja"),
            (new Vector3(88.5f, 5.65f, 0f), 3.6f, "EnemigoCabSau_6_AlmacenSegundoPiso"),
            (new Vector3(103.5f, 0.65f, 0f), 4.8f, "EnemigoCabSau_7_AlmacenAntesDeMeta")
        };

        if (cabSauPrefab != null)
        {
            for (int i = 0; i < cabSauPlacements.Length; i++)
            {
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(cabSauPrefab);
                instance.name = cabSauPlacements[i].label;
                instance.transform.SetParent(container.transform);
                instance.transform.position = cabSauPlacements[i].pos;

                EnemigoCabSau script = instance.GetComponent<EnemigoCabSau>();
                if (script != null)
                {
                    script.distanciaPatrulla = cabSauPlacements[i].patrolDist;
                    script.detectarBordes = true;
                    script.frenarEnBordesDuranteSprint = true;
                    script.capaSuelo = sueloMask;

                    Transform db = instance.transform.Find("DetectorBorde");
                    if (db == null)
                    {
                        GameObject dbObj = new GameObject("DetectorBorde");
                        dbObj.transform.SetParent(instance.transform);
                        dbObj.transform.localPosition = new Vector3(0.48f, -0.18f, 0f);
                        db = dbObj.transform;
                    }
                    script.detectorBorde = db;
                    EditorUtility.SetDirty(script);
                }
            }
        }

        // Solo 2 uvas disparadoras (EnemigoUva) en puntos elevados estratégicos
        GameObject merlotPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(EnemigoUvaPrefabPath);
        if (merlotPrefab != null)
        {
            (Vector3 pos, string name)[] rangedGrapePlacements = new (Vector3, string)[]
            {
                (new Vector3(28.0f, 4.60f, 0f), "EnemigoUva_1_PlataformaZ1"), // Plataforma elevada Y=3 en Zona 1
                (new Vector3(49.5f, 4.60f, 0f), "EnemigoUva_2_ArcoElevadoZ2") // Plataforma elevada sobre el pilar central en Zona 2
            };

            for (int i = 0; i < rangedGrapePlacements.Length; i++)
            {
                GameObject merlotInst = (GameObject)PrefabUtility.InstantiatePrefab(merlotPrefab);
                merlotInst.name = rangedGrapePlacements[i].name;
                merlotInst.transform.SetParent(container.transform);
                merlotInst.transform.position = rangedGrapePlacements[i].pos;
            }
        }
    }

    private static void RegisterScenesInBuildSettings()
    {
        List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>();

        if (File.Exists(Nivel1ScenePath))
            scenes.Add(new EditorBuildSettingsScene(Nivel1ScenePath, true));
        if (File.Exists(Nivel2ScenePath))
            scenes.Add(new EditorBuildSettingsScene(Nivel2ScenePath, true));

        foreach (var existing in EditorBuildSettings.scenes)
        {
            if (existing.path != Nivel1ScenePath && existing.path != Nivel2ScenePath && File.Exists(existing.path))
            {
                scenes.Add(existing);
            }
        }

        EditorBuildSettings.scenes = scenes.ToArray();
    }

    // =========================================================================
    // GENERACIÓN DE SPRITES Y ASSETS DE TILE (16x16 PX) DE LAS INSTALACIONES DE LA BODEGA
    // =========================================================================
    private static void CreateWinerySpritesAndTiles(
        out Sprite barrelSprite,
        out Sprite crateSprite,
        out Sprite archSprite,
        out Sprite ladderSprite,
        out Sprite acidSurfaceSprite,
        out Sprite woodPlatformSprite,
        out Tile tileStoneTop,
        out Tile tileStoneDeep,
        out Tile tileWoodWalkway,
        out Tile tileBrickWall,
        out Tile tileStonePillar,
        out Tile tileLadder,
        out Tile tileAcidSurface,
        out Tile tileAcidDeep)
    {
        // 1. Losa Superior de Piedra Gris Cincelada (16x16 px - Sin pasto)
        {
            int w = 16, h = 16;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color mortar = new Color(0.14f, 0.14f, 0.18f, 1f);
            Color stoneTopRim = new Color(0.62f, 0.63f, 0.68f, 1f);
            Color stoneLight = new Color(0.48f, 0.49f, 0.55f, 1f);
            Color stoneMid = new Color(0.36f, 0.37f, 0.43f, 1f);
            Color stoneDark = new Color(0.25f, 0.25f, 0.31f, 1f);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    if (y == 15 || y == 10 || y == 5 || y == 0)
                    {
                        tex.SetPixel(x, y, mortar);
                    }
                    else if ((y > 10 && (x == 0 || x == 8)) ||
                             (y > 5 && y < 10 && (x == 4 || x == 12)) ||
                             (y < 5 && (x == 2 || x == 10)))
                    {
                        tex.SetPixel(x, y, mortar);
                    }
                    else if (y == 14)
                    {
                        tex.SetPixel(x, y, stoneTopRim);
                    }
                    else if (y == 13 || y == 9 || y == 4)
                    {
                        tex.SetPixel(x, y, stoneLight);
                    }
                    else if ((x + y) % 5 == 0)
                    {
                        tex.SetPixel(x, y, stoneDark);
                    }
                    else
                    {
                        tex.SetPixel(x, y, stoneMid);
                    }
                }
            }
            tex.Apply();
            File.WriteAllBytes(StoneTopSpritePath, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
        }
        AssetDatabase.ImportAsset(StoneTopSpritePath, ImportAssetOptions.ForceSynchronousImport);
        ConfigurePixelArtSprite(StoneTopSpritePath, 16f);
        Sprite stoneTopSpr = AssetDatabase.LoadAssetAtPath<Sprite>(StoneTopSpritePath);

        // 2. Piedra Gris Profunda / Cimiento de Bodega (16x16 px)
        {
            int w = 16, h = 16;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color mortar = new Color(0.11f, 0.11f, 0.15f, 1f);
            Color stoneMid = new Color(0.28f, 0.29f, 0.35f, 1f);
            Color stoneDark = new Color(0.20f, 0.21f, 0.26f, 1f);
            Color stoneHighlight = new Color(0.36f, 0.37f, 0.43f, 1f);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    bool horizontalJoint = (y % 4 == 0);
                    int row = y / 4;
                    bool verticalJoint = ((x + (row % 2) * 4) % 8 == 0);

                    if (horizontalJoint || verticalJoint)
                        tex.SetPixel(x, y, mortar);
                    else if (y % 4 == 3)
                        tex.SetPixel(x, y, stoneHighlight);
                    else if ((x + y) % 3 == 0)
                        tex.SetPixel(x, y, stoneDark);
                    else
                        tex.SetPixel(x, y, stoneMid);
                }
            }
            tex.Apply();
            File.WriteAllBytes(StoneDeepSpritePath, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
        }
        AssetDatabase.ImportAsset(StoneDeepSpritePath, ImportAssetOptions.ForceSynchronousImport);
        ConfigurePixelArtSprite(StoneDeepSpritePath, 16f);
        Sprite stoneDeepSpr = AssetDatabase.LoadAssetAtPath<Sprite>(StoneDeepSpritePath);

        // 3. Muro de Ladrillo de Bodega (Fondo en Structures, 16x16 px)
        {
            int w = 16, h = 16;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color mortar = new Color(0.10f, 0.07f, 0.11f, 0.94f);
            Color brickA = new Color(0.26f, 0.16f, 0.20f, 0.94f);
            Color brickB = new Color(0.22f, 0.13f, 0.17f, 0.94f);
            Color brickTop = new Color(0.32f, 0.20f, 0.24f, 0.94f);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    bool hJoint = (y % 4 == 0);
                    int row = y / 4;
                    bool vJoint = ((x + (row % 2) * 4) % 8 == 0);

                    if (hJoint || vJoint)
                        tex.SetPixel(x, y, mortar);
                    else if (y % 4 == 3)
                        tex.SetPixel(x, y, brickTop);
                    else if ((x + row) % 2 == 0)
                        tex.SetPixel(x, y, brickA);
                    else
                        tex.SetPixel(x, y, brickB);
                }
            }
            tex.Apply();
            File.WriteAllBytes(BrickWallSpritePath, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
        }
        AssetDatabase.ImportAsset(BrickWallSpritePath, ImportAssetOptions.ForceSynchronousImport);
        ConfigurePixelArtSprite(BrickWallSpritePath, 16f);
        Sprite brickWallSpr = AssetDatabase.LoadAssetAtPath<Sprite>(BrickWallSpritePath);

        // 4. Pilar / Columna de Piedra de Bodega (16x16 px)
        {
            int w = 16, h = 16;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color edge = new Color(0.13f, 0.13f, 0.17f, 0.98f);
            Color pillarDark = new Color(0.26f, 0.27f, 0.33f, 0.98f);
            Color pillarMid = new Color(0.35f, 0.36f, 0.43f, 0.98f);
            Color pillarLight = new Color(0.45f, 0.46f, 0.54f, 0.98f);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    if (x == 0 || x == w - 1 || y == 0 || y == 8)
                        tex.SetPixel(x, y, edge);
                    else if (x == 3 || x == 4)
                        tex.SetPixel(x, y, pillarLight);
                    else if (x >= 11)
                        tex.SetPixel(x, y, pillarDark);
                    else
                        tex.SetPixel(x, y, pillarMid);
                }
            }
            tex.Apply();
            File.WriteAllBytes(StonePillarSpritePath, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
        }
        AssetDatabase.ImportAsset(StonePillarSpritePath, ImportAssetOptions.ForceSynchronousImport);
        ConfigurePixelArtSprite(StonePillarSpritePath, 16f);
        Sprite stonePillarSpr = AssetDatabase.LoadAssetAtPath<Sprite>(StonePillarSpritePath);

        // 5. Barril de Vino de Roble con Aros de Hierro (16x18 px)
        {
            int w = 16, h = 18;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color clear = new Color(0, 0, 0, 0);
            Color outline = new Color(0.12f, 0.06f, 0.05f, 1f);
            Color oakDark = new Color(0.36f, 0.20f, 0.11f, 1f);
            Color oakMid = new Color(0.52f, 0.30f, 0.16f, 1f);
            Color oakHighlight = new Color(0.66f, 0.42f, 0.22f, 1f);
            Color ironBand = new Color(0.28f, 0.29f, 0.34f, 1f);
            Color wineTap = new Color(0.75f, 0.14f, 0.24f, 1f);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int margin = (y <= 1 || y >= h - 2) ? 3 : (y <= 3 || y >= h - 4) ? 2 : 1;
                    if (x < margin || x >= w - margin)
                    {
                        tex.SetPixel(x, y, clear);
                        continue;
                    }
                    if (x == margin || x == w - margin - 1 || y == 0 || y == h - 1)
                        tex.SetPixel(x, y, outline);
                    else if (y == 4 || y == 5 || y == 12 || y == 13)
                        tex.SetPixel(x, y, ironBand);
                    else if (x == 7 && y == 8)
                        tex.SetPixel(x, y, wineTap);
                    else if (x == 4 || x == 5)
                        tex.SetPixel(x, y, oakHighlight);
                    else if (x % 3 == 0)
                        tex.SetPixel(x, y, oakDark);
                    else
                        tex.SetPixel(x, y, oakMid);
                }
            }
            tex.Apply();
            File.WriteAllBytes(BarrelSpritePath, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
        }
        AssetDatabase.ImportAsset(BarrelSpritePath, ImportAssetOptions.ForceSynchronousImport);
        ConfigurePixelArtSprite(BarrelSpritePath, 16f);
        barrelSprite = AssetDatabase.LoadAssetAtPath<Sprite>(BarrelSpritePath);

        // 6. Caja de Madera de Vendimia (16x16 px)
        {
            int w = 16, h = 16;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color outline = new Color(0.14f, 0.08f, 0.05f, 1f);
            Color woodDark = new Color(0.42f, 0.24f, 0.12f, 1f);
            Color woodMid = new Color(0.58f, 0.36f, 0.18f, 1f);
            Color woodLight = new Color(0.72f, 0.48f, 0.25f, 1f);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    bool isOuterBorder = (x == 0 || x == w - 1 || y == 0 || y == h - 1);
                    bool isFrame = (x <= 2 || x >= w - 3 || y <= 2 || y >= h - 3);
                    bool isDiagonal = Mathf.Abs(x - y) <= 1;

                    if (isOuterBorder) tex.SetPixel(x, y, outline);
                    else if (isFrame) tex.SetPixel(x, y, woodLight);
                    else if (isDiagonal) tex.SetPixel(x, y, woodMid);
                    else if (y % 3 == 0) tex.SetPixel(x, y, outline);
                    else tex.SetPixel(x, y, woodDark);
                }
            }
            tex.Apply();
            File.WriteAllBytes(CrateSpritePath, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
        }
        AssetDatabase.ImportAsset(CrateSpritePath, ImportAssetOptions.ForceSynchronousImport);
        ConfigurePixelArtSprite(CrateSpritePath, 16f);
        crateSprite = AssetDatabase.LoadAssetAtPath<Sprite>(CrateSpritePath);

        // 7. Pasarela / Plataforma de Madera de Bodega (16x16 px)
        {
            int w = 16, h = 16;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color outline = new Color(0.12f, 0.06f, 0.04f, 1f);
            Color plankTop = new Color(0.72f, 0.46f, 0.24f, 1f);
            Color plankMid = new Color(0.52f, 0.31f, 0.15f, 1f);
            Color beamDark = new Color(0.32f, 0.18f, 0.09f, 1f);
            Color ironBolt = new Color(0.30f, 0.32f, 0.38f, 1f);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    if (y == 15 || y == 0 || x == 0 || x == w - 1 || y == 8)
                        tex.SetPixel(x, y, outline);
                    else if ((x == 3 || x == 12) && (y == 12 || y == 4))
                        tex.SetPixel(x, y, ironBolt);
                    else if (y >= 12)
                        tex.SetPixel(x, y, plankTop);
                    else if (y >= 9)
                        tex.SetPixel(x, y, plankMid);
                    else
                        tex.SetPixel(x, y, beamDark);
                }
            }
            tex.Apply();
            File.WriteAllBytes(WoodPlatformSpritePath, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
        }
        AssetDatabase.ImportAsset(WoodPlatformSpritePath, ImportAssetOptions.ForceSynchronousImport);
        ConfigurePixelArtSprite(WoodPlatformSpritePath, 16f);
        woodPlatformSprite = AssetDatabase.LoadAssetAtPath<Sprite>(WoodPlatformSpritePath);

        // 8. Escalera de Mano de Madera (16x16 px)
        {
            int w = 16, h = 16;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color clear = new Color(0, 0, 0, 0);
            Color outline = new Color(0.16f, 0.08f, 0.04f, 1f);
            Color rail = new Color(0.52f, 0.30f, 0.15f, 1f);
            Color rung = new Color(0.72f, 0.46f, 0.24f, 1f);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    if (x == 2 || x == 13)
                        tex.SetPixel(x, y, outline);
                    else if (x == 3 || x == 4 || x == 11 || x == 12)
                        tex.SetPixel(x, y, rail);
                    else if (x > 4 && x < 11 && (y == 3 || y == 7 || y == 11 || y == 15))
                        tex.SetPixel(x, y, rung);
                    else if (x > 4 && x < 11 && (y == 2 || y == 6 || y == 10 || y == 14))
                        tex.SetPixel(x, y, outline);
                    else
                        tex.SetPixel(x, y, clear);
                }
            }
            tex.Apply();
            File.WriteAllBytes(LadderSpritePath, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
        }
        AssetDatabase.ImportAsset(LadderSpritePath, ImportAssetOptions.ForceSynchronousImport);
        ConfigurePixelArtSprite(LadderSpritePath, 16f);
        ladderSprite = AssetDatabase.LoadAssetAtPath<Sprite>(LadderSpritePath);

        // 9. Arco de Piedra de la Bodega (32x32 px)
        {
            int w = 32, h = 32;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color clear = new Color(0, 0, 0, 0);
            Color stoneDark = new Color(0.22f, 0.20f, 0.26f, 1f);
            Color stoneMid = new Color(0.36f, 0.34f, 0.42f, 1f);
            Color stoneLight = new Color(0.50f, 0.48f, 0.56f, 1f);
            Color innerShadow = new Color(0.10f, 0.07f, 0.14f, 0.85f);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float dx = x - 15.5f;
                    float dy = y - 16f;
                    float distArch = Mathf.Sqrt(dx * dx + Mathf.Max(0f, dy) * Mathf.Max(0f, dy));

                    bool insideOuter = (y < 18 && x >= 2 && x <= 29) || (y >= 18 && distArch <= 14f);
                    bool insideInner = (y < 16 && x >= 7 && x <= 24) || (y >= 16 && distArch <= 9f);

                    if (!insideOuter)
                        tex.SetPixel(x, y, clear);
                    else if (insideInner)
                        tex.SetPixel(x, y, innerShadow);
                    else if ((x + y) % 5 == 0)
                        tex.SetPixel(x, y, stoneDark);
                    else if (y % 4 == 0)
                        tex.SetPixel(x, y, stoneLight);
                    else
                        tex.SetPixel(x, y, stoneMid);
                }
            }
            tex.Apply();
            File.WriteAllBytes(ArchSpritePath, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
        }
        AssetDatabase.ImportAsset(ArchSpritePath, ImportAssetOptions.ForceSynchronousImport);
        ConfigurePixelArtSprite(ArchSpritePath, 16f);
        archSprite = AssetDatabase.LoadAssetAtPath<Sprite>(ArchSpritePath);

        // 10. Superficie y Fondo de Ácido Verde / Mosto Fermentado (16x16 px)
        {
            int w = 16, h = 16;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color clear = new Color(0, 0, 0, 0);
            Color acidFoam = new Color(0.84f, 1.0f, 0.34f, 0.96f);
            Color acidBright = new Color(0.45f, 0.92f, 0.16f, 0.94f);
            Color acidDeep = new Color(0.18f, 0.55f, 0.12f, 0.92f);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int waveTop = 13 + Mathf.RoundToInt(Mathf.Sin(x * 0.8f) * 1.2f);
                    if (y > waveTop)
                        tex.SetPixel(x, y, clear);
                    else if (y >= waveTop - 1)
                        tex.SetPixel(x, y, acidFoam);
                    else if (y >= 6 || (x == 5 && y == 4) || (x == 11 && y == 8))
                        tex.SetPixel(x, y, acidBright);
                    else
                        tex.SetPixel(x, y, acidDeep);
                }
            }
            tex.Apply();
            File.WriteAllBytes(AcidSurfaceSpritePath, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
        }
        AssetDatabase.ImportAsset(AcidSurfaceSpritePath, ImportAssetOptions.ForceSynchronousImport);
        ConfigurePixelArtSprite(AcidSurfaceSpritePath, 16f);
        acidSurfaceSprite = AssetDatabase.LoadAssetAtPath<Sprite>(AcidSurfaceSpritePath);

        {
            int w = 16, h = 16;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color acidMid = new Color(0.24f, 0.68f, 0.14f, 0.94f);
            Color acidDeep = new Color(0.14f, 0.44f, 0.10f, 0.94f);
            Color bubble = new Color(0.75f, 0.98f, 0.30f, 0.95f);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    if ((x == 4 && y == 10) || (x == 11 && y == 5) || (x == 8 && y == 13))
                        tex.SetPixel(x, y, bubble);
                    else if ((x + y) % 3 == 0)
                        tex.SetPixel(x, y, acidMid);
                    else
                        tex.SetPixel(x, y, acidDeep);
                }
            }
            tex.Apply();
            File.WriteAllBytes(AcidDeepSpritePath, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
        }
        AssetDatabase.ImportAsset(AcidDeepSpritePath, ImportAssetOptions.ForceSynchronousImport);
        ConfigurePixelArtSprite(AcidDeepSpritePath, 16f);
        Sprite acidDeepSpr = AssetDatabase.LoadAssetAtPath<Sprite>(AcidDeepSpritePath);

        // Crear / Actualizar los assets Tile para pintar en TilePiso, Structures y Elements
        tileStoneTop = CreateOrUpdateTileAsset(TileStoneTopPath, stoneTopSpr, Tile.ColliderType.Grid);
        tileStoneDeep = CreateOrUpdateTileAsset(TileStoneDeepPath, stoneDeepSpr, Tile.ColliderType.Grid);
        tileWoodWalkway = CreateOrUpdateTileAsset(TileWoodWalkwayPath, woodPlatformSprite, Tile.ColliderType.Grid);
        tileBrickWall = CreateOrUpdateTileAsset(TileBrickWallPath, brickWallSpr, Tile.ColliderType.None);
        tileStonePillar = CreateOrUpdateTileAsset(TileStonePillarPath, stonePillarSpr, Tile.ColliderType.None);
        tileLadder = CreateOrUpdateTileAsset(TileLadderPath, ladderSprite, Tile.ColliderType.None);
        tileAcidSurface = CreateOrUpdateTileAsset(TileAcidSurfacePath, acidSurfaceSprite, Tile.ColliderType.None);
        tileAcidDeep = CreateOrUpdateTileAsset(TileAcidDeepPath, acidDeepSpr, Tile.ColliderType.None);
    }

    private static Tile CreateOrUpdateTileAsset(string assetPath, Sprite sprite, Tile.ColliderType colliderType)
    {
        Tile tile = AssetDatabase.LoadAssetAtPath<Tile>(assetPath);
        if (tile == null)
        {
            tile = ScriptableObject.CreateInstance<Tile>();
            AssetDatabase.CreateAsset(tile, assetPath);
        }

        tile.sprite = sprite;
        tile.color = Color.white;
        tile.colliderType = colliderType;
        EditorUtility.SetDirty(tile);
        return tile;
    }

    private static void CreateFallbackCabSauTexture(string path, int idx)
    {
        int w = 48, h = 48;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Color clear = new Color(0, 0, 0, 0);
        Color ruby = new Color(0.75f, 0.10f, 0.22f, 1f);
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                tex.SetPixel(x, y, Vector2.Distance(new Vector2(x, y), new Vector2(24, 24)) < 16f ? ruby : clear);
        tex.Apply();
        File.WriteAllBytes(path, tex.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(tex);
    }

    // =========================================================================
    // GENERACIÓN DE EFECTOS DE SONIDO (SFX) PARA CAB. SAU Y ÁCIDO
    // =========================================================================
    private static void GenerateCabSauAndAcidAudio(
        out AudioClip alertClip,
        out AudioClip hitClip,
        out AudioClip deathClip,
        out AudioClip acidClip)
    {
        if (!File.Exists(SfxCabSauAlertPath))
        {
            WriteSynthesizedWav(SfxCabSauAlertPath, 0.28f, (t, dur) =>
            {
                float freq = Mathf.Lerp(190f, 360f, t / dur);
                float vibrato = Mathf.Sin(2f * Mathf.PI * 28f * t) * 25f;
                float env = Mathf.Sin(Mathf.Clamp01(t / dur) * Mathf.PI);
                return Mathf.Sin(2f * Mathf.PI * (freq + vibrato) * t) * env * 0.65f;
            });
            AssetDatabase.ImportAsset(SfxCabSauAlertPath, ImportAssetOptions.ForceSynchronousImport);
        }

        if (!File.Exists(SfxCabSauHitPath))
        {
            WriteSynthesizedWav(SfxCabSauHitPath, 0.18f, (t, dur) =>
            {
                float freq = Mathf.Lerp(320f, 140f, t / dur);
                float env = 1f - (t / dur);
                return Mathf.Sin(2f * Mathf.PI * freq * t) * env * 0.7f;
            });
            AssetDatabase.ImportAsset(SfxCabSauHitPath, ImportAssetOptions.ForceSynchronousImport);
        }

        if (!File.Exists(SfxCabSauDeathPath))
        {
            WriteSynthesizedWav(SfxCabSauDeathPath, 0.34f, (t, dur) =>
            {
                float freq = t < 0.1f ? Mathf.Lerp(240f, 520f, t / 0.1f) : Mathf.Lerp(520f, 130f, (t - 0.1f) / 0.24f);
                float env = 1f - (t / dur);
                return Mathf.Sin(2f * Mathf.PI * freq * t) * env * 0.75f;
            });
            AssetDatabase.ImportAsset(SfxCabSauDeathPath, ImportAssetOptions.ForceSynchronousImport);
        }

        if (!File.Exists(SfxAcidSplashPath))
        {
            WriteSynthesizedWav(SfxAcidSplashPath, 0.30f, (t, dur) =>
            {
                float freq = 280f + Mathf.Sin(t * 95f) * 110f;
                float env = 1f - (t / dur);
                return Mathf.Sin(2f * Mathf.PI * freq * t) * env * 0.6f;
            });
            AssetDatabase.ImportAsset(SfxAcidSplashPath, ImportAssetOptions.ForceSynchronousImport);
        }

        alertClip = AssetDatabase.LoadAssetAtPath<AudioClip>(SfxCabSauAlertPath);
        hitClip = AssetDatabase.LoadAssetAtPath<AudioClip>(SfxCabSauHitPath);
        deathClip = AssetDatabase.LoadAssetAtPath<AudioClip>(SfxCabSauDeathPath);
        acidClip = AssetDatabase.LoadAssetAtPath<AudioClip>(SfxAcidSplashPath);
    }

    private static void WriteSynthesizedWav(string path, float durationSeconds, Func<float, float, float> sampleFunc)
    {
        int sampleRate = 44100;
        int sampleCount = Mathf.Max(1, Mathf.RoundToInt(sampleRate * durationSeconds));
        byte[] pcmBytes = new byte[sampleCount * 2];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;
            float sample = Mathf.Clamp(sampleFunc(t, durationSeconds), -1f, 1f);
            short s = (short)Mathf.RoundToInt(sample * 32767f);
            pcmBytes[i * 2] = (byte)(s & 0xff);
            pcmBytes[i * 2 + 1] = (byte)((s >> 8) & 0xff);
        }

        using (FileStream fs = new FileStream(path, FileMode.Create, FileAccess.Write))
        using (BinaryWriter bw = new BinaryWriter(fs))
        {
            int byteRate = sampleRate * 2;
            bw.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
            bw.Write(36 + pcmBytes.Length);
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
            bw.Write(pcmBytes.Length);
            bw.Write(pcmBytes);
        }
    }
}
#endif
