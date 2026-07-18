using UnityEngine;

namespace CrossHop.Characters
{
    /// <summary>
    /// One collectible character, authored entirely as data. Adding a character means
    /// creating an asset — no code changes. This is the core of the "50+ characters"
    /// content pipeline.
    /// </summary>
    [CreateAssetMenu(fileName = "Character", menuName = "CrossHop/Character")]
    public sealed class CharacterData : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Stable unique id used in save data. Never change once shipped.")]
        public string id;
        public string displayName;
        public Rarity rarity = Rarity.Common;

        [Header("Presentation")]
        [Tooltip("Prefab shown in-game and in the collection screen.")]
        public GameObject modelPrefab;
        public Sprite thumbnail;
        [Tooltip("Optional SFX played on each hop. Falls back to a default if empty.")]
        public AudioClip hopSound;

        [Header("Economy")]
        [Tooltip("Coin cost to unlock via the prize machine, if directly purchasable.")]
        [Min(0)] public int unlockCost;
        [Tooltip("If true, this character is only obtainable via IAP, not coins.")]
        public bool premiumOnly;

        [Header("Gameplay (optional)")]
        [Tooltip("Leave empty for a cosmetic-only character.")]
        public CharacterAbility ability;

        public bool HasAbility => ability != null;
    }
}
