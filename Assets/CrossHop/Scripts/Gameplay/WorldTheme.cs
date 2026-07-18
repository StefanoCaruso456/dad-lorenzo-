using UnityEngine;

namespace CrossHop.Gameplay
{
    /// <summary>
    /// A complete world — the biome a character brings with them. One authored asset
    /// bundles everything the run needs to reskin the four lane archetypes: the lane
    /// set, the difficulty ramp, presentation, and an optional signature hazard.
    ///
    /// This is the single source of truth for "what world am I playing". A character
    /// points at its <see cref="WorldTheme"/>; the <see cref="LaneGenerator"/> is
    /// configured from it at run start. Adding a world = authoring one of these.
    /// </summary>
    [CreateAssetMenu(fileName = "World", menuName = "CrossHop/World Theme")]
    public sealed class WorldTheme : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Stable unique id used in save data / analytics. Never change once shipped.")]
        public string id;
        public string displayName;

        [Header("Lanes")]
        [Tooltip("The safe (grass) lane for this world. Required — used for the start strip.")]
        public LaneDefinition safeLane;

        [Tooltip("Hazard lane variants (road / water / rail dressings). Generator picks among these.")]
        public LaneDefinition[] hazardLanes;

        [Tooltip("Difficulty ramp for this world. Falls back to a linear default if empty.")]
        public DifficultyCurve difficulty;

        [Header("Presentation")]
        public Material groundMaterial;
        public Material skyboxMaterial;
        public AudioClip music;
        [Tooltip("Accent colour used to tint this world's UI / selection card.")]
        public Color uiTint = Color.white;

        [Header("Signature (optional)")]
        [Tooltip("A single readable twist prefab — low-gravity field, lava pulse, boulder chase. Leave empty for a straight world.")]
        public GameObject signatureHazardPrefab;

        public bool IsValid => safeLane != null && hazardLanes != null && hazardLanes.Length > 0;
    }
}
