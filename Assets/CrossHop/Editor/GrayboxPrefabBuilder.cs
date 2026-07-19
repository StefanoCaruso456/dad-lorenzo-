using CrossHop.Gameplay;
using UnityEditor;
using UnityEngine;

namespace CrossHop.EditorTools
{
    /// <summary>
    /// Generates the primitive prefabs the gray-box needs — a lane strip, a moving
    /// obstacle, and a coin — and assigns the obstacle to every hazard LaneDefinition so
    /// traffic actually spawns. Lets you press Play without hand-building prefabs.
    ///
    /// Menu: Tools ▸ CrossHop ▸ Build Gray-box Prefabs.
    /// </summary>
    public static class GrayboxPrefabBuilder
    {
        private const string Dir = "Assets/CrossHop/Art/_Graybox";

        [MenuItem("Tools/CrossHop/Build Gray-box Prefabs")]
        public static void BuildAndAssign()
        {
            Lane lane = EnsureLanePrefab();
            MovingObstacle obstacle = EnsureObstaclePrefab();
            Coin coin = EnsureCoinPrefab();

            // Give every hazard lane an obstacle so roads/rails/rivers aren't empty.
            int assigned = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:LaneDefinition"))
            {
                var def = AssetDatabase.LoadAssetAtPath<LaneDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                if (def == null || def.type == LaneType.Safe) continue;
                if (def.obstaclePrefab == null)
                {
                    var so = new SerializedObject(def);
                    so.FindProperty("obstaclePrefab").objectReferenceValue = obstacle.gameObject;
                    so.ApplyModifiedPropertiesWithoutUndo();
                    assigned++;
                }
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[CrossHop] Gray-box prefabs ready (lane, obstacle, coin). " +
                      $"Assigned the obstacle to {assigned} hazard lane(s). " +
                      "Assign the lane prefab to LaneGenerator and the coin to CoinField " +
                      "(the scene builder does this for you).");
            _ = (lane, coin);
        }

        // ---- Prefab factories (public so the scene builder can reuse them) ----

        public static Lane EnsureLanePrefab()
        {
            string path = $"{Dir}/GrayboxLane.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<Lane>(path);
            if (existing != null) return existing;

            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "GrayboxLane";
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.localScale = new Vector3(9f, 0.1f, 1f); // restyled per-grid at Init

            var lane = go.AddComponent<Lane>();
            var so = new SerializedObject(lane);
            so.FindProperty("bodyRenderer").objectReferenceValue = go.GetComponent<MeshRenderer>();
            so.ApplyModifiedPropertiesWithoutUndo();

            Lane result = SaveAndDestroy(go, path).GetComponent<Lane>();
            return result;
        }

        public static MovingObstacle EnsureObstaclePrefab()
        {
            string path = $"{Dir}/GrayboxObstacle.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<MovingObstacle>(path);
            if (existing != null) return existing;

            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "GrayboxObstacle";
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.localScale = new Vector3(0.9f, 0.6f, 0.9f);
            Tint(go, new Color(0.85f, 0.30f, 0.25f));
            go.AddComponent<MovingObstacle>();

            return SaveAndDestroy(go, path).GetComponent<MovingObstacle>();
        }

        public static Coin EnsureCoinPrefab()
        {
            string path = $"{Dir}/GrayboxCoin.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<Coin>(path);
            if (existing != null) return existing;

            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = "GrayboxCoin";
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.localScale = new Vector3(0.35f, 0.04f, 0.35f); // a flat disc
            Tint(go, new Color(1f, 0.81f, 0.27f));
            go.AddComponent<Coin>();

            return SaveAndDestroy(go, path).GetComponent<Coin>();
        }

        // ---- Helpers ------------------------------------------------------

        private static GameObject SaveAndDestroy(GameObject go, string path)
        {
            if (!AssetDatabase.IsValidFolder(Dir))
                System.IO.Directory.CreateDirectory(Dir);
            AssetDatabase.Refresh();
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab;
        }

        private static void Tint(GameObject go, Color color)
        {
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"))
            {
                color = color
            };
            mat.SetColor("_BaseColor", color);
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        }
    }
}
