namespace CrossHop.Gameplay
{
    /// <summary>
    /// Per-run multipliers applied on top of the base hop feel in <see cref="Core.GridSettings"/>.
    /// Abilities (e.g. Low-G Hop) tweak these without touching the grid config, so the
    /// change is scoped to the run and resets automatically.
    /// </summary>
    public struct HopProfile
    {
        public float HeightMultiplier;
        public float DurationMultiplier;

        public static HopProfile Default => new() { HeightMultiplier = 1f, DurationMultiplier = 1f };
    }
}
