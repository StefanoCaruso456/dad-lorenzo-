using UnityEngine;

namespace CrossHop.Gameplay
{
    /// <summary>
    /// Authored template for one kind of lane. The generator picks a definition and
    /// stamps out a lane from it. All tuning (speed, spawn gaps, prefabs) is data,
    /// so designers build difficulty without touching code.
    /// </summary>
    [CreateAssetMenu(fileName = "LaneDefinition", menuName = "CrossHop/Lane Definition")]
    public sealed class LaneDefinition : ScriptableObject
    {
        public LaneType type = LaneType.Road;

        [Header("Visuals")]
        [Tooltip("Ground tile prefab tiled across the lane width.")]
        public GameObject groundPrefab;

        [Header("Obstacles")]
        [Tooltip("Obstacle/log prefab that moves along the lane. Null for Safe lanes.")]
        public GameObject obstaclePrefab;

        [Tooltip("Obstacle travel speed in cells/second.")]
        [Min(0f)] public float minSpeed = 2f;
        [Min(0f)] public float maxSpeed = 4f;

        [Tooltip("Seconds between obstacle spawns.")]
        [Min(0.05f)] public float minSpawnInterval = 1.2f;
        [Min(0.05f)] public float maxSpawnInterval = 2.5f;

        [Tooltip("How many cells an obstacle occupies (a truck/log spans several).")]
        [Min(1)] public int minLength = 1;
        [Min(1)] public int maxLength = 2;

        public bool IsDeadlyOnContact => type == LaneType.Road || type == LaneType.Rail;
        public bool RequiresRiding => type == LaneType.Water;

        private void OnValidate()
        {
            maxSpeed = Mathf.Max(minSpeed, maxSpeed);
            maxSpawnInterval = Mathf.Max(minSpawnInterval, maxSpawnInterval);
            maxLength = Mathf.Max(minLength, maxLength);
        }
    }
}
