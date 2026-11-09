#if UNITY_EDITOR
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class Level3Builder
{
    private const string Level3Path = "Assets/Scenes/Levels/Level3.unity";
    private const string Level1Path = "Assets/Scenes/Levels/Level1.unity";
    private const string Level2Path = "Assets/Scenes/Levels/Level2.unity";
    private const string LavaFolder = "Assets/Art/Hazards";
    private const string GameUIPrefabPath = "Assets/Prefabs/UI/GameUI.prefab";

    private static readonly Color RockTint = new Color(1f, 0.62f, 0.52f, 1f);
    private static readonly Color BackdropTint = Color.white;

    [MenuItem("Tools/Game Setup/Level3/Build Level3 (The Spire)")]
    public static void BuildAll()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        GenerateLavaSprites();
        AddBossBarAndVictoryToGameUI();
        BuildScene();
        LevelLayoutBuilder.BuildLevel3();
        PostLayout();
        LinkLevel2ToLevel3();
        GameSetupBuilder.SetupBuildSettings();
        Debug.Log("[Level3] The Spire is built.");
    }

    [MenuItem("Tools/Game Setup/Level3/Generate Lava Sprites")]
    public static void GenerateLavaSprites()
    {
        Directory.CreateDirectory(LavaFolder);
        var rng = new System.Random(7);

        var surf = new Texture2D(32, 16, TextureFormat.RGBA32, false);
        for (int x = 0; x < 32; x++)
        {
            int crest = 12 + Mathf.RoundToInt(Mathf.Sin(x / 32f * Mathf.PI * 4f) * 1.5f);
            for (int y = 0; y < 16; y++)
            {
                Color c;
                if (y > crest) c = new Color(0, 0, 0, 0);
                else if (y >= crest - 1) c = new Color(1f, 0.93f, 0.55f, 1f);
                else if (y >= crest - 3) c = new Color(1f, 0.68f, 0.18f, 1f);
                else
                {
                    float t = y / (float)crest;
                    c = Color.Lerp(new Color(0.72f, 0.12f, 0.05f, 1f), new Color(0.98f, 0.38f, 0.08f, 1f), t);
                    if (rng.NextDouble() < 0.06) c = new Color(1f, 0.75f, 0.25f, 1f);
                }
                surf.SetPixel(x, y, c);
            }
        }
        Save(surf, "LavaSurface");

        var deep = new Texture2D(8, 8, TextureFormat.RGBA32, false);
        for (int x = 0; x < 8; x++) for (int y = 0; y < 8; y++)
            deep.SetPixel(x, y, new Color(0.62f, 0.09f, 0.04f, 1f));
        Save(deep, "LavaDeep");
        AssetDatabase.Refresh();

        foreach (var n in new[] { "LavaSurface", "LavaDeep" })
        {
            var imp = (TextureImporter)AssetImporter.GetAtPath($"{LavaFolder}/{n}.png");
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.spritePixelsPerUnit = 16;
            imp.filterMode = FilterMode.Point;
            imp.wrapMode = TextureWrapMode.Repeat;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            var settings = new TextureImporterSettings();
            imp.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            imp.SetTextureSettings(settings);
            imp.SaveAndReimport();
        }
    }

    private static void Save(Texture2D tex, string name)
    {
        File.WriteAllBytes($"{LavaFolder}/{name}.png", tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
    }

    [MenuItem("Tools/Game Setup/Level3/Add Boss Bar + Victory Screen To GameUI")]
    public static void AddBossBarAndVictoryToGameUI()
    {
        var root = PrefabUtility.LoadPrefabContents(GameUIPrefabPath);
        var controller = root.GetComponent<GameUIController>();
        var canvas = root.transform.Find("Canvas");
        var so = new SerializedObject(controller);

        var oldBar = canvas.Find("BossBar");
        if (oldBar != null) Object.DestroyImmediate(oldBar.gameObject);
        var bar = GameSetupBuilder.UIObject("BossBar", canvas);
        GameSetupBuilder.Anchor(bar, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 50), new Vector2(900, 80));

        var name = GameSetupBuilder.Text("BossName", bar.transform, "CALDERUS", 34, new Color(1f, 0.827451f, 0.41568628f, 1f), TextAlignmentOptions.Center);
        GameSetupBuilder.Anchor(name.gameObject, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), Vector2.zero, new Vector2(0, 40));
        name.gameObject.AddComponent<Shadow>().effectColor = new Color(0, 0, 0, 0.85f);

        var bg = GameSetupBuilder.UIObject("Background", bar.transform);
        var bgImg = bg.AddComponent<Image>();
        bgImg.sprite = GameSetupBuilder.UISprite(); bgImg.type = Image.Type.Sliced;
        bgImg.color = new Color(0.16862746f, 0.101960786f, 0.101960786f, 1f);
        GameSetupBuilder.Anchor(bg, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), Vector2.zero, new Vector2(0, 30));
        var outline = bg.AddComponent<Outline>(); outline.effectColor = new Color(0, 0, 0, 0.85f); outline.effectDistance = new Vector2(3, -3);

        var fill = GameSetupBuilder.UIObject("Fill", bg.transform);
        var fillImg = fill.AddComponent<Image>();
        fillImg.sprite = GameSetupBuilder.UISprite();
        fillImg.color = new Color(0.95f, 0.36f, 0.1f, 1f);
        fillImg.type = Image.Type.Filled; fillImg.fillMethod = Image.FillMethod.Horizontal; fillImg.fillAmount = 1f;
        GameSetupBuilder.Stretch(fill, 4);
        bar.SetActive(false);

        so.FindProperty("bossBarRoot").objectReferenceValue = bar;
        so.FindProperty("bossBarFill").objectReferenceValue = fillImg;
        so.FindProperty("bossNameText").objectReferenceValue = name;

        var oldVictory = canvas.Find("VictoryPanel");
        if (oldVictory != null) Object.DestroyImmediate(oldVictory.gameObject);
        var victory = GameSetupBuilder.BuildMenuPanel("VictoryPanel", canvas, "VICTORY", "Calderus has fallen. The kingdom is free.", out var buttons);
        var playAgain = GameSetupBuilder.MenuButton("PlayAgainButton", buttons, "PLAY AGAIN", controller.PlayFromStart);
        GameSetupBuilder.MenuButton("MainMenuButton", buttons, "MAIN MENU", controller.GoToMainMenu);
        GameSetupBuilder.MenuButton("ExitButton", buttons, "EXIT GAME", controller.ExitGame);
        victory.SetActive(false);

        so.FindProperty("victoryPanel").objectReferenceValue = victory;
        so.FindProperty("victoryFirstSelected").objectReferenceValue = playAgain.gameObject;
        so.FindProperty("victorySound").objectReferenceValue = GameSetupBuilder.Clip("victory");
        so.ApplyModifiedPropertiesWithoutUndo();

        PrefabUtility.SaveAsPrefabAsset(root, GameUIPrefabPath);
        PrefabUtility.UnloadPrefabContents(root);
        Debug.Log("[Level3] GameUI: boss bar + victory screen added.");
    }

    public static void BuildScene()
    {
        if (File.Exists(Level3Path))
        {
            Directory.CreateDirectory("Assets/Backups");
            const string backup = "Assets/Backups/Level3_before_spire.unity";
            if (!File.Exists(backup)) AssetDatabase.CopyAsset(Level3Path, backup);
        }

        var l1 = EditorSceneManager.OpenScene(Level1Path, OpenSceneMode.Additive);
        var l2 = EditorSceneManager.OpenScene(Level2Path, OpenSceneMode.Additive);
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);

        GameObject CopyFrom(Scene src, System.Func<GameObject, bool> match, string newName = null)
        {
            var original = src.GetRootGameObjects().FirstOrDefault(match);
            if (original == null) { Debug.LogWarning("[Level3] Couldn't find object to copy in " + src.name); return null; }
            var copy = Object.Instantiate(original);
            copy.name = newName ?? original.name;
            SceneManager.MoveGameObjectToScene(copy, scene);
            return copy;
        }

        var cam = CopyFrom(l2, g => g.GetComponent<Camera>() != null, "Main Camera");
        var player = CopyFrom(l2, g => g.CompareTag("Player"), "Vaelor");
        var c = cam.GetComponent<Camera>();
        c.backgroundColor = new Color(0.12f, 0.04f, 0.03f, 1f);
        c.clearFlags = CameraClearFlags.SolidColor;
        var follow = new SerializedObject(cam.GetComponent<CameraFollow>());
        follow.FindProperty("testPlayer").objectReferenceValue = player.transform;
        follow.ApplyModifiedPropertiesWithoutUndo();
        player.transform.position = new Vector3(3f, -1.45f, 0f);

        CopyFrom(l1, g => g.GetComponentInChildren<MushroomPatrol>(true) != null, "MushroomPatrol");
        CopyFrom(l2, g => g.GetComponentInChildren<GolemPatrol>(true) != null, "GolemPatrol");
        CopyFrom(l2, g => g.GetComponentInChildren<EyePatrol>(true) != null, "EyePatrol");

        var globalLight = new GameObject("Global Light 2D");
        SceneManager.MoveGameObjectToScene(globalLight, scene);
        var gl = globalLight.AddComponent<Light2D>();
        gl.lightType = Light2D.LightType.Global;
        gl.color = new Color(1f, 0.82f, 0.72f, 1f);
        gl.intensity = 0.8f;

        var backdrop = new GameObject("Backdrop - Volcanic Spire");
        backdrop.transform.SetParent(cam.transform, false);
        var bsr = backdrop.AddComponent<SpriteRenderer>();
        bsr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/Ember Kingdom Beneath the Volcanic Spire.png");
        bsr.color = BackdropTint;
        bsr.sortingOrder = -60;
        backdrop.AddComponent<FitBackgroundToCamera>();

        var templates = new GameObject("_Templates");
        SceneManager.MoveGameObjectToScene(templates, scene);
        var tileSprites = AssetDatabase.LoadAllAssetsAtPath("Assets/Art/Terrain/CAVETERRAINS.png").OfType<Sprite>().ToList();
        foreach (var n in new[] { "spritesheet_1", "spritesheet_2", "spritesheet_3" })
        {
            var t = new GameObject(n);
            t.transform.SetParent(templates.transform, false);
            var tsr = t.AddComponent<SpriteRenderer>();
            tsr.sprite = tileSprites.FirstOrDefault(sp => sp.name == n);
            tsr.color = RockTint;
        }
        templates.SetActive(false);

        var calderusPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Boss/CalderusBoss.prefab");
        var calderus = (GameObject)PrefabUtility.InstantiatePrefab(calderusPrefab, scene);
        calderus.name = "Calderus";
        calderus.transform.position = new Vector3(91f, LevelLayoutBuilder.Level3GroundTop + 2.25f, 0f);

        EditorSceneManager.SaveScene(scene, Level3Path);
        EditorSceneManager.CloseScene(l1, true);
        EditorSceneManager.CloseScene(l2, true);
        EditorSceneManager.CloseScene(scene, true);

        GameSetupBuilder.SetupLevel(Level3Path, "MainMenu");
    }

    public static void PostLayout()
    {
        var scene = SceneManager.GetSceneByPath(Level3Path);
        bool open = scene.IsValid() && scene.isLoaded;
        if (!open) scene = EditorSceneManager.OpenScene(Level3Path, OpenSceneMode.Additive);
        var roots = scene.GetRootGameObjects();

        var old = roots.FirstOrDefault(r => r.name == "Hazards");
        if (old != null) Object.DestroyImmediate(old);
        var hazards = new GameObject("Hazards");
        SceneManager.MoveGameObjectToScene(hazards, scene);
        var surface = AssetDatabase.LoadAssetAtPath<Sprite>($"{LavaFolder}/LavaSurface.png");
        var deep = AssetDatabase.LoadAssetAtPath<Sprite>($"{LavaFolder}/LavaDeep.png");
        var burn = GameSetupBuilder.Clip("player_death");
        const float LakeLeft = -15f, LakeRight = 115f;
        float lavaSurfaceTop = LevelLayoutBuilder.Level3GroundTop - 0.5f;
        float killTop = LevelLayoutBuilder.Level3GroundTop - 1.25f;
        float w = LakeRight - LakeLeft, cx = (LakeLeft + LakeRight) * 0.5f;

        var lake = new GameObject("Lava Lake");
        lake.transform.SetParent(hazards.transform, false);
        lake.transform.position = new Vector3(cx, lavaSurfaceTop - 0.5f, 0f);

        var sr = lake.AddComponent<SpriteRenderer>();
        sr.sprite = surface;
        sr.drawMode = SpriteDrawMode.Tiled;
        sr.size = new Vector2(w, 1f);
        sr.sortingOrder = -5;

        var depth = new GameObject("Depth");
        depth.transform.SetParent(lake.transform, false);
        depth.transform.localPosition = new Vector3(0f, -2.5f, 0f);
        var dsr = depth.AddComponent<SpriteRenderer>();
        dsr.sprite = deep;
        dsr.drawMode = SpriteDrawMode.Tiled;
        dsr.size = new Vector2(w, 4f);
        dsr.sortingOrder = -5;

        var col = lake.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(w, 2f);
        col.offset = new Vector2(0f, killTop - lake.transform.position.y - 1f);

        var hz = lake.AddComponent<Hazard>();
        var hso = new SerializedObject(hz);
        hso.FindProperty("burnSound").objectReferenceValue = burn;
        hso.ApplyModifiedPropertiesWithoutUndo();

        void Glow(float x, float intensity, float radius)
        {
            var g = new GameObject("Glow");
            g.transform.SetParent(lake.transform, false);
            g.transform.position = new Vector3(x, lavaSurfaceTop + 0.6f, 0f);
            var light = g.AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Point;
            light.color = new Color(1f, 0.45f, 0.12f, 1f);
            light.intensity = intensity;
            light.pointLightOuterRadius = radius;
            light.pointLightInnerRadius = 0.5f;
        }
        for (float x = 0f; x <= 100f; x += 12f) Glow(x, 0.7f, 7f);
        foreach (var (x0, x1) in LevelLayoutBuilder.Level3LavaPits)
            Glow((x0 + x1) * 0.5f, 1.4f, Mathf.Max(4f, (x1 - x0) * 0.75f));

        foreach (var ea in roots.SelectMany(r => r.GetComponentsInChildren<EnemyAttack>(true)))
        {
            var so = new SerializedObject(ea);
            so.FindProperty("damage").floatValue *= 2f;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
        foreach (var eh in roots.SelectMany(r => r.GetComponentsInChildren<EnemyHealth>(true)).Where(e => e.GetComponent<CalderusBoss>() == null))
        {
            var so = new SerializedObject(eh);
            so.FindProperty("maxHealth").floatValue *= 1.5f;
            so.FindProperty("displayName").stringValue = "the " + so.FindProperty("displayName").stringValue;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        var calderus = roots.SelectMany(r => r.GetComponentsInChildren<CalderusBoss>(true)).First();
        var ceh = calderus.GetComponent<EnemyHealth>();
        if (ceh == null) ceh = calderus.gameObject.AddComponent<EnemyHealth>();
        var cso = new SerializedObject(ceh);
        cso.FindProperty("displayName").stringValue = "Calderus";
        cso.FindProperty("maxHealth").floatValue = 400f;
        cso.FindProperty("invulnerabilityAfterHit").floatValue = 0.25f;
        cso.FindProperty("knockbackDistance").floatValue = 0f;
        cso.FindProperty("keepOnGround").boolValue = false;
        cso.FindProperty("immuneToHazards").boolValue = true;
        cso.FindProperty("hideBodyDelay").floatValue = -1f;
        cso.FindProperty("hurtTrigger").stringValue = "";
        cso.FindProperty("deadBool").stringValue = "";
        cso.FindProperty("speedFloat").stringValue = "";
        cso.FindProperty("hurtSound").objectReferenceValue = GameSetupBuilder.Clip("enemy_hurt");
        cso.FindProperty("deathSound").objectReferenceValue = GameSetupBuilder.Clip("enemy_death");
        cso.ApplyModifiedPropertiesWithoutUndo();

        var bso = new SerializedObject(calderus);
        bso.FindProperty("roarSound").objectReferenceValue = GameSetupBuilder.Clip("enemy_appear");
        bso.FindProperty("meleeSound").objectReferenceValue = GameSetupBuilder.Clip("enemy_attack");
        bso.FindProperty("fireballSound").objectReferenceValue = GameSetupBuilder.Clip("sword_swing");
        bso.ApplyModifiedPropertiesWithoutUndo();

        FixCalderusHurtEvent();

        var goals = roots.SelectMany(r => r.GetComponentsInChildren<LevelGoalManager>(true)).First();
        var gso = new SerializedObject(goals);
        var minions = roots.SelectMany(r => r.GetComponentsInChildren<EnemyHealth>(true))
            .Where(e => e != ceh)
            .OrderBy(e => e.transform.position.x)
            .ToList();
        var list = gso.FindProperty("enemiesInOrder");
        list.arraySize = minions.Count + 1;
        for (int i = 0; i < minions.Count; i++)
        {
            var el = list.GetArrayElementAtIndex(i);
            el.FindPropertyRelative("enemy").objectReferenceValue = minions[i];
            el.FindPropertyRelative("revealRoot").objectReferenceValue =
                minions[i].transform.parent != null ? minions[i].transform.parent.gameObject : minions[i].gameObject;
        }
        var bossEl = list.GetArrayElementAtIndex(minions.Count);
        bossEl.FindPropertyRelative("enemy").objectReferenceValue = ceh;
        bossEl.FindPropertyRelative("revealRoot").objectReferenceValue = calderus.gameObject;
        gso.FindProperty("objectiveFormat").stringValue = "Defeat {0}  ({1}/{2})";
        gso.FindProperty("levelCompleteText").stringValue = "Calderus is defeated!";
        gso.FindProperty("showVictoryScreen").boolValue = true;
        gso.FindProperty("delayBeforeLoading").floatValue = 3f;
        gso.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        if (!open) EditorSceneManager.CloseScene(scene, true);
        Debug.Log("[Level3] Lava, Calderus and goals wired.");
    }

    private static void FixCalderusHurtEvent()
    {
        var ac = AssetDatabase.LoadAssetAtPath<UnityEditor.Animations.AnimatorController>("Assets/Animations/Boss/CalderusController.controller");
        if (ac == null) return;
        foreach (var clip in ac.animationClips.Where(c => c != null && c.name.Contains("TakeHit")).Distinct())
        {
            var events = AnimationUtility.GetAnimationEvents(clip);
            bool changed = false;
            foreach (var e in events)
                if (e.functionName == "TakeDamage") { e.functionName = "FinishHurt"; changed = true; }
            if (changed)
            {
                AnimationUtility.SetAnimationEvents(clip, events);
                EditorUtility.SetDirty(clip);
                Debug.Log("[Level3] Fixed Calderus TakeHit event -> FinishHurt");
            }
        }
        AssetDatabase.SaveAssets();
    }

    private static void LinkLevel2ToLevel3()
    {
        var s = SceneManager.GetSceneByPath(Level2Path);
        bool open = s.IsValid() && s.isLoaded;
        if (!open) s = EditorSceneManager.OpenScene(Level2Path, OpenSceneMode.Additive);
        var goals = s.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<LevelGoalManager>(true)).FirstOrDefault();
        if (goals != null)
        {
            var so = new SerializedObject(goals);
            so.FindProperty("nextSceneName").stringValue = "Level3";
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(s);
            EditorSceneManager.SaveScene(s);
        }
        if (!open) EditorSceneManager.CloseScene(s, true);
    }
}
#endif
