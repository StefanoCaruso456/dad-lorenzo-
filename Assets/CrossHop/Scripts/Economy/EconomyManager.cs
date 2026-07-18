using System;
using System.Collections.Generic;
using CrossHop.Characters;
using UnityEngine;

namespace CrossHop.Economy
{
    /// <summary>
    /// Owns the player's persistent wallet, collection and best score. All meta-progress
    /// reads and writes route through here; it saves lazily and raises events so UI
    /// never polls. Backed by <see cref="SaveSystem"/> (local JSON, no backend).
    /// </summary>
    public sealed class EconomyManager : MonoBehaviour
    {
        [SerializeField] private CharacterDB characterDB;

        private SaveData _data;
        private HashSet<string> _owned;

        public event Action<int> OnCoinsChanged;
        public event Action<int> OnBestScoreChanged;
        public event Action<CharacterData> OnCharacterSelected;
        public event Action<CharacterData> OnCharacterUnlocked;

        public int Coins => _data.coins;
        public int BestScore => _data.bestScore;
        public IReadOnlyCollection<string> OwnedIds => _owned;

        private void Awake()
        {
            _data = SaveSystem.Load();
            _owned = new HashSet<string>(_data.ownedCharacterIds);
            EnsureDefaultCharacter();
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused) Flush();
        }

        private void OnApplicationQuit() => Flush();

        // ---- Wallet -------------------------------------------------------

        public void AddCoins(int amount)
        {
            if (amount <= 0) return;
            _data.coins += amount;
            OnCoinsChanged?.Invoke(_data.coins);
        }

        public bool TrySpend(int amount)
        {
            if (amount < 0 || _data.coins < amount) return false;
            _data.coins -= amount;
            OnCoinsChanged?.Invoke(_data.coins);
            Flush();
            return true;
        }

        // ---- Score --------------------------------------------------------

        /// <summary>Records a finished run's score; returns true if it's a new best.</summary>
        public bool ReportScore(int score)
        {
            if (score <= _data.bestScore) return false;
            _data.bestScore = score;
            OnBestScoreChanged?.Invoke(_data.bestScore);
            Flush();
            return true;
        }

        // ---- Collection ---------------------------------------------------

        public bool IsOwned(string characterId) => _owned.Contains(characterId);

        public CharacterData SelectedCharacter
            => characterDB != null ? characterDB.GetById(_data.selectedCharacterId) : null;

        /// <summary>Grant a character (from gacha or IAP). Returns false if already owned.</summary>
        public bool Unlock(CharacterData character)
        {
            if (character == null || !_owned.Add(character.id)) return false;
            _data.ownedCharacterIds.Add(character.id);
            OnCharacterUnlocked?.Invoke(character);
            Flush();
            return true;
        }

        public bool Select(CharacterData character)
        {
            if (character == null || !_owned.Contains(character.id)) return false;
            _data.selectedCharacterId = character.id;
            OnCharacterSelected?.Invoke(character);
            Flush();
            return true;
        }

        public void Flush() => SaveSystem.Save(_data);

        private void EnsureDefaultCharacter()
        {
            if (characterDB == null || characterDB.DefaultCharacter == null) return;
            CharacterData def = characterDB.DefaultCharacter;
            if (_owned.Add(def.id)) _data.ownedCharacterIds.Add(def.id);
            if (string.IsNullOrEmpty(_data.selectedCharacterId))
                _data.selectedCharacterId = def.id;
        }
    }
}
