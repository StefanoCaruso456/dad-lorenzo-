using System.Collections.Generic;
using System.IO;
using CrossHop.Characters;
using CrossHop.Characters.Abilities;
using CrossHop.Gameplay;
using UnityEditor;
using UnityEngine;

namespace CrossHop.EditorTools
{
    /// <summary>
    /// Generates the full 20-character roster from the design bible: for each character
    /// a <see cref="WorldTheme"/> (with its four <see cref="LaneDefinition"/>s), a
    /// <see cref="CharacterData"/> wired to it, and a populated <see cref="CharacterDB"/>.
    /// Ids, rarities, unlock costs and accent colours are set here so they're consistent;
    /// model/thumbnail/lane-prefab slots are left empty for art to drop into.
    ///
    /// Idempotent — re-running updates existing assets rather than duplicating them.
    /// Menu: Tools ▸ CrossHop ▸ Build Full Roster (20 characters).
    /// </summary>
    public static class RosterBuilder
    {
        private const string Root = "Assets/CrossHop";

        // id, name, worldId, worldName, worldFolder, rarity, accent hex, premiumOnly, abilityNote
        private readonly struct Entry
        {
            public readonly string Id, Name, WorldId, WorldName, Folder, Accent, AbilityNote;
            public readonly Rarity Rarity;
            public readonly bool Premium;

            public Entry(string id, string name, string worldId, string worldName, string folder,
                         Rarity rarity, string accent, bool premium = false, string abilityNote = null)
            {
                Id = id; Name = name; WorldId = worldId; WorldName = worldName; Folder = folder;
                Rarity = rarity; Accent = accent; Premium = premium; AbilityNote = abilityNote;
            }
        }

        private static readonly Entry[] Roster =
        {
            // ---- Current roster ----
            new("cluck",  "Cluck",  "farmland",     "Farmland Road",     "Farmland",         Rarity.Common, "#d9534a"),
            new("croco",  "Croco",  "bayou",        "Bayou Delta",       "Bayou",            Rarity.Rare,   "#4faa4f"),
            new("zippy",  "Zippy",  "cosmic",       "Cosmic Highway",    "CosmicHighway",    Rarity.Epic,   "#6ee0a0"),
            new("fuzz",   "Fuzz",   "monsterville", "Monsterville",      "Monsterville",     Rarity.Epic,   "#9b7bef"),
            new("piggy",  "Piggy",  "market",       "Mud & Market",      "MudAndMarket",     Rarity.Common, "#e07a9c"),
            new("byte",   "Byte",   "factory",      "The Factory",       "Factory",          Rarity.Rare,   "#3fc2dc"),
            new("shelly", "Shelly", "lazyriver",    "Lazy River",        "LazyRiver",        Rarity.Common, "#4faf8a"),
            new("waddle", "Waddle", "harbor",       "Frozen Harbor",     "FrozenHarbor",     Rarity.Rare,   "#4f8fc9"),
            new("quack",  "Quack",  "citypark",     "City Park",         "CityPark",         Rarity.Common, "#eabf30"),
            new("blocko", "Blocko", "construction", "Construction Site", "ConstructionSite", Rarity.Common, "#e0863c"),
            // ---- New arrivals ----
            new("finn",   "Finn",   "coralcoast",   "Coral Coast",       "CoralCoast",       Rarity.Rare,      "#29c2d6"),
            new("eileen", "Eileen", "moon",         "The Moon",          "TheMoon",          Rarity.Legendary, "#9b8cff", false, "LowGravity"),
            new("blaze",  "Blaze",  "emberridge",   "Ember Ridge",       "EmberRidge",       Rarity.Epic,      "#ff6a3d", false, "Fireproof"),
            new("frost",  "Frost",  "glacierpass",  "Glacier Pass",      "GlacierPass",      Rarity.Rare,      "#5bb8f5"),
            new("sandy",  "Sandy",  "dunesea",      "Dune Sea",          "DuneSea",          Rarity.Common,    "#e8a53a"),
            new("rex",    "Rex",    "tarpit",       "Tar-Pit Jungle",    "TarPitJungle",     Rarity.Epic,      "#7bb74a"),
            new("wisp",   "Wisp",   "hollowhill",   "Hollow Hill",       "HollowHill",       Rarity.Epic,      "#7ce0b8"),
            new("sprout", "Sprout", "rainforest",   "Rainforest Pond",   "RainforestPond",   Rarity.Common,    "#7bc23f"),
            new("pixel",  "Pixel",  "neongrid",     "Neon Grid",         "NeonGrid",         Rarity.Legendary, "#ff4fd8", true,  "CoinMagnet"),
            new("coco",   "Coco",   "temple",       "Temple Ruins",      "TempleRuins",      Rarity.Rare,      "#35b083"),
        };

        [MenuItem("Tools/CrossHop/Build Full Roster (20 characters)")]
        public static void Build()
        {
            if (!EditorUtility.DisplayDialog("Build Full Roster",
                    $"Generate {Roster.Length} worlds + characters (art slots left empty). " +
                    "Existing assets are updated, not duplicated. Continue?",
                    "Build", "Cancel"))
                return;

            EnsureFolder($"{Root}/Settings");
            EnsureFolder($"{Root}/Art/Worlds");
            EnsureFolder($"{Root}/Art/Characters");

            DifficultyCurve difficulty = CreateOrLoad<DifficultyCurve>($"{Root}/Settings/Difficulty_Default.asset");
            var abilitiesAssigned = new List<string>();

            AssetDatabase.StartAssetEditing();
            var characters = new List<CharacterData>(Roster.Length);
            try
            {
                foreach (Entry e in Roster)
                {
                    string worldDir = $"{Root}/Art/Worlds/{e.Folder}";
                    EnsureFolder(worldDir);

                    LaneDefinition safe = MakeLane(worldDir, e.WorldId, "Safe", LaneType.Safe);
                    LaneDefinition road = MakeLane(worldDir, e.WorldId, "Road", LaneType.Road);
                    LaneDefinition water = MakeLane(worldDir, e.WorldId, "Water", LaneType.Water);
                    LaneDefinition rail = MakeLane(worldDir, e.WorldId, "Rail", LaneType.Rail);

                    var world = CreateOrLoad<WorldTheme>($"{worldDir}/World_{e.Folder}.asset");
                    world.id = e.WorldId;
                    world.displayName = e.WorldName;
                    world.safeLane = safe;
                    world.hazardLanes = new[] { road, water, rail };
                    world.difficulty = difficulty;
                    world.uiTint = ParseColor(e.Accent);
                    EditorUtility.SetDirty(world);

                    var character = CreateOrLoad<CharacterData>($"{Root}/Art/Characters/{e.Name}.asset");
                    character.id = e.Id;
                    character.displayName = e.Name;
                    character.rarity = e.Rarity;
                    character.premiumOnly = e.Premium;
                    character.unlockCost = CostFor(e);
                    character.defaultWorld = world;
                    if (!string.IsNullOrEmpty(e.AbilityNote))
                    {
                        character.ability = CreateAbility(e.AbilityNote);
                        abilitiesAssigned.Add($"{e.Name} → {e.AbilityNote}");
                    }
                    EditorUtility.SetDirty(character);

                    characters.Add(character);
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }

            // Roster DB — Cluck is the free starter / default.
            var db = CreateOrLoad<CharacterDB>($"{Root}/Art/Characters/CharacterDB.asset");
            var so = new SerializedObject(db);
            SerializedProperty list = so.FindProperty("characters");
            list.ClearArray();
            for (int i = 0; i < characters.Count; i++)
            {
                list.InsertArrayElementAtIndex(i);
                list.GetArrayElementAtIndex(i).objectReferenceValue = characters[i];
            }
            so.FindProperty("defaultCharacter").objectReferenceValue = characters[0]; // Cluck
            so.ApplyModifiedPropertiesWithoutUndo();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeObject = db;

            Debug.Log($"[CrossHop] Roster built: {characters.Count} characters + worlds. " +
                      "Next: drop voxel prefabs on each CharacterData.modelPrefab, assign lane " +
                      "ground/obstacle prefabs, then run Bake Character Icons.\n" +
                      $"Abilities assigned: {string.Join(", ", abilitiesAssigned)}.");
        }

        private static CharacterAbility CreateAbility(string kind)
        {
            string dir = $"{Root}/Art/Characters/Abilities";
            EnsureFolder(dir);
            switch (kind)
            {
                case "LowGravity":
                    var lg = CreateOrLoad<LowGravityAbility>($"{dir}/LowGravity.asset");
                    lg.description = "Moon-jump: a floatier, higher hop.";
                    EditorUtility.SetDirty(lg);
                    return lg;
                case "Fireproof":
                    var fp = CreateOrLoad<FireproofAbility>($"{dir}/Fireproof.asset");
                    fp.description = "Survive the first fatal hazard each run.";
                    EditorUtility.SetDirty(fp);
                    return fp;
                case "CoinMagnet":
                    var cm = CreateOrLoad<CoinMagnetAbility>($"{dir}/CoinMagnet.asset");
                    cm.description = "Pulls nearby coins toward you.";
                    EditorUtility.SetDirty(cm);
                    return cm;
                default:
                    Debug.LogWarning($"[CrossHop] Unknown ability kind '{kind}'.");
                    return null;
            }
        }

        private static int CostFor(Entry e)
        {
            if (e.Id == "cluck") return 0;      // free starter
            if (e.Premium) return 0;            // IAP-only, no coin price
            return e.Rarity switch
            {
                Rarity.Common => 300,
                Rarity.Rare => 1200,
                Rarity.Epic => 3500,
                Rarity.Legendary => 8000,
                _ => 500
            };
        }

        private static LaneDefinition MakeLane(string dir, string worldId, string suffix, LaneType type)
        {
            var lane = CreateOrLoad<LaneDefinition>($"{dir}/{worldId}_{suffix}.asset");
            lane.type = type;
            EditorUtility.SetDirty(lane);
            return lane;
        }

        private static Color ParseColor(string hex)
            => ColorUtility.TryParseHtmlString(hex, out Color c) ? c : Color.white;

        private static T CreateOrLoad<T>(string path) where T : ScriptableObject
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null) return existing;
            var asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
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
