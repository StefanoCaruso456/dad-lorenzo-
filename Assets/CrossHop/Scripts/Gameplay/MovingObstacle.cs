using UnityEngine;

namespace CrossHop.Gameplay
{
    /// <summary>
    /// A single moving thing in a lane — a car, truck, train car or floating log.
    /// Pooled: it drives itself along the lane and reports back to its owning
    /// <see cref="Lane"/> when it leaves the playfield so it can be recycled.
    /// </summary>
    public sealed class MovingObstacle : MonoBehaviour
    {
        private float _speed;          // cells/sec, signed by direction
        private float _despawnX;       // world x at which we recycle
        private System.Action<MovingObstacle> _onExit;

        /// <summary>True for logs — the player rides these instead of dying.</summary>
        public bool IsRideable { get; private set; }

        public void Launch(float speed, float despawnX, bool rideable,
                           System.Action<MovingObstacle> onExit)
        {
            _speed = speed;
            _despawnX = despawnX;
            IsRideable = rideable;
            _onExit = onExit;
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
    }
}
