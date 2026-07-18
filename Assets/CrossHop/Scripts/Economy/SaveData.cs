using System;
using System.Collections.Generic;

namespace CrossHop.Economy
{
    /// <summary>
    /// Serializable snapshot of all persistent player state. Plain, versioned POCO
    /// so it survives Unity's JsonUtility and future migrations.
    /// </summary>
    [Serializable]
    public sealed class SaveData
    {
        /// <summary>Bump when the shape changes; migrate on load if needed.</summary>
        public int version = 1;

        public int coins;
        public int bestScore;
        public string selectedCharacterId;
        public List<string> ownedCharacterIds = new();
    }
}
