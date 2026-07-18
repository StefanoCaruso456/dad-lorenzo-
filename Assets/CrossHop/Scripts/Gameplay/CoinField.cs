using System;
using System.Collections.Generic;
using CrossHop.Core;
using UnityEngine;

namespace CrossHop.Gameplay
{
    /// <summary>
    /// Abilities depend on this narrow surface rather than the concrete field, so the
    /// Characters layer never references gameplay internals directly.
    /// </summary>
    public interface ICoinField
    {
        /// <summary>Pull every coin within <paramref name="radius"/> of <paramref name="target"/> toward it.</summary>
        void PullToward(Vector3 target, float radius, float speed, float dt);
    }

    /// <summary>
    /// Spawns collectible coins on safe lanes ahead of the player and recycles them
    /// behind — pooled, so it allocates nothing after warm-up. Handles pickup by
    /// proximity and raises <see cref="OnCoinCollected"/>. The Coin Magnet ability
    /// uses <see cref="PullToward"/> to draw coins in.
    /// </summary>
    public sealed class CoinField : MonoBehaviour, ICoinField
    {
        [Header("Refs")]
        [SerializeField] private GridSettings grid;
        [SerializeField] private LaneGenerator laneGenerator;
        [SerializeField] private PlayerController player;
        [Tooltip("Coin prefab (needs a Coin component). Field is inert until assigned.")]
        [SerializeField] private Coin coinPrefab;

        [Header("Spawning")]
        [SerializeField] private int coinValue = 1;
        [Range(0f, 1f)] [SerializeField] private float spawnChancePerSafeCell = 0.12f;
        [SerializeField] private float coinHeight = 0.4f;
        [SerializeField] private int rowsAhead = 18;
        [SerializeField] private int rowsBehind = 4;

        [Header("Pickup")]
        [SerializeField] private float pickupRadius = 0.5f;

        private ObjectPool _pool;
        private readonly Dictionary<Vector2Int, Coin> _coins = new();
        private readonly List<Vector2Int> _scratch = new();
        private int _highestRowConsidered;
        private bool _running;

        /// <summary>Raised with the coin's value each time one is picked up.</summary>
        public event Action<int> OnCoinCollected;

        private void Awake()
        {
            if (coinPrefab != null)
            {
                var root = new GameObject("CoinPool").transform;
                root.SetParent(transform, false);
                _pool = new ObjectPool(coinPrefab.gameObject, root, prewarm: 24);
            }
        }

        /// <summary>Clear and begin spawning for a fresh run.</summary>
        public void Begin()
        {
            ClearAll();
            _highestRowConsidered = -rowsBehind - 1;
            _running = coinPrefab != null;
        }

        public void Stop()
        {
            _running = false;
            ClearAll();
        }

        private void Update()
        {
            if (!_running || player == null || !player.IsAlive) return;

            StreamRows();
            RecycleBehind();
            CollectNearPlayer();
        }

        // ---- ICoinField ---------------------------------------------------

        public void PullToward(Vector3 target, float radius, float speed, float dt)
        {
            if (_coins.Count == 0) return;
            float sqr = radius * radius;
            foreach (Coin coin in _coins.Values)
            {
                Vector3 p = coin.transform.position;
                Vector3 flat = new(target.x - p.x, 0f, target.z - p.z);
                if (flat.sqrMagnitude > sqr) continue;
                Vector3 step = Vector3.ClampMagnitude(flat, speed * dt);
                coin.transform.position = new Vector3(p.x + step.x, p.y, p.z + step.z);
            }
        }

        // ---- Internals ----------------------------------------------------

        private void StreamRows()
        {
            int target = player.FurthestRow + rowsAhead;
            while (_highestRowConsidered < target)
                ConsiderRow(++_highestRowConsidered);
        }

        private void ConsiderRow(int row)
        {
            if (row < 1) return; // keep the start strip clear
            Lane lane = laneGenerator.GetLane(row);
            if (lane == null || lane.Type != LaneType.Safe) return;

            for (int col = grid.MinColumn; col <= grid.MaxColumn; col++)
            {
                if (UnityEngine.Random.value >= spawnChancePerSafeCell) continue;
                var key = new Vector2Int(col, row);
                if (_coins.ContainsKey(key)) continue;

                Vector3 pos = grid.CellToWorld(col, row) + Vector3.up * coinHeight;
                GameObject go = _pool.Get(pos, Quaternion.identity);
                var coin = go.GetComponent<Coin>();
                coin.Init(coinValue);
                _coins[key] = coin;
            }
        }

        private void RecycleBehind()
        {
            int cutoff = player.FurthestRow - rowsBehind;
            _scratch.Clear();
            foreach (KeyValuePair<Vector2Int, Coin> kv in _coins)
                if (kv.Key.y < cutoff) _scratch.Add(kv.Key);

            foreach (Vector2Int key in _scratch)
            {
                _pool.Release(_coins[key].gameObject);
                _coins.Remove(key);
            }
        }

        private void CollectNearPlayer()
        {
            Vector3 pp = player.transform.position;
            float sqr = pickupRadius * pickupRadius;
            _scratch.Clear();
            foreach (KeyValuePair<Vector2Int, Coin> kv in _coins)
            {
                Vector3 p = kv.Value.transform.position;
                float dx = p.x - pp.x, dz = p.z - pp.z;
                if (dx * dx + dz * dz <= sqr) _scratch.Add(kv.Key);
            }

            foreach (Vector2Int key in _scratch)
            {
                Coin coin = _coins[key];
                OnCoinCollected?.Invoke(coin.Value);
                _pool.Release(coin.gameObject);
                _coins.Remove(key);
            }
        }

        private void ClearAll()
        {
            if (_pool != null)
                foreach (Coin coin in _coins.Values)
                    _pool.Release(coin.gameObject);
            _coins.Clear();
        }
    }
}
