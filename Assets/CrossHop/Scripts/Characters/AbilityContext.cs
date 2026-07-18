using CrossHop.Gameplay;
using UnityEngine;

namespace CrossHop.Characters
{
    /// <summary>
    /// The runtime handles an ability may read or affect during a run. Passed to the
    /// ability's runtime on start so abilities stay decoupled from concrete manager
    /// types — they see only the player transform, the coin field, and the cell size.
    /// </summary>
    public readonly struct AbilityContext
    {
        /// <summary>The active player's transform (magnet centre, float origin, etc.).</summary>
        public readonly Transform Player;

        /// <summary>The field to pull coins from — null in scenes without a coin field.</summary>
        public readonly ICoinField CoinField;

        /// <summary>World size of one grid cell, so abilities can reason in cells.</summary>
        public readonly float CellSize;

        public AbilityContext(Transform player, ICoinField coinField, float cellSize)
        {
            Player = player;
            CoinField = coinField;
            CellSize = cellSize;
        }
    }
}
