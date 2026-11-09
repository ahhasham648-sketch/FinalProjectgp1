#if UNITY_EDITOR
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class ContentBuilder
{
    public const string PotionFolder = "Assets/Art/Items";
    public const string MusicFolder = "Assets/Audio/Music";

    private static readonly string[] Bottle =
    {
        "................",
        "......oooo......",
        ".....okkkko.....",
        ".....okkkko.....",
        "......oggo......",
        "......oggo......",
        "....oggggggo....",
        "...oggggggggo...",
        "..owlllllllllo..",
        "..owlllllllllo..",
        "..olllllllllLo..",
        "..olllllllllLo..",
        "..olllllllllLo..",
        "...oLLLLLLLLo...",
        "....oooooooo....",
        "................",
    };

    [MenuItem("Tools/Game Setup/Content/Generate Potion Sprites")]
    public static void GeneratePotionSprites()
    {
        Directory.CreateDirectory(PotionFolder);
        MakePotion("HealthPotion", new Color(0.92f, 0.16f, 0.2f));
        MakePotion("AttackPotion", new Color(1f, 0.56f, 0.1f));
        MakePotion("SpeedPotion", new Color(0.82f, 0.3f, 0.95f));
        AssetDatabase.Refresh();

        foreach (var n in new[] { "HealthPotion", "AttackPotion", "SpeedPotion" })
        {
            var imp = (TextureImporter)AssetImporter.GetAtPath($"{PotionFolder}/{n}.png");
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.spritePixelsPerUnit = 16;
            imp.filterMode = FilterMode.Point;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.alphaIsTransparency = true;
            imp.SaveAndReimport();
        }
        Debug.Log("[Content] Potion sprites written to " + PotionFolder);
    }

    private static void MakePotion(string name, Color liquid)
    {
        var tex = new Texture2D(16, 16, TextureFormat.RGBA32, false);
        Color dark = liquid * 0.65f; dark.a = 1f;
        for (int row = 0; row < 16; row++)
        {
            for (int x = 0; x < 16; x++)
            {
                char c = Bottle[row][x];
                Color col;
                switch (c)
                {
                    case 'o': col = new Color(0.1f, 0.07f, 0.09f, 1f); break;
                    case 'k': col = new Color(0.55f, 0.35f, 0.2f, 1f); break;
                    case 'g': col = new Color(0.78f, 0.88f, 0.95f, 0.55f); break;
                    case 'w': col = new Color(1f, 1f, 1f, 0.9f); break;
                    case 'l': col = liquid; break;
                    case 'L': col = dark; break;
                    default: col = new Color(0, 0, 0, 0); break;
                }
                tex.SetPixel(x, 15 - row, col);
            }
        }
        File.WriteAllBytes($"{PotionFolder}/{name}.png", tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
    }

    public static Sprite PotionSprite(PotionType type) =>
        AssetDatabase.LoadAssetAtPath<Sprite>($"{PotionFolder}/{type}Potion.png");

    public static GameObject CreatePotion(Transform parent, PotionType type, Vector3 position)
    {
        var go = new GameObject(type + " Potion");
        go.transform.SetParent(parent, false);
        go.transform.position = position;
        go.transform.localScale = Vector3.one * 0.8f;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = PotionSprite(type);
        sr.sortingOrder = 3;

        var col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.5f;

        var p = go.AddComponent<PotionPickup>();
        var so = new SerializedObject(p);
        so.FindProperty("type").enumValueIndex = (int)type;
        so.FindProperty("pickupSound").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/potion_pickup.wav");
        so.ApplyModifiedPropertiesWithoutUndo();
        return go;
    }

    public static AudioClip FindMusic(string keyword)
    {
        Directory.CreateDirectory(MusicFolder);
        return AssetDatabase.FindAssets("t:AudioClip", new[] { MusicFolder })
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(p => Path.GetFileNameWithoutExtension(p).ToLowerInvariant().Replace("_", " ").Replace("-", " ").Contains(keyword))
            .Select(AssetDatabase.LoadAssetAtPath<AudioClip>)
            .FirstOrDefault();
    }

    public const string PlaceholderFolder = MusicFolder + "/Placeholder";

    [MenuItem("Tools/Game Setup/Content/Generate Placeholder Music")]
    public static void GeneratePlaceholderMusic()
    {
        Directory.CreateDirectory(PlaceholderFolder);
        const int sr = 44100;

        float[][] forest =
        {
            new[] { 146.83f, 174.61f, 220.00f },
            new[] { 130.81f, 164.81f, 196.00f },
            new[] { 116.54f, 146.83f, 174.61f },
            new[] { 130.81f, 164.81f, 196.00f },
        };
        GameSetupBuilder.Write("placeholder_forest_music", 48f, sr,
            (t, d) => Song(t, forest, chordLen: 3f, stepLen: 0.375f, pad: 0.12f, pluck: 0.22f, bass: 0.18f, pattern: new[] { 0, 1, 2, 1, 2, 1, 0, 2 }),
            0.35f, 0.35f, PlaceholderFolder);

        float[][] caves =
        {
            new[] { 110.00f, 130.81f, 164.81f },
            new[] { 87.31f, 110.00f, 130.81f },
            new[] { 82.41f, 98.00f, 123.47f },
            new[] { 110.00f, 130.81f, 164.81f },
        };
        GameSetupBuilder.Write("placeholder_caves_music", 48f, sr,
            (t, d) => Song(t, caves, chordLen: 4f, stepLen: 1f, pad: 0.16f, pluck: 0.16f, bass: 0.22f, pattern: new[] { 2, 0, 1, 0 }) + 0.05f * Mathf.Sin(2f * Mathf.PI * 55f * t),
            0.25f, 0.25f, PlaceholderFolder);

        AssetDatabase.Refresh();
        foreach (var n in new[] { "placeholder_forest_music", "placeholder_caves_music" })
        {
            var imp = (AudioImporter)AssetImporter.GetAtPath($"{PlaceholderFolder}/{n}.wav");
            var s = imp.defaultSampleSettings;
            s.loadType = AudioClipLoadType.CompressedInMemory;
            s.compressionFormat = AudioCompressionFormat.Vorbis;
            s.quality = 0.6f;
            imp.defaultSampleSettings = s;
            imp.SaveAndReimport();
        }
        Debug.Log("[Content] Placeholder music written to " + PlaceholderFolder);
    }

    private static float Song(float t, float[][] chords, float chordLen, float stepLen, float pad, float pluck, float bass, int[] pattern)
    {
        float loop = chords.Length * chordLen;
        t %= loop;
        int ci = Mathf.FloorToInt(t / chordLen) % chords.Length;
        float ct = t - ci * chordLen;
        var chord = chords[ci];

        float padEnv = Mathf.Clamp01(ct / 0.6f) * Mathf.Clamp01((chordLen - ct) / 0.4f + 0.35f);
        float p = 0f;
        foreach (var f in chord) p += Mathf.Sin(2f * Mathf.PI * f * t) + 0.25f * Mathf.Sin(2f * Mathf.PI * f * 2.003f * t);
        p *= pad * padEnv / chord.Length;

        int step = Mathf.FloorToInt(t / stepLen);
        float lt = t - step * stepLen;
        float nf = chord[pattern[step % pattern.Length]] * 2f;
        float ph = 2f * Mathf.PI * nf * lt;
        float pl = (Mathf.Sin(ph) + 0.35f * Mathf.Sin(2f * ph) + 0.15f * Mathf.Sin(3f * ph)) * Mathf.Exp(-lt * 5f) * Mathf.Min(1f, lt * 300f) * pluck;

        float b = Mathf.Sin(2f * Mathf.PI * chord[0] * 0.5f * t) * Mathf.Exp(-ct * 0.6f) * Mathf.Min(1f, ct * 50f) * bass;

        return p + pl + b;
    }

    private static AudioClip Placeholder(string keyword)
    {
        string name = keyword.Contains("valley") ? "placeholder_forest_music" : keyword.Contains("tomorrow") ? "placeholder_caves_music" : null;
        return name == null ? null : AssetDatabase.LoadAssetAtPath<AudioClip>($"{PlaceholderFolder}/{name}.wav");
    }

    public static void EnsureLevelMusic(Scene scene, string keyword)
    {
        var existing = scene.GetRootGameObjects().Select(r => r.GetComponentInChildren<LevelMusic>(true)).FirstOrDefault(m => m != null);
        GameObject go;
        if (existing != null) go = existing.gameObject;
        else
        {
            go = new GameObject("LevelMusic", typeof(AudioSource), typeof(LevelMusic));
            SceneManager.MoveGameObjectToScene(go, scene);
        }
        var clip = FindMusic(keyword);
        bool placeholder = false;
        if (clip == null) { clip = Placeholder(keyword); placeholder = clip != null; }
        var so = new SerializedObject(go.GetComponent<LevelMusic>());
        if (clip != null) so.FindProperty("music").objectReferenceValue = clip;
        so.ApplyModifiedPropertiesWithoutUndo();
        Debug.Log($"[Content] {scene.name} music: " + (clip == null ? "none" : clip.name) +
                  (placeholder || clip == null ? $" (add a file with '{keyword}' in its name to {MusicFolder} and re-run Assign Level Music)" : ""));
    }

    [MenuItem("Tools/Game Setup/Content/Assign Level Music")]
    public static void AssignMusic()
    {
        foreach (var (path, key) in new[] { ("Assets/Scenes/Levels/Level1.unity", "valley of dreams"), ("Assets/Scenes/Levels/Level2.unity", "tomorrow the battle") })
        {
            var s = SceneManager.GetSceneByPath(path);
            bool open = s.IsValid() && s.isLoaded;
            if (!open) s = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            EnsureLevelMusic(s, key);
            EditorSceneManager.MarkSceneDirty(s);
            EditorSceneManager.SaveScene(s);
            if (!open) EditorSceneManager.CloseScene(s, true);
        }
    }

    [MenuItem("Tools/Game Setup/Content/Add Boost Text To GameUI")]
    public static void AddBuffTextToGameUI()
    {
        const string path = "Assets/Prefabs/UI/GameUI.prefab";
        var root = PrefabUtility.LoadPrefabContents(path);
        var hud = root.transform.Find("Canvas/HUD");
        var lives = hud.Find("LivesText").GetComponent<TextMeshProUGUI>();

        var t = hud.Find("BuffText");
        TextMeshProUGUI text;
        if (t == null)
        {
            var go = Object.Instantiate(lives.gameObject, hud);
            go.name = "BuffText";
            text = go.GetComponent<TextMeshProUGUI>();
        }
        else text = t.GetComponent<TextMeshProUGUI>();

        var rt = (RectTransform)text.transform;
        var lrt = (RectTransform)lives.transform;
        rt.anchorMin = lrt.anchorMin; rt.anchorMax = lrt.anchorMax; rt.pivot = lrt.pivot;
        rt.sizeDelta = new Vector2(700, 44);
        rt.anchoredPosition = lrt.anchoredPosition + new Vector2(0, -46);
        text.fontSize = 26;
        text.richText = true;
        text.text = "";

        var so = new SerializedObject(root.GetComponent<GameUIController>());
        so.FindProperty("buffText").objectReferenceValue = text;
        so.ApplyModifiedPropertiesWithoutUndo();

        PrefabUtility.SaveAsPrefabAsset(root, path);
        PrefabUtility.UnloadPrefabContents(root);
        Debug.Log("[Content] BuffText added to GameUI HUD.");
    }

    public const string CreditsText =
@"IN THE SPIRE
Rise. Fight. Conquer the Spire.

DEVELOPED BY
Hallowed Anvil Labs

PROGRAMMING
Ahmed Al-Hasham

CONCEPT & STORY DESIGN
Alexis Gabriel Morales

TESTING
Adrian Hociota

CHARACTER ART & ANIMATION
Vaelor - Hero Knight pixel art pack
Calderus - Fantasy Creatures Pixel Art Pack
Enemies - Monsters Creatures Fantasy 1 & 2
Unity Asset Store

ENVIRONMENT ART
The Forest - Woods Tileset & Background by karsiori
The Caves - Pixel Fantasy Caves

MAIN MENU ART & LOGO
Volcanic Spire Background and In The Spire Logo
Created with OpenAI image-generation tools

MUSIC
Valley of Dreams - The Forest
Tomorrow the Battle Will Be Long - The Caves
Good Day To Die - The Spire
Non-copyrighted music, sourced from YouTube

SOUND EFFECTS
Created for In The Spire by Hallowed Anvil Labs

SPECIAL THANKS
Centennial College
Our instructors, classmates, friends and playtesters

All third-party assets remain the property
of their respective creators.

Copyright 2026 Hallowed Anvil Labs";

    public const string StoryText =
@"<size=40><color=#FFD36A>A FALLEN KINGDOM</color></size>

A fallen kingdom. A guilty knight.
One final climb to end Calderus's reign.

Vaelor must fight through the ruins, face his past,
and reach the top of the Spire.
The kingdom has fallen, but Vaelor's journey is only beginning.
To save what remains, Vaelor must defeat Calderus
and reclaim the Spire.

From guilt and destruction rises one final chance for redemption.

<size=40><color=#FFD36A>VAELOR</color></size>

Vaelor is a skilled warrior determined to reclaim his fallen kingdom.
He fights with his sword, his speed and his combat skill.
He carries the guilt of failing to protect his home.
His journey is one of revenge, redemption,
and the defeat of Calderus.
His final goal is to reach the top of the Spire
and save what remains of his kingdom.

<size=40><color=#FFD36A>CALDERUS</color></size>

Calderus is a powerful fire creature
who destroyed Vaelor's kingdom.
He now rules from the top of the Spire.
He commands devastating fire attacks
and crushing blows up close.
He is Vaelor's greatest challenge.
Defeating Calderus ends his reign
and completes Vaelor's journey.

<size=40><color=#FFD36A>THE JOURNEY</color></size>

The Forest  -  The Caves  -  The Spire

Rise. Fight. Conquer the Spire.";

    [MenuItem("Tools/Game Setup/Content/Add Story To Main Menu")]
    public static void AddStoryToMainMenu()
    {
        const string path = "Assets/Scenes/MainMenu.unity";
        var s = SceneManager.GetSceneByPath(path);
        bool open = s.IsValid() && s.isLoaded;
        if (!open) s = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);

        var controller = s.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<MainMenuController>(true)).First();
        var cso = new SerializedObject(controller);
        var credits = (GameObject)cso.FindProperty("creditsPanel").objectReferenceValue;

        var oldStory = (GameObject)cso.FindProperty("storyPanel").objectReferenceValue;
        if (oldStory != null) Object.DestroyImmediate(oldStory);
        var story = Object.Instantiate(credits, credits.transform.parent);
        story.name = "StoryPanel";
        story.transform.SetSiblingIndex(credits.transform.GetSiblingIndex() + 1);

        foreach (var t in story.GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            if (t.name == "CreditsTitle") { t.name = "StoryTitle"; t.text = "THE STORY"; }
            else if (t.name == "CreditsText")
            {
                t.name = "StoryText";
                t.richText = true;
                t.text = StoryText;
                t.ForceMeshUpdate();
                float h = t.preferredHeight + 60f;
                var rt = (RectTransform)t.transform;
                if (rt.rect.height < h) rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, h);
                var content = t.transform.parent as RectTransform;
                if (content != null && content.name == "Content") content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, h);
            }
        }
        foreach (var b in story.GetComponentsInChildren<Button>(true))
            if (b.name == "CreditsBackButton") b.name = "StoryBackButton";
        var scroll = story.GetComponentInChildren<ScrollRect>(true);
        if (scroll != null) scroll.verticalNormalizedPosition = 1f;
        story.SetActive(false);
        cso.FindProperty("storyPanel").objectReferenceValue = story;
        cso.ApplyModifiedPropertiesWithoutUndo();

        var creditsButton = s.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Button>(true)).First(b => b.name == "CreditsButton");
        var container = creditsButton.transform.parent;
        var oldBtn = container.Find("StoryButton");
        if (oldBtn != null) Object.DestroyImmediate(oldBtn.gameObject);
        var storyButton = Object.Instantiate(creditsButton.gameObject, container).GetComponent<Button>();
        storyButton.name = "StoryButton";
        storyButton.transform.SetSiblingIndex(creditsButton.transform.GetSiblingIndex());
        while (storyButton.onClick.GetPersistentEventCount() > 0)
            UnityEditor.Events.UnityEventTools.RemovePersistentListener(storyButton.onClick, 0);
        UnityEditor.Events.UnityEventTools.AddPersistentListener(storyButton.onClick, controller.OpenStory);
        storyButton.GetComponentInChildren<TextMeshProUGUI>(true).text = "STORY";

        var layout = container.GetComponent<VerticalLayoutGroup>();
        int count = container.Cast<Transform>().Count(c => c.gameObject.activeSelf);
        if (layout != null)
        {
            layout.spacing = 18f;
            var crt = (RectTransform)container;
            float h = count * 80f + (count - 1) * layout.spacing;
            crt.sizeDelta = new Vector2(crt.sizeDelta.x, h);
        }

        EditorSceneManager.MarkSceneDirty(s);
        EditorSceneManager.SaveScene(s);
        if (!open) EditorSceneManager.CloseScene(s, true);
        Debug.Log($"[Content] Story added to main menu ({count} buttons).");
    }

    [MenuItem("Tools/Game Setup/Content/Update Main Menu Credits")]
    public static void UpdateCredits()
    {
        const string path = "Assets/Scenes/MainMenu.unity";
        var s = SceneManager.GetSceneByPath(path);
        bool open = s.IsValid() && s.isLoaded;
        if (!open) s = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);

        var credits = s.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<TextMeshProUGUI>(true)).FirstOrDefault(t => t.name == "CreditsText");
        if (credits == null) Debug.LogError("[Content] CreditsText not found in MainMenu.");
        else
        {
            credits.text = CreditsText;
            credits.ForceMeshUpdate();
            var rt = (RectTransform)credits.transform;
            float h = credits.preferredHeight + 40f;
            if (rt.rect.height < h) rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, h);
            var content = credits.transform.parent as RectTransform;
            if (content != null && content.name == "Content" && content.rect.height < h)
                content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, h);
            Debug.Log("[Content] Credits updated (height " + h + ").");
        }

        EditorSceneManager.MarkSceneDirty(s);
        EditorSceneManager.SaveScene(s);
        if (!open) EditorSceneManager.CloseScene(s, true);
    }
}
#endif
