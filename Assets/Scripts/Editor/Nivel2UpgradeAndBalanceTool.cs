#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

/// <summary>
/// Aplica de forma no destructiva las mejoras solicitadas para Nivel 2 y el balance de enemigos:
/// 1. Reemplaza el fondo del Nivel 2 por el nuevo interior pixel-art de bodega (solo fondo, sin elementos de gameplay).
/// 2. Expande horizontalmente Nivel 2 (X = 119 a X = 178) conservando intacto todo el mapa pintado en X <= 118.
/// 3. Ajusta la escala visual y físicas de EnemigoCabSau (PPU = 26, tamaño acorde al jugador y Merlot).
/// 4. Garantiza que las Uvas Merlot (EnemigoUva / MerlotGrape) resistan mínimo 3 golpes.
/// </summary>
[InitializeOnLoad]
public static class Nivel2UpgradeAndBalanceTool
{
    private const string UpgradePrefKey = "Vineyard_Nivel2_BgExpansionBalance_V1";
    private const int UpgradeVersion = 1;

    private const string Nivel2ScenePath = "Assets/Scenes/Nivel2.unity";
    private const string NewBgAssetPath = "Assets/Sprites/Fondos/FondoNivel2_BodegaInterior.jpg";
    private const string CabSauSpriteFolder = "Assets/Sprites/Enemigos/CabSau";
    private const string CabSauPrefabPath = "Assets/Prefabs/EnemigoCabSau.prefab";
    private const string EnemigoUvaPrefabPath = "Assets/Prefabs/EnemigoUva.prefab";
    private const string MerlotPrefabPath = "Assets/Prefabs/MerlotGrape.prefab";
    private const string CorazonPrefabPath = "Assets/Prefabs/CorazonVida.prefab";

    private const string BarrelSpritePath = "Assets/Sprites/Nivel2Props/BarrilVino.png";
    private const string CrateSpritePath = "Assets/Sprites/Nivel2Props/CajaMadera.png";
    private const string ArchSpritePath = "Assets/Sprites/Nivel2Props/ArcoPiedraBodega.png";
    private const string SfxAcidSplashPath = "Assets/Audio/SFX_AcidoBurbuja.wav";

    private const string BgContainerName = "FondosBodega_Nivel2";
    private const string ExpansionContainerName = "ExpansionZona4_Nivel2";

    static Nivel2UpgradeAndBalanceTool()
    {
        EditorApplication.delayCall += RunOnceIfNeeded;
    }

    private static void RunOnceIfNeeded()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        if (EditorPrefs.GetInt(UpgradePrefKey, 0) >= UpgradeVersion)
            return;

        if (!File.Exists(NewBgAssetPath) || !File.Exists(Nivel2ScenePath))
            return;

        ApplyAllUpgrades(silent: true);
    }

    [MenuItem("Herramientas Vineyard/Aplicar Mejoras Nivel 2 (Fondo, Expansión, CabSau y Merlot)")]
    public static void ApplyFromMenu()
    {
        ApplyAllUpgrades(silent: false);
        EditorUtility.DisplayDialog(
            "Vineyard - Mejoras Aplicadas",
            "¡Listo! Se aplicaron todas las mejoras sin borrar tu diseño actual:\n\n" +
            "• Fondo interior pixel-art de bodega aplicado en todo el Nivel 2.\n" +
            "• Nivel 2 expandido horizontalmente hasta X = 178 (nueva Zona 4: Galería de Calderas y Bóveda Final, Meta en X = 173.5).\n" +
            "• Uvas Cab. Sau agrandadas (PPU 42 → 26) con collider y sensores ajustados.\n" +
            "• Uvas Merlot ajustadas a 3 golpes mínimos (3 HP + corrección de doble daño del corcho).",
            "¡Genial!"
        );
    }

    public static void ApplyAllUpgrades(bool silent)
    {
        // 1. Configurar el nuevo sprite de fondo de bodega
        Sprite newBgSprite = ConfigureNewBackgroundSprite();

        // 2. Asegurar PPU = 26 en los 13 sprites de Cab. Sau y actualizar prefabs
        EnsureCabSauSpritesAndPrefabSize();
        EnsureMerlotPrefabsMinThreeHits();

        // 3. Aplicar cambios en la escena Nivel2.unity sin destruir lo pintado en X <= 118
        ApplyChangesToNivel2Scene(newBgSprite);

        EditorPrefs.SetInt(UpgradePrefKey, UpgradeVersion);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        if (!silent)
        {
            Debug.Log("[Vineyard] Mejoras de Nivel 2, fondo, Cab. Sau y Merlot aplicadas correctamente.");
        }
    }

    private static Sprite ConfigureNewBackgroundSprite()
    {
        if (!File.Exists(NewBgAssetPath))
        {
            Debug.LogWarning($"[Nivel2Upgrade] No se encontró el fondo en {NewBgAssetPath}.");
            return null;
        }

        AssetDatabase.ImportAsset(NewBgAssetPath, ImportAssetOptions.ForceSynchronousImport);
        TextureImporter imp = AssetImporter.GetAtPath(NewBgAssetPath) as TextureImporter;
        if (imp != null)
        {
            bool changed = imp.textureType != TextureImporterType.Sprite ||
                           imp.spriteImportMode != SpriteImportMode.Single ||
                           !Mathf.Approximately(imp.spritePixelsPerUnit, 34f) ||
                           imp.filterMode != FilterMode.Point ||
                           imp.maxTextureSize < 2048 ||
                           imp.textureCompression != TextureImporterCompression.Uncompressed;

            if (changed)
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
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(NewBgAssetPath);
    }

    private static void EnsureCabSauSpritesAndPrefabSize()
    {
        for (int i = 1; i <= 13; i++)
        {
            string path = $"{CabSauSpriteFolder}/cabsau_{i}.png";
            if (!File.Exists(path)) continue;

            TextureImporter imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp != null && !Mathf.Approximately(imp.spritePixelsPerUnit, 26f))
            {
                imp.spritePixelsPerUnit = 26f;
                imp.filterMode = FilterMode.Point;
                imp.SaveAndReimport();
            }
        }

        if (File.Exists(CabSauPrefabPath))
        {
            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(CabSauPrefabPath);
            if (prefabRoot != null)
            {
                BoxCollider2D col = prefabRoot.GetComponent<BoxCollider2D>();
                if (col != null)
                {
                    col.size = new Vector2(1.18f, 1.26f);
                    col.offset = new Vector2(0f, -0.06f);
                }

                Transform detector = prefabRoot.transform.Find("DetectorBorde");
                if (detector != null)
                {
                    detector.localPosition = new Vector3(0.72f, -0.28f, 0f);
                }

                EnemigoCabSau cab = prefabRoot.GetComponent<EnemigoCabSau>();
                if (cab != null)
                {
                    cab.distanciaSensorBordeX = 0.72f;
                    cab.longitudRayoSuelo = 1.65f;
                    cab.distanciaSensorPared = 0.78f;
                }

                PrefabUtility.SaveAsPrefabAsset(prefabRoot, CabSauPrefabPath);
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }
    }

    private static void EnsureMerlotPrefabsMinThreeHits()
    {
        string[] paths = new string[] { EnemigoUvaPrefabPath, MerlotPrefabPath };
        for (int i = 0; i < paths.Length; i++)
        {
            if (!File.Exists(paths[i])) continue;

            GameObject root = PrefabUtility.LoadPrefabContents(paths[i]);
            if (root != null)
            {
                MerlotGrapeEnemy merlot = root.GetComponent<MerlotGrapeEnemy>();
                if (merlot != null)
                {
                    if (merlot.maxHealth < 3) merlot.maxHealth = 3;
                    merlot.danoPorSaltoEncima = 1;
                }
                PrefabUtility.SaveAsPrefabAsset(root, paths[i]);
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
    }

    private static void ApplyChangesToNivel2Scene(Sprite newBgSprite)
    {
        Scene activeScene = EditorSceneManager.GetActiveScene();
        bool wasNivel2Open = activeScene.path == Nivel2ScenePath;
        string previousScenePath = activeScene.path;

        Scene scene2 = activeScene;
        if (!wasNivel2Open)
        {
            if (activeScene.isDirty)
            {
                EditorSceneManager.SaveOpenScenes();
            }
            scene2 = EditorSceneManager.OpenScene(Nivel2ScenePath, OpenSceneMode.Single);
        }

        // 1. Reemplazar el fondo por el nuevo interior de bodega (6 paneles continuos de X=-32 a X=210)
        if (newBgSprite != null)
        {
            ReplaceNivel2Background(newBgSprite);
        }

        // 2. Ajustar altura de spawn de Cab. Sau existentes para que no queden enterrados tras agrandarlos
        AdjustExistingCabSauPositions();

        // 3. Expandir horizontalmente el nivel desde X = 119 hasta X = 178 sin tocar X <= 118
        ExpandNivel2Horizontally();

        EditorSceneManager.MarkSceneDirty(scene2);
        EditorSceneManager.SaveScene(scene2);

        if (!wasNivel2Open && !string.IsNullOrEmpty(previousScenePath) && File.Exists(previousScenePath))
        {
            // Mantener Nivel 2 abierto para que el usuario vea el resultado al volver a Unity
            Debug.Log("[Nivel2Upgrade] Escena Nivel2 actualizada y guardada.");
        }
    }

    private static void ReplaceNivel2Background(Sprite bgSprite)
    {
        // Eliminar el contenedor previo si se re-ejecuta el script
        GameObject oldContainer = GameObject.Find(BgContainerName);
        if (oldContainer != null)
        {
            Object.DestroyImmediate(oldContainer);
        }

        // Desactivar los viejos paneles de cielo de atardecer (Nivel 1)
        string[] legacyBgNames = new string[] { "fondo1", "fondo (1)", "fondo (2)", "fondo1 (3)", "fondo1 (4)" };
        for (int i = 0; i < legacyBgNames.Length; i++)
        {
            GameObject legacy = GameObject.Find(legacyBgNames[i]);
            if (legacy != null)
            {
                legacy.SetActive(false);
                EditorUtility.SetDirty(legacy);
            }
        }

        GameObject container = new GameObject(BgContainerName);
        float panelWidth = bgSprite.bounds.size.x;
        if (panelWidth < 1f) panelWidth = 40.47f;

        // Pequeño solape de 0.08 unidades para evitar cualquier línea divisoria de 1px
        float stepX = panelWidth - 0.08f;
        float startCenterX = -10f;
        float centerY = 7.6f;

        // 6 paneles cubren desde X ≈ -30 hasta X ≈ 212
        for (int i = 0; i < 6; i++)
        {
            GameObject panel = new GameObject($"FondoBodega_{i + 1}");
            panel.transform.SetParent(container.transform);
            panel.transform.position = new Vector3(startCenterX + i * stepX, centerY, 0f);
            panel.transform.localScale = new Vector3((i % 2 == 0) ? 1f : -1f, 1f, 1f);

            SpriteRenderer sr = panel.AddComponent<SpriteRenderer>();
            sr.sprite = bgSprite;
            sr.sortingOrder = -10;
            // Ligero oscurecimiento atmosférico para máxima legibilidad del jugador, plataformas y enemigos
            sr.color = new Color(0.84f, 0.80f, 0.84f, 1f);
        }
    }

    private static void AdjustExistingCabSauPositions()
    {
        EnemigoCabSau[] existing = Object.FindObjectsByType<EnemigoCabSau>(FindObjectsSortMode.None);
        for (int i = 0; i < existing.Length; i++)
        {
            if (existing[i] == null) continue;
            Vector3 p = existing[i].transform.position;

            // Caso especial: EnemigoCabSau_4 estaba en (57.5, 4.65) justo dentro de la plataforma Y=5..6
            if (existing[i].name.Contains("PasarelaFermentacion") && p.y > 4.0f && p.y < 6.5f)
            {
                existing[i].transform.position = new Vector3(p.x, 7.90f, p.z);
                EditorUtility.SetDirty(existing[i].transform);
            }
            // Subir ligeramente los que estaban en Y = *.65 para acomodar el collider más alto
            else if (Mathf.Abs((p.y - Mathf.Floor(p.y)) - 0.65f) < 0.08f)
            {
                existing[i].transform.position = new Vector3(p.x, p.y + 0.28f, p.z);
                EditorUtility.SetDirty(existing[i].transform);
            }
        }
    }

    private static void ExpandNivel2Horizontally()
    {
        GameObject oldExp = GameObject.Find(ExpansionContainerName);
        if (oldExp != null)
        {
            Object.DestroyImmediate(oldExp);
        }

        GameObject tilePisoObj = GameObject.Find("TilePiso");
        GameObject structuresObj = GameObject.Find("Structures");
        GameObject elementsObj = GameObject.Find("Elements");

        Tilemap tilePiso = tilePisoObj != null ? tilePisoObj.GetComponent<Tilemap>() : null;
        Tilemap structuresMap = structuresObj != null ? structuresObj.GetComponent<Tilemap>() : null;
        Tilemap elementsMap = elementsObj != null ? elementsObj.GetComponent<Tilemap>() : null;

        if (tilePiso == null)
        {
            Debug.LogWarning("[Nivel2Upgrade] No se encontró TilePiso en Nivel2.unity.");
            return;
        }

        // Limpiar únicamente la zona de expansión (X >= 119) por si se re-ejecuta el menú
        ClearTileRange(tilePiso, 119, 185, -8, 16);
        if (structuresMap != null) ClearTileRange(structuresMap, 119, 185, -8, 16);
        if (elementsMap != null) ClearTileRange(elementsMap, 119, 185, -8, 16);

        // Cargar los mismos tiles de tileset-ok-lvl2 que ya usa Nivel 2 en X <= 118
        TileBase[] groundTop = LoadTiles(
            "Assets/Tiles/Nivel2/tileset-ok-lvl2_12.asset",
            "Assets/Tiles/Nivel2/tileset-ok-lvl2_13.asset",
            "Assets/Tiles/Nivel2/tileset-ok-lvl2_14.asset",
            "Assets/Tiles/Nivel2/tileset-ok-lvl2_15.asset"
        );
        TileBase[] groundMid = LoadTiles(
            "Assets/Tiles/Nivel2/tileset-ok-lvl2_28.asset",
            "Assets/Tiles/Nivel2/tileset-ok-lvl2_29.asset",
            "Assets/Tiles/Nivel2/tileset-ok-lvl2_30.asset",
            "Assets/Tiles/Nivel2/tileset-ok-lvl2_31.asset"
        );
        TileBase[] groundDeep = LoadTiles(
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

        TileBase fallbackTop = AssetDatabase.LoadAssetAtPath<TileBase>("Assets/Tiles/Nivel2/Tile_PiedraGrisSuperficie.asset");
        TileBase fallbackDeep = AssetDatabase.LoadAssetAtPath<TileBase>("Assets/Tiles/Nivel2/Tile_PiedraGrisProfunda.asset");
        TileBase tileWood = AssetDatabase.LoadAssetAtPath<TileBase>("Assets/Tiles/Nivel2/Tile_PasarelaMadera.asset") ?? platTop[0] ?? fallbackTop;
        TileBase tileLadder = AssetDatabase.LoadAssetAtPath<TileBase>("Assets/Tiles/Nivel2/Tile_EscaleraMano.asset");
        TileBase tileAcidSurf = AssetDatabase.LoadAssetAtPath<TileBase>("Assets/Tiles/Nivel2/Tile_AcidoSuperficie.asset");
        TileBase tileAcidDeep = AssetDatabase.LoadAssetAtPath<TileBase>("Assets/Tiles/Nivel2/Tile_AcidoProfundo.asset");

        // =====================================================================
        // ZONA 4 (EXPANSIÓN): GALERÍA DE CALDERAS Y BÓVEDA FINAL (X = 119 a 178)
        // =====================================================================

        // 1. Tramo de transición desde el final original (X = 119..126)
        PaintStoneFloor(tilePiso, 119, 126, -6, -1, groundTop, groundMid, groundDeep, fallbackTop, fallbackDeep);
        PaintPlatformBlock(tilePiso, 121, 126, 3, 4, platTop, platBot, tileWood);

        // 2. Foso de Calderas 5 (X = 127..134) con puente intermedio y plataforma alta del 3er Corazón
        PaintStoneFloor(tilePiso, 127, 134, -6, -5, groundTop, groundMid, groundDeep, fallbackTop, fallbackDeep);
        PaintPlatformBlock(tilePiso, 129, 132, 1, 2, platTop, platBot, tileWood);
        PaintPlatformBlock(tilePiso, 130, 136, 5, 6, platTop, platBot, tileWood);

        // 3. Isla Central de Fermentación (X = 135..143)
        PaintStoneFloor(tilePiso, 135, 143, -6, -1, groundTop, groundMid, groundDeep, fallbackTop, fallbackDeep);

        // 4. Foso de Calderas 6 (X = 144..151) con pasarelas escalonadas
        PaintStoneFloor(tilePiso, 144, 151, -6, -5, groundTop, groundMid, groundDeep, fallbackTop, fallbackDeep);
        PaintHorizontalLine(tilePiso, 145, 147, 2, tileWood);
        PaintHorizontalLine(tilePiso, 149, 151, 4, tileWood);

        // 5. Bóveda Final de la Bodega (X = 152..178) + Mezzanine (X = 155..167, Y = 3..4) + Muro cierre derecho
        PaintStoneFloor(tilePiso, 152, 178, -6, -1, groundTop, groundMid, groundDeep, fallbackTop, fallbackDeep);
        PaintPlatformBlock(tilePiso, 155, 167, 3, 4, platTop, platBot, tileWood);
        PaintStoneFloor(tilePiso, 177, 179, 0, 9, groundTop, groundMid, groundDeep, fallbackTop, fallbackDeep);

        // 6. Decoración de arcos de ladrillo en Structures (el mismo patrón 4x4 usado en X = 115..118)
        if (structuresMap != null)
        {
            PaintBrickStructureArch(structuresMap, 137, 0);
            PaintBrickStructureArch(structuresMap, 158, 0);
            PaintBrickStructureArch(structuresMap, 172, 0);
        }

        // 7. Contenedor para props, ácido, escalera, corazón y nuevos enemigos de Zona 4
        GameObject expRoot = new GameObject(ExpansionContainerName);
        int sueloLayer = LayerMask.NameToLayer("Suelo");
        if (sueloLayer < 0) sueloLayer = 6;

        AudioClip acidClip = AssetDatabase.LoadAssetAtPath<AudioClip>(SfxAcidSplashPath);
        Sprite barrelSprite = AssetDatabase.LoadAssetAtPath<Sprite>(BarrelSpritePath);
        Sprite crateSprite = AssetDatabase.LoadAssetAtPath<Sprite>(CrateSpritePath);
        Sprite archSprite = AssetDatabase.LoadAssetAtPath<Sprite>(ArchSpritePath);

        // Pintar ácido en Elements + Triggers ZonaAcido para Foso 5 y Foso 6
        CreateAcidPit(elementsMap, expRoot.transform, "FosoFermentacion_5", 127, 134, tileAcidSurf, tileAcidDeep, acidClip);
        CreateAcidPit(elementsMap, expRoot.transform, "FosoFermentacion_6", 144, 151, tileAcidSurf, tileAcidDeep, acidClip);

        // Escalera de mano para subir a la mezzanine de la Bóveda Final (X = 155, Y = 0..4)
        CreateLadder(elementsMap, expRoot.transform, "Escalera_Z4_MezzanineBoveda", 155, 0, 4, tileLadder);

        // Props sólidos (cajas y barriles) + arco triunfal en la meta
        if (crateSprite != null)
        {
            CreateCrateStack(expRoot.transform, "Cajas_Z4_Entrada", 120.2f, 0f, crateSprite, sueloLayer);
            CreateCrateStack(expRoot.transform, "Cajas_Z4_Boveda", 168.8f, 0f, crateSprite, sueloLayer);
        }
        if (barrelSprite != null)
        {
            CreateBarrelStack(expRoot.transform, "Barriles_Z4_Isla", 136.2f, 0f, barrelSprite, sueloLayer);
            CreateBarrelStack(expRoot.transform, "Barriles_Z4_Boveda", 153.4f, 0f, barrelSprite, sueloLayer);
        }
        if (archSprite != null)
        {
            GameObject goalArch = new GameObject("ArcoTriunfal_MetaZ4");
            goalArch.transform.SetParent(expRoot.transform);
            goalArch.transform.position = new Vector3(173.5f, 1.4f, 0f);
            goalArch.transform.localScale = new Vector3(1.5f, 1.5f, 1f);
            SpriteRenderer sr = goalArch.AddComponent<SpriteRenderer>();
            sr.sprite = archSprite;
            sr.sortingOrder = -1;
        }

        // 3er Corazón de Vida en la plataforma alta sobre Foso 5 (X = 133, Y = 7.85)
        GameObject heartPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CorazonPrefabPath);
        if (heartPrefab != null)
        {
            GameObject heart3 = (GameObject)PrefabUtility.InstantiatePrefab(heartPrefab);
            heart3.name = "CorazonVida_3_Zona4";
            heart3.transform.SetParent(expRoot.transform);
            heart3.transform.position = new Vector3(133.0f, 7.85f, 0f);
        }

        // Enemigos de Zona 4: 4 Cab. Sau + 2 Uvas Merlot (con 3 golpes mínimos)
        GameObject cabSauPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CabSauPrefabPath);
        int sueloMask = 1 << sueloLayer;
        if (cabSauPrefab != null)
        {
            (Vector3 pos, float patrol, string name)[] newCabSaus = new (Vector3, float, string)[]
            {
                (new Vector3(123.0f, 0.95f, 0f), 3.0f, "EnemigoCabSau_8_GaleriaEntrada"),
                (new Vector3(139.5f, 0.95f, 0f), 3.4f, "EnemigoCabSau_9_IslaCalderas"),
                (new Vector3(161.0f, 0.95f, 0f), 5.2f, "EnemigoCabSau_10_BovedaPlantaBaja"),
                (new Vector3(160.0f, 5.95f, 0f), 3.8f, "EnemigoCabSau_11_BovedaMezzanine")
            };

            for (int i = 0; i < newCabSaus.Length; i++)
            {
                GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(cabSauPrefab);
                inst.name = newCabSaus[i].name;
                inst.transform.SetParent(expRoot.transform);
                inst.transform.position = newCabSaus[i].pos;

                EnemigoCabSau cs = inst.GetComponent<EnemigoCabSau>();
                if (cs != null)
                {
                    cs.distanciaPatrulla = newCabSaus[i].patrol;
                    cs.capaSuelo = sueloMask;
                    EditorUtility.SetDirty(cs);
                }
            }
        }

        GameObject merlotPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(EnemigoUvaPrefabPath);
        if (merlotPrefab != null)
        {
            (Vector3 pos, string name)[] newMerlots = new (Vector3, string)[]
            {
                (new Vector3(134.8f, 7.80f, 0f), "EnemigoUva_3_PasarelaCalderasZ4"),
                (new Vector3(165.5f, 5.80f, 0f), "EnemigoUva_4_GuardianMetaZ4")
            };

            for (int i = 0; i < newMerlots.Length; i++)
            {
                GameObject mInst = (GameObject)PrefabUtility.InstantiatePrefab(merlotPrefab);
                mInst.name = newMerlots[i].name;
                mInst.transform.SetParent(expRoot.transform);
                mInst.transform.position = newMerlots[i].pos;
            }
        }

        // Mover MetaNivel2 al final de la expansión (X = 173.5, Y = 1.0)
        MetaNivel meta = Object.FindFirstObjectByType<MetaNivel>();
        if (meta != null)
        {
            meta.transform.position = new Vector3(173.5f, 1.0f, 0f);
            EditorUtility.SetDirty(meta.transform);
        }

        // Actualizar límites del mapa (LimiteDerecha y LimiteAbajo)
        GameObject limDer = GameObject.Find("LimiteDerecha");
        if (limDer != null)
        {
            limDer.transform.position = new Vector3(179.0f, 4.0f, 0f);
            BoxCollider2D colDer = limDer.GetComponent<BoxCollider2D>();
            if (colDer != null)
            {
                colDer.size = new Vector2(1.5f, 40f);
                EditorUtility.SetDirty(colDer);
            }
            EditorUtility.SetDirty(limDer.transform);
        }

        GameObject limAbajo = GameObject.Find("LimiteAbajo");
        if (limAbajo != null)
        {
            limAbajo.transform.position = new Vector3(85.0f, -8.5f, 0f);
            BoxCollider2D colAbajo = limAbajo.GetComponent<BoxCollider2D>();
            if (colAbajo != null)
            {
                colAbajo.size = new Vector2(230f, 1.5f);
                EditorUtility.SetDirty(colAbajo);
            }
            EditorUtility.SetDirty(limAbajo.transform);
        }

        // Refrescar Tilemaps y regenerar colisiones
        RefreshTilemapAndColliders(tilePiso);
        if (structuresMap != null) RefreshTilemapAndColliders(structuresMap);
        if (elementsMap != null) RefreshTilemapAndColliders(elementsMap);
        Physics2D.SyncTransforms();
    }

    private static void ClearTileRange(Tilemap map, int xMin, int xMax, int yMin, int yMax)
    {
        for (int x = xMin; x <= xMax; x++)
        {
            for (int y = yMin; y <= yMax; y++)
            {
                map.SetTile(new Vector3Int(x, y, 0), null);
            }
        }
    }

    private static TileBase[] LoadTiles(params string[] paths)
    {
        TileBase[] result = new TileBase[paths.Length];
        for (int i = 0; i < paths.Length; i++)
        {
            result[i] = AssetDatabase.LoadAssetAtPath<TileBase>(paths[i]);
        }
        return result;
    }

    private static TileBase PickTile(TileBase[] arr, int index, TileBase fallback)
    {
        if (arr != null && arr.Length > 0)
        {
            TileBase t = arr[Mathf.Abs(index) % arr.Length];
            if (t != null) return t;
        }
        return fallback;
    }

    private static void PaintStoneFloor(
        Tilemap map, int xStart, int xEnd, int yBottom, int yTop,
        TileBase[] topArr, TileBase[] midArr, TileBase[] deepArr,
        TileBase fallbackTop, TileBase fallbackDeep)
    {
        for (int x = xStart; x <= xEnd; x++)
        {
            for (int y = yBottom; y <= yTop; y++)
            {
                TileBase tile;
                if (y == yTop)
                    tile = PickTile(topArr, x, fallbackTop);
                else if (Mathf.Abs(yTop - y) % 2 == 1)
                    tile = PickTile(midArr, x, fallbackDeep);
                else
                    tile = PickTile(deepArr, x, fallbackDeep);

                map.SetTile(new Vector3Int(x, y, 0), tile);
            }
        }
    }

    private static void PaintPlatformBlock(
        Tilemap map, int xStart, int xEnd, int yBottom, int yTop,
        TileBase[] topArr, TileBase[] botArr, TileBase fallback)
    {
        for (int x = xStart; x <= xEnd; x++)
        {
            map.SetTile(new Vector3Int(x, yTop, 0), PickTile(topArr, x, fallback));
            if (yBottom < yTop)
            {
                map.SetTile(new Vector3Int(x, yBottom, 0), PickTile(botArr, x, fallback));
            }
        }
    }

    private static void PaintHorizontalLine(Tilemap map, int xStart, int xEnd, int y, TileBase tile)
    {
        for (int x = xStart; x <= xEnd; x++)
        {
            map.SetTile(new Vector3Int(x, y, 0), tile);
        }
    }

    private static void PaintBrickStructureArch(Tilemap structuresMap, int xStart, int yStart)
    {
        int[][] tileIds = new int[][]
        {
            new int[] { 124, 125, 126, 127 }, // y + 0
            new int[] { 108, 109, 110, 111 }, // y + 1
            new int[] { 92,  93,  94,  95  }, // y + 2
            new int[] { 76,  77,  78,  79  }  // y + 3
        };

        for (int row = 0; row < tileIds.Length; row++)
        {
            for (int col = 0; col < tileIds[row].Length; col++)
            {
                string path = $"Assets/Tiles/Nivel2/tileset-ok-lvl2_{tileIds[row][col]}.asset";
                TileBase t = AssetDatabase.LoadAssetAtPath<TileBase>(path);
                if (t != null)
                {
                    structuresMap.SetTile(new Vector3Int(xStart + col, yStart + row, 0), t);
                }
            }
        }
    }

    private static void CreateAcidPit(
        Tilemap elementsMap, Transform parent, string name,
        int x0, int x1, TileBase tileSurf, TileBase tileDeep, AudioClip acidClip)
    {
        if (elementsMap != null)
        {
            for (int x = x0; x <= x1; x++)
            {
                if (tileDeep != null)
                {
                    elementsMap.SetTile(new Vector3Int(x, -4, 0), tileDeep);
                    elementsMap.SetTile(new Vector3Int(x, -3, 0), tileDeep);
                }
                if (tileSurf != null)
                {
                    elementsMap.SetTile(new Vector3Int(x, -2, 0), tileSurf);
                }
            }
        }

        int width = x1 - x0 + 1;
        GameObject poolObj = new GameObject(name);
        poolObj.transform.SetParent(parent);
        poolObj.transform.position = new Vector3(x0 + width * 0.5f, -2.5f, 0f);

        BoxCollider2D col = poolObj.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(width, 2.2f);

        ZonaAcido zona = poolObj.AddComponent<ZonaAcido>();
        zona.danoPorContacto = 18f;
        zona.intervaloDano = 0.85f;
        zona.impulsoReboteVertical = 10.5f;
        zona.sonidoSalpicaduraAcido = acidClip;

        GameObject bubbles = new GameObject("BurbujasAcido_VFX");
        bubbles.transform.SetParent(poolObj.transform);
        bubbles.transform.localPosition = new Vector3(0f, 0.9f, 0f);

        ParticleSystem ps = bubbles.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, 1.5f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.4f, 1.0f);
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
        shape.scale = new Vector3(width * 0.9f, 0.25f, 0.1f);

        ParticleSystemRenderer psr = bubbles.GetComponent<ParticleSystemRenderer>();
        if (psr != null)
        {
            psr.material = EnemyDeathFeedback.GetPixelParticleMaterial();
            psr.sortingOrder = 5;
        }
    }

    private static void CreateLadder(
        Tilemap elementsMap, Transform parent, string name,
        int xCell, int yBottom, int yTop, TileBase tileLadder)
    {
        if (elementsMap != null && tileLadder != null)
        {
            for (int y = yBottom; y <= yTop; y++)
            {
                elementsMap.SetTile(new Vector3Int(xCell, y, 0), tileLadder);
            }
        }

        float height = (yTop - yBottom + 1);
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent);
        obj.transform.position = new Vector3(xCell + 0.5f, yBottom + height * 0.5f, 0f);

        BoxCollider2D col = obj.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(0.95f, height + 0.5f);

        EscaleraMano ladder = obj.AddComponent<EscaleraMano>();
        ladder.velocidadSubida = 5.6f;
    }

    private static void CreateBarrelStack(Transform parent, string name, float centerX, float groundY, Sprite spr, int layer)
    {
        GameObject grp = new GameObject(name);
        grp.transform.SetParent(parent);
        Vector3[] pos = new Vector3[]
        {
            new Vector3(centerX - 0.48f, groundY + 0.52f, 0f),
            new Vector3(centerX + 0.48f, groundY + 0.52f, 0f),
            new Vector3(centerX, groundY + 1.48f, 0f)
        };
        for (int i = 0; i < pos.Length; i++)
        {
            CreateSolidProp(grp.transform, $"Barril_{i + 1}", pos[i], spr, new Vector2(0.88f, 0.98f), layer);
        }
    }

    private static void CreateCrateStack(Transform parent, string name, float centerX, float groundY, Sprite spr, int layer)
    {
        GameObject grp = new GameObject(name);
        grp.transform.SetParent(parent);
        Vector3[] pos = new Vector3[]
        {
            new Vector3(centerX - 0.48f, groundY + 0.48f, 0f),
            new Vector3(centerX + 0.48f, groundY + 0.48f, 0f),
            new Vector3(centerX + 0.48f, groundY + 1.42f, 0f)
        };
        for (int i = 0; i < pos.Length; i++)
        {
            CreateSolidProp(grp.transform, $"Caja_{i + 1}", pos[i], spr, new Vector2(0.92f, 0.92f), layer);
        }
    }

    private static void CreateSolidProp(Transform parent, string name, Vector3 pos, Sprite spr, Vector2 size, int layer)
    {
        GameObject prop = new GameObject(name);
        prop.transform.SetParent(parent);
        prop.transform.position = pos;
        prop.layer = layer;

        SpriteRenderer sr = prop.AddComponent<SpriteRenderer>();
        sr.sprite = spr;
        sr.sortingOrder = 2;

        BoxCollider2D col = prop.AddComponent<BoxCollider2D>();
        col.size = size;
    }

    private static void RefreshTilemapAndColliders(Tilemap map)
    {
        map.CompressBounds();
        map.RefreshAllTiles();

        TilemapCollider2D tmCol = map.GetComponent<TilemapCollider2D>();
        if (tmCol != null)
        {
            tmCol.ProcessTilemapChanges();
            EditorUtility.SetDirty(tmCol);
        }

        CompositeCollider2D compCol = map.GetComponent<CompositeCollider2D>();
        if (compCol != null)
        {
            compCol.GenerateGeometry();
            EditorUtility.SetDirty(compCol);
        }

        EditorUtility.SetDirty(map);
    }
}
#endif
