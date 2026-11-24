#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

/// <summary>
/// Construye por completo el Nivel 4 ("Las Ruinas de la Cava y el Jefe Final Wine Monster", Assets/Scenes/Nivel4.unity):
/// 1. Importa el fondo pixel-art de la Cava en Ruinas (FondoNivel4_CavaRuinas.jpg) y los 16 sprites del Wine Monster (PPU = 12).
/// 2. Genera SFX exclusivos del jefe final (rugido, ola de vino, invocación, chorro carmesí, sellado en botella).
/// 3. Crea el prefab Assets/Prefabs/JefeWineMonster.prefab (3000 HP, 2 fases, invocación de las 3 uvas, corazón cada 6 derrotadas).
/// 4. Construye Assets/Scenes/Nivel4.unity con:
///    - Mapa extenso en ruinas con barricas volcadas, cajas rotas, arcos agrietados y 7 fosos de ácido (X = 0..211).
///    - 3 Checkpoints (Faroles de la Cava en X = 72, X = 143.5 y X = 207.5 justo antes del jefe).
///    - Gran Arena del Jefe Final (X = 212..257) con 5 plataformas elevadas en 2 niveles para esquivar la ola del piso.
///    - HUD de Barra de Vida del Jefe (3000 HP), indicador de Fase y contador de 6 esbirros -> Corazón.
///    - Clímax final: sellado del Wine Monster dentro de la Botella Sagrada del Nivel 3 -> "¡JUEGO COMPLETADO!".
/// 5. Registra Nivel1 -> Nivel2 -> Nivel3 -> Nivel4 en Build Settings.
/// </summary>
[InitializeOnLoad]
public static class Nivel4RuinasYBossAutoSetup
{
    private const string SetupPrefKey = "Vineyard_Nivel4_RuinasBoss_V2";
    private const int CurrentVersion = 2;

    private const string Nivel1ScenePath = "Assets/Scenes/Nivel1.unity";
    private const string Nivel2ScenePath = "Assets/Scenes/Nivel2.unity";
    private const string Nivel3ScenePath = "Assets/Scenes/Nivel3.unity";
    private const string Nivel4ScenePath = "Assets/Scenes/Nivel4.unity";

    private const string RuinasBgAssetPath = "Assets/Sprites/Fondos/FondoNivel4_CavaRuinas.jpg";
    private const string WineMonsterSpriteFolder = "Assets/Sprites/Enemigos/WineMonster";
    private const string WineMonsterPrefabPath = "Assets/Prefabs/JefeWineMonster.prefab";

    private const string MerlotPrefabPath = "Assets/Prefabs/EnemigoUva.prefab";
    private const string CabSauPrefabPath = "Assets/Prefabs/EnemigoCabSau.prefab";
    private const string NebbioloPrefabPath = "Assets/Prefabs/EnemigoNebbiolo.prefab";
    private const string CorazonPrefabPath = "Assets/Prefabs/CorazonVida.prefab";
    private const string MetaPrefabPath = "Assets/Prefabs/MetaNivel.prefab";

    private const string BottleCompletePath = "Assets/Sprites/Items/BotellaNivel3/BotellaCompleta.png";
    private const string BottleAuraPath = "Assets/Sprites/Items/BotellaNivel3/AuraAltarBotella.png";
    private const string CheckpointOffPath = "Assets/Sprites/Items/BotellaNivel3/FarolCheckpoint_Apagado.png";
    private const string CheckpointOnPath = "Assets/Sprites/Items/BotellaNivel3/FarolCheckpoint_Encendido.png";

    private const string BarrelSpritePath = "Assets/Sprites/Nivel2Props/BarrilVino.png";
    private const string CrateSpritePath = "Assets/Sprites/Nivel2Props/CajaMadera.png";
    private const string ArchSpritePath = "Assets/Sprites/Nivel2Props/ArcoPiedraBodega.png";
    private const string ZeroFrictionMatPath = "Assets/Settings/JugadorSinFriccion.physicsMaterial2D";

    private const string SfxBossRoarPath = "Assets/Audio/SFX_BossRugido.wav";
    private const string SfxBossWavePath = "Assets/Audio/SFX_BossOla.wav";
    private const string SfxBossSummonPath = "Assets/Audio/SFX_BossInvocar.wav";
    private const string SfxBossStreamPath = "Assets/Audio/SFX_BossChorro.wav";
    private const string SfxBossSealPath = "Assets/Audio/SFX_BossSellado.wav";
    private const string SfxSuperCorkPath = "Assets/Audio/SFX_SuperCorchazo.wav";
    private const string SfxSuperCorkReadyPath = "Assets/Audio/SFX_SuperCorchazoListo.wav";
    private const string SfxCheckpointPath = "Assets/Audio/SFX_CheckpointCava.wav";
    private const string SfxAcidSplashPath = "Assets/Audio/SFX_AcidoBurbuja.wav";
    private const string SfxHealPath = "Assets/Audio/SFX_CurarCorazon.wav";
    private const string SfxGrapeHitPath = "Assets/Audio/SFX_ImpactoUva.wav";

    static Nivel4RuinasYBossAutoSetup()
    {
        EditorApplication.delayCall += RunOnceIfNeeded;
    }

    private static void RunOnceIfNeeded()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        bool needsVersion = EditorPrefs.GetInt(SetupPrefKey, 0) < CurrentVersion;
        bool needsScene = !File.Exists(Nivel4ScenePath);
        bool needsPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(WineMonsterPrefabPath) == null;

        string baseScene = File.Exists(Nivel3ScenePath) ? Nivel3ScenePath : Nivel2ScenePath;
        if ((needsVersion || needsScene || needsPrefab) && File.Exists(baseScene))
        {
            BuildNivel4AndBoss(silent: true, keepNivel4Open: true);
        }
    }

    [MenuItem("Herramientas Vineyard/Construir Nivel 4 (Ruinas de la Cava y Jefe Wine Monster)")]
    public static void BuildFromMenu()
    {
        BuildNivel4AndBoss(silent: false, keepNivel4Open: true);
        EditorUtility.DisplayDialog(
            "Vineyard - Nivel 4 (Ruinas de la Cava y Jefe Final) Listo",
            "¡Se construyó Assets/Scenes/Nivel4.unity con éxito!\n\n" +
            "• Cava en Ruinas con fondo exclusivo, barricas volcadas, cajas rotas, arcos agrietados y 7 fosos de ácido (X = 0..211).\n" +
            "• 3 Checkpoints (X = 72, X = 143.5 y X = 207.5 justo antes de la Arena del Jefe).\n" +
            "• Gran Arena del Jefe Final (X = 212..257) con 5 plataformas elevadas en 2 niveles para esquivar la ola del piso.\n" +
            "• Jefe Wine Monster (3000 HP, 70 daño/corcho, 2 Fases: Ola por piso + Invocación de Merlot/Cab.Sau/Nebbiolo + Chorro de Vino en Fase 2).\n" +
            "• Cada 6 enemigos invocados derrotados aparece un Corazón curativo cerca del jugador.\n" +
            "• Al llegar a 0 HP, el Wine Monster es sellado en la Botella Sagrada -> ¡JUEGO COMPLETADO!",
            "¡A vencer al Jefe Final!"
        );
    }

    [MenuItem("Herramientas Vineyard/Abrir Escena Nivel 4 (Ruinas y Jefe Final)")]
    public static void OpenNivel4Menu()
    {
        if (!File.Exists(Nivel4ScenePath))
        {
            BuildNivel4AndBoss(silent: true, keepNivel4Open: true);
            return;
        }
        EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
        EditorSceneManager.OpenScene(Nivel4ScenePath, OpenSceneMode.Single);
    }

    public static void BuildNivel4AndBoss(bool silent, bool keepNivel4Open)
    {
        EnsureDirectories();

        // 1. Generar audio exclusivo del Jefe Final
        GenerateBossAudio(
            out AudioClip sfxRoar,
            out AudioClip sfxWave,
            out AudioClip sfxSummon,
            out AudioClip sfxStream,
            out AudioClip sfxSeal
        );

        // 2. Importar los 16 sprites del Wine Monster (PPU = 12 => ~4 unidades de altura imponente)
        Sprite[] bossSprites = ConfigureWineMonsterSprites();

        // 3. Importar fondo pixel-art de Cava en Ruinas
        Sprite ruinasBgSprite = ConfigureRuinasBackgroundSprite();

        // 4. Crear Prefab del Jefe Wine Monster
        GameObject bossPrefab = CreateOrUpdateWineMonsterPrefab(
            bossSprites,
            sfxRoar,
            sfxWave,
            sfxSummon,
            sfxStream,
            sfxSeal
        );

        // 5. Construir Assets/Scenes/Nivel4.unity
        ConstructNivel4Scene(bossPrefab, ruinasBgSprite, keepNivel4Open);

        // 6. Registrar Nivel1 -> Nivel2 -> Nivel3 -> Nivel4 en Build Settings
        RegisterAllFourScenesInBuildSettings();

        EditorPrefs.SetInt(SetupPrefKey, CurrentVersion);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        if (!silent)
        {
            Debug.Log("[Vineyard] Nivel 4 (Ruinas de la Cava + Jefe Final Wine Monster 3000 HP) creado correctamente.");
        }
    }

    private static void EnsureDirectories()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Sprites")) AssetDatabase.CreateFolder("Assets", "Sprites");
        if (!AssetDatabase.IsValidFolder("Assets/Sprites/Fondos")) AssetDatabase.CreateFolder("Assets/Sprites", "Fondos");
        if (!AssetDatabase.IsValidFolder("Assets/Sprites/Enemigos")) AssetDatabase.CreateFolder("Assets/Sprites", "Enemigos");
        if (!AssetDatabase.IsValidFolder(WineMonsterSpriteFolder)) AssetDatabase.CreateFolder("Assets/Sprites/Enemigos", "WineMonster");
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs")) AssetDatabase.CreateFolder("Assets", "Prefabs");
        if (!AssetDatabase.IsValidFolder("Assets/Audio")) AssetDatabase.CreateFolder("Assets", "Audio");
    }

    private static Sprite[] ConfigureWineMonsterSprites()
    {
        Sprite[] sprites = new Sprite[16];
        for (int i = 1; i <= 16; i++)
        {
            string path = $"{WineMonsterSpriteFolder}/winemonster_{i}.png";
            if (!File.Exists(path)) continue;

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            TextureImporter imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp != null)
            {
                imp.textureType = TextureImporterType.Sprite;
                imp.spriteImportMode = SpriteImportMode.Single;
                imp.spritePixelsPerUnit = 12f; // 48px / 12 PPU = 4.0 unidades de alto (imponente jefe final)
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

    private static Sprite ConfigureRuinasBackgroundSprite()
    {
        if (!File.Exists(RuinasBgAssetPath))
        {
            Debug.LogWarning($"[Nivel4Setup] No se encontró {RuinasBgAssetPath}");
            return null;
        }

        AssetDatabase.ImportAsset(RuinasBgAssetPath, ImportAssetOptions.ForceSynchronousImport);
        TextureImporter imp = AssetImporter.GetAtPath(RuinasBgAssetPath) as TextureImporter;
        if (imp != null)
        {
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.spritePixelsPerUnit = 34f;
            imp.filterMode = FilterMode.Point;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.maxTextureSize = 2048;
            imp.mipmapEnabled = false;
            imp.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(RuinasBgAssetPath);
    }

    private static GameObject CreateOrUpdateWineMonsterPrefab(
        Sprite[] s,
        AudioClip sfxRoar,
        AudioClip sfxWave,
        AudioClip sfxSummon,
        AudioClip sfxStream,
        AudioClip sfxSeal)
    {
        GameObject temp = new GameObject("JefeWineMonster");
        try { temp.tag = "Enemy"; } catch { }

        SpriteRenderer sr = temp.AddComponent<SpriteRenderer>();
        sr.sprite = (s != null && s.Length > 0) ? s[0] : null;
        sr.sortingOrder = 4;

        Rigidbody2D rb = temp.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        BoxCollider2D col = temp.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(2.65f, 2.75f);
        col.offset = new Vector2(0f, -0.30f);

        AudioSource audioSrc = temp.AddComponent<AudioSource>();
        audioSrc.playOnAwake = false;
        audioSrc.spatialBlend = 0f;

        JefeWineMonster boss = temp.AddComponent<JefeWineMonster>();
        boss.maxHealth = 3000;
        boss.currentHealth = 3000;
        boss.danoPorCorchazo = 70;
        boss.danoContactoJugador = 24f;
        boss.cooldownContacto = 0.85f;
        boss.fuerzaEmpujeContacto = 7.5f;

        boss.arenaMinX = 215.0f;
        boss.arenaMaxX = 255.0f;
        boss.sueloY = 0f;
        boss.triggerCombateX = 216.5f;

        boss.prefabMerlot = AssetDatabase.LoadAssetAtPath<GameObject>(MerlotPrefabPath);
        boss.prefabCabSau = AssetDatabase.LoadAssetAtPath<GameObject>(CabSauPrefabPath);
        boss.prefabNebbiolo = AssetDatabase.LoadAssetAtPath<GameObject>(NebbioloPrefabPath);
        boss.prefabCorazon = AssetDatabase.LoadAssetAtPath<GameObject>(CorazonPrefabPath);
        boss.esbirrosParaCorazon = 6;
        boss.maxEsbirrosActivosFase1 = 4;
        boss.maxEsbirrosActivosFase2 = 6;

        boss.spriteBotellaSellado = AssetDatabase.LoadAssetAtPath<Sprite>(BottleCompletePath);
        boss.spriteAuraBotella = AssetDatabase.LoadAssetAtPath<Sprite>(BottleAuraPath);

        boss.audioSource = audioSrc;
        boss.sonidoRugido = sfxRoar;
        boss.sonidoOla = sfxWave;
        boss.sonidoInvocar = sfxSummon;
        boss.sonidoChorro = sfxStream;
        boss.sonidoSellado = sfxSeal;
        boss.sonidoGolpe = AssetDatabase.LoadAssetAtPath<AudioClip>(SfxGrapeHitPath);
        boss.sonidoCorazonSpawn = AssetDatabase.LoadAssetAtPath<AudioClip>(SfxHealPath);

        // Mapeo exacto verificado visualmente de los 16 sprites del Wine Monster (índice 0-based = #sprite - 1):
        // Idle/Movimiento: 1, 2, 11
        // Preparar Ola: 10, 7
        // Embestida Ola por piso: 14, 8, 3, 5
        // Invocar Uvas: 4, 13
        // Preparar Chorro (vórtice): 9
        // Disparar Chorro (Fase 2): 12, 6, 16
        // Erupción / Fase 2 / Sellado: 15
        if (s != null && s.Length >= 16)
        {
            boss.spritesIdleMovimiento = new Sprite[] { s[0], s[1], s[10] };
            boss.spritesPrepararOla = new Sprite[] { s[9], s[6] };
            boss.spritesEmbestidaOla = new Sprite[] { s[13], s[7], s[2], s[4] };
            boss.spritesInvocar = new Sprite[] { s[3], s[12] };
            boss.spritesPrepararChorro = new Sprite[] { s[8] };
            boss.spritesDispararChorro = new Sprite[] { s[11], s[5], s[15] };
            boss.spriteErupcion = s[14];
        }

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(temp, WineMonsterPrefabPath);
        UnityEngine.Object.DestroyImmediate(temp);
        return prefab;
    }

    // =========================================================================
    // CONSTRUCCIÓN DE LA ESCENA ASSETS/SCENES/NIVEL4.UNITY
    // =========================================================================
    private static void ConstructNivel4Scene(GameObject bossPrefab, Sprite ruinasBgSprite, bool keepNivel4Open)
    {
        string sourceScene = File.Exists(Nivel3ScenePath) ? Nivel3ScenePath : Nivel2ScenePath;
        if (!File.Exists(sourceScene))
        {
            Debug.LogWarning("[Nivel4Setup] No se encontró Nivel3.unity ni Nivel2.unity como base.");
            return;
        }

        string previousScenePath = EditorSceneManager.GetActiveScene().path;
        if (EditorSceneManager.GetActiveScene().isDirty)
        {
            EditorSceneManager.SaveOpenScenes();
        }

        if (File.Exists(Nivel4ScenePath))
        {
            AssetDatabase.DeleteAsset(Nivel4ScenePath);
        }
        AssetDatabase.CopyAsset(sourceScene, Nivel4ScenePath);
        AssetDatabase.ImportAsset(Nivel4ScenePath, ImportAssetOptions.ForceSynchronousImport);

        Scene scene4 = EditorSceneManager.OpenScene(Nivel4ScenePath, OpenSceneMode.Single);

        // 1. Limpiar objetos/tilemaps heredados manteniendo Character, Cámara, VineyardCanvas y GameOverManager
        CleanBaseSceneForNivel4(out Tilemap tilePiso, out Tilemap structuresMap, out Tilemap elementsMap);

        // 2. Configurar física suave del jugador (sin atorarse, salto ágil para plataformas de la arena)
        ConfigureSmoothPlayerInNivel4();

        // 3. Fondo de Cava en Ruinas (8 paneles espejados de X = -30 a X = 294)
        if (ruinasBgSprite != null)
        {
            BuildRuinasBackground(ruinasBgSprite);
        }

        // 4. Construir mapa extenso en Ruinas (X = 0..211) + Gran Arena del Jefe con Plataformas Elevadas (X = 212..257)
        AudioClip sfxAcid = AssetDatabase.LoadAssetAtPath<AudioClip>(SfxAcidSplashPath);
        GameObject bossGate = BuildRuinedCellarAndBossArena(tilePiso, structuresMap, elementsMap, sfxAcid);

        // 5. Colocar los 3 Checkpoints (Faroles de la Cava) — incluyendo uno justo antes de la Arena del Jefe
        SpawnCheckpointsInNivel4();

        // 6. Colocar los 3 Corazones Curativos del recorrido (incluido uno antes de entrar al jefe)
        SpawnHeartsInNivel4();

        // 7. Colocar enemigos a lo largo de las 4 Zonas en Ruinas (Merlot, Cab. Sau y Nebbiolo)
        SpawnRuinsEnemiesInNivel4();

        // 8. Crear HUD del Jefe (3000 HP + Fase + Esbirros 0/6 -> ♥) + MetaFinal oculta + Instanciar Wine Monster
        SpawnBossAndBossHUD(bossPrefab, bossGate);

        EditorSceneManager.MarkSceneDirty(scene4);
        EditorSceneManager.SaveScene(scene4);

        if (!keepNivel4Open && !string.IsNullOrEmpty(previousScenePath) && File.Exists(previousScenePath))
        {
            EditorSceneManager.OpenScene(previousScenePath, OpenSceneMode.Single);
        }
    }

    private static void CleanBaseSceneForNivel4(
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

        // Desactivar lluvia ácida (estamos bajo tierra en las ruinas de la cava)
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

        // Borrar contenedores de niveles anteriores
        string[] toDelete = new string[]
        {
            "Enemigos_Nivel1", "Enemigos_Nivel2", "Enemigos_Nivel3_Cava", "Enemigos_Nivel4_Ruinas",
            "ItemsCurativos_Nivel1", "ItemsCurativos_Nivel2", "ItemsCurativos_Nivel3", "ItemsCurativos_Nivel4",
            "InstalacionesBodega_Nivel2", "ExpansionZona4_Nivel2", "CavaSubterranea_Nivel3", "RuinasCava_Nivel4",
            "FondosBodega_Nivel2", "FondosCava_Nivel3", "FondosRuinas_Nivel4",
            "fondo1", "fondo (1)", "fondo (2)", "fondo1 (3)", "fondo1 (4)",
            "PiezasBotella_Nivel3", "GestorBotellaNivel3", "Checkpoints_Nivel3", "Checkpoints_Nivel4",
            "MetaNivel3", "MetaNivel2", "MetaNivel", "MetaNivel4_BossVictory",
            "BotellaHUDPanel", "AvisoBotellaCavaText", "BossHUDPanel", "AvisoNivel4Text", "SuperCorchazoHUDPanel"
        };
        for (int i = 0; i < toDelete.Length; i++)
        {
            GameObject g = GameObject.Find(toDelete[i]);
            if (g != null) UnityEngine.Object.DestroyImmediate(g);
        }

        // Borrar cualquier enemigo, jefe, pieza, checkpoint, corazón o meta suelta
        foreach (var e in UnityEngine.Object.FindObjectsByType<EnemigoCabSau>(FindObjectsSortMode.None))
            UnityEngine.Object.DestroyImmediate(e.gameObject);
        foreach (var e in UnityEngine.Object.FindObjectsByType<MerlotGrapeEnemy>(FindObjectsSortMode.None))
            UnityEngine.Object.DestroyImmediate(e.gameObject);
        foreach (var e in UnityEngine.Object.FindObjectsByType<EnemigoNebbiolo>(FindObjectsSortMode.None))
            UnityEngine.Object.DestroyImmediate(e.gameObject);
        foreach (var b in UnityEngine.Object.FindObjectsByType<JefeWineMonster>(FindObjectsSortMode.None))
            UnityEngine.Object.DestroyImmediate(b.gameObject);
        foreach (var h in UnityEngine.Object.FindObjectsByType<CorazonVida>(FindObjectsSortMode.None))
            UnityEngine.Object.DestroyImmediate(h.gameObject);
        foreach (var c in UnityEngine.Object.FindObjectsByType<CheckpointCava>(FindObjectsSortMode.None))
            UnityEngine.Object.DestroyImmediate(c.gameObject);
        foreach (var p in UnityEngine.Object.FindObjectsByType<PiezaBotellaColeccionable>(FindObjectsSortMode.None))
            UnityEngine.Object.DestroyImmediate(p.gameObject);
        foreach (var g in UnityEngine.Object.FindObjectsByType<GestorBotellaNivel3>(FindObjectsSortMode.None))
            UnityEngine.Object.DestroyImmediate(g.gameObject);
        foreach (var m in UnityEngine.Object.FindObjectsByType<MetaNivel>(FindObjectsSortMode.None))
            UnityEngine.Object.DestroyImmediate(m.gameObject);

        // Posicionar Character y cámaras al inicio de las Ruinas (X = 3.0, Y = 1.4)
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

        // Límites del mapa para X = 0..259
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
            limDer.transform.position = new Vector3(258.5f, 6f, 0f);
            BoxCollider2D c = limDer.GetComponent<BoxCollider2D>();
            if (c != null) { c.offset = Vector2.zero; c.size = new Vector2(1.5f, 40f); }
        }
        GameObject limAbajo = GameObject.Find("LimiteAbajo");
        if (limAbajo != null)
        {
            limAbajo.transform.position = new Vector3(130f, -8.5f, 0f);
            BoxCollider2D c = limAbajo.GetComponent<BoxCollider2D>();
            if (c != null) { c.offset = Vector2.zero; c.size = new Vector2(310f, 1.5f); }
        }
        GameObject limArriba = GameObject.Find("LimiteArriba");
        if (limArriba != null)
        {
            limArriba.transform.position = new Vector3(130f, 20f, 0f);
            BoxCollider2D c = limArriba.GetComponent<BoxCollider2D>();
            if (c != null) { c.offset = Vector2.zero; c.size = new Vector2(310f, 1.5f); }
        }
    }

    private static void ConfigureSmoothPlayerInNivel4()
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
            mov.velocidadMovimiento = 8.2f;
            mov.fuerzaSalto = 11.5f;
            mov.tiempoCoyote = 0.14f;
            mov.tiempoBufferSalto = 0.14f;
            mov.multiplicadorCaida = 1.32f;
            mov.capaSuelo = 1 << sueloLayer;
            if (puntoSuelo != null) mov.comprobadorSuelo = puntoSuelo;
        }
    }

    private static void BuildRuinasBackground(Sprite bgSprite)
    {
        GameObject container = new GameObject("FondosRuinas_Nivel4");
        float panelW = bgSprite.bounds.size.x;
        if (panelW < 1f) panelW = 40.47f;
        float stepX = panelW - 0.08f;
        float startX = -10f;
        float centerY = 7.2f;

        for (int i = 0; i < 8; i++)
        {
            GameObject panel = new GameObject($"FondoRuinas_{i + 1}");
            panel.transform.SetParent(container.transform);
            panel.transform.position = new Vector3(startX + i * stepX, centerY, 0f);
            panel.transform.localScale = new Vector3((i % 2 == 0) ? 1f : -1f, 1f, 1f);

            SpriteRenderer sr = panel.AddComponent<SpriteRenderer>();
            sr.sprite = bgSprite;
            sr.sortingOrder = -10;
            sr.color = new Color(0.90f, 0.85f, 0.88f, 1f);
        }
    }

    // =========================================================================
    // TERRENO DE LA CAVA EN RUINAS (X = 0..211) + ARENA DEL JEFE (X = 212..257)
    // =========================================================================
    private static GameObject BuildRuinedCellarAndBossArena(
        Tilemap tilePiso,
        Tilemap structuresMap,
        Tilemap elementsMap,
        AudioClip sfxAcid)
    {
        GameObject root = new GameObject("RuinasCava_Nivel4");
        int sueloLayer = LayerMask.NameToLayer("Suelo");
        if (sueloLayer < 0) sueloLayer = 6;

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

        // Muros de contención laterales
        PaintStoneBlock(tilePiso, -3, -1, -6, 14, stoneTop, stoneMid, stoneDeep, fallbackTop, fallbackDeep);
        PaintStoneBlock(tilePiso, 258, 261, -6, 16, stoneTop, stoneMid, stoneDeep, fallbackTop, fallbackDeep);

        // =====================================================================
        // ZONA 1 (X = 0..54): ENTRADA COLAPSADA DE LAS RUINAS
        // =====================================================================
        PaintStoneBlock(tilePiso, 0, 24, -6, -1, stoneTop, stoneMid, stoneDeep, fallbackTop, fallbackDeep);
        PaintPlatform(tilePiso, 9, 16, 3, 4, platTop, platBot, tileWood);
        PaintLine(tilePiso, 19, 24, 6, tileWood);

        // Foso Ácido 1 (X = 25..33) con pasarela rota
        PaintStoneBlock(tilePiso, 25, 33, -6, -5, stoneTop, stoneMid, stoneDeep, fallbackTop, fallbackDeep);
        PaintLine(tilePiso, 27, 30, 2, tileWood);
        CreateAcidPit(elementsMap, root.transform, "FosoRuinas_1", 25, 33, tileAcidSurf, tileAcidDeep, sfxAcid);

        // Isla en ruinas (X = 34..54) con doble galería derruida
        PaintStoneBlock(tilePiso, 34, 54, -6, -1, stoneTop, stoneMid, stoneDeep, fallbackTop, fallbackDeep);
        PaintPlatform(tilePiso, 37, 47, 3, 4, platTop, platBot, tileWood);
        PaintPlatform(tilePiso, 41, 46, 7, 8, platTop, platBot, tileWood);
        CreateLadder(elementsMap, root.transform, "Escalera_R1_Galeria", 37, 0, 4, tileLadder);

        if (structuresMap != null)
        {
            PaintRuinedArch4x4(structuresMap, 3, 0);
            PaintRuinedArch4x4(structuresMap, 40, 0);
        }
        if (barrelSprite != null)
        {
            CreateRuinedBarrelCluster(root.transform, "BarrilesRuina_Z1_A", 7.2f, 0f, barrelSprite, sueloLayer, solid: true);
            CreateRuinedBarrelCluster(root.transform, "BarrilesRuina_Z1_B", 51.5f, 0f, barrelSprite, sueloLayer, solid: true);
        }
        if (crateSprite != null)
        {
            CreateRuinedCrateRubble(root.transform, "CajasRuina_Z1", 17.5f, 0f, crateSprite, sueloLayer);
        }

        // =====================================================================
        // ZONA 2 (X = 55..112): GALERÍA DE BARRICAS QUEBRADAS + CHECKPOINT #1 (X = 72)
        // =====================================================================
        // Foso Ácido 2 (X = 55..63)
        PaintStoneBlock(tilePiso, 55, 63, -6, -5, stoneTop, stoneMid, stoneDeep, fallbackTop, fallbackDeep);
        PaintLine(tilePiso, 57, 58, 2, tileWood);
        PaintLine(tilePiso, 60, 61, 3, tileWood);
        CreateAcidPit(elementsMap, root.transform, "FosoRuinas_2", 55, 63, tileAcidSurf, tileAcidDeep, sfxAcid);

        // Plaza del Checkpoint #1 (X = 64..82)
        PaintStoneBlock(tilePiso, 64, 82, -6, -1, stoneTop, stoneMid, stoneDeep, fallbackTop, fallbackDeep);
        PaintPlatform(tilePiso, 67, 77, 4, 5, platTop, platBot, tileWood);
        CreateLadder(elementsMap, root.transform, "Escalera_R2_Plaza", 67, 0, 5, tileLadder);

        // Foso Ácido 3 (X = 83..91)
        PaintStoneBlock(tilePiso, 83, 91, -6, -5, stoneTop, stoneMid, stoneDeep, fallbackTop, fallbackDeep);
        PaintLine(tilePiso, 85, 89, 2, tileWood);
        CreateAcidPit(elementsMap, root.transform, "FosoRuinas_3", 83, 91, tileAcidSurf, tileAcidDeep, sfxAcid);

        // Bóveda de barricas derruidas (X = 92..112)
        PaintStoneBlock(tilePiso, 92, 112, -6, -1, stoneTop, stoneMid, stoneDeep, fallbackTop, fallbackDeep);
        PaintPlatform(tilePiso, 95, 102, 3, 4, platTop, platBot, tileWood);
        PaintPlatform(tilePiso, 104, 110, 6, 7, platTop, platBot, tileWood);
        CreateLadder(elementsMap, root.transform, "Escalera_R2_Barricas", 95, 0, 4, tileLadder);

        if (structuresMap != null)
        {
            PaintRuinedArch4x4(structuresMap, 70, 0);
            PaintRuinedArch4x4(structuresMap, 98, 0);
        }
        if (barrelSprite != null)
        {
            CreateRuinedBarrelCluster(root.transform, "BarrilesRuina_Z2_CP", 65.5f, 0f, barrelSprite, sueloLayer, solid: true);
            CreateRuinedBarrelCluster(root.transform, "BarrilesRuina_Z2_Boveda", 108.5f, 0f, barrelSprite, sueloLayer, solid: true);
        }
        if (crateSprite != null)
        {
            CreateRuinedCrateRubble(root.transform, "CajasRuina_Z2", 80.5f, 0f, crateSprite, sueloLayer);
        }

        // =====================================================================
        // ZONA 3 (X = 113..166): ABISMO DE LA BÓVEDA ROTA + CHECKPOINT #2 (X = 143.5)
        // =====================================================================
        // Foso Ácido 4 (X = 113..120)
        PaintStoneBlock(tilePiso, 113, 120, -6, -5, stoneTop, stoneMid, stoneDeep, fallbackTop, fallbackDeep);
        PaintLine(tilePiso, 115, 118, 2, tileWood);
        CreateAcidPit(elementsMap, root.transform, "FosoRuinas_4", 113, 120, tileAcidSurf, tileAcidDeep, sfxAcid);

        // Isla Pilar Agrietado (X = 121..128)
        PaintStoneBlock(tilePiso, 121, 128, -6, -1, stoneTop, stoneMid, stoneDeep, fallbackTop, fallbackDeep);
        PaintPlatform(tilePiso, 122, 127, 4, 5, platTop, platBot, tileWood);

        // Foso Ácido 5 (X = 129..137)
        PaintStoneBlock(tilePiso, 129, 137, -6, -5, stoneTop, stoneMid, stoneDeep, fallbackTop, fallbackDeep);
        PaintLine(tilePiso, 131, 132, 2, tileWood);
        PaintLine(tilePiso, 134, 135, 4, tileWood);
        CreateAcidPit(elementsMap, root.transform, "FosoRuinas_5", 129, 137, tileAcidSurf, tileAcidDeep, sfxAcid);

        // Terraza de Checkpoint #2 y Ruinas Colapsadas (X = 138..166)
        PaintStoneBlock(tilePiso, 138, 166, -6, -1, stoneTop, stoneMid, stoneDeep, fallbackTop, fallbackDeep);
        PaintPlatform(tilePiso, 148, 156, 3, 4, platTop, platBot, tileWood);
        PaintPlatform(tilePiso, 158, 164, 6, 7, platTop, platBot, tileWood);
        CreateLadder(elementsMap, root.transform, "Escalera_R3_Colapso", 148, 0, 4, tileLadder);

        if (structuresMap != null)
        {
            PaintRuinedArch4x4(structuresMap, 123, 0);
            PaintRuinedArch4x4(structuresMap, 142, 0);
            PaintRuinedArch4x4(structuresMap, 154, 0);
        }
        if (barrelSprite != null)
        {
            CreateRuinedBarrelCluster(root.transform, "BarrilesRuina_Z3_A", 140.0f, 0f, barrelSprite, sueloLayer, solid: true);
            CreateRuinedBarrelCluster(root.transform, "BarrilesRuina_Z3_B", 163.5f, 0f, barrelSprite, sueloLayer, solid: true);
        }

        // =====================================================================
        // ZONA 4 (X = 167..211): ANTESALA DEL MONSTRUO + CHECKPOINT #3 (X = 207.5)
        // =====================================================================
        // Foso Ácido 6 (X = 167..175)
        PaintStoneBlock(tilePiso, 167, 175, -6, -5, stoneTop, stoneMid, stoneDeep, fallbackTop, fallbackDeep);
        PaintLine(tilePiso, 169, 173, 2, tileWood);
        CreateAcidPit(elementsMap, root.transform, "FosoRuinas_6", 167, 175, tileAcidSurf, tileAcidDeep, sfxAcid);

        // Pasillo de escombros (X = 176..193)
        PaintStoneBlock(tilePiso, 176, 193, -6, -1, stoneTop, stoneMid, stoneDeep, fallbackTop, fallbackDeep);
        PaintPlatform(tilePiso, 179, 190, 4, 5, platTop, platBot, tileWood);
        CreateLadder(elementsMap, root.transform, "Escalera_R4_Escombros", 179, 0, 5, tileLadder);

        // Foso Ácido 7 (X = 194..201)
        PaintStoneBlock(tilePiso, 194, 201, -6, -5, stoneTop, stoneMid, stoneDeep, fallbackTop, fallbackDeep);
        PaintLine(tilePiso, 196, 199, 3, tileWood);
        CreateAcidPit(elementsMap, root.transform, "FosoRuinas_7", 194, 201, tileAcidSurf, tileAcidDeep, sfxAcid);

        // Santuario previo al Jefe Final (X = 202..211) con Checkpoint #3 (X = 207.5) y Corazón #3 (X = 204.5)
        PaintStoneBlock(tilePiso, 202, 211, -6, -1, stoneTop, stoneMid, stoneDeep, fallbackTop, fallbackDeep);
        if (structuresMap != null)
        {
            PaintRuinedArch4x4(structuresMap, 182, 0);
            PaintRuinedArch4x4(structuresMap, 205, 0);
        }
        if (barrelSprite != null)
        {
            CreateRuinedBarrelCluster(root.transform, "BarrilesRuina_Z4_Antesala", 177.5f, 0f, barrelSprite, sueloLayer, solid: true);
            CreateRuinedBarrelCluster(root.transform, "BarrilesRuina_Z4_EntradaJefe", 209.8f, 0f, barrelSprite, sueloLayer, solid: false);
        }
        if (crateSprite != null)
        {
            CreateRuinedCrateRubble(root.transform, "CajasRuina_Z4", 191.5f, 0f, crateSprite, sueloLayer);
        }

        // =====================================================================
        // GRAN ARENA DEL JEFE FINAL: WINE MONSTER (X = 212..257)
        // Piso plano limpio para la ola del Wine Monster + 5 Plataformas Elevadas en 2 niveles
        // =====================================================================
        PaintStoneBlock(tilePiso, 212, 257, -6, -1, stoneTop, stoneMid, stoneDeep, fallbackTop, fallbackDeep);

        // Nivel 1 de plataformas elevadas (Y = 4 => superficie Y = 5, salto fácil por encima de la ola!)
        PaintPlatform(tilePiso, 217, 223, 3, 4, platTop, platBot, tileWood); // Plataforma Izquierda
        PaintPlatform(tilePiso, 231, 238, 3, 4, platTop, platBot, tileWood); // Plataforma Central
        PaintPlatform(tilePiso, 246, 252, 3, 4, platTop, platBot, tileWood); // Plataforma Derecha

        // Nivel 2 de plataformas elevadas (Y = 8 => superficie Y = 9, refugio estratégico y posición de tiro)
        PaintPlatform(tilePiso, 223, 229, 7, 8, platTop, platBot, tileWood); // Balcón Superior Izquierdo
        PaintPlatform(tilePiso, 240, 246, 7, 8, platTop, platBot, tileWood); // Balcón Superior Derecho

        // Escaleras laterales para acceso rápido a las plataformas elevadas
        CreateLadder(elementsMap, root.transform, "Escalera_Arena_Izq", 217, 0, 4, tileLadder);
        CreateLadder(elementsMap, root.transform, "Escalera_Arena_Der", 252, 0, 4, tileLadder);

        // Decoración en ruinas de fondo dentro de la Arena (sin colisionador para no estorbar la pelea!)
        if (structuresMap != null)
        {
            PaintRuinedArch4x4(structuresMap, 220, 0);
            PaintRuinedArch4x4(structuresMap, 233, 0);
            PaintRuinedArch4x4(structuresMap, 246, 0);
        }
        if (barrelSprite != null)
        {
            CreateRuinedBarrelCluster(root.transform, "BarrilesFondo_Arena_Izq", 215.0f, 0f, barrelSprite, sueloLayer, solid: false);
            CreateRuinedBarrelCluster(root.transform, "BarrilesFondo_Arena_Centro", 234.5f, 0f, barrelSprite, sueloLayer, solid: false);
            CreateRuinedBarrelCluster(root.transform, "BarrilesFondo_Arena_Der", 254.5f, 0f, barrelSprite, sueloLayer, solid: false);
        }

        // Arcos monumentales agrietados a lo largo de las ruinas y enmarcando la Arena
        if (archSprite != null)
        {
            float[] archXs = new float[] { 4.5f, 44.0f, 72.0f, 102.0f, 143.5f, 184.0f, 211.5f, 234.5f, 255.0f };
            for (int i = 0; i < archXs.Length; i++)
            {
                GameObject arch = new GameObject($"ArcoRuinas_{i + 1}");
                arch.transform.SetParent(root.transform);
                arch.transform.position = new Vector3(archXs[i], 1.45f, 0f);
                arch.transform.localScale = new Vector3(1.55f, 1.55f, 1f);
                // Leve inclinación en algunos arcos para acentuar el deterioro de ruinas
                float tilt = (i % 3 == 1) ? -2.2f : (i % 3 == 2 ? 1.8f : 0f);
                arch.transform.rotation = Quaternion.Euler(0f, 0f, tilt);

                SpriteRenderer sr = arch.AddComponent<SpriteRenderer>();
                sr.sprite = archSprite;
                sr.sortingOrder = -1;
                sr.color = new Color(0.82f, 0.78f, 0.80f, 1f);
            }
        }

        // Puerta sellable de la Arena del Jefe (se activa al cruzar X = 216.5)
        GameObject bossGate = CreateBossArenaGate(root.transform, sueloLayer);

        RefreshTilemapAndColliders(tilePiso);
        if (structuresMap != null) RefreshTilemapAndColliders(structuresMap);
        if (elementsMap != null) RefreshTilemapAndColliders(elementsMap);
        Physics2D.SyncTransforms();

        return bossGate;
    }

    private static GameObject CreateBossArenaGate(Transform parent, int sueloLayer)
    {
        GameObject gate = new GameObject("PuertaArenaJefe");
        gate.transform.SetParent(parent);
        gate.transform.position = new Vector3(212.5f, 6.0f, 0f);
        gate.layer = sueloLayer;
        try { gate.tag = "Obstacle"; } catch { }

        // Textura pixel art de reja / piedra sellada de la cava
        Texture2D tex = new Texture2D(16, 64, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        Color ironDark = new Color(0.18f, 0.14f, 0.16f, 1f);
        Color ironMid = new Color(0.36f, 0.28f, 0.32f, 1f);
        Color runeGlow = new Color(0.88f, 0.18f, 0.30f, 1f);
        Color clear = new Color(0.12f, 0.05f, 0.08f, 0.55f);

        for (int y = 0; y < 64; y++)
        {
            for (int x = 0; x < 16; x++)
            {
                bool bar = (x == 2 || x == 3 || x == 7 || x == 8 || x == 12 || x == 13);
                bool cross = (y % 12 == 0 || y % 12 == 1);
                if (x == 0 || x == 15 || y == 0 || y == 63)
                    tex.SetPixel(x, y, ironDark);
                else if (cross && (x == 7 || x == 8))
                    tex.SetPixel(x, y, runeGlow);
                else if (bar || cross)
                    tex.SetPixel(x, y, (x % 2 == 0) ? ironMid : ironDark);
                else
                    tex.SetPixel(x, y, clear);
            }
        }
        tex.Apply();

        SpriteRenderer sr = gate.AddComponent<SpriteRenderer>();
        sr.sprite = Sprite.Create(tex, new Rect(0, 0, 16, 64), new Vector2(0.5f, 0.5f), 8f);
        sr.sortingOrder = 2;
        gate.transform.localScale = new Vector3(0.75f, 1.55f, 1f);

        BoxCollider2D col = gate.AddComponent<BoxCollider2D>();
        col.size = new Vector2(1.4f, 8.2f);

        gate.SetActive(false); // Se activa cuando el jugador entra a la arena
        return gate;
    }

    // =========================================================================
    // CHECKPOINTS, CORAZONES Y ENEMIGOS DEL RECORRIDO
    // =========================================================================
    private static void SpawnCheckpointsInNivel4()
    {
        GameObject container = new GameObject("Checkpoints_Nivel4");
        Sprite offSprite = AssetDatabase.LoadAssetAtPath<Sprite>(CheckpointOffPath);
        Sprite onSprite = AssetDatabase.LoadAssetAtPath<Sprite>(CheckpointOnPath);
        Sprite auraSprite = AssetDatabase.LoadAssetAtPath<Sprite>(BottleAuraPath);
        AudioClip sfxCp = AssetDatabase.LoadAssetAtPath<AudioClip>(SfxCheckpointPath);

        (string name, string zona, Vector3 pos)[] cps = new (string, string, Vector3)[]
        {
            ("Checkpoint_1_RuinasBarricas", "Ruinas de las Barricas (1/3)", new Vector3(72.0f, 0.0f, 0f)),
            ("Checkpoint_2_BovedaColapsada", "Bóveda Colapsada (2/3)", new Vector3(143.5f, 0.0f, 0f)),
            ("Checkpoint_3_PuertaJefeFinal", "Puerta del Jefe Final (3/3)", new Vector3(207.5f, 0.0f, 0f)),
        };

        for (int i = 0; i < cps.Length; i++)
        {
            GameObject cpObj = new GameObject(cps[i].name);
            cpObj.transform.SetParent(container.transform);
            cpObj.transform.position = cps[i].pos;

            SpriteRenderer sr = cpObj.AddComponent<SpriteRenderer>();
            sr.sprite = offSprite;
            sr.sortingOrder = 2;

            BoxCollider2D col = cpObj.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(2.4f, 3.6f);
            col.offset = new Vector2(0f, 1.6f);

            GameObject halo = new GameObject("HaloFarol");
            halo.transform.SetParent(cpObj.transform);
            halo.transform.localPosition = new Vector3(0f, 1.75f, 0f);
            halo.transform.localScale = new Vector3(0.85f, 0.85f, 1f);
            SpriteRenderer haloSr = halo.AddComponent<SpriteRenderer>();
            haloSr.sprite = auraSprite;
            haloSr.sortingOrder = 1;
            haloSr.enabled = false;

            CheckpointCava cp = cpObj.AddComponent<CheckpointCava>();
            cp.indiceCheckpoint = i;
            cp.nombreZona = cps[i].zona;
            cp.spriteApagado = offSprite;
            cp.spriteEncendido = onSprite;
            cp.haloLuz = haloSr;
            cp.curacionAlActivar = 30f;
            cp.sonidoActivar = sfxCp;
        }
    }

    private static void SpawnHeartsInNivel4()
    {
        GameObject heartPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CorazonPrefabPath);
        if (heartPrefab == null) return;

        GameObject container = new GameObject("ItemsCurativos_Nivel4");
        Vector3[] positions = new Vector3[]
        {
            new Vector3(43.5f, 9.2f, 0f),   // Zona 1: galería alta en ruinas
            new Vector3(107.0f, 8.2f, 0f),  // Zona 2: bóveda de barricas
            new Vector3(204.5f, 1.6f, 0f)   // Zona 4: santuario previo al Jefe Final (entra con vida llena!)
        };

        for (int i = 0; i < positions.Length; i++)
        {
            GameObject h = (GameObject)PrefabUtility.InstantiatePrefab(heartPrefab);
            h.name = $"CorazonRuinas_{i + 1}";
            h.transform.SetParent(container.transform);
            h.transform.position = positions[i];
        }
    }

    private static void SpawnRuinsEnemiesInNivel4()
    {
        GameObject merlotPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MerlotPrefabPath);
        GameObject cabSauPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CabSauPrefabPath);
        GameObject nebbioloPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(NebbioloPrefabPath);

        GameObject container = new GameObject("Enemigos_Nivel4_Ruinas");

        // Mezcla variada de los 3 tipos de uvas a lo largo de las 4 zonas en ruinas
        (string kind, string name, Vector3 pos)[] spawns = new (string, string, Vector3)[]
        {
            // Zona 1
            ("merlot",   "Merlot_R1_Balcon",    new Vector3(13.0f, 5.2f, 0f)),
            ("cabsau",   "CabSau_R1_Entrada",   new Vector3(21.0f, 1.4f, 0f)),
            ("nebbiolo", "Nebbiolo_R1_Galeria", new Vector3(42.5f, 5.2f, 0f)),
            ("merlot",   "Merlot_R1_Salida",    new Vector3(49.0f, 1.4f, 0f)),

            // Zona 2
            ("cabsau",   "CabSau_R2_Plaza",     new Vector3(75.5f, 6.2f, 0f)),
            ("nebbiolo", "Nebbiolo_R2_Suelo",   new Vector3(96.0f, 1.4f, 0f)),
            ("merlot",   "Merlot_R2_Balcon",    new Vector3(99.0f, 5.2f, 0f)),
            ("cabsau",   "CabSau_R2_Barricas",  new Vector3(106.0f, 1.4f, 0f)),

            // Zona 3
            ("nebbiolo", "Nebbiolo_R3_Isla",    new Vector3(124.5f, 1.4f, 0f)),
            ("merlot",   "Merlot_R3_PilarAlto", new Vector3(124.5f, 6.2f, 0f)),
            ("cabsau",   "CabSau_R3_Colapso",   new Vector3(152.5f, 5.2f, 0f)),
            ("nebbiolo", "Nebbiolo_R3_Suelo",   new Vector3(158.5f, 1.4f, 0f)),
            ("merlot",   "Merlot_R3_Alto",      new Vector3(161.0f, 8.2f, 0f)),

            // Zona 4 (Antesala)
            ("nebbiolo", "Nebbiolo_R4_Pasillo", new Vector3(181.5f, 1.4f, 0f)),
            ("cabsau",   "CabSau_R4_Alto",      new Vector3(185.0f, 6.2f, 0f)),
            ("merlot",   "Merlot_R4_Guardia",   new Vector3(189.5f, 1.4f, 0f)),
        };

        int sueloLayer = LayerMask.NameToLayer("Suelo");
        int sueloMask = sueloLayer >= 0 ? (1 << sueloLayer) : (1 << 6);

        for (int i = 0; i < spawns.Length; i++)
        {
            GameObject prefab = spawns[i].kind == "nebbiolo" ? nebbioloPrefab
                              : (spawns[i].kind == "cabsau" ? cabSauPrefab : merlotPrefab);
            if (prefab == null) continue;

            GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            inst.name = spawns[i].name;
            inst.transform.SetParent(container.transform);
            inst.transform.position = spawns[i].pos;

            EnemigoCabSau cab = inst.GetComponent<EnemigoCabSau>();
            if (cab != null)
            {
                cab.capaSuelo = sueloMask;
                cab.velocidadSprint = 6.0f;
                EditorUtility.SetDirty(cab);
            }
        }
    }

    // =========================================================================
    // JEFE FINAL WINE MONSTER + HUD DE 3000 HP + META FINAL OCULTA
    // =========================================================================
    private static void SpawnBossAndBossHUD(GameObject bossPrefab, GameObject bossGate)
    {
        // 1. Crear MetaNivel4_BossVictory (disparada únicamente por JefeWineMonster al sellarlo en la botella)
        MetaNivel metaFinal = null;
        GameObject metaPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MetaPrefabPath);
        if (metaPrefab != null)
        {
            GameObject metaObj = (GameObject)PrefabUtility.InstantiatePrefab(metaPrefab);
            metaObj.name = "MetaNivel4_BossVictory";
            metaObj.transform.position = new Vector3(235.0f, 1.5f, 0f);
            // Ocultar bandera y desactivar collider: la victoria se dispara al sellar al jefe
            SpriteRenderer msr = metaObj.GetComponent<SpriteRenderer>();
            if (msr != null) msr.enabled = false;
            Collider2D mcol = metaObj.GetComponent<Collider2D>();
            if (mcol != null) mcol.enabled = false;

            metaFinal = metaObj.GetComponent<MetaNivel>();
            if (metaFinal != null)
            {
                metaFinal.tituloPersonalizado = "¡JUEGO COMPLETADO!\nWine Monster Sellado";
            }
        }

        // 2. Construir HUD del Jefe Final dentro de VineyardCanvas
        Canvas canvas = UnityEngine.Object.FindAnyObjectByType<Canvas>();
        GameObject bossHudPanel = null;
        Slider hpSlider = null;
        Text hpText = null;
        Text phaseText = null;
        Text minionHeartText = null;
        Text bannerText = null;

        if (canvas != null)
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // Panel superior central para la barra de vida del Jefe (3000 HP)
            bossHudPanel = new GameObject("BossHUDPanel");
            bossHudPanel.transform.SetParent(canvas.transform, false);
            RectTransform panelRt = bossHudPanel.AddComponent<RectTransform>();
            panelRt.anchorMin = new Vector2(0.5f, 1f);
            panelRt.anchorMax = new Vector2(0.5f, 1f);
            panelRt.pivot = new Vector2(0.5f, 1f);
            panelRt.anchoredPosition = new Vector2(0f, -14f);
            panelRt.sizeDelta = new Vector2(560f, 92f);

            Image panelBg = bossHudPanel.AddComponent<Image>();
            panelBg.color = new Color(0.14f, 0.03f, 0.07f, 0.88f);

            Outline panelOutline = bossHudPanel.AddComponent<Outline>();
            panelOutline.effectColor = new Color(0.86f, 0.22f, 0.34f, 0.95f);
            panelOutline.effectDistance = new Vector2(2f, -2f);

            // Texto de nombre + HP (3000 / 3000)
            GameObject titleObj = new GameObject("BossHealthText");
            titleObj.transform.SetParent(bossHudPanel.transform, false);
            RectTransform titleRt = titleObj.AddComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0f, 1f);
            titleRt.anchorMax = new Vector2(1f, 1f);
            titleRt.pivot = new Vector2(0.5f, 1f);
            titleRt.anchoredPosition = new Vector2(0f, -6f);
            titleRt.sizeDelta = new Vector2(-20f, 24f);
            hpText = titleObj.AddComponent<Text>();
            hpText.font = font;
            hpText.fontSize = 18;
            hpText.fontStyle = FontStyle.Bold;
            hpText.alignment = TextAnchor.MiddleCenter;
            hpText.color = new Color(1.0f, 0.88f, 0.72f, 1f);
            hpText.text = "WINE MONSTER  —  3000 / 3000 HP";
            titleObj.AddComponent<Shadow>().effectColor = Color.black;

            // Barra de Vida Slider
            GameObject sliderObj = new GameObject("BossHealthSlider");
            sliderObj.transform.SetParent(bossHudPanel.transform, false);
            RectTransform sliderRt = sliderObj.AddComponent<RectTransform>();
            sliderRt.anchorMin = new Vector2(0.5f, 1f);
            sliderRt.anchorMax = new Vector2(0.5f, 1f);
            sliderRt.pivot = new Vector2(0.5f, 1f);
            sliderRt.anchoredPosition = new Vector2(0f, -33f);
            sliderRt.sizeDelta = new Vector2(510f, 22f);

            Image bgImg = sliderObj.AddComponent<Image>();
            bgImg.color = new Color(0.24f, 0.08f, 0.12f, 1f);

            GameObject fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(sliderObj.transform, false);
            RectTransform faRt = fillArea.AddComponent<RectTransform>();
            faRt.anchorMin = Vector2.zero;
            faRt.anchorMax = Vector2.one;
            faRt.offsetMin = new Vector2(3f, 3f);
            faRt.offsetMax = new Vector2(-3f, -3f);

            GameObject fill = new GameObject("Fill");
            fill.transform.SetParent(fillArea.transform, false);
            RectTransform fillRt = fill.AddComponent<RectTransform>();
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = Vector2.one;
            fillRt.offsetMin = Vector2.zero;
            fillRt.offsetMax = Vector2.zero;
            Image fillImg = fill.AddComponent<Image>();
            fillImg.color = new Color(0.84f, 0.10f, 0.24f, 1f);

            hpSlider = sliderObj.AddComponent<Slider>();
            hpSlider.interactable = false;
            hpSlider.transition = Selectable.Transition.None;
            hpSlider.fillRect = fillRt;
            hpSlider.minValue = 0;
            hpSlider.maxValue = 3000;
            hpSlider.value = 3000;

            // Subtítulo de Fase (izquierda) y Contador 0/6 -> Corazón (derecha)
            GameObject phaseObj = new GameObject("BossPhaseText");
            phaseObj.transform.SetParent(bossHudPanel.transform, false);
            RectTransform phaseRt = phaseObj.AddComponent<RectTransform>();
            phaseRt.anchorMin = new Vector2(0f, 0f);
            phaseRt.anchorMax = new Vector2(0.52f, 0f);
            phaseRt.pivot = new Vector2(0f, 0f);
            phaseRt.anchoredPosition = new Vector2(14f, 6f);
            phaseRt.sizeDelta = new Vector2(-14f, 22f);
            phaseText = phaseObj.AddComponent<Text>();
            phaseText.font = font;
            phaseText.fontSize = 13;
            phaseText.fontStyle = FontStyle.Bold;
            phaseText.alignment = TextAnchor.MiddleLeft;
            phaseText.color = new Color(1.0f, 0.65f, 0.72f, 1f);
            phaseText.text = "FASE I: MAREA DE LA CAVA";

            GameObject minionObj = new GameObject("BossMinionHeartText");
            minionObj.transform.SetParent(bossHudPanel.transform, false);
            RectTransform minionRt = minionObj.AddComponent<RectTransform>();
            minionRt.anchorMin = new Vector2(0.48f, 0f);
            minionRt.anchorMax = new Vector2(1f, 0f);
            minionRt.pivot = new Vector2(1f, 0f);
            minionRt.anchoredPosition = new Vector2(-14f, 6f);
            minionRt.sizeDelta = new Vector2(-14f, 22f);
            minionHeartText = minionObj.AddComponent<Text>();
            minionHeartText.font = font;
            minionHeartText.fontSize = 13;
            minionHeartText.fontStyle = FontStyle.Bold;
            minionHeartText.alignment = TextAnchor.MiddleRight;
            minionHeartText.color = new Color(0.60f, 1.0f, 0.76f, 1f);
            minionHeartText.text = "Uvas invocadas derrotadas: 0 / 6  →  ♥ Corazón";

            bossHudPanel.SetActive(false);

            // Banner de avisos flotantes (Checkpoints, Fase 2, Corazón, Sellado)
            GameObject bannerObj = new GameObject("AvisoNivel4Text");
            bannerObj.transform.SetParent(canvas.transform, false);
            RectTransform bannerRt = bannerObj.AddComponent<RectTransform>();
            bannerRt.anchorMin = new Vector2(0.5f, 1f);
            bannerRt.anchorMax = new Vector2(0.5f, 1f);
            bannerRt.pivot = new Vector2(0.5f, 1f);
            bannerRt.anchoredPosition = new Vector2(0f, -118f);
            bannerRt.sizeDelta = new Vector2(780f, 44f);

            bannerText = bannerObj.AddComponent<Text>();
            bannerText.font = font;
            bannerText.fontSize = 20;
            bannerText.fontStyle = FontStyle.Bold;
            bannerText.alignment = TextAnchor.MiddleCenter;
            bannerText.color = new Color(1.0f, 0.94f, 0.56f, 1f);
            bannerText.text = "";
            Outline bannerOutline = bannerObj.AddComponent<Outline>();
            bannerOutline.effectColor = Color.black;
            bannerOutline.effectDistance = new Vector2(2f, -2f);
            bannerObj.SetActive(false);

            // =================================================================
            // HUD DEL GOLPE ESPECIAL DEL JUGADOR ("SUPER CORCHAZO DE RESERVA")
            // =================================================================
            GameObject superPanel = new GameObject("SuperCorchazoHUDPanel");
            superPanel.transform.SetParent(canvas.transform, false);
            RectTransform spRt = superPanel.AddComponent<RectTransform>();
            spRt.anchorMin = new Vector2(0f, 1f);
            spRt.anchorMax = new Vector2(0f, 1f);
            spRt.pivot = new Vector2(0f, 1f);
            spRt.anchoredPosition = new Vector2(20f, -78f);
            spRt.sizeDelta = new Vector2(290f, 50f);

            Image spBg = superPanel.AddComponent<Image>();
            spBg.color = new Color(0.14f, 0.09f, 0.03f, 0.86f);
            Outline spOut = superPanel.AddComponent<Outline>();
            spOut.effectColor = new Color(0.95f, 0.76f, 0.22f, 0.95f);
            spOut.effectDistance = new Vector2(2f, -2f);

            GameObject spTxtObj = new GameObject("SuperCorchazoText");
            spTxtObj.transform.SetParent(superPanel.transform, false);
            RectTransform spTxtRt = spTxtObj.AddComponent<RectTransform>();
            spTxtRt.anchorMin = new Vector2(0f, 1f);
            spTxtRt.anchorMax = new Vector2(1f, 1f);
            spTxtRt.pivot = new Vector2(0.5f, 1f);
            spTxtRt.anchoredPosition = new Vector2(0f, -4f);
            spTxtRt.sizeDelta = new Vector2(-12f, 20f);
            Text spText = spTxtObj.AddComponent<Text>();
            spText.font = font;
            spText.fontSize = 12;
            spText.fontStyle = FontStyle.Bold;
            spText.alignment = TextAnchor.MiddleCenter;
            spText.color = new Color(0.98f, 0.90f, 0.65f, 1f);
            spText.text = "SUPER CORCHAZO: 50%  (Pelea para cargar)";
            spTxtObj.AddComponent<Shadow>().effectColor = Color.black;

            GameObject spSliderObj = new GameObject("SuperCorchazoSlider");
            spSliderObj.transform.SetParent(superPanel.transform, false);
            RectTransform spSlRt = spSliderObj.AddComponent<RectTransform>();
            spSlRt.anchorMin = new Vector2(0.5f, 0f);
            spSlRt.anchorMax = new Vector2(0.5f, 0f);
            spSlRt.pivot = new Vector2(0.5f, 0f);
            spSlRt.anchoredPosition = new Vector2(0f, 7f);
            spSlRt.sizeDelta = new Vector2(266f, 16f);

            Image spSlBg = spSliderObj.AddComponent<Image>();
            spSlBg.color = new Color(0.24f, 0.16f, 0.06f, 1f);

            GameObject spFillArea = new GameObject("Fill Area");
            spFillArea.transform.SetParent(spSliderObj.transform, false);
            RectTransform spFaRt = spFillArea.AddComponent<RectTransform>();
            spFaRt.anchorMin = Vector2.zero;
            spFaRt.anchorMax = Vector2.one;
            spFaRt.offsetMin = new Vector2(2f, 2f);
            spFaRt.offsetMax = new Vector2(-2f, -2f);

            GameObject spFill = new GameObject("Fill");
            spFill.transform.SetParent(spFillArea.transform, false);
            RectTransform spFillRt = spFill.AddComponent<RectTransform>();
            spFillRt.anchorMin = Vector2.zero;
            spFillRt.anchorMax = Vector2.one;
            spFillRt.offsetMin = Vector2.zero;
            spFillRt.offsetMax = Vector2.zero;
            Image spFillImg = spFill.AddComponent<Image>();
            spFillImg.color = new Color(0.95f, 0.72f, 0.16f, 1f);

            Slider spSlider = spSliderObj.AddComponent<Slider>();
            spSlider.interactable = false;
            spSlider.transition = Selectable.Transition.None;
            spSlider.fillRect = spFillRt;
            spSlider.minValue = 0f;
            spSlider.maxValue = 100f;
            spSlider.value = 50f;

            // Conectar JugadorGolpeEspecial al Character
            GameObject character = GameObject.Find("Character");
            if (character != null)
            {
                JugadorGolpeEspecial especial = character.GetComponent<JugadorGolpeEspecial>();
                if (especial == null) especial = character.AddComponent<JugadorGolpeEspecial>();
                especial.cargaMaxima = 100f;
                especial.cargaActual = 50f;
                especial.danoEspecialAlJefe = 350;
                especial.barraCargaEspecial = spSlider;
                especial.imagenRellenoBarra = spFillImg;
                especial.textoEstadoEspecial = spText;
                especial.sonidoLanzarSuperCorcho = AssetDatabase.LoadAssetAtPath<AudioClip>(SfxSuperCorkPath);
                especial.sonidoCargaLista = AssetDatabase.LoadAssetAtPath<AudioClip>(SfxSuperCorkReadyPath);
                EditorUtility.SetDirty(especial);
            }
        }

        // 3. Instanciar Jefe Wine Monster en el lado derecho de la Arena (X = 247, Y = 2.0)
        if (bossPrefab != null)
        {
            GameObject bossInst = (GameObject)PrefabUtility.InstantiatePrefab(bossPrefab);
            bossInst.name = "JefeWineMonster";
            bossInst.transform.position = new Vector3(247.0f, 2.0f, 0f);

            JefeWineMonster boss = bossInst.GetComponent<JefeWineMonster>();
            if (boss != null)
            {
                boss.puertaBloqueoArena = bossGate;
                boss.metaFinalNivel4 = metaFinal;
                boss.panelHudBoss = bossHudPanel;
                boss.barraVidaBoss = hpSlider;
                boss.textoVidaBoss = hpText;
                boss.textoFaseBoss = phaseText;
                boss.textoEsbirrosCorazon = minionHeartText;
                boss.textoBannerAviso = bannerText;
                EditorUtility.SetDirty(boss);
            }
        }
    }

    // =========================================================================
    // BUILD SETTINGS: NIVEL 1 -> NIVEL 2 -> NIVEL 3 -> NIVEL 4
    // =========================================================================
    private static void RegisterAllFourScenesInBuildSettings()
    {
        var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>();
        if (File.Exists(Nivel1ScenePath)) scenes.Add(new EditorBuildSettingsScene(Nivel1ScenePath, true));
        if (File.Exists(Nivel2ScenePath)) scenes.Add(new EditorBuildSettingsScene(Nivel2ScenePath, true));
        if (File.Exists(Nivel3ScenePath)) scenes.Add(new EditorBuildSettingsScene(Nivel3ScenePath, true));
        if (File.Exists(Nivel4ScenePath)) scenes.Add(new EditorBuildSettingsScene(Nivel4ScenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    // =========================================================================
    // HELPERS DE CONSTRUCCIÓN DE RUINAS Y PROPS (BARRICAS VOLCADAS / CAJAS ROTAS)
    // =========================================================================
    private static void CreateRuinedBarrelCluster(
        Transform parent,
        string name,
        float baseX,
        float floorY,
        Sprite barrelSprite,
        int sueloLayer,
        bool solid)
    {
        GameObject cluster = new GameObject(name);
        cluster.transform.SetParent(parent);

        // Barrica base de pie ligeramente desgastada
        CreateSingleBarrel(cluster.transform, "Barrica_Pie", new Vector3(baseX, floorY + 0.60f, 0f), 0f, barrelSprite, sueloLayer, solid);
        // Barrica volcada / inclinada al lado (estilo ruinas deterioradas)
        CreateSingleBarrel(cluster.transform, "Barrica_Volcada", new Vector3(baseX + 1.15f, floorY + 0.48f, 0f), -68f, barrelSprite, sueloLayer, false);
        // Tercera barrica apilada e inclinada
        CreateSingleBarrel(cluster.transform, "Barrica_Rota", new Vector3(baseX + 0.45f, floorY + 1.55f, 0f), 16f, barrelSprite, sueloLayer, false);
    }

    private static void CreateSingleBarrel(
        Transform parent,
        string name,
        Vector3 pos,
        float rotZ,
        Sprite sprite,
        int sueloLayer,
        bool addCollider)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent);
        obj.transform.position = pos;
        obj.transform.rotation = Quaternion.Euler(0f, 0f, rotZ);
        obj.transform.localScale = new Vector3(1.05f, 1.05f, 1f);

        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = addCollider ? 1 : -2;
        sr.color = new Color(0.84f, 0.78f, 0.76f, 1f);

        if (addCollider)
        {
            obj.layer = sueloLayer;
            BoxCollider2D col = obj.AddComponent<BoxCollider2D>();
            col.size = new Vector2(0.95f, 1.15f);
        }
    }

    private static void CreateRuinedCrateRubble(
        Transform parent,
        string name,
        float baseX,
        float floorY,
        Sprite crateSprite,
        int sueloLayer)
    {
        GameObject rubble = new GameObject(name);
        rubble.transform.SetParent(parent);

        GameObject c1 = new GameObject("Caja_Base");
        c1.transform.SetParent(rubble.transform);
        c1.transform.position = new Vector3(baseX, floorY + 0.52f, 0f);
        c1.transform.rotation = Quaternion.Euler(0f, 0f, -6f);
        c1.layer = sueloLayer;
        SpriteRenderer sr1 = c1.AddComponent<SpriteRenderer>();
        sr1.sprite = crateSprite;
        sr1.sortingOrder = 1;
        sr1.color = new Color(0.80f, 0.74f, 0.72f, 1f);
        c1.AddComponent<BoxCollider2D>().size = new Vector2(0.95f, 0.95f);

        GameObject c2 = new GameObject("Caja_Caida");
        c2.transform.SetParent(rubble.transform);
        c2.transform.position = new Vector3(baseX + 0.95f, floorY + 0.44f, 0f);
        c2.transform.rotation = Quaternion.Euler(0f, 0f, 24f);
        SpriteRenderer sr2 = c2.AddComponent<SpriteRenderer>();
        sr2.sprite = crateSprite;
        sr2.sortingOrder = -2;
        sr2.color = new Color(0.72f, 0.66f, 0.65f, 1f);
    }

    private static void PaintRuinedArch4x4(Tilemap structuresMap, int startX, int startY)
    {
        int[][] ids = new int[][]
        {
            new int[] { 124, 125, 126, 127 },
            new int[] { 108, 109, 110, 111 },
            new int[] { 92,  93,  94,  95  },
            new int[] { 76,  77,  78,  79  }
        };
        for (int r = 0; r < ids.Length; r++)
        {
            for (int c = 0; c < ids[r].Length; c++)
            {
                // Omitir una piedra superior para darle aspecto agrietado / en ruinas
                if (r == 3 && c == 3 && (startX % 2 == 0)) continue;
                TileBase t = LoadTile($"Assets/Tiles/Nivel2/tileset-ok-lvl2_{ids[r][c]}.asset");
                if (t != null)
                {
                    structuresMap.SetTile(new Vector3Int(startX + c, startY + r, 0), t);
                }
            }
        }
    }

    private static void CreateAcidPit(
        Tilemap elementsMap,
        Transform parent,
        string name,
        int xMin,
        int xMax,
        TileBase surfTile,
        TileBase deepTile,
        AudioClip sfxAcid)
    {
        if (elementsMap != null)
        {
            for (int x = xMin; x <= xMax; x++)
            {
                if (surfTile != null) elementsMap.SetTile(new Vector3Int(x, -2, 0), surfTile);
                if (deepTile != null)
                {
                    elementsMap.SetTile(new Vector3Int(x, -3, 0), deepTile);
                    elementsMap.SetTile(new Vector3Int(x, -4, 0), deepTile);
                }
            }
        }

        int width = xMax - xMin + 1;
        GameObject pit = new GameObject(name);
        pit.transform.SetParent(parent);
        pit.transform.position = new Vector3(xMin + width * 0.5f, -2.5f, 0f);

        BoxCollider2D col = pit.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(width, 2.2f);

        ZonaAcido zona = pit.AddComponent<ZonaAcido>();
        zona.danoPorContacto = 22f;
        zona.intervaloDano = 0.8f;
        zona.impulsoReboteVertical = 11.0f;
        zona.sonidoSalpicaduraAcido = sfxAcid;

        GameObject vfx = new GameObject("BurbujasAcido_VFX");
        vfx.transform.SetParent(pit.transform);
        vfx.transform.localPosition = new Vector3(0f, 0.9f, 0f);
        ParticleSystem ps = vfx.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, 1.5f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.4f, 1.0f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.16f);
        main.gravityModifier = -0.15f;
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.45f, 0.98f, 0.18f, 0.85f), new Color(0.82f, 1f, 0.32f, 0.9f));
        var em = ps.emission;
        em.rateOverTime = 12f;
        var sh = ps.shape;
        sh.shapeType = ParticleSystemShapeType.Box;
        sh.scale = new Vector3(width * 0.9f, 0.25f, 0.1f);
        var psr = vfx.GetComponent<ParticleSystemRenderer>();
        if (psr != null)
        {
            psr.material = EnemyDeathFeedback.GetPixelParticleMaterial();
            psr.sortingOrder = 5;
        }
    }

    private static void CreateLadder(
        Tilemap elementsMap,
        Transform parent,
        string name,
        int x,
        int yStart,
        int yEnd,
        TileBase ladderTile)
    {
        if (elementsMap != null && ladderTile != null)
        {
            for (int y = yStart; y <= yEnd; y++)
            {
                elementsMap.SetTile(new Vector3Int(x, y, 0), ladderTile);
            }
        }

        GameObject ladder = new GameObject(name);
        ladder.transform.SetParent(parent);
        float height = (yEnd - yStart + 1) + 0.5f;
        ladder.transform.position = new Vector3(x + 0.5f, (yStart + yEnd + 1) * 0.5f, 0f);

        BoxCollider2D col = ladder.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(0.95f, height);

        EscaleraMano esc = ladder.AddComponent<EscaleraMano>();
        esc.velocidadSubida = 5.8f;
    }

    private static void PaintStoneBlock(
        Tilemap map,
        int xMin,
        int xMax,
        int yMin,
        int yMax,
        TileBase[] top,
        TileBase[] mid,
        TileBase[] deep,
        TileBase fallbackTop,
        TileBase fallbackDeep)
    {
        if (map == null) return;
        for (int x = xMin; x <= xMax; x++)
        {
            int idx = Mathf.Abs(x) % 4;
            for (int y = yMin; y <= yMax; y++)
            {
                TileBase t;
                if (y == yMax)
                    t = (top != null && top.Length > 0) ? top[idx % top.Length] : fallbackTop;
                else if (y == yMax - 1)
                    t = (mid != null && mid.Length > 0) ? mid[idx % mid.Length] : fallbackDeep;
                else
                    t = (deep != null && deep.Length > 0) ? deep[idx % deep.Length] : fallbackDeep;

                if (t != null) map.SetTile(new Vector3Int(x, y, 0), t);
            }
        }
    }

    private static void PaintPlatform(
        Tilemap map,
        int xMin,
        int xMax,
        int yBot,
        int yTop,
        TileBase[] top,
        TileBase[] bot,
        TileBase fallback)
    {
        if (map == null) return;
        for (int x = xMin; x <= xMax; x++)
        {
            int idx = Mathf.Abs(x) % 2;
            TileBase tTop = (top != null && top.Length > 0) ? top[idx % top.Length] : fallback;
            TileBase tBot = (bot != null && bot.Length > 0) ? bot[idx % bot.Length] : fallback;
            if (tTop != null) map.SetTile(new Vector3Int(x, yTop, 0), tTop);
            if (tBot != null && yBot != yTop) map.SetTile(new Vector3Int(x, yBot, 0), tBot);
        }
    }

    private static void PaintLine(Tilemap map, int xMin, int xMax, int y, TileBase tile)
    {
        if (map == null || tile == null) return;
        for (int x = xMin; x <= xMax; x++)
        {
            map.SetTile(new Vector3Int(x, y, 0), tile);
        }
    }

    private static TileBase LoadTile(string path) => AssetDatabase.LoadAssetAtPath<TileBase>(path);

    private static TileBase[] LoadTiles(params string[] paths)
    {
        var list = new System.Collections.Generic.List<TileBase>();
        for (int i = 0; i < paths.Length; i++)
        {
            TileBase t = AssetDatabase.LoadAssetAtPath<TileBase>(paths[i]);
            if (t != null) list.Add(t);
        }
        return list.ToArray();
    }

    private static void RefreshTilemapAndColliders(Tilemap map)
    {
        if (map == null) return;
        map.CompressBounds();
        map.RefreshAllTiles();
        TilemapCollider2D tc = map.GetComponent<TilemapCollider2D>();
        if (tc != null) tc.ProcessTilemapChanges();
        CompositeCollider2D cc = map.GetComponent<CompositeCollider2D>();
        if (cc != null) cc.GenerateGeometry();
    }

    // =========================================================================
    // SÍNTESIS DE SFX EXCLUSIVOS DEL JEFE FINAL WINE MONSTER
    // =========================================================================
    private static void GenerateBossAudio(
        out AudioClip sfxRoar,
        out AudioClip sfxWave,
        out AudioClip sfxSummon,
        out AudioClip sfxStream,
        out AudioClip sfxSeal)
    {
        // 1. Rugido profundo burbujeante del Wine Monster
        CreateProceduralWav(SfxBossRoarPath, 0.85f, (t, dur) =>
        {
            float p = t / dur;
            float env = Mathf.Sin(Mathf.PI * p) * Mathf.Exp(-p * 0.9f);
            float freq = Mathf.Lerp(135f, 52f, p) + Mathf.Sin(t * 28f) * 14f;
            float growl = Mathf.Sin(2f * Mathf.PI * freq * t) * 0.55f
                        + Mathf.Sin(2f * Mathf.PI * (freq * 1.5f) * t) * 0.25f;
            float bubble = Mathf.Sin(2f * Mathf.PI * (240f + Mathf.Sin(t * 45f) * 90f) * t) * 0.18f;
            return (growl + bubble) * env;
        });

        // 2. Ola de vino rompiendo por el piso
        CreateProceduralWav(SfxBossWavePath, 0.60f, (t, dur) =>
        {
            float p = t / dur;
            float env = Mathf.Sin(Mathf.PI * p);
            float noise = Mathf.Sin(t * 1731f) * Mathf.Cos(t * 2917f);
            float swell = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(95f, 190f, p) * t);
            return (noise * 0.45f + swell * 0.35f) * env;
        });

        // 3. Invocación mística de uvas
        CreateProceduralWav(SfxBossSummonPath, 0.55f, (t, dur) =>
        {
            float p = t / dur;
            float env = Mathf.Sin(Mathf.PI * p);
            float f1 = Mathf.Lerp(260f, 520f, p);
            float f2 = Mathf.Lerp(390f, 780f, p);
            return (Mathf.Sin(2f * Mathf.PI * f1 * t) * 0.45f + Mathf.Sin(2f * Mathf.PI * f2 * t) * 0.35f) * env;
        });

        // 4. Chorro de vino presurizado (Fase 2)
        CreateProceduralWav(SfxBossStreamPath, 0.48f, (t, dur) =>
        {
            float p = t / dur;
            float env = Mathf.Exp(-p * 2.1f);
            float jet = Mathf.Sin(t * 3200f) * Mathf.Sin(t * 1450f) * 0.5f;
            float tone = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(420f, 160f, p) * t) * 0.4f;
            return (jet + tone) * env;
        });

        // 5. Sellado mágico del Wine Monster en la Botella Sagrada + fanfarria triunfal
        CreateProceduralWav(SfxBossSealPath, 1.35f, (t, dur) =>
        {
            float p = t / dur;
            float env = (1f - p * 0.25f) * Mathf.Min(1f, t * 14f);
            float vortex = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(180f, 880f, p * p) * t) * 0.40f;
            float chime = (p > 0.65f)
                ? (Mathf.Sin(2f * Mathf.PI * 523.25f * t) + Mathf.Sin(2f * Mathf.PI * 659.25f * t) + Mathf.Sin(2f * Mathf.PI * 783.99f * t)) * 0.22f
                : 0f;
            return (vortex + chime) * env;
        });

        // 6. Disparo potente del golpe especial "¡SUPER CORCHAZO DE RESERVA!"
        CreateProceduralWav(SfxSuperCorkPath, 0.45f, (t, dur) =>
        {
            float p = t / dur;
            float env = Mathf.Exp(-p * 3.2f);
            float boom = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(320f, 75f, p) * t) * 0.55f;
            float pop = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(920f, 440f, p) * t) * 0.35f;
            float sparkle = Mathf.Sin(2f * Mathf.PI * 1320f * t) * 0.15f * (1f - p);
            return (boom + pop + sparkle) * env;
        });

        // 7. Campanilla dorada cuando el Super Corchazo llega al 100%
        CreateProceduralWav(SfxSuperCorkReadyPath, 0.48f, (t, dur) =>
        {
            float p = t / dur;
            float env = Mathf.Sin(Mathf.PI * p);
            float f = p < 0.45f ? 659.25f : 987.77f;
            return (Mathf.Sin(2f * Mathf.PI * f * t) * 0.45f + Mathf.Sin(2f * Mathf.PI * (f * 1.5f) * t) * 0.25f) * env;
        });

        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        sfxRoar = AssetDatabase.LoadAssetAtPath<AudioClip>(SfxBossRoarPath);
        sfxWave = AssetDatabase.LoadAssetAtPath<AudioClip>(SfxBossWavePath);
        sfxSummon = AssetDatabase.LoadAssetAtPath<AudioClip>(SfxBossSummonPath);
        sfxStream = AssetDatabase.LoadAssetAtPath<AudioClip>(SfxBossStreamPath);
        sfxSeal = AssetDatabase.LoadAssetAtPath<AudioClip>(SfxBossSealPath);
    }

    private static void CreateProceduralWav(string assetPath, float durationSeconds, Func<float, float, float> sampleFunc)
    {
        const int sampleRate = 22050;
        int totalSamples = Mathf.Max(1, Mathf.RoundToInt(sampleRate * durationSeconds));
        short[] pcm = new short[totalSamples];

        for (int i = 0; i < totalSamples; i++)
        {
            float t = (float)i / sampleRate;
            float val = Mathf.Clamp(sampleFunc(t, durationSeconds), -1f, 1f);
            pcm[i] = (short)(val * 32000f);
        }

        using (MemoryStream ms = new MemoryStream())
        using (BinaryWriter bw = new BinaryWriter(ms))
        {
            int byteRate = sampleRate * 2;
            int dataBytes = totalSamples * 2;
            bw.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
            bw.Write(36 + dataBytes);
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
            bw.Write(dataBytes);
            for (int i = 0; i < totalSamples; i++) bw.Write(pcm[i]);
            File.WriteAllBytes(assetPath, ms.ToArray());
        }
    }
}
#endif
