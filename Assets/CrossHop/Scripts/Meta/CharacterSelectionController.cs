using System;
using System.Collections.Generic;
using CrossHop.Characters;
using CrossHop.Economy;
using UnityEngine;

namespace CrossHop.Meta
{
    /// <summary>
    /// Drives the collection / character-select user flow. It sits on top of the data
    /// layer (<see cref="EconomyManager"/> owns state, <see cref="CharacterDB"/> owns
    /// content, <see cref="GachaController"/> owns roll logic) and exposes the three
    /// player actions — <b>select</b>, <b>buy</b>, <b>roll</b> — as intent methods that
    /// raise events. The UI binds to those events and never touches save data directly.
    /// </summary>
    public sealed class CharacterSelectionController : MonoBehaviour
    {
        [SerializeField] private CharacterDB db;
        [SerializeField] private EconomyManager economy;

        [Header("Prize machine")]
        [Tooltip("Coin cost for one gacha roll.")]
        [SerializeField] private int rollCost = 100;
        [Tooltip("Coins refunded when a roll yields a character you already own.")]
        [SerializeField] private int duplicateRefund = 40;

        private GachaController _gacha;

        /// <summary>Fired when the equipped character changes.</summary>
        public event Action<CharacterData> OnSelectionChanged;
        /// <summary>Fired after a purchase or roll changes what the player owns.</summary>
        public event Action OnRosterChanged;
        /// <summary>Fired with the result of a prize-machine roll (new or duplicate).</summary>
        public event Action<GachaResult> OnRolled;

        public IReadOnlyList<CharacterData> Roster => db.All;
        public int Coins => economy.Coins;
        public int RollCost => rollCost;
        public bool CanRoll => economy.Coins >= rollCost && db.GetGachaPool().Count > 0;

        private void Awake() => _gacha = new GachaController(db);

        public bool IsOwned(CharacterData c) => c != null && economy.IsOwned(c.id);
        public bool IsSelected(CharacterData c) => c != null && economy.SelectedCharacter == c;

        /// <summary>Can the player buy this character right now (owned check + funds)?</summary>
        public bool CanBuy(CharacterData c)
            => c != null && !c.premiumOnly && !economy.IsOwned(c.id) && economy.Coins >= c.unlockCost;

        /// <summary>Equip an owned character. Returns false if it isn't owned.</summary>
        public bool Select(CharacterData c)
        {
            if (!economy.Select(c)) return false;
            OnSelectionChanged?.Invoke(c);
            return true;
        }

        /// <summary>Buy a character with coins, then equip it.</summary>
        public bool Buy(CharacterData c)
        {
            if (!CanBuy(c)) return false;
            if (!economy.TrySpend(c.unlockCost)) return false;

            economy.Unlock(c);
            economy.Select(c);
            OnRosterChanged?.Invoke();
            OnSelectionChanged?.Invoke(c);
            return true;
        }

        /// <summary>
        /// Spend coins on one prize-machine roll. New characters are unlocked; duplicates
        /// are refunded. Returns false (and rolls nothing) if the player can't afford it.
        /// </summary>
        public bool Roll(out GachaResult result)
        {
            result = default;
            if (!CanRoll || !economy.TrySpend(rollCost)) return false;

            result = _gacha.Roll(economy.OwnedIds);
            if (result.IsNew)
                economy.Unlock(result.Character);
            else
                economy.AddCoins(duplicateRefund);

            OnRolled?.Invoke(result);
            OnRosterChanged?.Invoke();
            return true;
        }
    }
}
