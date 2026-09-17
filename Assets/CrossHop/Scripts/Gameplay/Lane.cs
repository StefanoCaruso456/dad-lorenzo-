using System.Collections.Generic;
using CrossHop.Core;
using UnityEngine;

namespace CrossHop.Gameplay
{
    /// <summary>
    /// One row of the world. Owns its ground tiles and the obstacles travelling
    /// across it, spawning them on a timer and recycling them through pools.
    /// A lane is itself pooled and reset by the <see cref="LaneGenerator"/>.
    /// </summary>
    public sealed class Lane : MonoBehaviour
    {
        [Tooltip("Optional strip mesh stretched to the lane width and tinted by type. " +
                 "Used for the gray-box look; real worlds can leave this empty and use art.")]
        [SerializeField] private Renderer bodyRenderer;

        private GridSettings _grid;
        private LaneDefinition _def;
        private int _row;
        private float _speed;            // signed cells/sec
        private float _spawnInterval;
        private float _spawnTimer;
        private LaneGenerator _generator;
        private GameObject[] _variants;
        private readonly List<Active> _active = new();

        private struct Active { public MovingObstacle Obstacle; public ObjectPool Pool; }

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private MaterialPropertyBlock _mpb;
        private Transform _roadLines;

        public LaneType Type => _def != null ? _def.type : LaneType.Safe;
        public int Row => _row;

        /// <summary>Configure a freshly-pooled lane for a specific row and difficulty.</summary>
        public void Init(GridSettings grid, LaneDefinition def, int row,
                         float speed, float spawnInterval, LaneGenerator generator)
        {
            _grid = grid;
            _def = def;
            _row = row;
            _speed = speed;
            _spawnInterval = spawnInterval;
            _generator = generator;
            _variants = ResolveVariants(def);

            // Stagger the first spawn so lanes don't pulse in lockstep.
            _spawnTimer = Random.Range(0f, spawnInterval);

            StyleBody();
            EnsureRoadLines(Type == LaneType.Road);
        }

        private static GameObject[] ResolveVariants(LaneDefinition def)
        {
            if (def.obstacleVariants != null && def.obstacleVariants.Length > 0) return def.obstacleVariants;
            if (def.obstaclePrefab != null) return new[] { def.obstaclePrefab };
            return System.Array.Empty<GameObject>();
        }

        private void StyleBody()
        {
            if (bodyRenderer == null) return;

            // Stretch to the full playfield width, one cell deep.
            bodyRenderer.transform.localScale =
                new Vector3(_grid.laneWidth * _grid.cellSize, bodyRenderer.transform.localScale.y, _grid.cellSize);

            _mpb ??= new MaterialPropertyBlock();
            Color c = Type switch
            {
                LaneType.Safe => new Color(0.42f, 0.68f, 0.35f),
                LaneType.Road => new Color(0.28f, 0.29f, 0.33f),
                LaneType.Water => new Color(0.25f, 0.55f, 0.80f),
                LaneType.Rail => new Color(0.45f, 0.36f, 0.28f),
                _ => Color.grey
            };
            bodyRenderer.GetPropertyBlock(_mpb);
            _mpb.SetColor(BaseColorId, c);
            _mpb.SetColor(ColorId, c);
            bodyRenderer.SetPropertyBlock(_mpb);
        }

        private void AnimateWater()
        {
            if (bodyRenderer == null) return;
            // Gentle shimmer, phase-shifted per row, so water reads as moving.
            float t = Mathf.Sin(Time.time * 1.6f + _row * 0.6f) * 0.5f + 0.5f;
            Color c = Color.Lerp(new Color(0.20f, 0.50f, 0.78f), new Color(0.32f, 0.66f, 0.92f), t);
            _mpb ??= new MaterialPropertyBlock();
            bodyRenderer.GetPropertyBlock(_mpb);
            _mpb.SetColor(BaseColorId, c);
            _mpb.SetColor(ColorId, c);
            bodyRenderer.SetPropertyBlock(_mpb);
        }

        private void EnsureRoadLines(bool active)
        {
            if (active && _roadLines == null) BuildRoadLines();
            if (_roadLines != null) _roadLines.gameObject.SetActive(active);
        }

        private void BuildRoadLines()
        {
            if (bodyRenderer == null) return;
            _roadLines = new GameObject("RoadLines").transform;
            _roadLines.SetParent(transform, false);

            float w = _grid.laneWidth * _grid.cellSize;
            float bodyY = bodyRenderer.transform.localScale.y;
            float c = _grid.cellSize;
            var yellow = new Color(0.95f, 0.85f, 0.22f);
            var mpb = new MaterialPropertyBlock();

            // Dashed centre line running along the road, counter-scaled against the stretched body.
            for (int col = _grid.MinColumn; col <= _grid.MaxColumn; col++)
            {
                var dash = GameObject.CreatePrimitive(PrimitiveType.Cube);
                dash.name = "Dash";
                var collider = dash.GetComponent<Collider>();
                if (collider != null) Destroy(collider);
                dash.transform.SetParent(_roadLines, false);
                dash.transform.localScale = new Vector3(0.5f / w, 0.06f / bodyY, 0.14f / c);
                dash.transform.localPosition = new Vector3(col * c / w, 0.07f / bodyY, 0f);
                var r = dash.GetComponent<Renderer>();
                r.GetPropertyBlock(mpb);
                mpb.SetColor(BaseColorId, yellow);
                mpb.SetColor(ColorId, yellow);
                r.SetPropertyBlock(mpb);
            }
        }

        private void Update()
        {
            if (Type == LaneType.Water) AnimateWater();

            if (_variants == null || _variants.Length == 0 || _generator == null)
                return;

            _spawnTimer -= Time.deltaTime;
            if (_spawnTimer <= 0f)
            {
                SpawnObstacle();
                _spawnTimer = _spawnInterval;
            }
        }

        private void SpawnObstacle()
        {
            GameObject prefab = _variants[Random.Range(0, _variants.Length)];
            ObjectPool pool = _generator.ObstaclePoolFor(prefab);
            if (pool == null) return;

            float half = (_grid.laneWidth / 2f + 2f) * _grid.cellSize;
            bool movingRight = _speed > 0f;
            float startX = movingRight ? -half : half;
            float despawnX = movingRight ? half : -half;

            var pos = new Vector3(startX, 0f, _row * _grid.cellSize);
            GameObject go = pool.Get(pos, Quaternion.identity);

            var obstacle = go.GetComponent<MovingObstacle>();
            if (obstacle == null)
            {
                Debug.LogError("[Lane] Obstacle prefab missing MovingObstacle component.", go);
                pool.Release(go);
                return;
            }

            obstacle.Launch(_speed, despawnX, _def.RequiresRiding, o => Recycle(o, pool));
            _active.Add(new Active { Obstacle = obstacle, Pool = pool });
        }

        private void Recycle(MovingObstacle obstacle, ObjectPool pool)
        {
            for (int i = _active.Count - 1; i >= 0; i--)
                if (_active[i].Obstacle == obstacle) { _active.RemoveAt(i); break; }
            pool.Release(obstacle.gameObject);
        }

        /// <summary>Return all obstacles to their pools. Called before the lane itself is recycled.</summary>
        public void ClearObstacles()
        {
            for (int i = _active.Count - 1; i >= 0; i--)
                _active[i].Pool.Release(_active[i].Obstacle.gameObject);
            _active.Clear();
        }

        /// <summary>
        /// Tests the player's column against this lane's obstacles.
        /// Returns the obstacle overlapping the player (rideable or deadly), else null.
        /// </summary>
        public MovingObstacle ObstacleAtColumn(int column)
        {
            float x = column * _grid.cellSize;
            foreach (Active a in _active)
            {
                MovingObstacle o = a.Obstacle;
                // Span-aware: an obstacle covers half its length either side of its centre.
                float half = o.LengthCells * 0.5f * _grid.cellSize;
                if (Mathf.Abs(o.transform.position.x - x) <= half + _grid.cellSize * 0.1f)
                    return o;
            }
            return null;
        }
    }
}
