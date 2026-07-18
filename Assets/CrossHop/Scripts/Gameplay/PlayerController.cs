using System;
using CrossHop.Core;
using CrossHop.Input;
using UnityEngine;

namespace CrossHop.Gameplay
{
    /// <summary>Why the player died — surfaced to UI/analytics.</summary>
    public enum DeathCause { None, Squished, Drowned, ScrolledOff }

    /// <summary>
    /// Grid-snapped hopper. Consumes hop requests, animates a discrete arc between
    /// cells, and each frame evaluates the current cell for death (traffic, water) and
    /// log-riding. Column is clamped to the playfield; forward advances score.
    ///
    /// Two ability hooks live here but the controller stays ignorant of abilities:
    /// a <see cref="HopProfile"/> scales the hop feel, and a <see cref="DeathGuard"/>
    /// delegate can absorb an otherwise-fatal hit (granting a brief invulnerability).
    /// </summary>
    [RequireComponent(typeof(Transform))]
    public sealed class PlayerController : MonoBehaviour
    {
        [SerializeField] private GridSettings grid;
        [SerializeField] private LaneGenerator laneGenerator;
        [SerializeField] private InputReader input;

        [Tooltip("Invulnerability granted after a death is absorbed, to hop clear.")]
        [SerializeField] private float absorbInvulnerability = 0.8f;

        public event Action<int> OnRowAdvanced;         // new furthest row
        public event Action<DeathCause> OnDied;

        /// <summary>
        /// Optional gate consulted before a fatal hit lands. Return true to absorb it.
        /// Set per run by the game manager from the active ability; cleared on death.
        /// </summary>
        public Func<DeathCause, bool> DeathGuard { get; set; }

        public int Column { get; private set; }
        public int Row { get; private set; }
        public int FurthestRow { get; private set; }
        public bool IsAlive { get; private set; }
        public bool IsInvulnerable => _invulnTimer > 0f;

        private Vector3 _hopStart;
        private Vector3 _hopEnd;
        private float _hopT;
        private bool _hopping;
        private MovingObstacle _ridingLog;   // non-null while carried across water
        private HopProfile _hopProfile = HopProfile.Default;
        private float _invulnTimer;

        private void OnEnable()
        {
            if (input != null) input.OnHop += HandleHop;
        }

        private void OnDisable()
        {
            if (input != null) input.OnHop -= HandleHop;
        }

        /// <summary>Apply an ability's hop feel for this run. Reset each run.</summary>
        public void SetHopProfile(HopProfile profile) => _hopProfile = profile;

        /// <summary>Place the player at the origin cell and begin a run.</summary>
        public void ResetToStart()
        {
            Column = 0;
            Row = 0;
            FurthestRow = 0;
            _hopping = false;
            _hopT = 0f;
            _ridingLog = null;
            _invulnTimer = 0f;
            _hopProfile = HopProfile.Default;
            IsAlive = true;
            transform.position = grid.CellToWorld(Column, Row);
        }

        private void Update()
        {
            if (!IsAlive) return;
            if (_invulnTimer > 0f) _invulnTimer -= Time.deltaTime;

            if (_hopping) AnimateHop();
            else if (_ridingLog != null) DriftWithLog();

            EvaluateCurrentCell();
        }

        private void HandleHop(HopDirection dir)
        {
            if (!IsAlive || _hopping) return;

            int col = Column;
            int row = Row;
            switch (dir)
            {
                case HopDirection.Forward: row += 1; break;
                case HopDirection.Back:    row -= 1; break;
                case HopDirection.Left:    col -= 1; break;
                case HopDirection.Right:   col += 1; break;
            }

            col = grid.ClampColumn(col);
            if (col == Column && row == Row) return; // clamped into a wall; ignore

            BeginHop(col, row);
        }

        private void BeginHop(int col, int row)
        {
            _ridingLog = null; // leaving the current cell releases any log
            Column = col;
            Row = row;

            _hopStart = transform.position;
            _hopEnd = grid.CellToWorld(col, row);
            _hopT = 0f;
            _hopping = true;

            if (row > FurthestRow)
            {
                FurthestRow = row;
                OnRowAdvanced?.Invoke(FurthestRow);
            }

            laneGenerator.UpdateStreaming(FurthestRow);
        }

        private void AnimateHop()
        {
            float duration = grid.hopDuration * _hopProfile.DurationMultiplier;
            _hopT += Time.deltaTime / Mathf.Max(0.0001f, duration);
            float t = Mathf.Clamp01(_hopT);

            Vector3 pos = Vector3.Lerp(_hopStart, _hopEnd, t);
            pos.y = Mathf.Sin(t * Mathf.PI) * grid.hopHeight * _hopProfile.HeightMultiplier; // arc
            transform.position = pos;

            if (t >= 1f)
            {
                _hopping = false;
                transform.position = _hopEnd;
            }
        }

        private void DriftWithLog()
        {
            // Carried sideways by the log; keep the player pinned to its x.
            Vector3 pos = transform.position;
            pos.x = _ridingLog.transform.position.x;
            transform.position = pos;

            // Update our logical column so hops resolve from where we actually are.
            Column = Mathf.RoundToInt(pos.x / grid.cellSize);

            // Carried off the edge = death.
            if (Column < grid.MinColumn - 1 || Column > grid.MaxColumn + 1)
                Die(DeathCause.Drowned);
        }

        private void EvaluateCurrentCell()
        {
            if (_hopping) return; // resolve hazards only once landed

            Lane lane = laneGenerator.GetLane(Row);
            if (lane == null) return;

            MovingObstacle obstacle = lane.ObstacleAtColumn(Column);

            switch (lane.Type)
            {
                case LaneType.Water:
                    if (obstacle != null && obstacle.IsRideable)
                        _ridingLog = obstacle;       // safe: riding a log
                    else if (_ridingLog == null)
                        Die(DeathCause.Drowned);      // stepped into open water
                    break;

                case LaneType.Road:
                case LaneType.Rail:
                    if (obstacle != null)
                        Die(DeathCause.Squished);
                    break;

                case LaneType.Safe:
                    _ridingLog = null;
                    break;
            }
        }

        /// <summary>Called by the camera when the player falls behind the death line.</summary>
        public void KillByScroll() => Die(DeathCause.ScrolledOff);

        private void Die(DeathCause cause)
        {
            if (!IsAlive || _invulnTimer > 0f) return;

            // Let an ability absorb the hit; survive with a brief window to escape.
            if (DeathGuard != null && DeathGuard(cause))
            {
                _invulnTimer = absorbInvulnerability;
                return;
            }

            IsAlive = false;
            _ridingLog = null;
            DeathGuard = null;
            OnDied?.Invoke(cause);
        }
    }
}
