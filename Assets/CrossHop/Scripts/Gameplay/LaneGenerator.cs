using System.Collections.Generic;
using CrossHop.Core;
using UnityEngine;

namespace CrossHop.Gameplay
{
    /// <summary>
    /// Streams the world ahead of the player and recycles it behind. Keeps a fixed
    /// window of lanes alive at all times; every lane and every obstacle is pooled,
    /// so a run of any length allocates nothing after warm-up.
    /// </summary>
    public sealed class LaneGenerator : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField] private GridSettings grid;
        [SerializeField] private DifficultyCurve difficulty;

        [Tooltip("Lane templates to choose from. Include at least one Safe lane.")]
        [SerializeField] private LaneDefinition[] laneDefinitions;

        [Tooltip("Empty GameObject with a Lane component used as the pooled lane prefab.")]
        [SerializeField] private Lane lanePrefab;

        [Header("Streaming window")]
        [Tooltip("Rows kept ahead of the player.")]
        [SerializeField] private int rowsAhead = 20;
        [Tooltip("Rows kept behind the player before recycling.")]
        [SerializeField] private int rowsBehind = 6;

        private readonly Dictionary<int, Lane> _lanes = new();
        private readonly Dictionary<LaneDefinition, ObjectPool> _obstaclePools = new();
        private ObjectPool _lanePool;
        private int _highestRowBuilt = -1;

        public GridSettings Grid => grid;

        private void Awake() => BuildPools();

        /// <summary>Reset the world to the start state for a new run.</summary>
        public void ResetWorld()
        {
            foreach (Lane lane in _lanes.Values)
            {
                lane.ClearObstacles();
                _lanePool.Release(lane.gameObject);
            }
            _lanes.Clear();
            _highestRowBuilt = -1;

            // Guarantee a safe starting strip so the player never spawns into traffic.
            for (int row = -rowsBehind; row <= rowsAhead; row++)
                BuildLane(row, forceSafe: row <= 1);
        }

        /// <summary>Call as the player advances; extends ahead and trims behind.</summary>
        public void UpdateStreaming(int playerRow)
        {
            int target = playerRow + rowsAhead;
            while (_highestRowBuilt < target)
                BuildLane(_highestRowBuilt + 1, forceSafe: false);

            int cutoff = playerRow - rowsBehind;
            var toRemove = new List<int>();
            foreach (KeyValuePair<int, Lane> kv in _lanes)
                if (kv.Key < cutoff) toRemove.Add(kv.Key);

            foreach (int row in toRemove)
            {
                Lane lane = _lanes[row];
                lane.ClearObstacles();
                _lanePool.Release(lane.gameObject);
                _lanes.Remove(row);
            }
        }

        public Lane GetLane(int row) => _lanes.TryGetValue(row, out Lane lane) ? lane : null;

        // ---- Internals ----------------------------------------------------

        private void BuildPools()
        {
            var laneRoot = new GameObject("LanePool").transform;
            laneRoot.SetParent(transform, false);
            _lanePool = new ObjectPool(lanePrefab.gameObject, laneRoot, prewarm: rowsAhead + rowsBehind + 2);

            foreach (LaneDefinition def in laneDefinitions)
            {
                if (def == null || def.obstaclePrefab == null) continue;
                var root = new GameObject($"ObstaclePool_{def.name}").transform;
                root.SetParent(transform, false);
                _obstaclePools[def] = new ObjectPool(def.obstaclePrefab, root, prewarm: 8);
            }
        }

        private void BuildLane(int row, bool forceSafe)
        {
            if (_lanes.ContainsKey(row)) return;

            LaneDefinition def = forceSafe ? FindSafeDefinition() : PickDefinition(row);
            var pos = new Vector3(0f, 0f, row * grid.cellSize);
            GameObject go = _lanePool.Get(pos, Quaternion.identity);
            go.name = $"Lane_{row}";

            var lane = go.GetComponent<Lane>();
            float speed = ResolveSpeed(def, row);
            float interval = ResolveSpawnInterval(def, row);
            ObjectPool obstaclePool = def.obstaclePrefab != null ? _obstaclePools[def] : null;

            lane.Init(grid, def, row, speed, interval, obstaclePool);

            _lanes[row] = lane;
            if (row > _highestRowBuilt) _highestRowBuilt = row;
        }

        private LaneDefinition PickDefinition(int row)
        {
            if (Random.value < difficulty.SafeLaneChance(row))
                return FindSafeDefinition();

            // Pick any non-safe lane; if none authored, fall back to safe.
            var hazards = new List<LaneDefinition>();
            foreach (LaneDefinition d in laneDefinitions)
                if (d != null && d.type != LaneType.Safe) hazards.Add(d);

            return hazards.Count > 0 ? hazards[Random.Range(0, hazards.Count)] : FindSafeDefinition();
        }

        private LaneDefinition FindSafeDefinition()
        {
            foreach (LaneDefinition d in laneDefinitions)
                if (d != null && d.type == LaneType.Safe) return d;

            Debug.LogError("[LaneGenerator] No Safe LaneDefinition assigned.");
            return laneDefinitions.Length > 0 ? laneDefinitions[0] : null;
        }

        private float ResolveSpeed(LaneDefinition def, int row)
        {
            if (def.obstaclePrefab == null) return 0f;
            float baseSpeed = Random.Range(def.minSpeed, def.maxSpeed) * difficulty.SpeedMultiplier(row);
            // Alternate travel direction by row parity for readable, varied traffic.
            float signed = (row % 2 == 0 ? 1f : -1f) * baseSpeed;
            return signed * grid.cellSize;
        }

        private float ResolveSpawnInterval(LaneDefinition def, int row)
            => Random.Range(def.minSpawnInterval, def.maxSpawnInterval)
               * difficulty.SpawnIntervalMultiplier(row);
    }
}
