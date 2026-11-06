#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class VineyardAttackAutoSetup
{
    private const string CorkSpritePath = "Assets/Sprites/Personajes/cork.png";
    private const string AttackSheetPath = "Assets/Sprites/Personajes/attack.png";
    private const string WalkSheetPath = "Assets/Sprites/Personajes/walk.png";
    private const string CorkPrefabDir = "Assets/Prefabs";
    private const string CorkPrefabPath = "Assets/Prefabs/Cork.prefab";
    private const string AttackAnimPath = "Assets/Animations/attack.anim";

    static VineyardAttackAutoSetup()
    {
        EditorApplication.delayCall += RunAutoSetupIfNeeded;
    }

    private static void RunAutoSetupIfNeeded()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        bool needsPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CorkPrefabPath) == null;
        bool needsCorkSprite = AssetDatabase.LoadAssetAtPath<Sprite>(CorkSpritePath) == null;
        bool needsAttackSprite = AssetDatabase.LoadAssetAtPath<Texture2D>(AttackSheetPath) == null;

        if (needsPrefab || needsCorkSprite || needsAttackSprite)
        {
            SetupAll();
        }
        else
        {
            // Asegurar que el jugador en la escena tenga conectado el PlayerAttack y el Prefab
            ConfigurePlayerInScene(silent: true);
        }
    }

    [MenuItem("Herramientas Vineyard/Configurar Ataque de Corcho Automáticamente")]
    public static void SetupAllMenu()
    {
        SetupAll();
        EditorUtility.DisplayDialog(
            "Vineyard - Ataque Configurado",
            "¡Se han creado y configurado automáticamente:\n\n" +
            "• Sprite del corcho (Assets/Sprites/Personajes/cork.png)\n" +
            "• Sprite sheet de ataque (Assets/Sprites/Personajes/attack.png)\n" +
            "• Animación de ataque (Assets/Animations/attack.anim)\n" +
            "• Prefab del corcho (Assets/Prefabs/Cork.prefab)\n" +
            "• Componente PlayerAttack y FirePoint en tu Jugador!",
            "¡Excelente!"
        );
    }

    public static void SetupAll()
    {
        EnsureDirectories();
        Sprite corkSprite = CreateAndImportCorkSprite();
        Sprite[] attackSprites = CreateAndImportAttackSprites();

        if (attackSprites != null && attackSprites.Length >= 4)
        {
            UpdateAttackAnimationClip(attackSprites);
        }

        GameObject corkPrefab = CreateOrUpdateCorkPrefab(corkSprite);
        ConfigurePlayerInScene(silent: false, corkPrefabOverride: corkPrefab);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static void EnsureDirectories()
    {
        if (!AssetDatabase.IsValidFolder(CorkPrefabDir))
        {
            AssetDatabase.CreateFolder("Assets", "Prefabs");
        }
        if (!AssetDatabase.IsValidFolder("Assets/Sprites/Personajes"))
        {
            if (!AssetDatabase.IsValidFolder("Assets/Sprites"))
                AssetDatabase.CreateFolder("Assets", "Sprites");
            AssetDatabase.CreateFolder("Assets/Sprites", "Personajes");
        }
    }

    private static Sprite CreateAndImportCorkSprite()
    {
        if (!File.Exists(CorkSpritePath))
        {
            int w = 32, h = 18;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color clear = new Color(0, 0, 0, 0);
            Color outline = new Color(0.16f, 0.09f, 0.04f, 1f);
            Color corkBase = new Color(0.83f, 0.63f, 0.39f, 1f);
            Color corkHighlight = new Color(0.93f, 0.77f, 0.54f, 1f);
            Color corkShadow = new Color(0.64f, 0.44f, 0.24f, 1f);
            Color wineTip = new Color(0.58f, 0.13f, 0.24f, 1f);
            Color wineDark = new Color(0.42f, 0.07f, 0.16f, 1f);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    // Forma ligeramente acampanada de corcho de vino
                    int topMargin = (x < 9) ? 2 : 3;
                    int botMargin = (x < 9) ? 2 : 3;

                    if (x < 3 || x > w - 4 || y < botMargin || y >= h - topMargin)
                    {
                        tex.SetPixel(x, y, clear);
                        continue;
                    }

                    bool isBorder = (x == 3 || x == w - 4 || y == botMargin || y == h - topMargin - 1);
                    if (isBorder)
                    {
                        tex.SetPixel(x, y, outline);
                    }
                    else if (x >= w - 9)
                    {
                        tex.SetPixel(x, y, (y < 6) ? wineDark : wineTip);
                    }
                    else if (y >= h - topMargin - 3)
                    {
                        tex.SetPixel(x, y, corkHighlight);
                    }
                    else if (y <= botMargin + 2 || ((x * 3 + y * 5) % 11 == 0))
                    {
                        tex.SetPixel(x, y, corkShadow);
                    }
                    else
                    {
                        tex.SetPixel(x, y, corkBase);
                    }
                }
            }

            tex.Apply();
            File.WriteAllBytes(CorkSpritePath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(CorkSpritePath, ImportAssetOptions.ForceUpdate);
        }

        TextureImporter importer = AssetImporter.GetAtPath(CorkSpritePath) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.spritePixelsPerUnit = 96f;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(CorkSpritePath);
    }

    private static Sprite[] CreateAndImportAttackSprites()
    {
        if (!File.Exists(AttackSheetPath) && File.Exists(WalkSheetPath))
        {
            byte[] walkBytes = File.ReadAllBytes(WalkSheetPath);
            Texture2D walkTex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            walkTex.LoadImage(walkBytes);

            int w = walkTex.width;
            int h = walkTex.height;
            Texture2D attackTex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            attackTex.SetPixels(walkTex.GetPixels());

            // Centros aproximados X de los 4 frames en walk.png (63..202, 311..445, 558..696, 804..940)
            int[] frameStartsX = { 63, 311, 558, 804 };
            int[] bottleOffsetsX = { 98, 108, 114, 104 };
            int[] bottleOffsetsY = { 78, 82, 84, 76 };

            for (int f = 0; f < 4; f++)
            {
                int bx = frameStartsX[f] + bottleOffsetsX[f];
                int by = bottleOffsetsY[f];
                DrawPixelWineBottleAndEffect(attackTex, bx, by, f);
            }

            attackTex.Apply();
            File.WriteAllBytes(AttackSheetPath, attackTex.EncodeToPNG());
            Object.DestroyImmediate(walkTex);
            Object.DestroyImmediate(attackTex);

            // Copiar configuración de slicing de walk.png.meta reemplazando walk_ por attack_
            string walkMetaPath = WalkSheetPath + ".meta";
            string attackMetaPath = AttackSheetPath + ".meta";
            AssetDatabase.ImportAsset(AttackSheetPath, ImportAssetOptions.ForceUpdate);

            if (File.Exists(walkMetaPath) && File.Exists(attackMetaPath))
            {
                string currentAttackMeta = File.ReadAllText(attackMetaPath);
                string guidLine = "";
                foreach (string line in currentAttackMeta.Split('\n'))
                {
                    if (line.TrimStart().StartsWith("guid:"))
                    {
                        guidLine = line.Trim();
                        break;
                    }
                }

                string walkMeta = File.ReadAllText(walkMetaPath);
                if (!string.IsNullOrEmpty(guidLine))
                {
                    walkMeta = System.Text.RegularExpressions.Regex.Replace(
                        walkMeta, @"guid:\s*[0-9a-fA-F]+", guidLine);
                }
                walkMeta = walkMeta.Replace("walk_", "attack_");
                // Ajustar ancho del rect para incluir la botella de vino y el destello
                walkMeta = walkMeta.Replace("width: 139", "width: 165")
                                   .Replace("width: 134", "width: 165")
                                   .Replace("width: 138", "width: 165")
                                   .Replace("width: 136", "width: 165");
                File.WriteAllText(attackMetaPath, walkMeta);
                AssetDatabase.ImportAsset(AttackSheetPath, ImportAssetOptions.ForceUpdate);
            }
        }

        Object[] allAssets = AssetDatabase.LoadAllAssetsAtPath(AttackSheetPath);
        System.Collections.Generic.List<Sprite> sprites = new System.Collections.Generic.List<Sprite>();
        foreach (Object obj in allAssets)
        {
            if (obj is Sprite s)
                sprites.Add(s);
        }
        sprites.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
        return sprites.ToArray();
    }

    private static void DrawPixelWineBottleAndEffect(Texture2D tex, int startX, int startY, int frameIndex)
    {
        Color outline = new Color(0.08f, 0.06f, 0.05f, 1f);
        Color glassDark = new Color(0.11f, 0.32f, 0.16f, 1f);
        Color glassMid = new Color(0.18f, 0.48f, 0.24f, 1f);
        Color glassShine = new Color(0.55f, 0.85f, 0.58f, 1f);
        Color labelColor = new Color(0.95f, 0.88f, 0.72f, 1f);
        Color wineRed = new Color(0.62f, 0.12f, 0.22f, 1f);
        Color corkCol = new Color(0.85f, 0.65f, 0.40f, 1f);
        Color popGold = new Color(1f, 0.92f, 0.35f, 1f);
        Color popWhite = new Color(1f, 0.98f, 0.90f, 1f);

        int scale = 4; // Escala de bloque pixel art acorde a walk.png

        // Cuerpo de la botella horizontal (8x4 bloques)
        for (int by = 0; by < 5; by++)
        {
            for (int bx = 0; bx < 8; bx++)
            {
                Color c = glassMid;
                if (bx == 0 || bx == 7 || by == 0 || by == 4) c = outline;
                else if (by == 3) c = glassShine;
                else if (by == 1) c = glassDark;
                else if (bx >= 2 && bx <= 4 && by == 2) c = (bx == 3) ? wineRed : labelColor;

                FillPixelBlock(tex, startX + bx * scale, startY + by * scale, scale, c);
            }
        }

        // Cuello de la botella (3x3 bloques a la derecha)
        for (int by = 1; by <= 3; by++)
        {
            for (int bx = 8; bx < 11; bx++)
            {
                Color c = (by == 1 || by == 3 || bx == 10) ? outline : glassDark;
                FillPixelBlock(tex, startX + bx * scale, startY + by * scale, scale, c);
            }
        }

        // Frame 0 y 1: Corcho asomándose en la punta
        if (frameIndex == 0 || frameIndex == 1)
        {
            int corkOffset = (frameIndex == 1) ? 12 : 11;
            FillPixelBlock(tex, startX + corkOffset * scale, startY + 2 * scale, scale, corkCol);
            FillPixelBlock(tex, startX + (corkOffset + 1) * scale, startY + 2 * scale, scale, outline);
        }

        // Frame 2: Explosión "¡POP!" al disparar el corcho
        if (frameIndex == 2)
        {
            FillPixelBlock(tex, startX + 12 * scale, startY + 2 * scale, scale, popWhite);
            FillPixelBlock(tex, startX + 13 * scale, startY + 3 * scale, scale, popGold);
            FillPixelBlock(tex, startX + 13 * scale, startY + 1 * scale, scale, popGold);
            FillPixelBlock(tex, startX + 14 * scale, startY + 2 * scale, scale, popGold);
            FillPixelBlock(tex, startX + 12 * scale, startY + 4 * scale, scale, wineRed);
            FillPixelBlock(tex, startX + 12 * scale, startY + 0 * scale, scale, wineRed);
        }
    }

    private static void FillPixelBlock(Texture2D tex, int x0, int y0, int size, Color col)
    {
        for (int dy = 0; dy < size; dy++)
        {
            for (int dx = 0; dx < size; dx++)
            {
                int px = x0 + dx;
                int py = y0 + dy;
                if (px >= 0 && px < tex.width && py >= 0 && py < tex.height)
                {
                    tex.SetPixel(px, py, col);
                }
            }
        }
    }

    private static void UpdateAttackAnimationClip(Sprite[] attackSprites)
    {
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(AttackAnimPath);
        if (clip == null) return;

        EditorCurveBinding spriteBinding = new EditorCurveBinding
        {
            type = typeof(SpriteRenderer),
            path = "",
            propertyName = "m_Sprite"
        };

        ObjectReferenceKeyframe[] keyframes = new ObjectReferenceKeyframe[4];
        float[] times = { 0f, 0.06f, 0.14f, 0.24f };
        for (int i = 0; i < 4; i++)
        {
            keyframes[i] = new ObjectReferenceKeyframe
            {
                time = times[i],
                value = attackSprites[i]
            };
        }

        AnimationUtility.SetObjectReferenceCurve(clip, spriteBinding, keyframes);
        EditorUtility.SetDirty(clip);
    }

    private static GameObject CreateOrUpdateCorkPrefab(Sprite corkSprite)
    {
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(CorkPrefabPath);
        if (existing != null)
            return existing;

        GameObject tempCork = new GameObject("Cork");

        SpriteRenderer sr = tempCork.AddComponent<SpriteRenderer>();
        sr.sprite = corkSprite;
        sr.sortingOrder = 5;

        Rigidbody2D rb = tempCork.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 0f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        BoxCollider2D col = tempCork.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(0.28f, 0.16f);

        CorkProjectile proj = tempCork.AddComponent<CorkProjectile>();
        proj.speed = 10f;
        proj.maxDistance = 4.2f;
        proj.lifeTime = 3f;
        proj.corkScale = 0.45f;
        proj.useRealisticThrowArc = true;
        proj.throwArcForce = 1.6f;
        proj.spinSpeed = 620f;
        proj.usePopAnimation = true;

        tempCork.transform.localScale = new Vector3(0.45f, 0.45f, 1f);

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(tempCork, CorkPrefabPath);
        Object.DestroyImmediate(tempCork);

        return prefab;
    }

    private static void ConfigurePlayerInScene(bool silent, GameObject corkPrefabOverride = null)
    {
        MovimientoJugador playerMove = Object.FindFirstObjectByType<MovimientoJugador>();
        if (playerMove == null) return;

        GameObject playerObj = playerMove.gameObject;
        bool modified = false;

        if (playerObj.tag != "Player")
        {
            playerObj.tag = "Player";
            modified = true;
        }

        Transform firePoint = playerObj.transform.Find("FirePoint");
        if (firePoint == null)
        {
            GameObject fpObj = new GameObject("FirePoint");
            fpObj.transform.SetParent(playerObj.transform);
            fpObj.transform.localPosition = new Vector3(0.8f, 0.1f, 0f);
            fpObj.transform.localRotation = Quaternion.identity;
            fpObj.transform.localScale = Vector3.one;
            firePoint = fpObj.transform;
            modified = true;
        }

        PlayerAttack attack = playerObj.GetComponent<PlayerAttack>();
        if (attack == null)
        {
            attack = playerObj.AddComponent<PlayerAttack>();
            modified = true;
        }

        GameObject corkPrefab = corkPrefabOverride != null
            ? corkPrefabOverride
            : AssetDatabase.LoadAssetAtPath<GameObject>(CorkPrefabPath);

        if (attack.corkPrefab == null && corkPrefab != null)
        {
            attack.corkPrefab = corkPrefab;
            modified = true;
        }

        if (attack.firePoint == null)
        {
            attack.firePoint = firePoint;
            modified = true;
        }

        if (attack.animator == null)
        {
            attack.animator = playerObj.GetComponent<Animator>();
            modified = true;
        }

        if (modified && !Application.isPlaying)
        {
            EditorUtility.SetDirty(playerObj);
            EditorSceneManager.MarkSceneDirty(playerObj.scene);
            if (!silent)
            {
                Debug.Log("[Vineyard] ¡PlayerAttack, FirePoint y Cork.prefab configurados automáticamente en el Jugador!");
            }
        }
    }
}
#endif
