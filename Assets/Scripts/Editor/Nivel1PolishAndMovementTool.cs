#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

/// <summary>
/// Mejora el Nivel 1 (Assets/Scenes/Nivel1.unity):
/// 1. Reemplaza el cielo plano por el nuevo fondo pixel-art de viñedo al atardecer con parallax suave.
/// 2. Soluciona los puntos donde el personaje se atoraba o no podía brincar (túneles estrechos,
///    collider sin fricción con esquinas redondeadas, plataformas de acceso a todas las zonas altas).
/// 3. Corrige el collider desplazado de LimiteDerecha y regenera las colisiones de TilePiso.
/// </summary>
[InitializeOnLoad]
public static class Nivel1PolishAndMovementTool
{
    private const string PrefKey = "Vineyard_Nivel1_PolishMovementBg_V1";
    private const int CurrentVersion = 1;

    private const string Nivel1ScenePath = "Assets/Scenes/Nivel1.unity";
    private const string NewBgAssetPath = "Assets/Sprites/Fondos/FondoNivel1_VinedoAtardecer.jpg";
    private const string ZeroFrictionMatPath = "Assets/Settings/JugadorSinFriccion.physicsMaterial2D";
    private const string BgContainerName = "FondosVinedo_Nivel1";

    static Nivel1PolishAndMovementTool()
    {
        EditorApplication.delayCall += RunOnceIfNeeded;
    }

    private static void RunOnceIfNeeded()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        if (EditorPrefs.GetInt(PrefKey, 0) >= CurrentVersion)
            return;

        if (!File.Exists(Nivel1ScenePath) || !File.Exists(NewBgAssetPath))
            return;

        ApplyAllNivel1Improvements(silent: true);
    }

    [MenuItem("Herramientas Vineyard/Mejorar Nivel 1 (Fondo, Diseño y Fluidez de Movimiento)")]
    public static void ApplyFromMenu()
    {
        ApplyAllNivel1Improvements(silent: false);
        EditorUtility.DisplayDialog(
            "Vineyard - Nivel 1 Mejorado",
            "¡Se aplicaron todas las mejoras al Nivel 1!\n\n" +
            "• Fondo nuevo de viñedo toscano al atardecer con parallax suave.\n" +
            "• Movimiento fluido anti-atoros: PhysicsMaterial2D sin fricción, collider más ágil con bordes redondeados, detección amplia de pies, Coyote Time y Jump Buffer.\n" +
            "• Túneles ampliados (X=48..52 y X=63..66) y plataformas escalonadas para recorrer todo el mapa libremente.\n" +
            "• LimiteDerecha corregido al nivel del suelo.",
            "¡A probarlo!"
        );
    }

    public static void ApplyAllNivel1Improvements(bool silent)
    {
        Sprite newBgSprite = ConfigureNewNivel1BackgroundSprite();
        PhysicsMaterial2D zeroFrictionMat = GetOrCreateZeroFrictionMaterial();

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

        // 1. Nuevo fondo de viñedo al atardecer con parallax suave
        if (newBgSprite != null)
        {
            ReplaceNivel1Background(newBgSprite);
        }

        // 2. Mejorar collider, PuntoSuelo y física de salto/movimiento del personaje
        UpgradePlayerInNivel1(zeroFrictionMat);

        // 3. Abrir zonas donde se atoraba y añadir plataformas escalonadas naturales
        ImproveNivel1TilemapDesign();

        // 4. Corregir LimiteDerecha (tenía offset Y=41 flotando en el cielo)
        FixNivel1Bounds();

        EditorSceneManager.MarkSceneDirty(scene1);
        EditorSceneManager.SaveScene(scene1);

        EditorPrefs.SetInt(PrefKey, CurrentVersion);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        if (!silent)
        {
            Debug.Log("[Vineyard] Mejoras de Nivel 1 aplicadas y guardadas correctamente.");
        }
    }

    private static Sprite ConfigureNewNivel1BackgroundSprite()
    {
        if (!File.Exists(NewBgAssetPath))
        {
            Debug.LogWarning($"[Nivel1Polish] No se encontró el fondo en {NewBgAssetPath}.");
            return null;
        }

        AssetDatabase.ImportAsset(NewBgAssetPath, ImportAssetOptions.ForceSynchronousImport);
        TextureImporter imp = AssetImporter.GetAtPath(NewBgAssetPath) as TextureImporter;
        if (imp != null)
        {
            bool changed = imp.textureType != TextureImporterType.Sprite ||
                           imp.spriteImportMode != SpriteImportMode.Single ||
                           !Mathf.Approximately(imp.spritePixelsPerUnit, 32f) ||
                           imp.filterMode != FilterMode.Point ||
                           imp.maxTextureSize < 2048 ||
                           imp.textureCompression != TextureImporterCompression.Uncompressed;

            if (changed)
            {
                imp.textureType = TextureImporterType.Sprite;
                imp.spriteImportMode = SpriteImportMode.Single;
                imp.spritePixelsPerUnit = 32f; // 1376x768 -> 43 x 24 unidades
                imp.filterMode = FilterMode.Point;
                imp.maxTextureSize = 2048;
                imp.textureCompression = TextureImporterCompression.Uncompressed;
                imp.mipmapEnabled = false;
                imp.SaveAndReimport();
            }
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(NewBgAssetPath);
    }

    private static PhysicsMaterial2D GetOrCreateZeroFrictionMaterial()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Settings"))
        {
            AssetDatabase.CreateFolder("Assets", "Settings");
        }

        PhysicsMaterial2D mat = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(ZeroFrictionMatPath);
        if (mat == null)
        {
            mat = new PhysicsMaterial2D("JugadorSinFriccion")
            {
                friction = 0f,
                bounciness = 0f
            };
            AssetDatabase.CreateAsset(mat, ZeroFrictionMatPath);
        }
        else
        {
            mat.friction = 0f;
            mat.bounciness = 0f;
            EditorUtility.SetDirty(mat);
        }

        return mat;
    }

    private static void ReplaceNivel1Background(Sprite bgSprite)
    {
        GameObject oldContainer = GameObject.Find(BgContainerName);
        if (oldContainer != null)
        {
            Object.DestroyImmediate(oldContainer);
        }

        // Desactivar los paneles planos anteriores del cielo vacío
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
        container.transform.position = Vector3.zero;

        ParallaxSuaveNivel1 parallax = container.AddComponent<ParallaxSuaveNivel1>();
        parallax.factorParallaxX = 0.28f;
        parallax.factorParallaxY = 0.14f;

        float panelWidth = bgSprite.bounds.size.x;
        if (panelWidth < 1f) panelWidth = 43f;

        float stepX = panelWidth - 0.08f;
        float startCenterX = -65f;
        float centerY = 10.6f;

        // 6 paneles espejados alternados cubren sobradamente de X ≈ -86 a X ≈ 170 incluso con parallax
        for (int i = 0; i < 6; i++)
        {
            GameObject panel = new GameObject($"FondoVinedo_{i + 1}");
            panel.transform.SetParent(container.transform);
            panel.transform.position = new Vector3(startCenterX + i * stepX, centerY, 0f);
            panel.transform.localScale = new Vector3((i % 2 == 0) ? 1f : -1f, 1f, 1f);

            SpriteRenderer sr = panel.AddComponent<SpriteRenderer>();
            sr.sprite = bgSprite;
            sr.sortingOrder = -10;
            sr.color = new Color(0.93f, 0.90f, 0.95f, 1f);
        }
    }

    private static void UpgradePlayerInNivel1(PhysicsMaterial2D zeroFrictionMat)
    {
        GameObject character = GameObject.Find("Character");
        if (character == null)
        {
            MovimientoJugador mj = Object.FindFirstObjectByType<MovimientoJugador>();
            if (mj != null) character = mj.gameObject;
        }

        if (character == null) return;

        // 1. BoxCollider2D más estrecho y con bordes redondeados (1.20 x 1.88 total) para no atorarse
        BoxCollider2D box = character.GetComponent<BoxCollider2D>();
        if (box != null)
        {
            box.size = new Vector2(1.04f, 1.72f);
            box.offset = new Vector2(0f, -0.08f);
            box.edgeRadius = 0.08f;
            box.sharedMaterial = zeroFrictionMat;
            EditorUtility.SetDirty(box);
        }

        // 2. Rigidbody2D suave, continuo y sin fricción
        Rigidbody2D rb = character.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.sharedMaterial = zeroFrictionMat;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            EditorUtility.SetDirty(rb);
        }

        // 3. Alinear PuntoSuelo justo en la base de las botas
        Transform puntoSuelo = character.transform.Find("PuntoSuelo");
        if (puntoSuelo != null)
        {
            puntoSuelo.localPosition = new Vector3(0f, -1.00f, 0f);
            EditorUtility.SetDirty(puntoSuelo);
        }

        // 4. Ajustar parámetros de MovimientoJugador para salto cómodo y responsivo
        MovimientoJugador mov = character.GetComponent<MovimientoJugador>();
        if (mov != null)
        {
            int sueloLayer = LayerMask.NameToLayer("Suelo");
            if (sueloLayer < 0) sueloLayer = 6;

            mov.velocidadMovimiento = 8f;
            mov.fuerzaSalto = 11.2f;
            mov.tiempoCoyote = 0.13f;
            mov.tiempoBufferSalto = 0.14f;
            mov.multiplicadorCaida = 1.35f;
            mov.permitirSaltoConWYFlechaArriba = true;
            mov.aplicarMaterialSinFriccion = true;
            mov.radioComprobacion = 0.28f;
            mov.capaSuelo = 1 << sueloLayer;
            if (puntoSuelo != null)
                mov.comprobadorSuelo = puntoSuelo;

            EditorUtility.SetDirty(mov);
        }
    }

    private static void ImproveNivel1TilemapDesign()
    {
        GameObject tilePisoObj = GameObject.Find("TilePiso");
        Tilemap tilePiso = tilePisoObj != null ? tilePisoObj.GetComponent<Tilemap>() : null;
        if (tilePiso == null) return;

        // Cargar los mismos tiles nativos de tileset-ok que ya usa el Nivel 1
        TileBase[] grassTop = LoadTiles(
            "Assets/Tilemaps/tileset-ok_1.asset",
            "Assets/Tilemaps/tileset-ok_2.asset",
            "Assets/Tilemaps/tileset-ok_3.asset",
            "Assets/Tilemaps/tileset-ok_4.asset"
        );

        // ---------------------------------------------------------------------
        // A. ABRIR ZONAS DONDE EL PERSONAJE SE ATORABA (TECHO MUY BAJO)
        // ---------------------------------------------------------------------
        // 1. Bajo la isla flotante inicial (X = -7..5): quitar salientes en Y = 9
        ClearRange(tilePiso, -5, -4, 9, 9);
        ClearRange(tilePiso, 3, 4, 9, 9);

        // 2. Túnel bajo el puente central (X = 48..66): tenía exactamente 2.0 tiles de altura
        //    y el personaje medía 2.06 -> abrir entrada izquierda (X=48..52, Y=7) y salida derecha (X=63..66, Y=6..7)
        ClearRange(tilePiso, 48, 52, 7, 7);
        ClearRange(tilePiso, 63, 66, 6, 7);

        // ---------------------------------------------------------------------
        // B. AÑADIR PLATAFORMAS ESCALONADAS PARA RECORRER TODO EL MAPA LIBREMENTE
        //    (todas dejan >= 3 tiles de altura libre por debajo para nunca atorarse)
        // ---------------------------------------------------------------------
        // 1. Plataforma inicial en el camino de apertura (rompe el tramo plano largo, 3 tiles libres abajo)
        PaintSingleRowGrass(tilePiso, -27, -23, 8, grassTop);

        // 2. Escalón hacia la isla alta de EnemigoUva_3 (X = -7..5, Y = 12)
        PaintSingleRowGrass(tilePiso, -11, -8, 8, grassTop);

        // 3. Escalón suave antes de la colina media (X = 30..46)
        PaintSingleRowGrass(tilePiso, 25, 28, 7, grassTop);

        // 4. Plataforma clave en el claro X = 65..68 (Y = 7):
        //    - Deja 3 tiles libres debajo (Y = 4..7) para pasar sin atorarse
        //    - Permite subir desde el suelo inferior hacia las terrazas altas (Y = 10)
        //    - Conecta el salto entre la cima central (X = 60) y la terraza derecha (X = 69)
        PaintSingleRowGrass(tilePiso, 65, 68, 7, grassTop);

        // 5. Plataforma de acceso/descenso junto al acantilado final (X = 96..99, Y = 8, 4 tiles libres abajo)
        PaintSingleRowGrass(tilePiso, 96, 99, 8, grassTop);

        // ---------------------------------------------------------------------
        // C. REGENERAR COLISIONES DE TILEPISO
        // ---------------------------------------------------------------------
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
            compCol.offsetDistance = 0.005f;
            compCol.GenerateGeometry();
            EditorUtility.SetDirty(compCol);
        }

        EditorUtility.SetDirty(tilePiso);
        Physics2D.SyncTransforms();
    }

    private static void FixNivel1Bounds()
    {
        GameObject limDer = GameObject.Find("LimiteDerecha");
        if (limDer != null)
        {
            limDer.transform.position = new Vector3(119.5f, 10f, 0f);
            BoxCollider2D col = limDer.GetComponent<BoxCollider2D>();
            if (col != null)
            {
                col.offset = Vector2.zero;
                col.size = new Vector2(1.5f, 36f);
                EditorUtility.SetDirty(col);
            }
            EditorUtility.SetDirty(limDer.transform);
        }
    }

    private static TileBase[] LoadTiles(params string[] paths)
    {
        TileBase[] arr = new TileBase[paths.Length];
        for (int i = 0; i < paths.Length; i++)
        {
            arr[i] = AssetDatabase.LoadAssetAtPath<TileBase>(paths[i]);
        }
        return arr;
    }

    private static TileBase Pick(TileBase[] arr, int x)
    {
        if (arr == null || arr.Length == 0) return null;
        return arr[Mathf.Abs(x) % arr.Length];
    }

    private static void ClearRange(Tilemap map, int xMin, int xMax, int yMin, int yMax)
    {
        for (int x = xMin; x <= xMax; x++)
        {
            for (int y = yMin; y <= yMax; y++)
            {
                map.SetTile(new Vector3Int(x, y, 0), null);
            }
        }
    }

    private static void PaintSingleRowGrass(Tilemap map, int xMin, int xMax, int y, TileBase[] grassTop)
    {
        for (int x = xMin; x <= xMax; x++)
        {
            TileBase t = Pick(grassTop, x);
            if (t != null)
                map.SetTile(new Vector3Int(x, y, 0), t);
        }
    }
}
#endif
