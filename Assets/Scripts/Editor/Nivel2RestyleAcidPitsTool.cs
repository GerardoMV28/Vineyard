#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

/// <summary>
/// Reemplaza únicamente el diseño visual de los fosos de fermentación antiguos
/// (tiles verdes de tileset-ok-lvl2 en TilePiso / Structures) por el mismo diseño
/// limpio de Tile_AcidoSuperficie + Tile_AcidoProfundo en Elements usado en los fosos nuevos.
/// No modifica ningún otro elemento, enemigo, plataforma ni fondo del nivel.
/// </summary>
[InitializeOnLoad]
public static class Nivel2RestyleAcidPitsTool
{
    private const string PrefKey = "Vineyard_Nivel2_RestyleAcidPits_V1";
    private const int CurrentVersion = 1;

    private const string Nivel2ScenePath = "Assets/Scenes/Nivel2.unity";
    private const string TileAcidSurfacePath = "Assets/Tiles/Nivel2/Tile_AcidoSuperficie.asset";
    private const string TileAcidDeepPath = "Assets/Tiles/Nivel2/Tile_AcidoProfundo.asset";

    // Nombres de los tiles verdes de ácido del tileset-ok-lvl2 (filas inferiores del tileset)
    private static readonly HashSet<string> LegacyAcidTileNames = new HashSet<string>
    {
        "tileset-ok-lvl2_222", "tileset-ok-lvl2_223",
        "tileset-ok-lvl2_224", "tileset-ok-lvl2_225",
        "tileset-ok-lvl2_226", "tileset-ok-lvl2_227",
        "tileset-ok-lvl2_228", "tileset-ok-lvl2_229",
        "tileset-ok-lvl2_236", "tileset-ok-lvl2_237",
        "tileset-ok-lvl2_238", "tileset-ok-lvl2_239",
        "tileset-ok-lvl2_240", "tileset-ok-lvl2_241",
        "tileset-ok-lvl2_242", "tileset-ok-lvl2_243"
    };

    static Nivel2RestyleAcidPitsTool()
    {
        EditorApplication.delayCall += RunOnceIfNeeded;
    }

    private static void RunOnceIfNeeded()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        if (EditorPrefs.GetInt(PrefKey, 0) >= CurrentVersion)
            return;

        if (!File.Exists(Nivel2ScenePath))
            return;

        RestyleAllAcidPits(silent: true);
    }

    [MenuItem("Herramientas Vineyard/Unificar Diseño de Fosos de Ácido (Nivel 2)")]
    public static void RestyleFromMenu()
    {
        int replaced = RestyleAllAcidPits(silent: false);
        EditorUtility.DisplayDialog(
            "Vineyard - Fosos de Ácido Actualizados",
            $"Se actualizaron {replaced} tiles de fosos de fermentación al mismo estilo de los fosos nuevos (Tile_AcidoSuperficie + Tile_AcidoProfundo en Elements).\n\nNo se modificó nada más del nivel.",
            "¡Perfecto!"
        );
    }

    public static int RestyleAllAcidPits(bool silent)
    {
        TileBase tileAcidSurface = AssetDatabase.LoadAssetAtPath<TileBase>(TileAcidSurfacePath);
        TileBase tileAcidDeep = AssetDatabase.LoadAssetAtPath<TileBase>(TileAcidDeepPath);

        if (tileAcidSurface == null || tileAcidDeep == null)
        {
            Debug.LogWarning("[Nivel2RestyleAcidPits] No se encontraron Tile_AcidoSuperficie o Tile_AcidoProfundo.");
            return 0;
        }

        Scene activeScene = EditorSceneManager.GetActiveScene();
        bool wasNivel2Open = activeScene.path == Nivel2ScenePath;
        Scene scene2 = activeScene;

        if (!wasNivel2Open)
        {
            if (activeScene.isDirty)
            {
                EditorSceneManager.SaveOpenScenes();
            }
            scene2 = EditorSceneManager.OpenScene(Nivel2ScenePath, OpenSceneMode.Single);
        }

        GameObject tilePisoObj = GameObject.Find("TilePiso");
        GameObject structuresObj = GameObject.Find("Structures");
        GameObject elementsObj = GameObject.Find("Elements");

        Tilemap tilePiso = tilePisoObj != null ? tilePisoObj.GetComponent<Tilemap>() : null;
        Tilemap structuresMap = structuresObj != null ? structuresObj.GetComponent<Tilemap>() : null;
        Tilemap elementsMap = elementsObj != null ? elementsObj.GetComponent<Tilemap>() : null;

        if (elementsMap == null)
        {
            Debug.LogWarning("[Nivel2RestyleAcidPits] No se encontró el Tilemap 'Elements' en Nivel2.");
            return 0;
        }

        // 1. Localizar todas las celdas con tiles de ácido antiguos en TilePiso y Structures
        HashSet<Vector3Int> legacyCells = new HashSet<Vector3Int>();
        List<Vector3Int> pisoCellsToClear = new List<Vector3Int>();
        List<Vector3Int> structCellsToClear = new List<Vector3Int>();

        CollectLegacyAcidCells(tilePiso, legacyCells, pisoCellsToClear);
        CollectLegacyAcidCells(structuresMap, legacyCells, structCellsToClear);

        // También registrar cualquier celda de ácido ya existente en Elements para detectar superficies correctamente
        HashSet<Vector3Int> allAcidCells = new HashSet<Vector3Int>(legacyCells);
        BoundsInt elemBounds = elementsMap.cellBounds;
        foreach (Vector3Int pos in elemBounds.allPositionsWithin)
        {
            TileBase t = elementsMap.GetTile(pos);
            if (t == tileAcidSurface || t == tileAcidDeep)
            {
                allAcidCells.Add(pos);
            }
        }

        // 2. Quitar los tiles de ácido antiguos de TilePiso y Structures
        foreach (Vector3Int pos in pisoCellsToClear)
        {
            tilePiso.SetTile(pos, null);
        }
        foreach (Vector3Int pos in structCellsToClear)
        {
            structuresMap.SetTile(pos, null);
        }

        // 3. Pintar los tiles de ácido con el mismo diseño limpio en Elements:
        //    - Fila superior (sin ácido encima) -> Tile_AcidoSuperficie
        //    - Filas inferiores (con ácido encima) -> Tile_AcidoProfundo
        foreach (Vector3Int pos in legacyCells)
        {
            Vector3Int above = new Vector3Int(pos.x, pos.y + 1, pos.z);
            bool hasAcidAbove = allAcidCells.Contains(above);
            elementsMap.SetTile(pos, hasAcidAbove ? tileAcidDeep : tileAcidSurface);
        }

        // 4. Refrescar únicamente los Tilemaps tocados y regenerar colisión de TilePiso
        if (tilePiso != null && pisoCellsToClear.Count > 0)
        {
            RefreshTilemap(tilePiso);
        }
        if (structuresMap != null && structCellsToClear.Count > 0)
        {
            RefreshTilemap(structuresMap);
        }
        if (legacyCells.Count > 0)
        {
            RefreshTilemap(elementsMap);
            Physics2D.SyncTransforms();
        }

        EditorSceneManager.MarkSceneDirty(scene2);
        EditorSceneManager.SaveScene(scene2);

        EditorPrefs.SetInt(PrefKey, CurrentVersion);
        AssetDatabase.SaveAssets();

        if (!silent)
        {
            Debug.Log($"[Nivel2RestyleAcidPits] ¡{legacyCells.Count} tiles de fosos de fermentación actualizados al nuevo diseño!");
        }

        return legacyCells.Count;
    }

    private static void CollectLegacyAcidCells(
        Tilemap map,
        HashSet<Vector3Int> unionSet,
        List<Vector3Int> listForMap)
    {
        if (map == null) return;

        BoundsInt bounds = map.cellBounds;
        foreach (Vector3Int pos in bounds.allPositionsWithin)
        {
            TileBase tile = map.GetTile(pos);
            if (tile != null && LegacyAcidTileNames.Contains(tile.name))
            {
                unionSet.Add(pos);
                listForMap.Add(pos);
            }
        }
    }

    private static void RefreshTilemap(Tilemap map)
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
