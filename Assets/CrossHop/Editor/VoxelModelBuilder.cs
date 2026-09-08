using System.Collections.Generic;
using System.IO;
using CrossHop.Gameplay;
using UnityEditor;
using UnityEngine;

namespace CrossHop.EditorTools
{
    /// <summary>
    /// Builds chunky Crossy-Road-style models out of cubes — cars, logs, a train, and a
    /// chick — as prefabs, then assigns them into every world's lanes by type
    /// (road→car, water→log, rail→train) and dresses the scene's player as the chick.
    ///
    /// Colouring is done at runtime via MaterialPropertyBlock on Unity's default material
    /// (<see cref="VoxelTint"/> for fixed parts, <see cref="RandomVoxelColor"/> for the
    /// per-spawn body colour). We never assign a custom material — that's what avoids the
    /// magenta "missing shader" look and gives a road of differently-coloured cars.
    ///
    /// Re-run any time (overwrites the prefabs in place). Menu: Tools ▸ CrossHop ▸ Build Voxel Models.
    /// </summary>
    public static class VoxelModelBuilder
    {
        private const string Dir = "Assets/CrossHop/Art/_Voxel";

        private static readonly Color[] CarPalette =
        {
            new(0.85f, 0.27f, 0.24f), new(0.25f, 0.5f, 0.85f), new(0.95f, 0.78f, 0.2f),
            new(0.36f, 0.75f, 0.32f), new(0.92f, 0.92f, 0.95f), new(0.91f, 0.53f, 0.24f),
            new(0.61f, 0.42f, 0.85f), new(0.2f, 0.69f, 0.65f),
        };
        private static readonly Color[] LogPalette =
        {
            new(0.42f, 0.29f, 0.17f), new(0.5f, 0.36f, 0.22f),
            new(0.36f, 0.24f, 0.14f), new(0.55f, 0.4f, 0.26f),
        };
        private static readonly Color[] TrainPalette =
        {
            new(0.25f, 0.5f, 0.85f), new(0.36f, 0.7f, 0.4f),
            new(0.78f, 0.27f, 0.27f), new(0.6f, 0.62f, 0.68f),
        };

        [MenuItem("Tools/CrossHop/Build Voxel Models")]
        public static void Build()
        {
            EnsureFolder(Dir);

            GameObject car = BuildCar();
            GameObject log = BuildLog();
            GameObject train = BuildTrain();
            GameObject chick = BuildChick();

            int roads = 0, waters = 0, rails = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:LaneDefinition"))
            {
                var def = AssetDatabase.LoadAssetAtPath<LaneDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                if (def == null) continue;
                switch (def.type)
                {
                    case LaneType.Road: Assign(def, car, 3f, 5f, 1.0f, 2.2f); roads++; break;
                    case LaneType.Water: Assign(def, log, 1.6f, 3f, 1.2f, 2.4f); waters++; break;
                    case LaneType.Rail: Assign(def, train, 8f, 11f, 3.5f, 6f); rails++; break;
                }
            }
            AssetDatabase.SaveAssets();

            var player = Object.FindFirstObjectByType<PlayerController>();
            string playerNote;
            if (player != null)
            {
                var so = new SerializedObject(player);
                so.FindProperty("characterModelPrefab").objectReferenceValue = chick;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(player);
                playerNote = "Player is the chick (save the scene: Cmd+S).";
            }
            else
            {
                playerNote = "No Player in the open scene — assign Chick to PlayerController.characterModelPrefab yourself.";
            }

            Debug.Log($"[CrossHop] Voxel models built — colourful cars on {roads} roads, logs on {waters} " +
                      $"rivers, trains on {rails} rails. Colours applied at runtime (no magenta). {playerNote}");
            Selection.activeObject = car;
        }

        // ---- Models -------------------------------------------------------

        private static GameObject BuildCar()
        {
            var root = new GameObject("Voxel_Car");
            SetLength(root.AddComponent<MovingObstacle>(), 1.1f);

            GameObject chassis = Cube(root, "Chassis", new(0f, 0.22f, 0f), new(1.15f, 0.4f, 0.7f));
            GameObject cabin = Cube(root, "Cabin", new(0.05f, 0.52f, 0f), new(0.62f, 0.34f, 0.62f));
            GameObject window = Cube(root, "Window", new(0.06f, 0.52f, 0f), new(0.64f, 0.22f, 0.5f));
            GameObject wFL = Cube(root, "WheelFL", new(0.38f, 0.08f, 0.32f), new(0.26f, 0.26f, 0.14f));
            GameObject wFR = Cube(root, "WheelFR", new(0.38f, 0.08f, -0.32f), new(0.26f, 0.26f, 0.14f));
            GameObject wBL = Cube(root, "WheelBL", new(-0.38f, 0.08f, 0.32f), new(0.26f, 0.26f, 0.14f));
            GameObject wBR = Cube(root, "WheelBR", new(-0.38f, 0.08f, -0.32f), new(0.26f, 0.26f, 0.14f));

            AddRandomColor(root, R(chassis, cabin), CarPalette);
            AddTint(root, R(window), new Color(0.55f, 0.8f, 0.95f));
            AddTint(root, R(wFL, wFR, wBL, wBR), new Color(0.12f, 0.12f, 0.14f));
            return SaveAndDestroy(root, $"{Dir}/Voxel_Car.prefab");
        }

        private static GameObject BuildLog()
        {
            var root = new GameObject("Voxel_Log");
            SetLength(root.AddComponent<MovingObstacle>(), 2f);

            GameObject trunk = Cube(root, "Trunk", new(0f, 0.16f, 0f), new(2.0f, 0.34f, 0.7f));
            GameObject ringL = Cube(root, "RingL", new(-1.0f, 0.16f, 0f), new(0.08f, 0.36f, 0.72f));
            GameObject ringR = Cube(root, "RingR", new(1.0f, 0.16f, 0f), new(0.08f, 0.36f, 0.72f));

            AddRandomColor(root, R(trunk), LogPalette);
            AddTint(root, R(ringL, ringR), new Color(0.68f, 0.52f, 0.34f));
            return SaveAndDestroy(root, $"{Dir}/Voxel_Log.prefab");
        }

        private static GameObject BuildTrain()
        {
            var root = new GameObject("Voxel_Train");
            SetLength(root.AddComponent<MovingObstacle>(), 5f);

            GameObject body = Cube(root, "Body", new(0f, 0.45f, 0f), new(4.9f, 0.85f, 0.8f));
            GameObject front = Cube(root, "Front", new(2.4f, 0.45f, 0f), new(0.25f, 0.85f, 0.82f));
            var windows = new List<GameObject>();
            for (int i = 0; i < 5; i++)
            {
                float x = -1.8f + i * 0.9f;
                windows.Add(Cube(root, $"Win{i}a", new(x, 0.55f, 0.42f), new(0.4f, 0.3f, 0.06f)));
                windows.Add(Cube(root, $"Win{i}b", new(x, 0.55f, -0.42f), new(0.4f, 0.3f, 0.06f)));
            }

            AddRandomColor(root, R(body), TrainPalette);
            AddTint(root, R(front), new Color(0.92f, 0.92f, 0.95f));
            AddTint(root, R(windows.ToArray()), new Color(1f, 0.9f, 0.5f));
            return SaveAndDestroy(root, $"{Dir}/Voxel_Train.prefab");
        }

        private static GameObject BuildChick()
        {
            var root = new GameObject("Voxel_Chick");
            GameObject bodyG = Cube(root, "Body", new(0f, 0.30f, 0f), new(0.5f, 0.45f, 0.5f));
            GameObject headG = Cube(root, "Head", new(0f, 0.64f, 0.02f), new(0.42f, 0.4f, 0.42f));
            GameObject combG = Cube(root, "Comb", new(0f, 0.88f, 0f), new(0.12f, 0.16f, 0.3f));
            GameObject beakG = Cube(root, "Beak", new(0f, 0.6f, 0.27f), new(0.16f, 0.12f, 0.16f));
            GameObject eyeL = Cube(root, "EyeL", new(-0.12f, 0.7f, 0.21f), new(0.08f, 0.08f, 0.06f));
            GameObject eyeR = Cube(root, "EyeR", new(0.12f, 0.7f, 0.21f), new(0.08f, 0.08f, 0.06f));
            GameObject footL = Cube(root, "FootL", new(-0.12f, 0.04f, 0.02f), new(0.1f, 0.08f, 0.22f));
            GameObject footR = Cube(root, "FootR", new(0.12f, 0.04f, 0.02f), new(0.1f, 0.08f, 0.22f));

            AddTint(root, R(bodyG, headG), new Color(1f, 0.83f, 0.25f));   // yellow
            AddTint(root, R(combG), new Color(0.86f, 0.24f, 0.22f));       // red comb
            AddTint(root, R(beakG, footL, footR), new Color(0.95f, 0.55f, 0.12f)); // orange
            AddTint(root, R(eyeL, eyeR), new Color(0.08f, 0.08f, 0.08f));  // black eyes
            return SaveAndDestroy(root, $"{Dir}/Voxel_Chick.prefab");
        }

        // ---- Helpers ------------------------------------------------------

        private static Renderer[] R(params GameObject[] parts)
        {
            var list = new List<Renderer>(parts.Length);
            foreach (GameObject g in parts)
            {
                var r = g.GetComponent<Renderer>();
                if (r != null) list.Add(r);
            }
            return list.ToArray();
        }

        private static void Assign(LaneDefinition def, GameObject prefab,
                                   float minSpeed, float maxSpeed, float minInterval, float maxInterval)
        {
            def.obstaclePrefab = prefab;
            def.minSpeed = minSpeed;
            def.maxSpeed = maxSpeed;
            def.minSpawnInterval = minInterval;
            def.maxSpawnInterval = maxInterval;
            EditorUtility.SetDirty(def);
        }

        private static void SetLength(MovingObstacle obs, float length)
        {
            var so = new SerializedObject(obs);
            so.FindProperty("lengthCells").floatValue = length;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AddRandomColor(GameObject root, Renderer[] targets, Color[] palette)
        {
            var comp = root.AddComponent<RandomVoxelColor>();
            var so = new SerializedObject(comp);
            SerializedProperty t = so.FindProperty("targets");
            t.arraySize = targets.Length;
            for (int i = 0; i < targets.Length; i++)
                t.GetArrayElementAtIndex(i).objectReferenceValue = targets[i];
            SerializedProperty p = so.FindProperty("palette");
            p.arraySize = palette.Length;
            for (int i = 0; i < palette.Length; i++)
                p.GetArrayElementAtIndex(i).colorValue = palette[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AddTint(GameObject root, Renderer[] targets, Color color)
        {
            var comp = root.AddComponent<VoxelTint>();
            var so = new SerializedObject(comp);
            SerializedProperty t = so.FindProperty("targets");
            t.arraySize = targets.Length;
            for (int i = 0; i < targets.Length; i++)
                t.GetArrayElementAtIndex(i).objectReferenceValue = targets[i];
            so.FindProperty("color").colorValue = color;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static GameObject Cube(GameObject parent, string name, Vector3 pos, Vector3 scale)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube); // keeps Unity's default (valid) material
            go.name = name;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent.transform, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            return go;
        }

        private static GameObject SaveAndDestroy(GameObject go, string path)
        {
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab;
        }

        private static void EnsureFolder(string folder)
        {
            if (!AssetDatabase.IsValidFolder(folder))
            {
                Directory.CreateDirectory(folder);
                AssetDatabase.Refresh();
            }
        }
    }
}
