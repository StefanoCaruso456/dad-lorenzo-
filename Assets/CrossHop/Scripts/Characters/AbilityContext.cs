using UnityEngine;

namespace CrossHop.Characters
{
    /// <summary>
    /// The runtime handles an ability may need to read or affect during a run.
    /// Passed to <see cref="CharacterAbility"/> hooks so abilities stay decoupled
    /// from concrete manager types.
    /// </summary>
    public readonly struct AbilityContext
    {
        /// <summary>The active player's transform (for magnet radius, floating, etc.).</summary>
        public readonly Transform Player;

        public AbilityContext(Transform player)
        {
            Player = player;
        }
    }
}
