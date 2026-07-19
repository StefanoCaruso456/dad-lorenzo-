using CrossHop.Core;
using CrossHop.Gameplay;
using CrossHop.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace CrossHop.EditorTools
{
    /// <summary>
    /// Assembles a press-Play-ready gray-box scene: lights, camera, player cube, the
    /// gameplay systems, generated gray-box prefabs (lane/obstacle/coin), a bootstrap
    /// that auto-starts a run, and a full HUD canvas (score, coins, game-over + retry) —
    /// all serialized refs wired. Swap in real art/prefabs later.
    ///
    /// Menu: Tools ▸ CrossHop ▸ Build Gray-box Scene.
    /// </summary>
    public static class GrayboxSceneBuilder
    {
        [MenuItem("Tools/CrossHop/Build Gray-box Scene")]
        public static void Build()
        {
            if (!EditorUtility.DisplayDialog(
                    "Build Gray-box Scene",
                    "This adds gray-box objects to the CURRENT scene and wires the core systems. Continue?",
                    "Build", "Cancel"))
                return;

            GridSettings grid = LoadOrCreateGrid();

            // --- Lighting & camera background (so meshes aren't unlit/black) ---
            if (Object.FindFirstObjectByType<Light>() == null)
            {
                var lightGo = new GameObject("Directional Light");
                var light = lightGo.AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 1.1f;
                lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            }

            // --- Player (a cube that hops) ---
            var playerGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            playerGo.name = "Player";
            playerGo.transform.localScale = Vector3.one * 0.8f;
            Object.DestroyImmediate(playerGo.GetComponent<Collider>());
            var player = playerGo.AddComponent<PlayerController>();
            var input = playerGo.AddComponent<CrossHop.Input.InputReader>();

            // --- Lane generator ---
            var genGo = new GameObject("LaneGenerator");
            var generator = genGo.AddComponent<LaneGenerator>();

            // --- Camera ---
            var camGo = new GameObject("Main Camera", typeof(Camera));
            camGo.tag = "MainCamera";
            var camera = camGo.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.55f, 0.78f, 0.95f);
            var camFollow = camGo.AddComponent<CameraFollow>();

            // --- Managers ---
            var economy = new GameObject("EconomyManager").AddComponent<CrossHop.Economy.EconomyManager>();
            var gm = new GameObject("GameManager").AddComponent<GameManager>();
            var coinField = new GameObject("CoinField").AddComponent<CoinField>();

            // --- Gray-box prefabs (generated) ---
            Lane lanePrefab = GrayboxPrefabBuilder.EnsureLanePrefab();
            MovingObstacle obstaclePrefab = GrayboxPrefabBuilder.EnsureObstaclePrefab();
            Coin coinPrefab = GrayboxPrefabBuilder.EnsureCoinPrefab();

            var world = FindFirstAsset<WorldTheme>();
            if (world != null) AssignObstacleToWorld(world, obstaclePrefab.gameObject);

            // --- Wire serialized references ---
            Wire(player, ("grid", grid), ("laneGenerator", generator), ("input", input));
            Wire(generator, ("grid", grid), ("lanePrefab", lanePrefab));
            Wire(camFollow, ("grid", grid), ("player", player));
            Wire(coinField, ("grid", grid), ("laneGenerator", generator), ("player", player),
                            ("coinPrefab", coinPrefab));
            Wire(gm, ("laneGenerator", generator), ("player", player), ("cameraFollow", camFollow),
                     ("economy", economy), ("coinField", coinField), ("fallbackWorld", world));

            // --- HUD + bootstrap ---
            BuildHud(gm, economy);
            var boot = new GameObject("Bootstrap").AddComponent<GameBootstrap>();
            Wire(boot, ("gameManager", gm));

            string worldNote = world != null
                ? $"Fallback world '{world.name}' wired."
                : "No WorldTheme found — run Create Sample Content or Build Full Roster first, then re-run.";
            Debug.Log($"[CrossHop] Gray-box scene built and wired — press Play to hop. {worldNote} " +
                      "(If HUD text is invisible, import TMP Essentials when Unity prompts.)");
            Selection.activeGameObject = gm.gameObject;
        }

        // ---- HUD construction ---------------------------------------------

        private static void BuildHud(GameManager gm, CrossHop.Economy.EconomyManager economy)
        {
            if (Object.FindFirstObjectByType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem", typeof(EventSystem));
                es.AddComponent<InputSystemUIInputModule>();
            }

            var canvasGo = new GameObject("HUD Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);

            TMP_Text score = CreateText(canvasGo.transform, "Score", new Vector2(0.5f, 1f), new Vector2(0f, -90f), 90f, TextAlignmentOptions.Center);
            score.text = "0";
            TMP_Text coins = CreateText(canvasGo.transform, "Coins", new Vector2(1f, 1f), new Vector2(-50f, -50f), 48f, TextAlignmentOptions.Right);
            coins.text = "0";

            // Game-over panel (starts hidden at runtime via HUD).
            var panel = new GameObject("GameOver", typeof(Image));
            panel.transform.SetParent(canvasGo.transform, false);
            Stretch(panel.GetComponent<RectTransform>());
            panel.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.6f);

            TMP_Text finalScore = CreateText(panel.transform, "Final", new Vector2(0.5f, 0.5f), new Vector2(0f, 150f), 96f, TextAlignmentOptions.Center);
            TMP_Text best = CreateText(panel.transform, "Best", new Vector2(0.5f, 0.5f), new Vector2(0f, 50f), 52f, TextAlignmentOptions.Center);
            TMP_Text earned = CreateText(panel.transform, "Earned", new Vector2(0.5f, 0.5f), new Vector2(0f, -20f), 52f, TextAlignmentOptions.Center);
            earned.color = new Color(1f, 0.81f, 0.27f);

            var btnGo = new GameObject("Retry", typeof(Image), typeof(Button));
            btnGo.transform.SetParent(panel.transform, false);
            var btnRt = btnGo.GetComponent<RectTransform>();
            btnRt.anchorMin = btnRt.anchorMax = btnRt.pivot = new Vector2(0.5f, 0.5f);
            btnRt.sizeDelta = new Vector2(360f, 120f);
            btnRt.anchoredPosition = new Vector2(0f, -170f);
            btnGo.GetComponent<Image>().color = new Color(1f, 0.81f, 0.27f);
            var retry = btnGo.GetComponent<Button>();
            TMP_Text label = CreateText(btnGo.transform, "Label", new Vector2(0.5f, 0.5f), Vector2.zero, 46f, TextAlignmentOptions.Center);
            label.text = "RETRY";
            label.color = Color.black;

            var hud = canvasGo.AddComponent<HUD>();
            Wire(hud, ("gameManager", gm), ("economy", economy),
                      ("scoreText", score), ("coinText", coins),
                      ("gameOverPanel", panel), ("finalScoreText", finalScore),
                      ("bestScoreText", best), ("earnedText", earned), ("retryButton", retry));
        }

        private static TMP_Text CreateText(Transform parent, string name, Vector2 anchor, Vector2 pos, float size, TextAlignmentOptions align)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var text = go.AddComponent<TextMeshProUGUI>();
            text.fontSize = size;
            text.alignment = align;
            text.color = Color.white;
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(900f, 140f);
            rt.anchorMin = rt.anchorMax = anchor;
            return text;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        // ---- Helpers ------------------------------------------------------

        private static void AssignObstacleToWorld(WorldTheme world, GameObject obstaclePrefab)
        {
            if (world.hazardLanes == null) return;
            foreach (LaneDefinition def in world.hazardLanes)
            {
                if (def == null || def.obstaclePrefab != null) continue;
                var so = new SerializedObject(def);
                so.FindProperty("obstaclePrefab").objectReferenceValue = obstaclePrefab;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void Wire(Object target, params (string field, Object value)[] refs)
        {
            var so = new SerializedObject(target);
            foreach ((string field, Object value) in refs)
            {
                SerializedProperty prop = so.FindProperty(field);
                if (prop != null) prop.objectReferenceValue = value;
                else Debug.LogWarning($"[CrossHop] Field '{field}' not found on {target.GetType().Name}.");
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static T FindFirstAsset<T>() where T : Object
        {
            string[] guids = AssetDatabase.FindAssets("t:" + typeof(T).Name);
            return guids.Length > 0
                ? AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guids[0]))
                : null;
        }

        private static GridSettings LoadOrCreateGrid()
        {
            const string path = "Assets/CrossHop/Settings/GridSettings.asset";
            var grid = AssetDatabase.LoadAssetAtPath<GridSettings>(path);
            if (grid != null) return grid;

            System.IO.Directory.CreateDirectory("Assets/CrossHop/Settings");
            grid = ScriptableObject.CreateInstance<GridSettings>();
            AssetDatabase.CreateAsset(grid, path);
            AssetDatabase.SaveAssets();
            return grid;
        }
    }
}
