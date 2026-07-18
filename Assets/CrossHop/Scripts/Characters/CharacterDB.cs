using System.Collections.Generic;
using UnityEngine;

namespace CrossHop.Characters
{
    /// <summary>
    /// The authored roster. A single asset holds every <see cref="CharacterData"/>,
    /// providing lookup by id and rarity-filtered queries for the gacha.
    /// </summary>
    [CreateAssetMenu(fileName = "CharacterDB", menuName = "CrossHop/Character DB")]
    public sealed class CharacterDB : ScriptableObject
    {
        [Tooltip("The full roster. Order here is the display order in the collection.")]
        [SerializeField] private List<CharacterData> characters = new();

        [Tooltip("Character granted at first launch so the player always has one.")]
        [SerializeField] private CharacterData defaultCharacter;

        private Dictionary<string, CharacterData> _byId;

        public IReadOnlyList<CharacterData> All => characters;
        public CharacterData DefaultCharacter => defaultCharacter;

        /// <summary>Resolve a character by its stable id, or null if unknown.</summary>
        public CharacterData GetById(string id)
        {
            BuildIndex();
            return id != null && _byId.TryGetValue(id, out CharacterData c) ? c : null;
        }

        /// <summary>All characters obtainable through the prize machine (coins, not IAP).</summary>
        public List<CharacterData> GetGachaPool()
        {
            var pool = new List<CharacterData>(characters.Count);
            foreach (CharacterData c in characters)
                if (c != null && !c.premiumOnly)
                    pool.Add(c);
            return pool;
        }

        private void BuildIndex()
        {
            if (_byId != null) return;
            _byId = new Dictionary<string, CharacterData>(characters.Count);
            foreach (CharacterData c in characters)
            {
                if (c == null || string.IsNullOrEmpty(c.id)) continue;
                if (!_byId.TryAdd(c.id, c))
                    Debug.LogError($"[CharacterDB] Duplicate character id '{c.id}'.", c);
            }
        }

        // Editor edits can change the list; drop the cache so it rebuilds lazily.
        private void OnValidate() => _byId = null;
    }
}
