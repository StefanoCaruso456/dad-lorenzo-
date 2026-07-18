using System;
using System.Collections.Generic;
using CrossHop.Core;
using UnityEngine;

namespace CrossHop.Gameplay
{
    /// <summary>
    /// Streams the world ahead of the player and recycles it behind. The lane set and
    /// difficulty come from a <see cref="WorldTheme"/> supplied by <see cref="Configure"/>
    /// at run start — so which world streams is driven by the selected character, not
    /// hard-wired here. Every lane and obstacle is pooled; a run of any length allocates
    /// nothing after warm-up. Obstacle pools are keyed by LaneDefinition and shared
    /// across worlds, so switching characters reuses them.
    /// </summary>
    public sealed class LaneGenerator : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField] private GridSettings grid;

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
        private Transform _obstacleRoot;
        private WorldTheme _world;
        private int _highestRowBuilt = -1;

        public GridSettings Grid => grid;
        public WorldTheme World => _world;

        private void Awake()
        {
            var laneRoot = new GameObject("LanePool").transform;
            laneRoot.SetParent(transform, false);
            _lanePool = new ObjectPool(lanePrefab.gameObject, laneRoot, prewarm: rowsAhead + rowsBehind + 2);

            _obstacleRoot = new GameObject("ObstaclePools").transform;
            _obstacleRoot.SetParent(transform, false);
        }

        /// <summary>Set the world to stream. Call before <see cref="ResetWorld"/>.</summary>
        public void Configure(WorldTheme world)
        {
            _world = world != null ? world : throw new ArgumentNullException(nameof(world));
            if (!world.IsValid)
                Debug.LogError($"[LaneGenerator] World '{world.name}' is missing a safe lane or hazard lanes.", world);
        }

        /// <summary>Reset the world to the start state for a new run.</summary>
        public void ResetWorld()
        {
            if (_world == null)
            {
                Debug.LogError("[LaneGenerator] Configure(world) must be called before ResetWorld().");
                return;
            }

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

        private ObjectPool EnsureObstaclePool(LaneDefinition def)
        {
            if (def == null || def.obstaclePrefab == null) return null;
            if (_obstaclePools.TryGetValue(def, out ObjectPool pool)) return pool;

            var root = new GameObject($"Pool_{def.name}").transform;
            root.SetParent(_obstacleRoot, false);
            pool = new ObjectPool(def.obstaclePrefab, root, prewarm: 8);
            _obstaclePools[def] = pool;
            return pool;
        }

        private void BuildLane(int row, bool forceSafe)
        {
            if (_lanes.ContainsKey(row)) return;

            LaneDefinition def = forceSafe ? _world.safeLane : PickDefinition(row);
            if (def == null) def = _world.safeLane;
            if (def == null) return; // world misconfigured; already logged in Configure

            var pos = new Vector3(0f, 0f, row * grid.cellSize);
            GameObject go = _lanePool.Get(pos, Quaternion.identity);
            go.name = $"Lane_{row}";

            var lane = go.GetComponent<Lane>();
            float speed = ResolveSpeed(def, row);
            float interval = ResolveSpawnInterval(def, row);
            ObjectPool obstaclePool = EnsureObstaclePool(def);

            lane.Init(grid, def, row, speed, interval, obstaclePool);

            _lanes[row] = lane;
            if (row > _highestRowBuilt) _highestRowBuilt = row;
        }

        private LaneDefinition PickDefinition(int row)
        {
            DifficultyCurve diff = _world.difficulty;
            float safeChance = diff != null ? diff.SafeLaneChance(row) : 0.35f;
            if (UnityEngine.Random.value < safeChance) return _world.safeLane;

            LaneDefinition[] hazards = _world.hazardLanes;
            return (hazards != null && hazards.Length > 0)
                ? hazards[UnityEngine.Random.Range(0, hazards.Length)]
                : _world.safeLane;
        }

        private float ResolveSpeed(LaneDefinition def, int row)
        {
            if (def.obstaclePrefab == null) return 0f;
            float mult = _world.difficulty != null ? _world.difficulty.SpeedMultiplier(row) : 1f;
            float baseSpeed = UnityEngine.Random.Range(def.minSpeed, def.maxSpeed) * mult;
            // Alternate travel direction by row parity for readable, varied traffic.
            float signed = (row % 2 == 0 ? 1f : -1f) * baseSpeed;
            return signed * grid.cellSize;
        }

        private float ResolveSpawnInterval(LaneDefinition def, int row)
        {
            float mult = _world.difficulty != null ? _world.difficulty.SpawnIntervalMultiplier(row) : 1f;
            return UnityEngine.Random.Range(def.minSpawnInterval, def.maxSpawnInterval) * mult;
        }
    }
}
