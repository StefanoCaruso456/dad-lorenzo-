using CrossHop.Gameplay;
using UnityEngine;

namespace CrossHop.Characters.Abilities
{
    /// <summary>
    /// Blaze's <b>Fireproof</b>: survive the first N fatal hazard hits each run. The
    /// player flashes and gets a short invulnerability window to hop clear. Only absorbs
    /// contact deaths (squished / drowned-in-lava) — never a scroll-off, which would be
    /// too strong.
    ///
    /// The charge count is per-run state, so it lives here in the runtime, never on the
    /// shared asset.
    /// </summary>
    [CreateAssetMenu(fileName = "Fireproof", menuName = "CrossHop/Abilities/Fireproof")]
    public sealed class FireproofAbility : CharacterAbility
    {
        [Tooltip("How many fatal hazard hits are absorbed per run.")]
        [Min(1)] public int charges = 1;

        public override AbilityRuntime CreateRuntime() => new Runtime(charges);

        private sealed class Runtime : AbilityRuntime
        {
            private int _charges;
            public Runtime(int charges) => _charges = charges;

            public override bool TryAbsorbDeath(DeathCause cause)
            {
                if (_charges <= 0) return false;
                if (cause != DeathCause.Squished && cause != DeathCause.Drowned) return false;
                _charges--;
                return true;
            }
        }
    }
}
