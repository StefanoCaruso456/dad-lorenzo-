using UnityEngine;

namespace CrossHop.Gameplay
{
    /// <summary>
    /// A single moving thing in a lane — a car, floating log, or train. Pooled: it drives
    /// itself along the lane and reports back to its owning <see cref="Lane"/> when it
    /// leaves the playfield so it can be recycled.
    ///
    /// If the prefab has a Renderer on its root, it colours itself by role for the
    /// gray-box (brown = ride, red = dodge). Multi-part voxel prefabs have an empty root
    /// (no renderer), so they keep their own authored colours untouched.
    /// </summary>
    public sealed class MovingObstacle : MonoBehaviour
    {
        [Tooltip("How many grid cells this obstacle spans (car≈1, log≈2, train≈5). Used for hit detection.")]
        [SerializeField] private float lengthCells = 1f;

        private float _speed;          // cells/sec, signed by direction
        private float _despawnX;       // world x at which we recycle
        private System.Action<MovingObstacle> _onExit;

        /// <summary>True for logs — the player rides these instead of dying.</summary>
        public bool IsRideable { get; private set; }

        /// <summary>Footprint length in cells, for span-aware collision.</summary>
        public float LengthCells => lengthCells;

        private Renderer _renderer;
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static Material _hazardMat;  // red — dodge
        private static Material _logMat;     // brown — ride

        private void Awake() => _renderer = GetComponent<Renderer>();

        public void Launch(float speed, float despawnX, bool rideable,
                           System.Action<MovingObstacle> onExit)
        {
            _speed = speed;
            _despawnX = despawnX;
            IsRideable = rideable;
            _onExit = onExit;

            // Only tint single-mesh gray-box prefabs; voxel prefabs (empty root) keep their colours.
            if (_renderer != null)
                _renderer.sharedMaterial = rideable ? LogMat() : HazardMat();
        }

        private void Update()
        {
            Vector3 p = transform.position;
            p.x += _speed * Time.deltaTime;
            transform.position = p;

            bool movingRight = _speed > 0f;
            if ((movingRight && p.x >= _despawnX) || (!movingRight && p.x <= _despawnX))
                _onExit?.Invoke(this);
        }

        private static Material HazardMat() => _hazardMat ??= Make(new Color(0.86f, 0.24f, 0.22f));
        private static Material LogMat() => _logMat ??= Make(new Color(0.55f, 0.40f, 0.24f));

        private static Material Make(Color c)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (shader == null) return null;
            var mat = new Material(shader) { color = c };
            mat.SetColor(BaseColorId, c);
            return mat;
        }
    }
}
