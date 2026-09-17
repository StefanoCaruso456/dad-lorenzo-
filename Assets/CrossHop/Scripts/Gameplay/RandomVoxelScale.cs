using UnityEngine;

namespace CrossHop.Gameplay
{
    /// <summary>
    /// Randomises a model's length and height a little on each spawn, so a single car
    /// prefab yields compact cars, longer sedans and tall vans — shape variety on top of
    /// the colour variety from <see cref="RandomVoxelColor"/>. Collision length stays the
    /// prefab's value, so the range is kept modest.
    /// </summary>
    public sealed class RandomVoxelScale : MonoBehaviour
    {
        [Tooltip("Transform to scale. Defaults to this object.")]
        [SerializeField] private Transform target;
        [SerializeField] private Vector2 lengthRange = new(0.9f, 1.35f);
        [SerializeField] private Vector2 heightRange = new(0.9f, 1.2f);

        private Vector3 _base;
        private bool _cached;

        private void OnEnable()
        {
            if (target == null) target = transform;
            if (!_cached) { _base = target.localScale; _cached = true; }
            target.localScale = new Vector3(
                _base.x * Random.Range(lengthRange.x, lengthRange.y),
                _base.y * Random.Range(heightRange.x, heightRange.y),
                _base.z);
        }
    }
}
