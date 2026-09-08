using UnityEngine;

namespace CrossHop.Gameplay
{
    /// <summary>
    /// A collectible coin on the field. Pooled and driven entirely by <see cref="CoinField"/>
    /// (which handles spawning, pickup detection and magnet pulls) — this component just
    /// carries its value and a gentle idle spin for readability. It colours itself gold so
    /// "grab" reads instantly apart from red "dodge" hazards.
    /// </summary>
    public sealed class Coin : MonoBehaviour
    {
        [Tooltip("Degrees/second the coin spins so it reads as a pickup.")]
        [SerializeField] private float spinSpeed = 120f;

        public int Value { get; private set; }

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static Material _goldMat;

        private void Awake()
        {
            var r = GetComponent<Renderer>();
            if (r != null) r.sharedMaterial = GoldMat();
        }

        public void Init(int value) => Value = value;

        private void Update() => transform.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.World);

        private static Material GoldMat()
        {
            if (_goldMat != null) return _goldMat;
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (shader == null) return null;
            var c = new Color(1f, 0.81f, 0.27f);
            _goldMat = new Material(shader) { color = c };
            _goldMat.SetColor(BaseColorId, c);
            return _goldMat;
        }
    }
}
