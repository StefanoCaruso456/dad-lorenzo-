using UnityEngine;

namespace CrossHop.Characters.Abilities
{
    /// <summary>
    /// Sample premium ability: the character earns double coins for the whole run.
    /// Demonstrates the ability pattern — subclass, override a hook, author an asset.
    /// </summary>
    [CreateAssetMenu(fileName = "DoubleCoins", menuName = "CrossHop/Abilities/Double Coins")]
    public sealed class DoubleCoinsAbility : CharacterAbility
    {
        [Tooltip("Coin reward multiplier applied for the run.")]
        [Min(1)] public int multiplier = 2;

        public override int ModifyCoinReward(int baseAmount) => baseAmount * multiplier;
    }
}
