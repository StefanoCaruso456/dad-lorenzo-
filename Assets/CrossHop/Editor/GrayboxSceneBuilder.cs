using CrossHop.Core;
using CrossHop.Gameplay;
using UnityEditor;
using UnityEngine;

namespace CrossHop.EditorTools
{
    /// <summary>
    /// Menu tool that assembles a runnable gray-box scene: player cube, lane
    /// generator, camera, managers — all serialized refs wired — so you can press
    /// Play immediately instead of hand-wiring the M1 milestone.
    ///
    /// It builds primitive prefabs in memory; swap in real art/prefabs later.
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
            var camFollow = camGo.AddComponent<CameraFollow>();

            // --- Managers ---
            var econGo = new GameObject("EconomyManager");
            var economy = econGo.AddComponent<CrossHop.Economy.EconomyManager>();

            var gmGo = new GameObject("GameManager");
            var gm = gmGo.AddComponent<GameManager>();

            // --- Field coins (inert until a Coin prefab is assigned) ---
            var coinGo = new GameObject("CoinField");
            var coinField = coinGo.AddComponent<CoinField>();

            // --- Wire serialized references via SerializedObject (survives save) ---
            var world = FindFirstAsset<CrossHop.Gameplay.WorldTheme>();
            Wire(player, ("grid", grid), ("laneGenerator", generator), ("input", input));
            Wire(generator, ("grid", grid));
            Wire(camFollow, ("grid", grid), ("player", player));
            Wire(coinField, ("grid", grid), ("laneGenerator", generator), ("player", player));
            Wire(gm, ("laneGenerator", generator), ("player", player), ("cameraFollow", camFollow),
                     ("economy", economy), ("coinField", coinField), ("fallbackWorld", world));

            string worldNote = world != null
                ? $"Fallback world '{world.name}' wired automatically."
                : "No WorldTheme found — run Tools ▸ CrossHop ▸ Create Sample Content first, then re-run this.";
            Debug.Log("[CrossHop] Gray-box scene built. Assign a Lane prefab + MovingObstacle prefab, " +
                      $"then press Play and call GameManager.StartRun(). {worldNote}");
            Selection.activeGameObject = gmGo;
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
