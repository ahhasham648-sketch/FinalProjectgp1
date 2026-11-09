#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class GameSetupBuilder
{
    private const string AudioFolder = "Assets/Audio/SFX";
    private const string PrefabFolder = "Assets/Prefabs/UI";
    private const string GameUIPrefabPath = PrefabFolder + "/GameUI.prefab";
    private const string LoadingScenePath = "Assets/Scenes/LoadingScreen.unity";
    private const string MainMenuScenePath = "Assets/Scenes/MainMenu.unity";
    private const string Level1Path = "Assets/Scenes/Levels/Level1.unity";
    private const string Level2Path = "Assets/Scenes/Levels/Level2.unity";

    private const string FontGuid = "8f586378b4e144a9851e7b34d9b748ee";
    private const string BackgroundSpriteGuid = "5fff9fc51191d8348b5ba8baf7fd2704";
    private const string LogoSpriteGuid = "b55f6b6e8afee144d8447371260b629f";
    private static readonly Color TextColor = new Color(0.9647059f, 0.83137256f, 0.6039216f, 1f);
    private static readonly Color TitleColor = new Color(1f, 0.827451f, 0.41568628f, 1f);
    private static readonly Color ButtonNormal = new Color(0.16862746f, 0.101960786f, 0.101960786f, 1f);
    private static readonly Color ButtonHighlight = new Color(0.5411765f, 0.19607843f, 0.12156863f, 1f);
    private static readonly Color ButtonPressed = new Color(0.81960785f, 0.2901961f, 0.1254902f, 1f);
    private static readonly Color ButtonDisabled = new Color(0.14117648f, 0.10980392f, 0.10980392f, 0.5019608f);
    private static readonly Color PanelColor = new Color(0.08627451f, 0.047058824f, 0.05882353f, 0.9490196f);
    private static readonly Color DimColor = new Color(0f, 0f, 0f, 0.6666667f);
    private static readonly Color AccentColor = new Color(0.84705883f, 0.47058824f, 0.19607843f, 1f);
    private static readonly Color HealthColor = new Color(0.75f, 0.16f, 0.12f, 1f);

    [MenuItem("Tools/Game Setup/Run Full Setup")]
    public static void RunAll()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        GenerateSounds();
        BuildGameUIPrefab();
        BuildLoadingScene();
        SetupMainMenu();
        FixPlayerAnimator();
        SetupLevel1();
        SetupLevel2();
        SetupBuildSettings();
        Debug.Log("[GameSetup] Full setup complete.");
    }

    [MenuItem("Tools/Game Setup/1. Generate Placeholder Sounds")]
    public static void GenerateSounds()
    {
        Directory.CreateDirectory(AudioFolder);
        const int sr = 44100;
        var rng = new System.Random(1234);
        float Noise() => (float)(rng.NextDouble() * 2.0 - 1.0);
        float Sq(double ph) => Math.Sin(ph) >= 0 ? 1f : -1f;

        Write("sword_swing", 0.22f, sr, (t, d) =>
        {
            float env = Mathf.Sin(Mathf.PI * Mathf.Clamp01(t / d));
            return Noise() * env * env * 0.5f;
        }, lowpassStart: 0.05f, lowpassEnd: 0.5f);

        double ph1 = 0;
        Write("hit_impact", 0.18f, sr, (t, d) =>
        {
            float f = Mathf.Lerp(160f, 50f, t / d);
            ph1 += 2 * Math.PI * f / sr;
            float env = Mathf.Exp(-t * 22f);
            return ((float)Math.Sin(ph1) * 0.8f + Noise() * 0.5f * Mathf.Exp(-t * 60f)) * env;
        });

        double ph2 = 0;
        Write("player_hurt", 0.25f, sr, (t, d) =>
        {
            float f = Mathf.Lerp(520f, 200f, t / d);
            ph2 += 2 * Math.PI * f / sr;
            return Sq(ph2) * 0.35f * Mathf.Exp(-t * 9f);
        }, lowpassStart: 0.3f, lowpassEnd: 0.15f);

        double ph3 = 0;
        Write("player_death", 1.1f, sr, (t, d) =>
        {
            float f = Mathf.Lerp(440f, 70f, Mathf.Pow(t / d, 0.6f));
            ph3 += 2 * Math.PI * f / sr;
            float vib = 1f + 0.3f * Mathf.Sin(t * 40f);
            return Sq(ph3) * 0.3f * vib * (1f - t / d);
        }, lowpassStart: 0.25f, lowpassEnd: 0.05f);

        double ph4 = 0;
        Write("enemy_hurt", 0.16f, sr, (t, d) =>
        {
            float f = Mathf.Lerp(300f, 130f, t / d);
            ph4 += 2 * Math.PI * f / sr;
            return (Sq(ph4) * 0.3f + Noise() * 0.25f) * Mathf.Exp(-t * 14f);
        }, lowpassStart: 0.25f, lowpassEnd: 0.1f);

        double ph5 = 0;
        Write("enemy_death", 0.8f, sr, (t, d) =>
        {
            float f = Mathf.Lerp(220f, 40f, t / d);
            ph5 += 2 * Math.PI * f / sr;
            return ((float)Math.Sin(ph5) * 0.5f + Noise() * 0.4f) * (1f - t / d) * Mathf.Min(1f, t * 60f);
        }, lowpassStart: 0.2f, lowpassEnd: 0.03f);

        double ph6 = 0;
        Write("enemy_attack", 0.3f, sr, (t, d) =>
        {
            float f = Mathf.Lerp(110f, 80f, t / d);
            ph6 += 2 * Math.PI * f / sr;
            float saw = (float)((ph6 / (2 * Math.PI)) % 1.0) * 2f - 1f;
            float env = Mathf.Sin(Mathf.PI * t / d);
            return (saw * 0.4f + Noise() * 0.25f) * env;
        }, lowpassStart: 0.12f, lowpassEnd: 0.3f);

        Write("ui_click", 0.06f, sr, (t, d) => (float)Math.Sin(2 * Math.PI * 1100 * t) * 0.4f * Mathf.Exp(-t * 70f));
        Write("ui_hover", 0.04f, sr, (t, d) => (float)Math.Sin(2 * Math.PI * 1600 * t) * 0.18f * Mathf.Exp(-t * 90f));

        Write("pause", 0.2f, sr, (t, d) =>
        {
            float f = t < 0.09f ? 660f : 880f;
            float lt = t < 0.09f ? t : t - 0.09f;
            return (float)Math.Sin(2 * Math.PI * f * t) * 0.3f * Mathf.Exp(-lt * 25f);
        });

        Write("victory", 1.4f, sr, (t, d) => Arpeggio(t, new[] { 523.25f, 659.25f, 783.99f, 1046.5f }, 0.14f, 0.7f));
        Write("respawn", 0.6f, sr, (t, d) => Arpeggio(t, new[] { 392f, 523.25f, 659.25f }, 0.09f, 0.35f));
        Write("potion_pickup", 0.55f, sr, (t, d) => Arpeggio(t, new[] { 659.25f, 880f, 1318.5f, 1760f }, 0.05f, 0.25f));
        Write("buff_end", 0.35f, sr, (t, d) => Arpeggio(t, new[] { 880f, 587.33f }, 0.08f, 0.2f));
        Write("load_complete", 0.5f, sr, (t, d) => Arpeggio(t, new[] { 783.99f, 1174.66f }, 0.08f, 0.3f));
        Write("death_screen", 1.6f, sr, (t, d) => Arpeggio(t, new[] { 440f, 349.23f, 293.66f, 220f }, 0.25f, 0.9f), lowpassStart: 0.2f, lowpassEnd: 0.1f);

        double ph7 = 0;
        Write("enemy_appear", 0.9f, sr, (t, d) =>
        {
            float f = Mathf.Lerp(45f, 110f, t / d);
            ph7 += 2 * Math.PI * f / sr;
            float env = Mathf.Sin(Mathf.PI * t / d);
            return ((float)Math.Sin(ph7) * 0.6f + Noise() * 0.3f) * env;
        }, lowpassStart: 0.04f, lowpassEnd: 0.12f);

        AssetDatabase.Refresh();
        Debug.Log("[GameSetup] Placeholder sounds written to " + AudioFolder);
    }

    private static float Arpeggio(float t, float[] notes, float step, float tail)
    {
        float s = 0f;
        for (int i = 0; i < notes.Length; i++)
        {
            float lt = t - i * step;
            if (lt < 0f) continue;
            float env = Mathf.Min(1f, lt * 200f) * Mathf.Exp(-lt * (3f / tail));
            float ph = 2f * Mathf.PI * notes[i] * lt;
            s += (Mathf.Sin(ph) * 0.6f + Mathf.Sin(ph * 2f) * 0.15f) * env * 0.35f;
        }
        return s;
    }

    internal static void Write(string name, float seconds, int sr, Func<float, float, float> gen, float lowpassStart = 1f, float lowpassEnd = 1f, string folder = null)
    {
        folder = folder ?? AudioFolder;
        int n = Mathf.CeilToInt(seconds * sr);
        var data = new float[n];
        float lp = 0f;
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / sr;
            float v = gen(t, seconds);
            float a = Mathf.Lerp(lowpassStart, lowpassEnd, (float)i / n);
            lp += (v - lp) * Mathf.Clamp01(a);
            data[i] = lp;
        }

        float peak = data.Max(Mathf.Abs);
        float gain = peak > 0.0001f ? 0.7f / peak : 1f;
        int fade = Mathf.Min(n, sr / 200);
        for (int i = 0; i < n; i++)
        {
            float f = i >= n - fade ? (n - i) / (float)fade : 1f;
            data[i] = Mathf.Clamp(data[i] * gain * f, -1f, 1f);
        }

        string path = Path.Combine(folder, name + ".wav");
        using (var fs = new FileStream(path, FileMode.Create))
        using (var w = new BinaryWriter(fs))
        {
            w.Write(new[] { 'R', 'I', 'F', 'F' });
            w.Write(36 + n * 2);
            w.Write(new[] { 'W', 'A', 'V', 'E', 'f', 'm', 't', ' ' });
            w.Write(16); w.Write((short)1); w.Write((short)1);
            w.Write(sr); w.Write(sr * 2); w.Write((short)2); w.Write((short)16);
            w.Write(new[] { 'd', 'a', 't', 'a' });
            w.Write(n * 2);
            foreach (var s in data) w.Write((short)(s * short.MaxValue));
        }
    }

    internal static AudioClip Clip(string name) => AssetDatabase.LoadAssetAtPath<AudioClip>($"{AudioFolder}/{name}.wav");

    [MenuItem("Tools/Game Setup/2. Build GameUI Prefab")]
    public static void BuildGameUIPrefab()
    {
        Directory.CreateDirectory(PrefabFolder);

        var root = new GameObject("GameUI");
        var controller = root.AddComponent<GameUIController>();

        var canvasGo = CreateCanvas("Canvas", root.transform, 100);
        var sounds = canvasGo.AddComponent<UIButtonSounds>();
        SetFields(sounds, ("clickSound", Clip("ui_click")), ("hoverSound", Clip("ui_hover")));

        var hud = UIObject("HUD", canvasGo.transform);
        Stretch(hud);

        var barBg = UIObject("HealthBar", hud.transform);
        var barBgImg = barBg.AddComponent<Image>();
        barBgImg.sprite = UISprite(); barBgImg.type = Image.Type.Sliced; barBgImg.color = ButtonNormal;
        Anchor(barBg, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(40, -40), new Vector2(420, 34));
        var barOutline = barBg.AddComponent<Outline>(); barOutline.effectColor = new Color(0, 0, 0, 0.8f); barOutline.effectDistance = new Vector2(3, -3);

        var fill = UIObject("Fill", barBg.transform);
        var fillImg = fill.AddComponent<Image>();
        fillImg.sprite = UISprite(); fillImg.color = HealthColor;
        fillImg.type = Image.Type.Filled; fillImg.fillMethod = Image.FillMethod.Horizontal; fillImg.fillOrigin = 0; fillImg.fillAmount = 1f;
        Stretch(fill, 4);

        var healthText = Text("HealthText", barBg.transform, "100 / 100", 24, TextColor, TextAlignmentOptions.Center);
        Stretch(healthText.gameObject);

        var livesText = Text("LivesText", hud.transform, "LIVES  3", 30, TextColor, TextAlignmentOptions.Left);
        Anchor(livesText.gameObject, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(40, -86), new Vector2(420, 44));

        var objectiveText = Text("ObjectiveText", hud.transform, "Defeat the Goblin  (0/3)", 34, TitleColor, TextAlignmentOptions.Center);
        Anchor(objectiveText.gameObject, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -36), new Vector2(1000, 56));
        objectiveText.gameObject.AddComponent<Shadow>().effectColor = new Color(0, 0, 0, 0.8f);

        var pause = BuildMenuPanel("PausePanel", canvasGo.transform, "PAUSED", "Take a breath, Vaelor.", out var pauseButtons);
        var resume = MenuButton("ResumeButton", pauseButtons, "RESUME", controller.Resume);
        MenuButton("PlayAgainButton", pauseButtons, "PLAY AGAIN", controller.PlayAgain);
        MenuButton("MainMenuButton", pauseButtons, "MAIN MENU", controller.GoToMainMenu);
        MenuButton("ExitButton", pauseButtons, "EXIT GAME", controller.ExitGame);

        var death = BuildMenuPanel("DeathPanel", canvasGo.transform, "YOU DIED", "The spire claims another soul.", out var deathButtons);
        var playAgain = MenuButton("PlayAgainButton", deathButtons, "PLAY AGAIN", controller.PlayAgain);
        MenuButton("MainMenuButton", deathButtons, "MAIN MENU", controller.GoToMainMenu);
        MenuButton("ExitButton", deathButtons, "EXIT GAME", controller.ExitGame);

        SetFields(controller,
            ("hudRoot", hud), ("pausePanel", pause), ("deathPanel", death),
            ("healthFill", fillImg), ("healthText", healthText), ("livesText", livesText), ("objectiveText", objectiveText),
            ("pauseFirstSelected", resume.gameObject), ("deathFirstSelected", playAgain.gameObject),
            ("pauseSound", Clip("pause")), ("deathScreenSound", Clip("death_screen")));

        pause.SetActive(false);
        death.SetActive(false);

        CreateEventSystem(root.transform);

        PrefabUtility.SaveAsPrefabAsset(root, GameUIPrefabPath);
        UnityEngine.Object.DestroyImmediate(root);
        Debug.Log("[GameSetup] GameUI prefab saved to " + GameUIPrefabPath);
    }

    internal static GameObject BuildMenuPanel(string name, Transform parent, string title, string subtitle, out Transform buttonContainer)
    {
        var dim = UIObject(name, parent);
        Stretch(dim);
        var dimImg = dim.AddComponent<Image>();
        dimImg.color = DimColor;

        var panel = UIObject("Panel", dim.transform);
        var panelImg = panel.AddComponent<Image>();
        panelImg.sprite = BackgroundSprite(); panelImg.type = Image.Type.Sliced; panelImg.color = PanelColor;
        Anchor(panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(640, 760));
        var outline = panel.AddComponent<Outline>(); outline.effectColor = new Color(ButtonHighlight.r, ButtonHighlight.g, ButtonHighlight.b, 0.9f); outline.effectDistance = new Vector2(4, -4);

        var titleText = Text("Title", panel.transform, title, 72, TitleColor, TextAlignmentOptions.Center);
        Anchor(titleText.gameObject, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -50), new Vector2(600, 100));

        var sub = Text("Subtitle", panel.transform, subtitle, 26, TextColor, TextAlignmentOptions.Center);
        Anchor(sub.gameObject, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -150), new Vector2(600, 40));

        var container = UIObject("ButtonContainer", panel.transform);
        Anchor(container, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -90), new Vector2(480, 440));
        var layout = container.AddComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.spacing = 25;
        layout.childControlWidth = true; layout.childControlHeight = true;
        layout.childForceExpandWidth = true; layout.childForceExpandHeight = false;

        buttonContainer = container.transform;
        return dim;
    }

    internal static Button MenuButton(string name, Transform parent, string label, UnityAction action)
    {
        var go = UIObject(name, parent);
        var img = go.AddComponent<Image>();
        img.sprite = UISprite(); img.type = Image.Type.Sliced; img.color = Color.white;

        var button = go.AddComponent<Button>();
        button.targetGraphic = img;
        button.transition = Selectable.Transition.ColorTint;
        button.colors = new ColorBlock
        {
            normalColor = ButtonNormal, highlightedColor = ButtonHighlight, pressedColor = ButtonPressed,
            selectedColor = ButtonHighlight, disabledColor = ButtonDisabled, colorMultiplier = 1f, fadeDuration = 0.1f
        };
        UnityEventTools.AddPersistentListener(button.onClick, action);

        var le = go.AddComponent<LayoutElement>();
        le.preferredWidth = 480; le.preferredHeight = 80;

        var text = Text("Text (TMP)", go.transform, label, 36, TextColor, TextAlignmentOptions.Center);
        Stretch(text.gameObject);
        return button;
    }

    [MenuItem("Tools/Game Setup/3. Build Loading Screen Scene")]
    public static void BuildLoadingScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);

        var camGo = new GameObject("Main Camera");
        var cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;
        cam.orthographic = true;
        camGo.AddComponent<AudioListener>();
        camGo.tag = "MainCamera";
        camGo.transform.position = new Vector3(0, 0, -10);
        SceneManager.MoveGameObjectToScene(camGo, scene);

        var canvasGo = CreateCanvas("Canvas", null, 0);
        SceneManager.MoveGameObjectToScene(canvasGo, scene);

        var bg = UIObject("Background", canvasGo.transform);
        var bgImg = bg.AddComponent<Image>();
        bgImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GUIDToAssetPath(BackgroundSpriteGuid));
        bgImg.color = bgImg.sprite != null ? Color.white : PanelColor;
        Stretch(bg);
        if (bgImg.sprite != null)
        {
            var fitter = bg.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = bgImg.sprite.rect.width / bgImg.sprite.rect.height;
        }

        var dim = UIObject("Dim", canvasGo.transform);
        Stretch(dim);
        dim.AddComponent<Image>().color = new Color(0, 0, 0, 0.45f);

        var logoSprite = AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GUIDToAssetPath(LogoSpriteGuid));
        if (logoSprite != null)
        {
            var logo = UIObject("LogoImage", canvasGo.transform);
            var logoImg = logo.AddComponent<Image>();
            logoImg.sprite = logoSprite; logoImg.preserveAspect = true;
            Anchor(logo, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 140), new Vector2(760, 420));
        }
        else
        {
            var title = Text("TitleText", canvasGo.transform, "IN THE SPIRE", 72, TitleColor, TextAlignmentOptions.Center);
            Anchor(title.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 140), new Vector2(1200, 120));
        }

        var strip = UIObject("BottomPanel", canvasGo.transform);
        strip.AddComponent<Image>().color = DimColor;
        Anchor(strip, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), Vector2.zero, new Vector2(0, 260));

        var levelTitle = Text("LevelTitleText", canvasGo.transform, "THE FOREST", 64, TitleColor, TextAlignmentOptions.Center);
        Anchor(levelTitle.gameObject, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 290), new Vector2(1200, 90));
        levelTitle.gameObject.AddComponent<Shadow>().effectColor = new Color(0, 0, 0, 0.85f);

        var progressText = Text("ProgressText", strip.transform, "LOADING...  0%", 40, TitleColor, TextAlignmentOptions.Center);
        Anchor(progressText.gameObject, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -30), new Vector2(900, 60));

        var barBg = UIObject("ProgressBar", strip.transform);
        var barBgImg = barBg.AddComponent<Image>();
        barBgImg.sprite = UISprite(); barBgImg.type = Image.Type.Sliced; barBgImg.color = ButtonNormal;
        Anchor(barBg, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -105), new Vector2(1000, 30));
        var barOutline = barBg.AddComponent<Outline>(); barOutline.effectColor = new Color(0, 0, 0, 0.8f); barOutline.effectDistance = new Vector2(3, -3);

        var fill = UIObject("Fill", barBg.transform);
        var fillImg = fill.AddComponent<Image>();
        fillImg.sprite = UISprite(); fillImg.color = AccentColor;
        fillImg.type = Image.Type.Filled; fillImg.fillMethod = Image.FillMethod.Horizontal; fillImg.fillAmount = 0f;
        Stretch(fill, 4);

        var tip = Text("TipText", strip.transform, "Press J or Left Mouse to attack.", 28, TextColor, TextAlignmentOptions.Center);
        Anchor(tip.gameObject, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -170), new Vector2(1400, 50));

        var controllerGo = new GameObject("LoadingScreenController");
        SceneManager.MoveGameObjectToScene(controllerGo, scene);
        var controller = controllerGo.AddComponent<LoadingScreenController>();
        SetFields(controller, ("levelTitleText", levelTitle), ("progressFill", fillImg), ("progressText", progressText), ("tipText", tip), ("loadCompleteSound", Clip("load_complete")));

        Directory.CreateDirectory(Path.GetDirectoryName(LoadingScenePath));
        EditorSceneManager.SaveScene(scene, LoadingScenePath);
        EditorSceneManager.CloseScene(scene, true);
        Debug.Log("[GameSetup] Loading screen saved to " + LoadingScenePath);
    }

    [MenuItem("Tools/Game Setup/4. Add Sounds To Main Menu")]
    public static void SetupMainMenu()
    {
        WithScene(MainMenuScenePath, scene =>
        {
            var canvas = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Canvas>(true)).FirstOrDefault();
            if (canvas == null) { Debug.LogWarning("[GameSetup] No Canvas in MainMenu."); return; }
            var sounds = canvas.GetComponent<UIButtonSounds>();
            if (sounds == null) sounds = canvas.gameObject.AddComponent<UIButtonSounds>();
            SetFields(sounds, ("clickSound", Clip("ui_click")), ("hoverSound", Clip("ui_hover")));
        });
    }

    [MenuItem("Tools/Game Setup/5. Setup Level1 (player, enemies, win logic)")]
    public static void SetupLevel1() => SetupLevel(Level1Path, "Level2");

    [MenuItem("Tools/Game Setup/6. Setup Level2 (player, enemies, win logic, ground)")]
    public static void SetupLevel2()
    {
        FixLevel2Ground();
        SetupLevel(Level2Path, "Level3");
    }

    public static void SetupLevel(string scenePath, string nextScene)
    {
        WithScene(scenePath, scene =>
        {
            var roots = scene.GetRootGameObjects();

            var playerGo = roots.SelectMany(r => r.GetComponentsInChildren<Transform>(true)).FirstOrDefault(t => t.CompareTag("Player"))?.gameObject;
            if (playerGo == null) { Debug.LogError("[GameSetup] No object tagged Player in " + scenePath); return; }
            var player = playerGo.GetComponent<Health>();
            if (player == null) player = playerGo.AddComponent<Health>();

            var so = new SerializedObject(player);
            if (so.FindProperty("startingHealth").floatValue <= 0f) so.FindProperty("startingHealth").floatValue = 100f;
            so.ApplyModifiedPropertiesWithoutUndo();
            SetFields(player, ("hurtSound", Clip("player_hurt")), ("deathSound", Clip("player_death")));

            var resp = player.GetComponent<respawn>();
            if (resp == null) resp = player.gameObject.AddComponent<respawn>();
            SetFields(resp, ("respawnSound", Clip("respawn")));

            var attack = player.GetComponent<PlayerAttack>();
            if (attack == null) attack = player.gameObject.AddComponent<PlayerAttack>();
            SetFields(attack, ("swingSound", Clip("sword_swing")), ("hitSound", Clip("hit_impact")));
            var aso = new SerializedObject(attack);
            aso.FindProperty("attackOffset").vector2Value = new Vector2(0.9f, 0.3f);
            aso.FindProperty("attackSize").vector2Value = new Vector2(1.8f, 2.8f);
            aso.ApplyModifiedPropertiesWithoutUndo();

            var enemies = roots.SelectMany(r => r.GetComponentsInChildren<MonoBehaviour>(true))
                .Where(mb => mb != null && mb.GetType().Name.EndsWith("Patrol") && mb.GetComponent<SpriteRenderer>() != null)
                .Select(mb => mb.gameObject).Distinct()
                .OrderBy(go => go.transform.position.x)
                .ToList();

            var entries = new List<(EnemyHealth health, GameObject root)>();
            foreach (var go in enemies)
            {
                string typeName = go.GetComponents<MonoBehaviour>().First(mb => mb.GetType().Name.EndsWith("Patrol")).GetType().Name;
                var stats = EnemyStats(typeName);

                if (go.GetComponent<Collider2D>() == null)
                {
                    var box = go.AddComponent<BoxCollider2D>();
                    box.isTrigger = true;
                    var sr = go.GetComponent<SpriteRenderer>();
                    if (sr.sprite != null)
                    {
                        box.size = sr.sprite.bounds.size * 0.6f;
                        box.offset = (Vector2)sr.sprite.bounds.center;
                    }
                }

                var eh = go.GetComponent<EnemyHealth>();
                if (eh == null) eh = go.AddComponent<EnemyHealth>();
                SetFields(eh, ("displayName", stats.name), ("maxHealth", stats.health), ("hurtTrigger", stats.hurtTrigger),
                    ("hurtSound", Clip("enemy_hurt")), ("deathSound", Clip("enemy_death")));

                var ea = go.GetComponent<EnemyAttack>();
                if (ea == null) ea = go.AddComponent<EnemyAttack>();
                float halfWidth = go.GetComponent<Collider2D>().bounds.extents.x;
                SetFields(ea, ("damage", stats.damage), ("attackCooldown", stats.cooldown),
                    ("attackRange", halfWidth + stats.reach), ("engageRange", halfWidth + stats.reach + 1.5f),
                    ("verticalTolerance", stats.verticalTolerance > 0f ? stats.verticalTolerance : Mathf.Max(1.5f, go.GetComponent<Collider2D>().bounds.extents.y + 0.5f)),
                    ("attackSound", Clip("enemy_attack")));
                var easo = new SerializedObject(ea);
                var trig = easo.FindProperty("attackTriggers");
                trig.arraySize = stats.attackTriggers.Length;
                for (int t = 0; t < stats.attackTriggers.Length; t++) trig.GetArrayElementAtIndex(t).stringValue = stats.attackTriggers[t];

                bool flyer = stats.verticalTolerance > 0f;
                easo.FindProperty("diveAtTarget").boolValue = flyer;
                easo.ApplyModifiedPropertiesWithoutUndo();
                if (flyer) foreach (var fc in go.GetComponents<Collider2D>()) fc.isTrigger = true;

                var parent = go.transform.parent;
                entries.Add((eh, parent != null ? parent.gameObject : go));
            }

            var goals = UnityEngine.Object.FindObjectsByType<LevelGoalManager>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .FirstOrDefault(g => g.gameObject.scene == scene);
            if (goals == null)
            {
                var goalsGo = new GameObject("LevelGoals");
                SceneManager.MoveGameObjectToScene(goalsGo, scene);
                goals = goalsGo.AddComponent<LevelGoalManager>();
            }
            var gso = new SerializedObject(goals);
            var list = gso.FindProperty("enemiesInOrder");
            list.arraySize = entries.Count;
            for (int i = 0; i < entries.Count; i++)
            {
                var el = list.GetArrayElementAtIndex(i);
                el.FindPropertyRelative("enemy").objectReferenceValue = entries[i].health;
                el.FindPropertyRelative("revealRoot").objectReferenceValue = entries[i].root;
            }
            gso.FindProperty("nextSceneName").stringValue = nextScene;
            gso.FindProperty("enemyAppearSound").objectReferenceValue = Clip("enemy_appear");
            gso.FindProperty("victorySound").objectReferenceValue = Clip("victory");
            gso.ApplyModifiedPropertiesWithoutUndo();

            AddGameUI(scene);
            Debug.Log($"[GameSetup] {scene.name} wired: player + {entries.Count} enemies in order: " + string.Join(" -> ", entries.Select(e => e.health.name)) + $" -> {nextScene}");
        });
    }

    private class Stats
    {
        public string name; public float health, damage, cooldown, reach, verticalTolerance;
        public string hurtTrigger = "takehit";
        public string[] attackTriggers = { "attack1", "attack2" };
    }

    private static Stats EnemyStats(string patrolType)
    {
        switch (patrolType.ToLowerInvariant())
        {
            case "goblinpatrol": return new Stats { name = "Goblin", health = 50f, damage = 10f, cooldown = 1.3f, reach = 0.9f };
            case "mushroompatrol": return new Stats { name = "Mushroom", health = 75f, damage = 15f, cooldown = 1.5f, reach = 0.9f };
            case "golempatrol": return new Stats { name = "Rock Golem", health = 120f, damage = 25f, cooldown = 2f, reach = 1.2f };
            case "batpatrol": return new Stats { name = "Bat", health = 40f, damage = 8f, cooldown = 1.2f, reach = 0.6f, verticalTolerance = 3f, attackTriggers = new[] { "Attack" } };
            case "eyepatrol": return new Stats { name = "Flying Eye", health = 60f, damage = 12f, cooldown = 1.4f, reach = 0.7f, verticalTolerance = 3.2f, hurtTrigger = "TakeHit", attackTriggers = new[] { "Attack1", "Attack2" } };
            default: return new Stats { name = patrolType.Replace("Patrol", ""), health = 60f, damage = 12f, cooldown = 1.5f, reach = 1f };
        }
    }

    [MenuItem("Tools/Game Setup/Fix Level2 Ground Colliders")]
    public static void FixLevel2Ground()
    {
        WithScene(Level2Path, scene =>
        {
            var roots = scene.GetRootGameObjects();
            int groundLayer = LayerMask.NameToLayer("Ground");

            var tileColliders = roots.Where(r => r.name.StartsWith("ground1"))
                .SelectMany(r => r.GetComponentsInChildren<BoxCollider2D>(true))
                .Where(c => !c.isTrigger)
                .ToList();
            if (tileColliders.Count == 0) { Debug.LogWarning("[GameSetup] No ground tiles found in Level2."); return; }

            float left = float.MaxValue, right = float.MinValue, top = float.MinValue, bottom = float.MaxValue;
            foreach (var c in tileColliders)
            {
                c.enabled = true;
                var b = c.bounds;
                left = Mathf.Min(left, b.min.x); right = Mathf.Max(right, b.max.x);
                top = Mathf.Max(top, b.max.y); bottom = Mathf.Min(bottom, b.min.y);
            }
            foreach (var c in tileColliders) c.enabled = false;

            var container = roots.FirstOrDefault(r => r.name == "Ground Terrain");
            if (container == null)
            {
                container = new GameObject("Ground Terrain");
                SceneManager.MoveGameObjectToScene(container, scene);
            }

            GameObject Child(string n)
            {
                var t = container.transform.Find(n);
                var go = t != null ? t.gameObject : new GameObject(n);
                go.transform.SetParent(container.transform, false);
                go.transform.position = Vector3.zero;
                go.transform.rotation = Quaternion.identity;
                go.transform.localScale = Vector3.one;
                if (groundLayer >= 0) go.layer = groundLayer;
                return go;
            }

            var ground = Child("GroundCollider");
            var box = ground.GetComponent<BoxCollider2D>();
            if (box == null) box = ground.AddComponent<BoxCollider2D>();
            box.offset = new Vector2((left + right) * 0.5f, (top + bottom) * 0.5f);
            box.size = new Vector2(right - left, top - bottom);

            const float wallHeight = 30f, wallWidth = 1f;
            var walls = Child("LevelBounds");
            foreach (var old in walls.GetComponents<BoxCollider2D>()) UnityEngine.Object.DestroyImmediate(old);
            var lw = walls.AddComponent<BoxCollider2D>();
            lw.offset = new Vector2(left - wallWidth * 0.5f, top + wallHeight * 0.5f - 1f);
            lw.size = new Vector2(wallWidth, wallHeight);
            var rw = walls.AddComponent<BoxCollider2D>();
            rw.offset = new Vector2(right + wallWidth * 0.5f, top + wallHeight * 0.5f - 1f);
            rw.size = new Vector2(wallWidth, wallHeight);
            walls.layer = 0;

            Debug.Log($"[GameSetup] Level2 ground: {tileColliders.Count} tile colliders replaced by one box x[{left:F2},{right:F2}] top={top:F2}, plus end walls.");
        });
    }

    [MenuItem("Tools/Game Setup/Fix HeroKnight Animator Transitions")]
    public static void FixPlayerAnimator()
    {
        var ac = AssetDatabase.LoadAssetAtPath<UnityEditor.Animations.AnimatorController>("Assets/Animations/Player/playerAnim.controller");
        if (ac == null) { Debug.LogWarning("[GameSetup] playerAnim.controller not found."); return; }
        var sm = ac.layers[0].stateMachine;
        var falling = sm.states.Select(s => s.state).FirstOrDefault(s => s.name == "falling");
        if (falling == null) return;

        foreach (var from in sm.states.Select(s => s.state).Where(s => s.name == "idle" || s.name == "Walking"))
        {
            if (from.transitions.Any(t => t.destinationState == falling)) continue;
            var t = from.AddTransition(falling);
            t.hasExitTime = false;
            t.duration = 0.05f;
            t.AddCondition(UnityEditor.Animations.AnimatorConditionMode.IfNot, 0, "Grounded");
            t.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Less, -0.5f, "Yvelocity");
        }
        EditorUtility.SetDirty(ac);
        AssetDatabase.SaveAssets();
        Debug.Log("[GameSetup] Added idle/Walking -> falling transitions.");
    }

    [MenuItem("Tools/Game Setup/Add GameUI To Level2 Only")]
    public static void AddGameUIToLevel2() => AddGameUIToScene(Level2Path);

    private static void AddGameUIToScene(string path)
    {
        if (!File.Exists(path)) { Debug.LogWarning("[GameSetup] Missing scene " + path); return; }
        WithScene(path, AddGameUI);
    }

    private static void AddGameUI(Scene scene)
    {
        bool hasUI = scene.GetRootGameObjects().Any(g => g.GetComponentInChildren<GameUIController>(true) != null);
        if (hasUI) return;

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(GameUIPrefabPath);
        if (prefab == null) { Debug.LogError("[GameSetup] Build the GameUI prefab first."); return; }
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);

        bool otherEventSystem = scene.GetRootGameObjects().Any(g => g != instance && g.GetComponentInChildren<EventSystem>(true) != null);
        if (otherEventSystem)
        {
            var es = instance.GetComponentInChildren<EventSystem>(true);
            if (es != null) es.gameObject.SetActive(false);
        }
    }

    [MenuItem("Tools/Game Setup/7. Update Build Settings")]
    public static void SetupBuildSettings()
    {
        var wanted = new[] { MainMenuScenePath, LoadingScenePath, Level1Path, Level2Path, "Assets/Scenes/Levels/Level3.unity" }.Where(File.Exists).ToList();
        var scenes = wanted.Select(p => new EditorBuildSettingsScene(p, true)).ToList();
        foreach (var existing in EditorBuildSettings.scenes)
        {
            if (!wanted.Contains(existing.path)) scenes.Add(existing);
        }
        EditorBuildSettings.scenes = scenes.ToArray();
        Debug.Log("[GameSetup] Build Settings: " + string.Join(", ", scenes.Select(s => s.path)));
    }

    private static void WithScene(string path, Action<Scene> action)
    {
        var scene = SceneManager.GetSceneByPath(path);
        bool wasOpen = scene.IsValid() && scene.isLoaded;
        if (!wasOpen) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);

        action(scene);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        if (!wasOpen) EditorSceneManager.CloseScene(scene, true);
    }

    private static void SetFields(UnityEngine.Object target, params (string name, object value)[] fields)
    {
        var so = new SerializedObject(target);
        foreach (var (name, value) in fields)
        {
            var p = so.FindProperty(name);
            if (p == null) { Debug.LogWarning($"[GameSetup] {target.GetType().Name} has no field '{name}'"); continue; }
            switch (value)
            {
                case UnityEngine.Object o: p.objectReferenceValue = o; break;
                case float f: p.floatValue = f; break;
                case int i: p.intValue = i; break;
                case string s: p.stringValue = s; break;
                case bool b: p.boolValue = b; break;
                case null: p.objectReferenceValue = null; break;
            }
        }
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    internal static GameObject CreateCanvas(string name, Transform parent, int sortingOrder)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = LayerMask.NameToLayer("UI");
        if (parent != null) go.transform.SetParent(parent, false);
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;
        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        go.AddComponent<GraphicRaycaster>();
        return go;
    }

    private static void CreateEventSystem(Transform parent)
    {
        var es = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        es.transform.SetParent(parent, false);
    }

    internal static GameObject UIObject(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = LayerMask.NameToLayer("UI");
        go.transform.SetParent(parent, false);
        return go;
    }

    internal static TextMeshProUGUI Text(string name, Transform parent, string text, float size, Color color, TextAlignmentOptions align)
    {
        var go = UIObject(name, parent);
        var t = go.AddComponent<TextMeshProUGUI>();
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(FontGuid));
        if (font != null) t.font = font;
        t.text = text;
        t.fontSize = size;
        t.color = color;
        t.alignment = align;
        t.textWrappingMode = TextWrappingModes.NoWrap;
        t.raycastTarget = false;
        return t;
    }

    internal static void Stretch(GameObject go, float inset = 0f)
    {
        var rt = (RectTransform)go.transform;
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(inset, inset); rt.offsetMax = new Vector2(-inset, -inset);
    }

    internal static void Anchor(GameObject go, Vector2 min, Vector2 max, Vector2 pivot, Vector2 pos, Vector2 size)
    {
        var rt = (RectTransform)go.transform;
        rt.anchorMin = min; rt.anchorMax = max; rt.pivot = pivot;
        rt.anchoredPosition = pos; rt.sizeDelta = size;
    }

    internal static Sprite UISprite() => AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
    internal static Sprite BackgroundSprite() => AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
}
#endif
