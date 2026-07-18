using UnityEngine;

namespace CrossHop.Characters.Abilities
{
    /// <summary>Earns multiplied coins for the whole run. The simplest ability shape.</summary>
    [CreateAssetMenu(fileName = "DoubleCoins", menuName = "CrossHop/Abilities/Double Coins")]
    public sealed class DoubleCoinsAbility : CharacterAbility
    {
        [Tooltip("Coin reward multiplier applied for the run.")]
        [Min(1)] public int multiplier = 2;

        public override AbilityRuntime CreateRuntime() => new Runtime(multiplier);

        private sealed class Runtime : AbilityRuntime
        {
            private readonly int _multiplier;
            public Runtime(int multiplier) => _multiplier = multiplier;
            public override int ModifyCoinReward(int baseAmount) => baseAmount * _multiplier;
        }
    }
}
