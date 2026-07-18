using UnityEngine;

namespace CrossHop.Core
{
    /// <summary>
    /// Central, designer-tunable configuration for the world grid.
    /// The whole game snaps to this grid, so every system reads cell size from here
    /// rather than hard-coding magic numbers. Authored as a single shared asset.
    /// </summary>
    [CreateAssetMenu(fileName = "GridSettings", menuName = "CrossHop/Grid Settings")]
    public sealed class GridSettings : ScriptableObject
    {
        [Tooltip("World-space size of one grid cell (metres). One hop = one cell.")]
        [Min(0.1f)] public float cellSize = 1f;

        [Tooltip("How many cells wide the playfield is. Player is clamped to this range.")]
        [Min(3)] public int laneWidth = 9;

        [Tooltip("Seconds a single hop animation takes. Lower = snappier.")]
        [Min(0.01f)] public float hopDuration = 0.12f;

        [Tooltip("Peak height of the hop arc (metres).")]
        [Min(0f)] public float hopHeight = 0.35f;

        /// <summary>Left-most valid column index (columns are centred on 0).</summary>
        public int MinColumn => -(laneWidth / 2);

        /// <summary>Right-most valid column index.</summary>
        public int MaxColumn => laneWidth / 2;

        /// <summary>Converts a grid coordinate to a world position on the ground plane.</summary>
        public Vector3 CellToWorld(int column, int row)
            => new Vector3(column * cellSize, 0f, row * cellSize);

        /// <summary>Clamps a column index to the playable width.</summary>
        public int ClampColumn(int column) => Mathf.Clamp(column, MinColumn, MaxColumn);
    }
}
