using UnityEngine;

namespace CrossHop.Gameplay
{
    /// <summary>
    /// Maps distance travelled to difficulty knobs. Authored as an asset so the
    /// ramp can be tuned visually. Keeps "how hard is it right now" in one place.
    /// </summary>
    [CreateAssetMenu(fileName = "DifficultyCurve", menuName = "CrossHop/Difficulty Curve")]
    public sealed class DifficultyCurve : ScriptableObject
    {
        [Tooltip("Chance (0..1) a generated lane is a Safe lane, by distance (rows).")]
        public AnimationCurve safeLaneChance =
            AnimationCurve.Linear(0f, 0.5f, 200f, 0.15f);

        [Tooltip("Multiplier applied to obstacle speed, by distance (rows).")]
        public AnimationCurve speedMultiplier =
            AnimationCurve.Linear(0f, 1f, 200f, 1.8f);

        [Tooltip("Multiplier applied to spawn interval (lower = denser), by distance.")]
        public AnimationCurve spawnIntervalMultiplier =
            AnimationCurve.Linear(0f, 1f, 200f, 0.6f);

        public float SafeLaneChance(int row) => Mathf.Clamp01(safeLaneChance.Evaluate(row));
        public float SpeedMultiplier(int row) => Mathf.Max(0.1f, speedMultiplier.Evaluate(row));
        public float SpawnIntervalMultiplier(int row) => Mathf.Max(0.1f, spawnIntervalMultiplier.Evaluate(row));
    }
}
