using CrossHop.Gameplay;
using UnityEngine;

namespace CrossHop.Characters.Abilities
{
    /// <summary>
    /// Eileen's <b>Low-G Hop</b>: a floatier, higher, slightly slower hop arc — the
    /// moon-jump feel. Purely a feel modifier (no reach change, so the grid game stays
    /// fair and readable).
    /// </summary>
    [CreateAssetMenu(fileName = "LowGravity", menuName = "CrossHop/Abilities/Low Gravity")]
    public sealed class LowGravityAbility : CharacterAbility
    {
        [Tooltip("Multiplier on hop arc height.")]
        [Min(1f)] public float hopHeightMultiplier = 2.2f;
        [Tooltip("Multiplier on hop duration (higher = slower, floatier).")]
        [Min(1f)] public float hopDurationMultiplier = 1.5f;

        public override AbilityRuntime CreateRuntime() => new Runtime(hopHeightMultiplier, hopDurationMultiplier);

        private sealed class Runtime : AbilityRuntime
        {
            private readonly float _height, _duration;
            public Runtime(float height, float duration) { _height = height; _duration = duration; }

            public override void ModifyHopProfile(ref HopProfile profile)
            {
                profile.HeightMultiplier *= _height;
                profile.DurationMultiplier *= _duration;
            }
        }
    }
}
