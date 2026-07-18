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
    /// cells, and each frame evaluates the current cell for death (traffic, water)
    /// and log-riding. Column is clamped to the playfield; forward advances score.
    /// </summary>
    [RequireComponent(typeof(Transform))]
    public sealed class PlayerController : MonoBehaviour
    {
        [SerializeField] private GridSettings grid;
        [SerializeField] private LaneGenerator laneGenerator;
        [SerializeField] private InputReader input;

        public event Action<int> OnRowAdvanced;         // new furthest row
        public event Action<DeathCause> OnDied;

        public int Column { get; private set; }
        public int Row { get; private set; }
        public int FurthestRow { get; private set; }
        public bool IsAlive { get; private set; }

        private Vector3 _hopStart;
        private Vector3 _hopEnd;
        private float _hopT;
        private bool _hopping;
        private MovingObstacle _ridingLog;   // non-null while carried across water

        private void OnEnable()
        {
            if (input != null) input.OnHop += HandleHop;
        }

        private void OnDisable()
        {
            if (input != null) input.OnHop -= HandleHop;
        }

        /// <summary>Place the player at the origin cell and begin a run.</summary>
        public void ResetToStart()
        {
            Column = 0;
            Row = 0;
            FurthestRow = 0;
            _hopping = false;
            _hopT = 0f;
            _ridingLog = null;
            IsAlive = true;
            transform.position = grid.CellToWorld(Column, Row);
        }

        private void Update()
        {
            if (!IsAlive) return;

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
            _hopT += Time.deltaTime / grid.hopDuration;
            float t = Mathf.Clamp01(_hopT);

            Vector3 pos = Vector3.Lerp(_hopStart, _hopEnd, t);
            pos.y = Mathf.Sin(t * Mathf.PI) * grid.hopHeight; // arc
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
            if (!IsAlive) return;
            IsAlive = false;
            _ridingLog = null;
            OnDied?.Invoke(cause);
        }
    }
}
