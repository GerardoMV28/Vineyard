#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

/// <summary>
/// Rediseña y expande el Nivel 1 (Assets/Scenes/Nivel1.unity) con una estética uniforme
/// de viñedo toscano coherente con el fondo al atardecer:
/// - Reemplaza plataformas flotantes sueltas y props desentonados por arquitectura integrada:
///   arcos de piedra/ladrillo con friso superior, acueducto de piedra, mirador simétrico
///   de finca y Gran Portón simétrico de entrada a la Bodega usando 100% tileset-ok nativo.
/// - Divide la expansión (X = 118..181) en 4 tramos simétricos y equilibrados (~16 tiles c/u).
/// - Distribuye uniformemente 11 Uvas Merlot a lo largo de todo el recorrido (cada ~18-20 tiles).
/// </summary>
[InitializeOnLoad]
public static class Nivel1ExpandAndStructuresTool
{
    private const string PrefKey = "Vineyard_Nivel1_ExpandStructures_V2";
    private const int CurrentVersion = 2;

    private const string Nivel1ScenePath = "Assets/Scenes/Nivel1.unity";
    private const string BgAssetPath = "Assets/Sprites/Fondos/FondoNivel1_VinedoAtardecer.jpg";
    private const string EnemigoUvaPrefabPath = "Assets/Prefabs/EnemigoUva.prefab";
    private const string MerlotPrefabPath = "Assets/Prefabs/MerlotGrape.prefab";
    private const string CorazonPrefabPath = "Assets/Prefabs/CorazonVida.prefab";

    private const string PropsContainerName = "ObstaculosYEstructuras_Nivel1";
    private const string BgContainerName = "FondosVinedo_Nivel1";

    static Nivel1ExpandAndStructuresTool()
    {
        EditorApplication.delayCall += RunOnceIfNeeded;
    }

    private static void RunOnceIfNeeded()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        if (EditorPrefs.GetInt(PrefKey, 0) >= CurrentVersion)
            return;

        if (!File.Exists(Nivel1ScenePath))
            return;

        ApplyNivel1AestheticRedesign(silent: true);
    }

    [MenuItem("Herramientas Vineyard/Expandir y Estilizar Nivel 1 (Diseño Toscano Uniforme)")]
    public static void ApplyFromMenu()
    {
        ApplyNivel1AestheticRedesign(silent: false);
        EditorUtility.DisplayDialog(
            "Vineyard - Nivel 1 Estilizado",
            "¡Se aplicó el rediseño estético uniforme del Nivel 1!\n\n" +
            "• 100% coherente con el fondo toscano al atardecer y tileset-ok nativo (sin props desentonados ni plataformas flotantes sin soporte).\n" +
            "• 4 secciones simétricas y equilibradas en la expansión (Paseo de Viñedos, Acueducto de Piedra, Mirador de la Finca y Gran Portón de la Bodega).\n" +
            "• Soportes de arcos de piedra bajo los puentes existentes para dar cohesión visual.\n" +
            "• 11 Uvas Merlot distribuidas uniformemente a lo largo de todo el nivel.",
            "¡Excelente!"
        );
    }

    public static void ApplyNivel1AestheticRedesign(bool silent)
    {
        Scene activeScene = EditorSceneManager.GetActiveScene();
        bool wasNivel1Open = activeScene.path == Nivel1ScenePath;
        Scene scene1 = activeScene;

        if (!wasNivel1Open)
        {
            if (activeScene.isDirty)
            {
                EditorSceneManager.SaveOpenScenes();
            }
            scene1 = EditorSceneManager.OpenScene(Nivel1ScenePath, OpenSceneMode.Single);
        }

        GameObject tilePisoObj = GameObject.Find("TilePiso");
        GameObject structuresObj = GameObject.Find("Structures");
        GameObject elementsObj = GameObject.Find("Elements");

        Tilemap tilePiso = tilePisoObj != null ? tilePisoObj.GetComponent<Tilemap>() : null;
        Tilemap structuresMap = structuresObj != null ? structuresObj.GetComponent<Tilemap>() : null;
        Tilemap elementsMap = elementsObj != null ? elementsObj.GetComponent<Tilemap>() : null;

        if (tilePiso == null)
        {
            Debug.LogWarning("[Nivel1Expand] No se encontró TilePiso en Nivel1.unity.");
            return;
        }

        // 1. Limpiar props sueltos anteriores (de Nivel2Props que desentonaban) y colocar solo el 3er Corazón
        CleanupOldPropsAndPlaceThirdHeart();

        // 2. Integrar arquitectura toscana coherente bajo los puentes existentes y construir los 4 tramos simétricos (X = 118..183)
        BuildCohesiveTuscanNivel1(tilePiso, structuresMap, elementsMap);

        // 3. Redistribuir uniformemente 11 Uvas Merlot (ritmo constante cada ~18-20 unidades)
        RedistributeUniformMerlotGrapes();

        // 4. Actualizar MetaNivel1 frente al Portón, límites y 8 paneles de fondo
        UpdateGoalBoundsAndBackground();

        EditorSceneManager.MarkSceneDirty(scene1);
        EditorSceneManager.SaveScene(scene1);

        EditorPrefs.SetInt(PrefKey, CurrentVersion);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        if (!silent)
        {
            Debug.Log("[Vineyard] Rediseño estético uniforme del Nivel 1 aplicado correctamente.");
        }
    }

    private static void CleanupOldPropsAndPlaceThirdHeart()
    {
        GameObject oldProps = GameObject.Find(PropsContainerName);
        if (oldProps != null)
        {
            Object.DestroyImmediate(oldProps);
        }

        GameObject root = new GameObject(PropsContainerName);

        // 3er Corazón Curativo centrado con simetría perfecta dentro de la pérgola del Mirador de la Finca (X = 158, Y = 10.6)
        GameObject heartPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CorazonPrefabPath);
        if (heartPrefab != null)
        {
            GameObject heart3 = (GameObject)PrefabUtility.InstantiatePrefab(heartPrefab);
            heart3.name = "CorazonVida_3_MiradorFinca";
            heart3.transform.SetParent(root.transform);
            heart3.transform.position = new Vector3(158.0f, 10.55f, 0f);
        }
    }

    private static void BuildCohesiveTuscanNivel1(
        Tilemap tilePiso,
        Tilemap structuresMap,
        Tilemap elementsMap)
    {
        // Limpiar completamente la zona de expansión (X >= 118) para reconstruirla limpia y simétrica
        ClearRange(tilePiso, 118, 190, -5, 18);
        if (structuresMap != null) ClearRange(structuresMap, 118, 190, -5, 18);
        if (elementsMap != null) ClearRange(elementsMap, 118, 190, -5, 18);

        // Limpiar pequeños restos decorativos de la versión 1 en la parte original antes de aplicar el diseño limpio
        if (structuresMap != null)
        {
            ClearRange(structuresMap, -31, -18, 5, 7);
            ClearRange(structuresMap, 15, 19, 6, 7);
            ClearRange(structuresMap, 96, 117, 4, 8);
            ClearRange(structuresMap, 65, 68, 4, 6);
        }

        TileBase[] grassTop = LoadTiles(
            "Assets/Tilemaps/tileset-ok_1.asset",
            "Assets/Tilemaps/tileset-ok_2.asset",
            "Assets/Tilemaps/tileset-ok_3.asset",
            "Assets/Tilemaps/tileset-ok_4.asset"
        );
        TileBase[] dirtMid = LoadTiles(
            "Assets/Tilemaps/tileset-ok_14.asset",
            "Assets/Tilemaps/tileset-ok_15.asset",
            "Assets/Tilemaps/tileset-ok_16.asset",
            "Assets/Tilemaps/tileset-ok_17.asset"
        );
        TileBase[] dirtDeep = LoadTiles(
            "Assets/Tilemaps/tileset-ok_45.asset",
            "Assets/Tilemaps/tileset-ok_46.asset",
            "Assets/Tilemaps/tileset-ok_47.asset",
            "Assets/Tilemaps/tileset-ok_48.asset"
        );
        TileBase[] stoneWall = LoadTiles(
            "Assets/Tilemaps/tileset-ok_68.asset",
            "Assets/Tilemaps/tileset-ok_69.asset",
            "Assets/Tilemaps/tileset-ok_70.asset",
            "Assets/Tilemaps/tileset-ok_71.asset"
        );
        TileBase[] brickTrim = LoadTiles(
            "Assets/Tilemaps/tileset-ok_76.asset",
            "Assets/Tilemaps/tileset-ok_77.asset",
            "Assets/Tilemaps/tileset-ok_78.asset",
            "Assets/Tilemaps/tileset-ok_79.asset"
        );
        TileBase[] stoneDeck = LoadTiles(
            "Assets/Tilemaps/tileset-ok_80.asset",
            "Assets/Tilemaps/tileset-ok_81.asset"
        );
        TileBase[] stoneCornice = LoadTiles(
            "Assets/Tilemaps/tileset-ok_82.asset",
            "Assets/Tilemaps/tileset-ok_83.asset"
        );

        // =====================================================================
        // A. INTEGRACIÓN ARQUITECTÓNICA EN LA ZONA ORIGINAL (soportes bajo plataformas)
        //    Cada plataforma ahora descansa sobre arcos de piedra toscanos coherentes
        // =====================================================================
        // 1. Arco-Puente de Bienvenida en X = -27..-24 (sustituye plataforma flotante sin soporte)
        ClearRange(tilePiso, -27, -23, 7, 8);
        PaintTileRow(tilePiso, -27, -24, 8, stoneDeck);
        if (structuresMap != null)
        {
            PaintStoneArch(structuresMap, -27, 5);
            PaintStoneArch(structuresMap, -25, 5);
            PaintTileRow(structuresMap, -27, -24, 7, stoneWall);
            PaintSignpost(structuresMap, -29, 5);
            PaintGrapeHedge(structuresMap, -22, 5, 4);
        }

        // 2. Viaducto de Piedra conectando las colinas centrales (X = 65..68, Y = 7)
        PaintTileRow(tilePiso, 65, 68, 7, stoneDeck);
        if (structuresMap != null)
        {
            PaintStoneArch(structuresMap, 65, 4);
            PaintStoneArch(structuresMap, 67, 4);
            PaintTileRow(structuresMap, 65, 68, 6, stoneWall);
        }

        // 3. Terraza de Arcos junto al acantilado final (X = 96..99, Y = 8)
        PaintTileRow(tilePiso, 96, 99, 8, stoneDeck);
        if (structuresMap != null)
        {
            PaintStoneArch(structuresMap, 96, 5);
            PaintStoneArch(structuresMap, 98, 5);
            PaintTileRow(structuresMap, 96, 99, 7, stoneWall);
            PaintWoodFence(structuresMap, 101, 5, 3);
            PaintGrapeHedge(structuresMap, 108, 5, 4);
            PaintWoodFence(structuresMap, 113, 5, 3);
        }

        // =====================================================================
        // B. EXPANSIÓN HORIZONTAL ESTILIZADA (4 SECCIONES SIMÉTRICAS DE 16 TILES: X = 118..183)
        // =====================================================================

        // ---------------------------------------------------------------------
        // SECCIÓN 1 (X = 118..133): PASEO DE LOS VIÑEDOS
        // Suelo verde continuo + ritmo simétrico de setos de uva, cercas y galería arqueada central
        // ---------------------------------------------------------------------
        PaintGrassTerrain(tilePiso, 118, 133, -2, 4, grassTop, dirtMid, dirtDeep);
        // Galería elevada simétrica en el centro (X = 123..128, cubierta de piedra en Y = 8 -> 3 tiles libres abajo)
        PaintTileRow(tilePiso, 123, 128, 8, stoneDeck);

        if (structuresMap != null)
        {
            // Simetría izquierda-derecha: Seto de uvas (118..121) | 3 Arcos de Piedra (123..128) | Seto de uvas (130..133)
            PaintGrapeHedge(structuresMap, 118, 5, 4);
            PaintStoneArch(structuresMap, 123, 5);
            PaintBrickArch(structuresMap, 125, 5);
            PaintStoneArch(structuresMap, 127, 5);
            PaintTileRow(structuresMap, 123, 128, 7, stoneWall);
            PaintVinePergola(structuresMap, 125, 9);
            PaintWoodFence(structuresMap, 123, 9, 2);
            PaintWoodFence(structuresMap, 127, 9, 2);
            PaintGrapeHedge(structuresMap, 130, 5, 4);
        }
        if (elementsMap != null)
        {
            // Barriles y cajas nativos del tileset-ok flanqueando simétricamente la galería
            PaintNativeBarrelStack(elementsMap, 122, 5);
            PaintNativeCratePair(elementsMap, 129, 5);
        }

        // ---------------------------------------------------------------------
        // SECCIÓN 2 (X = 134..149): ACUEDUCTO TOSCANO Y JARDÍN HUNDIDO
        // Jardín empedrado/verde hundido solo 2 tiles (Y = 2 -> sup. 3) cruzado por un Acueducto de 4 arcos
        // ---------------------------------------------------------------------
        PaintGrassTerrain(tilePiso, 134, 135, -2, 4, grassTop, dirtMid, dirtDeep);
        PaintGrassTerrain(tilePiso, 136, 147, -2, 2, grassTop, dirtMid, dirtDeep);
        PaintGrassTerrain(tilePiso, 148, 149, -2, 4, grassTop, dirtMid, dirtDeep);

        // Tablero principal del Acueducto (X = 137..146, Y = 6 -> 3 tiles libres sobre el jardín Y = 3!)
        PaintTileRow(tilePiso, 137, 146, 6, stoneDeck);

        if (structuresMap != null)
        {
            // 5 arcos de piedra continuos sosteniendo el Acueducto desde el jardín inferior (Y = 3..4) + friso en Y = 5
            for (int ax = 137; ax <= 145; ax += 2)
            {
                PaintStoneArch(structuresMap, ax, 3);
            }
            PaintTileRow(structuresMap, 137, 146, 5, stoneWall);

            // Cerca rústica simétrica en los accesos del Acueducto
            PaintWoodFence(structuresMap, 134, 5, 2);
            PaintWoodFence(structuresMap, 148, 5, 2);
        }
        if (elementsMap != null)
        {
            // Enredaderas colgando delante del friso de piedra del Acueducto
            PaintHangingVines(elementsMap, 138, 145, 5);
            // Arbustos simétricos en el jardín inferior
            SetTileById(elementsMap, 136, 3, 145);
            SetTileById(elementsMap, 141, 3, 152);
            SetTileById(elementsMap, 142, 3, 153);
            SetTileById(elementsMap, 147, 3, 146);
        }

        // ---------------------------------------------------------------------
        // SECCIÓN 3 (X = 150..165): MIRADOR DE LA FINCA (VILLA TOSCANA SIMÉTRICA)
        // Escalones laterales en piedra (Y=6) + Fachada de 3 arcos + Balcón con Pérgola del 3er Corazón
        // ---------------------------------------------------------------------
        PaintGrassTerrain(tilePiso, 150, 165, -2, 4, grassTop, dirtMid, dirtDeep);

        // Escalones simétricos de piedra flanqueando el Mirador (X=150..151 y X=164..165 en Y=6)
        PaintTileRow(tilePiso, 150, 151, 6, stoneDeck);
        PaintTileRow(tilePiso, 164, 165, 6, stoneDeck);

        // Balcón superior del Mirador (X = 153..162, Y = 8 -> superficie Y = 9, 3 tiles libres abajo)
        PaintTileRow(tilePiso, 153, 162, 8, stoneDeck);

        if (structuresMap != null)
        {
            // Soportes de piedra bajo los escalones laterales
            SetTileById(structuresMap, 150, 5, 68);
            SetTileById(structuresMap, 151, 5, 69);
            SetTileById(structuresMap, 164, 5, 69);
            SetTileById(structuresMap, 165, 5, 70);

            // Columnata simétrica de 3 arcos en planta baja (Y = 5..6) unidos por pilares de piedra y friso de ladrillo (Y = 7)
            PaintStoneArch(structuresMap, 153, 5);
            SetTileById(structuresMap, 155, 5, 69); SetTileById(structuresMap, 155, 6, 62);
            SetTileById(structuresMap, 156, 5, 70); SetTileById(structuresMap, 156, 6, 63);
            PaintBrickArch(structuresMap, 157, 5);
            SetTileById(structuresMap, 159, 5, 69); SetTileById(structuresMap, 159, 6, 62);
            SetTileById(structuresMap, 160, 5, 70); SetTileById(structuresMap, 160, 6, 63);
            PaintStoneArch(structuresMap, 161, 5);
            PaintTileRow(structuresMap, 153, 162, 7, brickTrim);

            // Balcón superior (Y = 9..10): Cercas a los lados + Pérgola de uvas perfectamente centrada en X = 157..158
            PaintWoodFence(structuresMap, 153, 9, 3);
            PaintVinePergola(structuresMap, 157, 9);
            PaintWoodFence(structuresMap, 160, 9, 3);
        }
        if (elementsMap != null)
        {
            PaintNativeBarrelStack(elementsMap, 156, 5);
            PaintNativeBarrelStack(elementsMap, 159, 5);
        }

        // ---------------------------------------------------------------------
        // SECCIÓN 4 (X = 166..183): PATIO Y GRAN PORTÓN SIMÉTRICO DE LA BODEGA
        // Acceso ceremonial bordeado de vides hacia la fachada de la Bodega (conecta con Nivel 2)
        // ---------------------------------------------------------------------
        PaintGrassTerrain(tilePiso, 166, 183, -2, 4, grassTop, dirtMid, dirtDeep);
        // Muro sobrio de piedra cerrando el límite derecho (X = 181..183)
        PaintStonePillarWall(tilePiso, 181, 183, 5, 13, stoneWall);

        if (structuresMap != null)
        {
            // Paseo ceremonial previo al Portón (X = 166..170)
            PaintGrapeHedge(structuresMap, 166, 5, 4);
            PaintSignpost(structuresMap, 170, 5);

            // FACHADA SIMÉTRICA DEL PORTÓN DE LA BODEGA (X = 172..179):
            //   172..173: Arco de Ladrillo Izquierdo
            //   174:      Pilar de Piedra
            //   175..176: Gran Puerta Doble de Madera de la Bodega (centro exacto!)
            //   177:      Pilar de Piedra
            //   178..179: Arco de Ladrillo Derecho
            PaintBrickArch(structuresMap, 172, 5);
            SetTileById(structuresMap, 174, 5, 69); SetTileById(structuresMap, 174, 6, 62);
            PaintWineryDoubleDoor(structuresMap, 175, 5);
            SetTileById(structuresMap, 177, 5, 69); SetTileById(structuresMap, 177, 6, 62);
            PaintBrickArch(structuresMap, 178, 5);

            // Friso horizontal limpio de ladrillo (Y = 7) + Cornisa de piedra (Y = 8) + Frontón central (X = 174..177)
            PaintTileRow(structuresMap, 172, 179, 7, brickTrim);
            PaintTileRow(structuresMap, 172, 179, 8, stoneCornice);
            PaintTileRow(structuresMap, 174, 177, 8, stoneDeck);
            PaintTileRow(structuresMap, 174, 177, 9, brickTrim);
            PaintTileRow(structuresMap, 174, 177, 10, stoneCornice);
        }
        if (elementsMap != null)
        {
            // Enredaderas sutiles y simétricas colgando delante del friso del Portón
            SetTileById(elementsMap, 172, 7, 124);
            SetTileById(elementsMap, 179, 7, 127);
            // Barriles y cajas nativas simétricas enmarcando el Portón
            PaintNativeCratePair(elementsMap, 171, 5);
            PaintNativeBarrelStack(elementsMap, 180, 5);
        }

        // Regenerar Tilemaps y colisiones
        RefreshTilemapAndColliders(tilePiso);
        if (structuresMap != null) RefreshTilemapAndColliders(structuresMap);
        if (elementsMap != null) RefreshTilemapAndColliders(elementsMap);
        Physics2D.SyncTransforms();
    }

    private static void RedistributeUniformMerlotGrapes()
    {
        GameObject uvaPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(EnemigoUvaPrefabPath)
                               ?? AssetDatabase.LoadAssetAtPath<GameObject>(MerlotPrefabPath);
        if (uvaPrefab == null)
            return;

        string[] oldNames = new string[] { "Enemigos_Nivel1", "Enemigos_UvasMerlot" };
        for (int i = 0; i < oldNames.Length; i++)
        {
            GameObject old = GameObject.Find(oldNames[i]);
            if (old != null) Object.DestroyImmediate(old);
        }

        MerlotGrapeEnemy[] stray = Object.FindObjectsByType<MerlotGrapeEnemy>(FindObjectsSortMode.None);
        for (int i = 0; i < stray.Length; i++)
        {
            if (stray[i] != null) Object.DestroyImmediate(stray[i].gameObject);
        }

        GameObject container = new GameObject("Enemigos_Nivel1");

        // 11 Uvas Merlot distribuidas de forma uniforme (cada ~18-20 unidades) alternando suelo y altura
        (float x, float rayY, string label)[] placements = new (float, float, string)[]
        {
            (-15.0f,  9.5f, "EnemigoUva_1_Entrada"),
            (0.0f,   15.5f, "EnemigoUva_2_IslaAlta"),
            (20.0f,   9.0f, "EnemigoUva_3_CaminoMedio"),
            (38.0f,  13.5f, "EnemigoUva_4_CimaColina"),
            (57.5f,  13.5f, "EnemigoUva_5_PuenteCentral"),
            (76.0f,  13.5f, "EnemigoUva_6_TerrazaAlta"),
            (95.0f,   8.5f, "EnemigoUva_7_SalidaAcantilado"),
            (125.5f, 11.5f, "EnemigoUva_8_PaseoVinedos"),
            (142.0f,  9.5f, "EnemigoUva_9_Acueducto"),
            (158.0f,  7.5f, "EnemigoUva_10_MiradorFinca"),
            (170.5f,  7.5f, "EnemigoUva_11_GuardianPorton")
        };

        int sueloMask = LayerMask.GetMask("Suelo");
        MovimientoJugador player = Object.FindFirstObjectByType<MovimientoJugador>();
        Physics2D.SyncTransforms();

        for (int i = 0; i < placements.Length; i++)
        {
            float sx = placements[i].x;
            float rayY = placements[i].rayY;
            Vector3 targetPos = new Vector3(sx, rayY - 1.5f, 0f);

            RaycastHit2D hit = Physics2D.Raycast(
                new Vector2(sx, rayY),
                Vector2.down,
                14f,
                sueloMask != 0 ? sueloMask : ~0
            );

            if (hit.collider != null && (player == null || hit.collider.gameObject != player.gameObject))
            {
                targetPos.y = hit.point.y + 0.72f;
            }

            GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(uvaPrefab);
            inst.name = placements[i].label;
            inst.transform.SetParent(container.transform);
            inst.transform.position = targetPos;
        }
    }

    private static void UpdateGoalBoundsAndBackground()
    {
        // MetaNivel1 perfectamente centrada frente a las Puertas Dobles de la Bodega (X = 176.0, Y = 5.95)
        MetaNivel meta = Object.FindFirstObjectByType<MetaNivel>();
        if (meta != null)
        {
            meta.transform.position = new Vector3(176.0f, 5.95f, 0f);
            EditorUtility.SetDirty(meta.transform);
        }

        GameObject limDer = GameObject.Find("LimiteDerecha");
        if (limDer != null)
        {
            limDer.transform.position = new Vector3(180.5f, 10f, 0f);
            BoxCollider2D col = limDer.GetComponent<BoxCollider2D>();
            if (col != null)
            {
                col.offset = Vector2.zero;
                col.size = new Vector2(1.5f, 38f);
                EditorUtility.SetDirty(col);
            }
            EditorUtility.SetDirty(limDer.transform);
        }

        GameObject limAbajo = GameObject.Find("LimiteAbajo");
        if (limAbajo != null)
        {
            limAbajo.transform.position = new Vector3(68f, -4.5f, 0f);
            BoxCollider2D col = limAbajo.GetComponent<BoxCollider2D>();
            if (col != null)
            {
                col.offset = Vector2.zero;
                col.size = new Vector2(260f, 1.5f);
                EditorUtility.SetDirty(col);
            }
            EditorUtility.SetDirty(limAbajo.transform);
        }

        GameObject limArriba = GameObject.Find("LimiteArriba");
        if (limArriba != null)
        {
            limArriba.transform.position = new Vector3(68f, 30f, 0f);
            BoxCollider2D col = limArriba.GetComponent<BoxCollider2D>();
            if (col != null)
            {
                col.offset = Vector2.zero;
                col.size = new Vector2(260f, 2f);
                EditorUtility.SetDirty(col);
            }
            EditorUtility.SetDirty(limArriba.transform);
        }

        // Asegurar 8 paneles espejados en FondosVinedo_Nivel1 para cubrir todo el mapa expandido
        Sprite bgSprite = AssetDatabase.LoadAssetAtPath<Sprite>(BgAssetPath);
        GameObject bgContainer = GameObject.Find(BgContainerName);
        if (bgSprite != null && bgContainer != null)
        {
            for (int i = bgContainer.transform.childCount - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(bgContainer.transform.GetChild(i).gameObject);
            }

            float panelWidth = bgSprite.bounds.size.x;
            if (panelWidth < 1f) panelWidth = 43f;
            float stepX = panelWidth - 0.08f;
            float startCenterX = -65f;
            float centerY = 10.6f;

            for (int i = 0; i < 8; i++)
            {
                GameObject panel = new GameObject($"FondoVinedo_{i + 1}");
                panel.transform.SetParent(bgContainer.transform);
                panel.transform.position = new Vector3(startCenterX + i * stepX, centerY, 0f);
                panel.transform.localScale = new Vector3((i % 2 == 0) ? 1f : -1f, 1f, 1f);

                SpriteRenderer sr = panel.AddComponent<SpriteRenderer>();
                sr.sprite = bgSprite;
                sr.sortingOrder = -10;
                sr.color = new Color(0.93f, 0.90f, 0.95f, 1f);
            }
        }
    }

    // =========================================================================
    // UTILIDADES DE PINTADO COHERENTE (100% TILESET-OK NATIVO)
    // =========================================================================
    private static TileBase LoadTile(string path) => AssetDatabase.LoadAssetAtPath<TileBase>(path);

    private static TileBase[] LoadTiles(params string[] paths)
    {
        TileBase[] arr = new TileBase[paths.Length];
        for (int i = 0; i < paths.Length; i++) arr[i] = LoadTile(paths[i]);
        return arr;
    }

    private static TileBase Pick(TileBase[] arr, int index)
    {
        if (arr == null || arr.Length == 0) return null;
        return arr[Mathf.Abs(index) % arr.Length];
    }

    private static void ClearRange(Tilemap map, int xMin, int xMax, int yMin, int yMax)
    {
        for (int x = xMin; x <= xMax; x++)
            for (int y = yMin; y <= yMax; y++)
                map.SetTile(new Vector3Int(x, y, 0), null);
    }

    private static void PaintGrassTerrain(
        Tilemap map, int xStart, int xEnd, int yBottom, int yTop,
        TileBase[] top, TileBase[] mid, TileBase[] deep)
    {
        for (int x = xStart; x <= xEnd; x++)
        {
            for (int y = yBottom; y <= yTop; y++)
            {
                TileBase t = (y == yTop) ? Pick(top, x)
                           : (y == yTop - 1) ? Pick(mid, x)
                           : Pick(deep, x + y);
                if (t != null) map.SetTile(new Vector3Int(x, y, 0), t);
            }
        }
    }

    private static void PaintTileRow(Tilemap map, int xStart, int xEnd, int y, TileBase[] tiles)
    {
        for (int x = xStart; x <= xEnd; x++)
        {
            TileBase t = Pick(tiles, x);
            if (t != null) map.SetTile(new Vector3Int(x, y, 0), t);
        }
    }

    private static void PaintStonePillarWall(Tilemap map, int xStart, int xEnd, int yBottom, int yTop, TileBase[] stoneWall)
    {
        for (int x = xStart; x <= xEnd; x++)
        {
            for (int y = yBottom; y <= yTop; y++)
            {
                TileBase t = Pick(stoneWall, x + y);
                if (t != null) map.SetTile(new Vector3Int(x, y, 0), t);
            }
        }
    }

    private static void Paint2x2(Tilemap map, int x, int y, int botL, int botR, int topL, int topR)
    {
        SetTileById(map, x,     y,     botL);
        SetTileById(map, x + 1, y,     botR);
        SetTileById(map, x,     y + 1, topL);
        SetTileById(map, x + 1, y + 1, topR);
    }

    private static void PaintStoneArch(Tilemap map, int x, int y) =>
        Paint2x2(map, x, y, 223, 224, 207, 208);

    private static void PaintBrickArch(Tilemap map, int x, int y) =>
        Paint2x2(map, x, y, 225, 226, 209, 210);

    private static void PaintWineryDoubleDoor(Tilemap map, int x, int y) =>
        Paint2x2(map, x, y, 230, 231, 214, 215);

    private static void PaintVinePergola(Tilemap map, int x, int y) =>
        Paint2x2(map, x, y, 202, 203, 186, 187);

    private static void PaintSignpost(Tilemap map, int x, int y)
    {
        SetTileById(map, x, y,     206);
        SetTileById(map, x, y + 1, 190);
    }

    private static void PaintNativeBarrelStack(Tilemap map, int x, int y)
    {
        SetTileById(map, x, y,     192);
        SetTileById(map, x, y + 1, 176);
    }

    private static void PaintNativeCratePair(Tilemap map, int x, int y)
    {
        SetTileById(map, x, y,     195);
        SetTileById(map, x, y + 1, 179);
    }

    private static void PaintWoodFence(Tilemap map, int xStart, int y, int width)
    {
        int[] topIds = new int[] { 89, 90, 91 };
        int[] botIds = new int[] { 105, 106, 107 };
        for (int i = 0; i < width; i++)
        {
            SetTileById(map, xStart + i, y,     botIds[i % 3]);
            SetTileById(map, xStart + i, y + 1, topIds[i % 3]);
        }
    }

    private static void PaintGrapeHedge(Tilemap map, int xStart, int y, int width)
    {
        int[] topIds = new int[] { 116, 117, 118, 119 };
        int[] botIds = new int[] { 130, 131, 132, 133 };
        for (int i = 0; i < width; i++)
        {
            SetTileById(map, xStart + i, y,     botIds[i % 4]);
            SetTileById(map, xStart + i, y + 1, topIds[i % 4]);
        }
    }

    private static void PaintHangingVines(Tilemap map, int xStart, int xEnd, int y)
    {
        int[] vineIds = new int[] { 124, 125, 126, 127 };
        for (int x = xStart; x <= xEnd; x += 2)
        {
            SetTileById(map, x, y, vineIds[Mathf.Abs(x) % vineIds.Length]);
        }
    }

    private static void SetTileById(Tilemap map, int x, int y, int tileId)
    {
        TileBase t = LoadTile($"Assets/Tilemaps/tileset-ok_{tileId}.asset");
        if (t != null) map.SetTile(new Vector3Int(x, y, 0), t);
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
            compCol.offsetDistance = 0.005f;
            compCol.GenerateGeometry();
            EditorUtility.SetDirty(compCol);
        }

        EditorUtility.SetDirty(map);
    }
}
#endif
