using System.Collections.Generic;
using CrossHop.Characters;
using UnityEngine;

namespace CrossHop.Meta
{
    /// <summary>Outcome of a single prize-machine roll.</summary>
    public readonly struct GachaResult
    {
        public readonly CharacterData Character;
        public readonly bool IsNew;

        public GachaResult(CharacterData character, bool isNew)
        {
            Character = character;
            IsNew = isNew;
        }
    }

    /// <summary>
    /// Rarity-weighted prize machine. Pure logic (no MonoBehaviour) so it is easy
    /// to unit-test. Weights are data; duplicates are allowed and reported so the
    /// meta layer can convert them to a coin refund.
    /// </summary>
    public sealed class GachaController
    {
        private readonly CharacterDB _db;
        private readonly System.Func<Rarity, int> _weightOf;

        /// <param name="db">Roster to draw from.</param>
        /// <param name="weightOf">Relative weight per rarity. Higher = more likely.</param>
        public GachaController(CharacterDB db, System.Func<Rarity, int> weightOf = null)
        {
            _db = db != null ? db : throw new System.ArgumentNullException(nameof(db));
            _weightOf = weightOf ?? DefaultWeight;
        }

        /// <summary>Roll one character, biased by rarity weight.</summary>
        /// <param name="ownedIds">Set of already-owned ids to flag duplicates.</param>
        public GachaResult Roll(IReadOnlyCollection<string> ownedIds)
        {
            List<CharacterData> pool = _db.GetGachaPool();
            if (pool.Count == 0)
                throw new System.InvalidOperationException("Gacha pool is empty.");

            int total = 0;
            foreach (CharacterData c in pool) total += Mathf.Max(1, _weightOf(c.rarity));

            int pick = Random.Range(0, total);
            foreach (CharacterData c in pool)
            {
                pick -= Mathf.Max(1, _weightOf(c.rarity));
                if (pick < 0)
                {
                    bool isNew = ownedIds == null || !ownedIds.Contains(c.id);
                    return new GachaResult(c, isNew);
                }
            }

            // Unreachable given the weight sum, but keep the compiler & safety happy.
            CharacterData fallback = pool[pool.Count - 1];
            return new GachaResult(fallback, ownedIds == null || !ownedIds.Contains(fallback.id));
        }

        private static int DefaultWeight(Rarity rarity) => rarity switch
        {
            Rarity.Common => 100,
            Rarity.Rare => 30,
            Rarity.Epic => 8,
            Rarity.Legendary => 2,
            _ => 1
        };
    }
}
