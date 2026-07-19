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
        private ObjectPool _obstaclePool;
        private readonly List<MovingObstacle> _active = new();

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private MaterialPropertyBlock _mpb;

        public LaneType Type => _def != null ? _def.type : LaneType.Safe;
        public int Row => _row;

        /// <summary>Configure a freshly-pooled lane for a specific row and difficulty.</summary>
        public void Init(GridSettings grid, LaneDefinition def, int row,
                         float speed, float spawnInterval, ObjectPool obstaclePool)
        {
            _grid = grid;
            _def = def;
            _row = row;
            _speed = speed;
            _spawnInterval = spawnInterval;
            _obstaclePool = obstaclePool;

            // Stagger the first spawn so lanes don't pulse in lockstep.
            _spawnTimer = Random.Range(0f, spawnInterval);

            StyleBody();
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

        private void Update()
        {
            if (_def == null || _def.obstaclePrefab == null || _obstaclePool == null)
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
            float half = (_grid.laneWidth / 2f + 2f) * _grid.cellSize;
            bool movingRight = _speed > 0f;
            float startX = movingRight ? -half : half;
            float despawnX = movingRight ? half : -half;

            var pos = new Vector3(startX, 0f, _row * _grid.cellSize);
            GameObject go = _obstaclePool.Get(pos, Quaternion.identity);

            var obstacle = go.GetComponent<MovingObstacle>();
            if (obstacle == null)
            {
                Debug.LogError("[Lane] Obstacle prefab missing MovingObstacle component.", go);
                _obstaclePool.Release(go);
                return;
            }

            obstacle.Launch(_speed, despawnX, _def.RequiresRiding, Recycle);
            _active.Add(obstacle);
        }

        private void Recycle(MovingObstacle obstacle)
        {
            _active.Remove(obstacle);
            _obstaclePool.Release(obstacle.gameObject);
        }

        /// <summary>Return all obstacles to the pool. Called before the lane itself is recycled.</summary>
        public void ClearObstacles()
        {
            for (int i = _active.Count - 1; i >= 0; i--)
                _obstaclePool.Release(_active[i].gameObject);
            _active.Clear();
        }

        /// <summary>
        /// Tests the player's column against this lane's obstacles.
        /// Returns the obstacle overlapping the player (rideable or deadly), else null.
        /// </summary>
        public MovingObstacle ObstacleAtColumn(int column)
        {
            float x = column * _grid.cellSize;
            float tolerance = _grid.cellSize * 0.5f;
            foreach (MovingObstacle o in _active)
            {
                if (Mathf.Abs(o.transform.position.x - x) <= tolerance)
                    return o;
            }
            return null;
        }
    }
}
