#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class LevelLayoutBuilder
{
    private struct Seg
    {
        public float x0, x1, top;
        public Seg(float x0, float x1, float top) { this.x0 = x0; this.x1 = x1; this.top = top; }
    }

    private struct EnemyPlacement
    {
        public string patrolType; public float startX, minX, maxX;
        public EnemyPlacement(string t, float s, float a, float b) { patrolType = t; startX = s; minX = a; maxX = b; }
    }

    private class Layout
    {
        public string scenePath;
        public float leftWall, rightWall, playerStartX, fallDeathY;
        public List<Seg> ground = new List<Seg>();
        public List<Seg> platforms = new List<Seg>();
        public List<float> checkpoints = new List<float>();
        public List<EnemyPlacement> enemies = new List<EnemyPlacement>();
        public List<(PotionType type, float x, float surfaceTop)> potions = new List<(PotionType, float, float)>();
        public string musicKeyword;
        public float? exitX;
        public float visualGroundOffset = -0.03f;
    }

    private static Layout Level1Layout()
    {
        const float G = -1.3f;
        var l = new Layout { scenePath = "Assets/Scenes/Levels/Level1.unity", leftWall = -19f, rightWall = 82f, playerStartX = -14.3f, fallDeathY = -8f, exitX = 80f, visualGroundOffset = 0.11f };

        l.ground.Add(new Seg(-19f, 0f, G));
        l.platforms.Add(new Seg(-9f, -6f, 0.4f));

        l.ground.Add(new Seg(2.5f, 17f, G));
        l.platforms.Add(new Seg(8.5f, 11.5f, 1.6f));
        l.enemies.Add(new EnemyPlacement("GoblinPatrol", 12f, 5f, 15.5f));

        l.ground.Add(new Seg(20f, 24f, G));
        l.platforms.Add(new Seg(25.3f, 27.7f, -0.4f));

        l.ground.Add(new Seg(29f, 46f, G));
        l.checkpoints.Add(30f);
        l.platforms.Add(new Seg(32.5f, 35.5f, 1.9f));
        l.platforms.Add(new Seg(40f, 43f, 1.9f));
        l.enemies.Add(new EnemyPlacement("MushroomPatrol", 39f, 31.5f, 44.5f));

        l.ground.Add(new Seg(48.5f, 54f, G));
        l.platforms.Add(new Seg(55.5f, 58f, -0.3f));
        l.platforms.Add(new Seg(59.5f, 62f, 0.9f));
        l.platforms.Add(new Seg(63f, 65f, -0.3f));

        l.ground.Add(new Seg(66f, 82f, G));
        l.checkpoints.Add(67f);
        l.enemies.Add(new EnemyPlacement("GolemPatrol", 74f, 68.5f, 78.5f));

        l.potions.Add((PotionType.Speed, -7.5f, 0.4f));
        l.potions.Add((PotionType.Health, 26.5f, -0.4f));
        l.potions.Add((PotionType.Health, 51f, G));
        l.potions.Add((PotionType.Attack, 60.75f, 0.9f));
        l.musicKeyword = "valley of dreams";
        return l;
    }

    private static Layout Level2Layout()
    {
        const float G = -2.19f;
        var l = new Layout { scenePath = "Assets/Scenes/Levels/Level2.unity", leftWall = -9.5f, rightWall = 64f, playerStartX = -7.7f, fallDeathY = -9f };

        l.ground.Add(new Seg(-9.5f, 5f, G));
        l.enemies.Add(new EnemyPlacement("batPatrol", 3f, -1f, 9f));

        l.ground.Add(new Seg(7.5f, 13f, G));
        l.platforms.Add(new Seg(13.8f, 16.5f, -0.9f));

        l.ground.Add(new Seg(17.5f, 33f, G));
        l.checkpoints.Add(18.5f);
        l.platforms.Add(new Seg(18.6f, 21.4f, 0.3f));
        l.enemies.Add(new EnemyPlacement("GolemPatrol", 28f, 25f, 31f));

        l.platforms.Add(new Seg(34.5f, 37.5f, -1.4f));
        l.platforms.Add(new Seg(39f, 42f, -0.5f));

        l.ground.Add(new Seg(43.5f, 64f, G));
        l.checkpoints.Add(45f);
        l.platforms.Add(new Seg(48f, 51f, -0.6f));
        l.platforms.Add(new Seg(56f, 59f, -0.6f));
        l.enemies.Add(new EnemyPlacement("EyePatrol", 55f, 47f, 61f));

        l.potions.Add((PotionType.Health, 15.15f, -0.9f));
        l.potions.Add((PotionType.Attack, 20f, 0.3f));
        l.potions.Add((PotionType.Speed, 36f, -1.4f));
        l.potions.Add((PotionType.Health, 49.5f, -0.6f));
        l.musicKeyword = "tomorrow the battle";
        return l;
    }

    public const float Level3GroundTop = -2.19f;

    private static Layout Level3Layout()
    {
        const float G = Level3GroundTop;
        var l = new Layout { scenePath = "Assets/Scenes/Levels/Level3.unity", leftWall = 0f, rightWall = 100f, playerStartX = 3f, fallDeathY = -9f };

        l.ground.Add(new Seg(0f, 14f, G));
        l.ground.Add(new Seg(17.5f, 32f, G));
        l.checkpoints.Add(18.5f);
        l.enemies.Add(new EnemyPlacement("MushroomPatrol", 25f, 20.5f, 29.5f));

        l.platforms.Add(new Seg(33.7f, 35.3f, -1.2f));
        l.ground.Add(new Seg(37f, 52f, G));
        l.checkpoints.Add(38f);
        l.enemies.Add(new EnemyPlacement("GolemPatrol", 46f, 42f, 49f));

        l.platforms.Add(new Seg(53.6f, 55.8f, -1.3f));
        l.platforms.Add(new Seg(57.2f, 59.4f, -0.4f));
        l.enemies.Add(new EnemyPlacement("EyePatrol", 57f, 53f, 61f));

        l.ground.Add(new Seg(62f, 100f, G));
        l.checkpoints.Add(63f);
        l.checkpoints.Add(71f);

        l.potions.Add((PotionType.Health, 34.5f, -1.2f));
        l.potions.Add((PotionType.Speed, 54.7f, -1.3f));
        l.potions.Add((PotionType.Attack, 66f, G));
        l.potions.Add((PotionType.Health, 68f, G));
        l.potions.Add((PotionType.Health, 73f, G));
        l.musicKeyword = "good day to die";
        return l;
    }

    public static readonly (float x0, float x1)[] Level3LavaPits = { (14f, 17.5f), (32f, 37f), (52f, 62f) };

    [MenuItem("Tools/Game Setup/Layout/Rebuild Level3 Layout")]
    public static void BuildLevel3() => Build(Level3Layout(), Level2Terrain);

    [MenuItem("Tools/Game Setup/Layout/Rebuild Level1 Layout")]
    public static void BuildLevel1() => Build(Level1Layout(), Level1Terrain);

    [MenuItem("Tools/Game Setup/Layout/Rebuild Level2 Layout")]
    public static void BuildLevel2() => Build(Level2Layout(), Level2Terrain);

    private delegate GameObject SegmentFactory(Scene scene, Transform parent, Seg seg, bool isPlatform, Dictionary<string, Object> templates);

    private static void Build(Layout layout, SegmentFactory factory)
    {
        var scene = SceneManager.GetSceneByPath(layout.scenePath);
        bool wasOpen = scene.IsValid() && scene.isLoaded;
        if (!wasOpen) scene = EditorSceneManager.OpenScene(layout.scenePath, OpenSceneMode.Additive);

        var previous = scene.GetRootGameObjects().FirstOrDefault(r => r.name == "Terrain");
        if (previous != null) Object.DestroyImmediate(previous);

        var roots = scene.GetRootGameObjects();
        var templates = new Dictionary<string, Object>();
        CollectTemplates(roots, templates);

        var old = roots.FirstOrDefault(r => r.name == "_OldTerrain");
        if (old == null)
        {
            old = new GameObject("_OldTerrain");
            SceneManager.MoveGameObjectToScene(old, scene);
            foreach (var r in roots)
            {
                if (r.name == "Ground Terrain" || r.name.StartsWith("Tilesheet - WOODS_37") || r.name.StartsWith("ground1"))
                    r.transform.SetParent(old.transform, true);
            }
        }
        old.SetActive(false);

        var terrain = new GameObject("Terrain");
        SceneManager.MoveGameObjectToScene(terrain, scene);
        var groundRoot = Child(terrain.transform, "Ground");
        var platformRoot = Child(terrain.transform, "Platforms");
        var boundsRoot = Child(terrain.transform, "LevelBounds");
        var checkpointRoot = Child(terrain.transform, "Checkpoints");

        foreach (var s in layout.ground) factory(scene, groundRoot, s, false, templates);
        foreach (var s in layout.platforms) factory(scene, platformRoot, s, true, templates);

        foreach (var x in new[] { layout.leftWall - 0.5f, layout.rightWall + 0.5f })
        {
            var w = new GameObject(x < 0 ? "LeftWall" : "RightWall");
            w.transform.SetParent(boundsRoot, false);
            w.transform.position = new Vector3(x, 5f, 0f);
            w.AddComponent<BoxCollider2D>().size = new Vector2(1f, 30f);
        }

        var cpSound = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/respawn.wav");
        foreach (var x in layout.checkpoints)
        {
            var groundTop = layout.ground.Where(g => x >= g.x0 && x <= g.x1).Select(g => g.top).DefaultIfEmpty(0f).First();
            var cp = new GameObject("Checkpoint");
            cp.transform.SetParent(checkpointRoot, false);
            cp.transform.position = new Vector3(x, groundTop + 1.5f, 0f);
            var box = cp.AddComponent<BoxCollider2D>();
            box.isTrigger = true; box.size = new Vector2(1f, 4f);
            var c = cp.AddComponent<Checkpoint>();
            var so = new SerializedObject(c);
            so.FindProperty("activateSound").objectReferenceValue = cpSound;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        var itemRoot = Child(terrain.transform, "Items");
        foreach (var p in layout.potions)
            ContentBuilder.CreatePotion(itemRoot, p.type, new Vector3(p.x, p.surfaceTop + 0.55f, 0f));

        if (!string.IsNullOrEmpty(layout.musicKeyword)) ContentBuilder.EnsureLevelMusic(scene, layout.musicKeyword);

        foreach (var e in layout.enemies) PlaceEnemy(roots, e, layout);

        var player = roots.SelectMany(r => r.GetComponentsInChildren<Transform>(true)).FirstOrDefault(t => t.CompareTag("Player"));
        if (player != null)
        {
            var pos = player.position; pos.x = layout.playerStartX; player.position = pos;
            var buffs = player.GetComponent<PlayerBuffs>();
            if (buffs == null) buffs = player.gameObject.AddComponent<PlayerBuffs>();
            var bso = new SerializedObject(buffs);
            bso.FindProperty("buffEndSound").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/buff_end.wav");
            bso.ApplyModifiedPropertiesWithoutUndo();
            var resp = player.GetComponent<respawn>();
            if (resp != null)
            {
                var so = new SerializedObject(resp);
                so.FindProperty("fallDeathY").floatValue = layout.fallDeathY;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        if (layout.exitX.HasValue)
        {
            var exit = roots.SelectMany(r => r.GetComponentsInChildren<LevelExit>(true)).FirstOrDefault();
            if (exit != null) { var p = exit.transform.position; p.x = layout.exitX.Value; exit.transform.position = p; }
        }

        var cam = roots.SelectMany(r => r.GetComponentsInChildren<CameraFollow>(true)).FirstOrDefault();
        if (cam != null)
        {
            var c = cam.GetComponent<Camera>();
            float halfW = c != null ? c.orthographicSize * 16f / 9f : 8.9f;
            var so = new SerializedObject(cam);
            so.FindProperty("clampX").boolValue = true;
            so.FindProperty("minX").floatValue = layout.leftWall + halfW;
            so.FindProperty("maxX").floatValue = layout.rightWall - halfW;
            so.ApplyModifiedPropertiesWithoutUndo();
            var cp = cam.transform.position; cp.x = Mathf.Clamp(layout.playerStartX, layout.leftWall + halfW, layout.rightWall - halfW); cam.transform.position = cp;
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        if (!wasOpen) EditorSceneManager.CloseScene(scene, true);
        Debug.Log($"[Layout] {scene.name}: {layout.ground.Count} ground runs, {layout.platforms.Count} platforms, {layout.checkpoints.Count} checkpoints, {layout.enemies.Count} enemies placed.");
    }

    private static void CollectTemplates(GameObject[] roots, Dictionary<string, Object> templates)
    {
        foreach (var sr in roots.SelectMany(r => r.GetComponentsInChildren<SpriteRenderer>(true)))
        {
            if (sr.sprite == null) continue;
            if (!templates.ContainsKey(sr.sprite.name)) templates[sr.sprite.name] = sr;
        }
    }

    private static Transform Child(Transform parent, string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go.transform;
    }

    private static void SetGround(GameObject go)
    {
        int layer = LayerMask.NameToLayer("Ground");
        if (layer >= 0) go.layer = layer;
    }

    private static GameObject Level1Terrain(Scene scene, Transform parent, Seg seg, bool isPlatform, Dictionary<string, Object> templates)
    {
        var dirt = templates.TryGetValue("Tilesheet - WOODS_37", out var t) ? (SpriteRenderer)t : null;
        var grass = templates.TryGetValue("Tilesheet - WOODS_25", out var g) ? (SpriteRenderer)g : null;
        if (dirt == null) { Debug.LogError("[Layout] Level1 strip sprite 'Tilesheet - WOODS_37' not found."); return null; }

        var go = new GameObject(isPlatform ? "Platform" : "Ground");
        go.transform.SetParent(parent, false);
        SetGround(go);

        float width = seg.x1 - seg.x0;
        float cx = (seg.x0 + seg.x1) * 0.5f;
        float height = isPlatform ? 0.6f : 0.7f;

        if (grass != null) Strip(go.transform, "Grass", grass, cx, seg.top + 0.13f, width, 0.8f);
        Strip(go.transform, "Dirt", dirt, cx, seg.top, width, height);

        var box = go.AddComponent<BoxCollider2D>();
        box.offset = new Vector2(cx, seg.top - height * 0.5f);
        box.size = new Vector2(width, height);
        return go;
    }

    private static void Strip(Transform parent, string name, SpriteRenderer template, float centerX, float top, float width, float height)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        SetGround(go);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = template.sprite;
        sr.sortingLayerID = template.sortingLayerID;
        sr.sortingOrder = template.sortingOrder;
        sr.color = template.color;
        Vector2 size = template.sprite.bounds.size;
        Vector2 center = template.sprite.bounds.center;
        go.transform.localScale = new Vector3(width / size.x, height / size.y, 1f);
        go.transform.position = new Vector3(centerX - center.x * go.transform.localScale.x,
                                            top - height * 0.5f - center.y * go.transform.localScale.y, 0f);
    }

    private const float TilePitch = 0.89f;

    private static GameObject Level2Terrain(Scene scene, Transform parent, Seg seg, bool isPlatform, Dictionary<string, Object> templates)
    {
        SpriteRenderer Tpl(string n) => templates.TryGetValue(n, out var o) ? (SpriteRenderer)o : null;
        var left = Tpl("spritesheet_1"); var mid = Tpl("spritesheet_2"); var right = Tpl("spritesheet_3");
        if (left == null || mid == null || right == null) { Debug.LogError("[Layout] Level2 tile sprites spritesheet_1/2/3 not found."); return null; }

        var go = new GameObject(isPlatform ? "Platform" : "Ground");
        go.transform.SetParent(parent, false);
        SetGround(go);

        int count = Mathf.Max(2, Mathf.RoundToInt((seg.x1 - seg.x0) / TilePitch));
        float tileH = mid.sprite.bounds.size.y;
        float centerY = seg.top - tileH * 0.5f;
        float firstX = seg.x0 + TilePitch * 0.5f;

        for (int i = 0; i < count; i++)
        {
            var src = i == 0 ? left : i == count - 1 ? right : mid;
            var tile = new GameObject("Tile");
            tile.transform.SetParent(go.transform, false);
            tile.transform.position = new Vector3(firstX + i * TilePitch - src.sprite.bounds.center.x, centerY - src.sprite.bounds.center.y, 0f);
            var sr = tile.AddComponent<SpriteRenderer>();
            sr.sprite = src.sprite;
            sr.sortingLayerID = src.sortingLayerID;
            sr.sortingOrder = src.sortingOrder;
            sr.color = src.color;
            SetGround(tile);
        }

        float realX1 = seg.x0 + count * TilePitch;
        var box = go.AddComponent<BoxCollider2D>();
        box.offset = new Vector2((seg.x0 + realX1) * 0.5f, centerY);
        box.size = new Vector2(realX1 - seg.x0, tileH);
        return go;
    }

    private static float? VisibleFeetY(SpriteRenderer sr)
    {
        var sp = sr.sprite;
        var tex = sp.texture;
        string path = AssetDatabase.GetAssetPath(tex);
        if (string.IsNullOrEmpty(path) || !System.IO.File.Exists(path)) return null;

        var src = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        try
        {
            if (!ImageConversion.LoadImage(src, System.IO.File.ReadAllBytes(path))) return null;
            float k = src.width / (float)tex.width;
            Rect r = sp.textureRect;
            int x0 = Mathf.FloorToInt(r.x * k), y0 = Mathf.FloorToInt(r.y * k);
            int w = Mathf.FloorToInt(r.width * k), h = Mathf.FloorToInt(r.height * k);

            int lowest = -1;
            for (int y = 0; y < h && lowest < 0; y++)
                for (int x = 0; x < w; x++)
                    if (src.GetPixel(x0 + x, y0 + y).a > 0.5f) { lowest = y; break; }
            if (lowest < 0) return null;

            float localY = (lowest / k + sp.textureRectOffset.y - sp.pivot.y) / sp.pixelsPerUnit;
            return sr.transform.TransformPoint(new Vector3(0f, localY, 0f)).y;
        }
        finally
        {
            Object.DestroyImmediate(src);
        }
    }

    private static void PlaceEnemy(GameObject[] roots, EnemyPlacement e, Layout layout)
    {
        var patrol = roots.SelectMany(r => r.GetComponentsInChildren<MonoBehaviour>(true))
            .FirstOrDefault(m => m != null && m.GetType().Name.Equals(e.patrolType, System.StringComparison.OrdinalIgnoreCase));
        if (patrol == null) { Debug.LogWarning("[Layout] No " + e.patrolType + " in scene."); return; }

        var so = new SerializedObject(patrol);
        var a = so.FindProperty("leftEdge").objectReferenceValue as Transform;
        var b = so.FindProperty("rightEdge").objectReferenceValue as Transform;

        var body = patrol.transform;
        var group = body.parent != null ? body.parent : body;
        var gp = group.position; gp.x += e.startX - body.position.x; group.position = gp;

        var bp = body.position; bp.x = e.startX; body.position = bp;

        var eh = body.GetComponent<EnemyHealth>();
        if (eh != null)
        {
            bool flyer = e.patrolType.ToLowerInvariant() == "batpatrol" || e.patrolType.ToLowerInvariant() == "eyepatrol";
            var eso = new SerializedObject(eh);
            eso.FindProperty("keepOnGround").boolValue = !flyer;
            eso.ApplyModifiedPropertiesWithoutUndo();
        }
        if (a != null) { var p = a.position; p.x = e.minX; a.position = p; }
        if (b != null) { var p = b.position; p.x = e.maxX; b.position = p; }

        bool isFlyer = e.patrolType.ToLowerInvariant() == "batpatrol" || e.patrolType.ToLowerInvariant() == "eyepatrol";
        var sr = body.GetComponent<SpriteRenderer>();
        if (!isFlyer && sr != null && sr.sprite != null)
        {
            var seg = layout.ground.Where(g => e.startX >= g.x0 && e.startX <= g.x1).Select(g => (Seg?)g).FirstOrDefault();
            float? feet = VisibleFeetY(sr);
            if (seg.HasValue && feet.HasValue)
            {
                float target = seg.Value.top + layout.visualGroundOffset;
                var p = body.position; p.y += target - feet.Value; body.position = p;
            }
        }
    }
}
#endif
