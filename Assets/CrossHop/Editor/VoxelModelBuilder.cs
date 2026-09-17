using System.Collections.Generic;
using System.IO;
using CrossHop.Gameplay;
using UnityEditor;
using UnityEngine;

namespace CrossHop.EditorTools
{
    /// <summary>
    /// Builds chunky Crossy-Road-style models out of cubes — four road vehicles
    /// (car, SUV, pickup, big truck), a log, a train, and a chick — as prefabs, then
    /// assigns a random *mix* of the vehicles to every road lane, a log to water and a
    /// train to rail, and dresses the scene's player as the chick.
    ///
    /// Colouring is done at runtime via MaterialPropertyBlock on Unity's default material
    /// (<see cref="VoxelTint"/> fixed parts, <see cref="RandomVoxelColor"/> per-spawn body),
    /// which avoids the magenta "missing shader" look and gives colourful, varied traffic.
    ///
    /// Re-run any time (overwrites prefabs in place). Menu: Tools ▸ CrossHop ▸ Build Voxel Models.
    /// </summary>
    public static class VoxelModelBuilder
    {
        private const string Dir = "Assets/CrossHop/Art/_Voxel";

        private static readonly Color Glass = new(0.55f, 0.8f, 0.95f);
        private static readonly Color WheelDark = new(0.12f, 0.12f, 0.14f);

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

            GameObject[] roadVehicles = { BuildCar(), BuildSUV(), BuildPickup(), BuildTruck() };
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
                    case LaneType.Road: Assign(def, roadVehicles, 3f, 5f, 1.0f, 2.4f); roads++; break;
                    case LaneType.Water: Assign(def, new[] { log }, 1.6f, 3f, 1.2f, 2.4f); waters++; break;
                    case LaneType.Rail: Assign(def, new[] { train }, 8f, 11f, 3.5f, 6f); rails++; break;
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

            Debug.Log($"[CrossHop] Voxel models built — {roads} roads get a mix of car/SUV/pickup/truck, " +
                      $"{waters} rivers get logs, {rails} rails get trains. {playerNote}");
            Selection.activeObject = roadVehicles[0];
        }

        // ---- Road vehicles ------------------------------------------------

        private static GameObject BuildCar()
        {
            var root = new GameObject("Voxel_Car");
            SetLength(root.AddComponent<MovingObstacle>(), 1.0f);

            GameObject chassis = Cube(root, "Chassis", new(0f, 0.22f, 0f), new(1.0f, 0.36f, 0.7f));
            GameObject cabin = Cube(root, "Cabin", new(0.02f, 0.5f, 0f), new(0.55f, 0.3f, 0.6f));
            GameObject window = Cube(root, "Window", new(0.03f, 0.5f, 0f), new(0.57f, 0.2f, 0.5f));
            var wheels = Wheels(root, 0.3f, 0.32f);

            AddRandomColor(root, R(chassis, cabin), CarPalette);
            AddTint(root, R(window), Glass);
            AddTint(root, wheels, WheelDark);
            return SaveAndDestroy(root, $"{Dir}/Voxel_Car.prefab");
        }

        private static GameObject BuildSUV()
        {
            var root = new GameObject("Voxel_SUV");
            SetLength(root.AddComponent<MovingObstacle>(), 1.2f);

            GameObject body = Cube(root, "Body", new(0f, 0.28f, 0f), new(1.15f, 0.5f, 0.74f));
            GameObject cabin = Cube(root, "Cabin", new(-0.05f, 0.66f, 0f), new(0.9f, 0.34f, 0.7f));
            GameObject window = Cube(root, "Window", new(-0.05f, 0.66f, 0f), new(0.92f, 0.24f, 0.58f));
            var wheels = Wheels(root, 0.36f, 0.34f);

            AddRandomColor(root, R(body, cabin), CarPalette);
            AddTint(root, R(window), Glass);
            AddTint(root, wheels, WheelDark);
            return SaveAndDestroy(root, $"{Dir}/Voxel_SUV.prefab");
        }

        private static GameObject BuildPickup()
        {
            var root = new GameObject("Voxel_Pickup");
            SetLength(root.AddComponent<MovingObstacle>(), 1.35f);

            GameObject cab = Cube(root, "Cab", new(0.42f, 0.4f, 0f), new(0.5f, 0.44f, 0.72f));
            GameObject cabWin = Cube(root, "CabWindow", new(0.42f, 0.52f, 0f), new(0.52f, 0.22f, 0.6f));
            GameObject bed = Cube(root, "Bed", new(-0.32f, 0.24f, 0f), new(0.85f, 0.18f, 0.72f));
            GameObject bedL = Cube(root, "BedWallL", new(-0.32f, 0.34f, 0.33f), new(0.85f, 0.2f, 0.06f));
            GameObject bedR = Cube(root, "BedWallR", new(-0.32f, 0.34f, -0.33f), new(0.85f, 0.2f, 0.06f));
            GameObject bedB = Cube(root, "BedWallBack", new(-0.73f, 0.34f, 0f), new(0.06f, 0.2f, 0.72f));
            var wheels = Wheels(root, 0.42f, 0.34f);

            AddRandomColor(root, R(cab, bed, bedL, bedR, bedB), CarPalette);
            AddTint(root, R(cabWin), Glass);
            AddTint(root, wheels, WheelDark);
            return SaveAndDestroy(root, $"{Dir}/Voxel_Pickup.prefab");
        }

        private static GameObject BuildTruck()
        {
            var root = new GameObject("Voxel_Truck");
            SetLength(root.AddComponent<MovingObstacle>(), 2.6f);

            GameObject cab = Cube(root, "Cab", new(1.05f, 0.42f, 0f), new(0.6f, 0.66f, 0.8f));
            GameObject cabWin = Cube(root, "CabWindow", new(1.08f, 0.58f, 0f), new(0.5f, 0.26f, 0.68f));
            GameObject trailer = Cube(root, "Trailer", new(-0.45f, 0.52f, 0f), new(1.9f, 0.82f, 0.82f));
            var wheels = new List<Renderer>();
            wheels.AddRange(Wheels(root, 1.0f, 0.36f, "F"));
            wheels.AddRange(Wheels(root, -0.2f, 0.36f, "M"));
            wheels.AddRange(Wheels(root, -1.1f, 0.36f, "B"));

            AddRandomColor(root, R(cab), CarPalette);             // colourful cab
            AddTint(root, R(trailer), new Color(0.9f, 0.9f, 0.92f)); // white box trailer
            AddTint(root, R(cabWin), Glass);
            AddTint(root, wheels.ToArray(), WheelDark);
            return SaveAndDestroy(root, $"{Dir}/Voxel_Truck.prefab");
        }

        // ---- Water / rail / player ---------------------------------------

        private static GameObject BuildLog()
        {
            var root = new GameObject("Voxel_Log");
            SetLength(root.AddComponent<MovingObstacle>(), 2f);

            GameObject trunk = Cube(root, "Trunk", new(0f, 0.16f, 0f), new(2.0f, 0.34f, 0.7f));
            GameObject ringL = Cube(root, "RingL", new(-1.0f, 0.16f, 0f), new(0.08f, 0.36f, 0.72f));
            GameObject ringR = Cube(root, "RingR", new(1.0f, 0.16f, 0f), new(0.08f, 0.36f, 0.72f));
            GameObject g1 = Cube(root, "Grain1", new(0f, 0.335f, 0.16f), new(1.9f, 0.05f, 0.08f));
            GameObject g2 = Cube(root, "Grain2", new(0f, 0.335f, -0.02f), new(1.9f, 0.05f, 0.06f));
            GameObject g3 = Cube(root, "Grain3", new(0f, 0.335f, -0.18f), new(1.9f, 0.05f, 0.08f));

            AddRandomColor(root, R(trunk), LogPalette);
            AddTint(root, R(ringL, ringR), new Color(0.68f, 0.52f, 0.34f));
            AddTint(root, R(g1, g2, g3), new Color(0.28f, 0.19f, 0.11f));
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

            AddTint(root, R(bodyG, headG), new Color(1f, 0.83f, 0.25f));
            AddTint(root, R(combG), new Color(0.86f, 0.24f, 0.22f));
            AddTint(root, R(beakG, footL, footR), new Color(0.95f, 0.55f, 0.12f));
            AddTint(root, R(eyeL, eyeR), new Color(0.08f, 0.08f, 0.08f));
            return SaveAndDestroy(root, $"{Dir}/Voxel_Chick.prefab");
        }

        // ---- Helpers ------------------------------------------------------

        private static Renderer[] Wheels(GameObject root, float x, float z, string tag = "")
        {
            return R(
                Cube(root, $"Wheel{tag}FL", new(x, 0.09f, z), new(0.26f, 0.26f, 0.14f)),
                Cube(root, $"Wheel{tag}FR", new(x, 0.09f, -z), new(0.26f, 0.26f, 0.14f)),
                Cube(root, $"Wheel{tag}BL", new(-x, 0.09f, z), new(0.26f, 0.26f, 0.14f)),
                Cube(root, $"Wheel{tag}BR", new(-x, 0.09f, -z), new(0.26f, 0.26f, 0.14f)));
        }

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

        private static void Assign(LaneDefinition def, GameObject[] variants,
                                   float minSpeed, float maxSpeed, float minInterval, float maxInterval)
        {
            def.obstacleVariants = variants;
            def.obstaclePrefab = variants.Length > 0 ? variants[0] : null;
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
