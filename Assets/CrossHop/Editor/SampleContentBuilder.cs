using System.IO;
using CrossHop.Characters;
using CrossHop.Gameplay;
using UnityEditor;
using UnityEngine;

namespace CrossHop.EditorTools
{
    /// <summary>
    /// Generates one correct, fully-wired content chain — the <b>Farmland</b> world and
    /// the <b>Cluck</b> starter character — so there's a working reference to clone for
    /// the other 19. Everything is created through the API (never hand-authored YAML),
    /// so script references resolve correctly. Prefab/material slots are left empty for
    /// the artist to fill.
    ///
    /// Menu: Tools ▸ CrossHop ▸ Create Sample Content (Farmland + Cluck).
    /// </summary>
    public static class SampleContentBuilder
    {
        private const string Root = "Assets/CrossHop";

        [MenuItem("Tools/CrossHop/Create Sample Content (Farmland + Cluck)")]
        public static void Build()
        {
            EnsureFolders(
                $"{Root}/Settings",
                $"{Root}/Art/Worlds/Farmland",
                $"{Root}/Art/Characters");

            DifficultyCurve difficulty = CreateOrLoad<DifficultyCurve>($"{Root}/Settings/Difficulty_Default.asset");

            LaneDefinition safe = MakeLane("Lane_Safe_Grass", LaneType.Safe);
            LaneDefinition road = MakeLane("Lane_Road_Farm", LaneType.Road);
            LaneDefinition water = MakeLane("Lane_Water_Irrigation", LaneType.Water);
            LaneDefinition rail = MakeLane("Lane_Rail_Freight", LaneType.Rail);

            var world = CreateOrLoad<WorldTheme>($"{Root}/Art/Worlds/Farmland/World_Farmland.asset");
            world.id = "farmland";
            world.displayName = "Farmland Road";
            world.safeLane = safe;
            world.hazardLanes = new[] { road, water, rail };
            world.difficulty = difficulty;
            world.uiTint = new Color(0.85f, 0.32f, 0.29f); // rooster red
            EditorUtility.SetDirty(world);

            var cluck = CreateOrLoad<CharacterData>($"{Root}/Art/Characters/Cluck.asset");
            cluck.id = "cluck";
            cluck.displayName = "Cluck";
            cluck.rarity = Rarity.Common;
            cluck.unlockCost = 0;
            cluck.defaultWorld = world;
            EditorUtility.SetDirty(cluck);

            var db = CreateOrLoad<CharacterDB>($"{Root}/Art/Characters/CharacterDB.asset");
            var so = new SerializedObject(db);
            SerializedProperty list = so.FindProperty("characters");
            bool present = false;
            for (int i = 0; i < list.arraySize; i++)
                if (list.GetArrayElementAtIndex(i).objectReferenceValue == cluck) present = true;
            if (!present)
            {
                list.InsertArrayElementAtIndex(list.arraySize);
                list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = cluck;
            }
            so.FindProperty("defaultCharacter").objectReferenceValue = cluck;
            so.ApplyModifiedPropertiesWithoutUndo();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeObject = world;
            Debug.Log("[CrossHop] Sample content created: Farmland world + Cluck. " +
                      "Assign ground/obstacle prefabs on the four LaneDefinitions, then set " +
                      "GameManager.fallbackWorld to World_Farmland and press Play.");

            LaneDefinition MakeLane(string name, LaneType type)
            {
                var lane = CreateOrLoad<LaneDefinition>($"{Root}/Art/Worlds/Farmland/{name}.asset");
                lane.type = type;
                EditorUtility.SetDirty(lane);
                return lane;
            }
        }

        private static T CreateOrLoad<T>(string path) where T : ScriptableObject
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null) return existing;
            var asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static void EnsureFolders(params string[] folders)
        {
            foreach (string folder in folders)
                if (!AssetDatabase.IsValidFolder(folder))
                    Directory.CreateDirectory(folder);
            AssetDatabase.Refresh();
        }
    }
}
