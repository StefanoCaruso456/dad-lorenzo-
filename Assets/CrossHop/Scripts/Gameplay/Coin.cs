using UnityEngine;

namespace CrossHop.Gameplay
{
    /// <summary>
    /// A collectible coin on the field. Pooled and driven entirely by <see cref="CoinField"/>
    /// (which handles spawning, pickup detection and magnet pulls) — this component just
    /// carries its value and a gentle idle spin for readability.
    /// </summary>
    public sealed class Coin : MonoBehaviour
    {
        [Tooltip("Degrees/second the coin spins so it reads as a pickup.")]
        [SerializeField] private float spinSpeed = 120f;

        public int Value { get; private set; }

        public void Init(int value) => Value = value;

        private void Update() => transform.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.World);
    }
}
