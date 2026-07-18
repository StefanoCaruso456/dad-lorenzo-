namespace CrossHop.Gameplay
{
    /// <summary>
    /// The kinds of lane the generator can stream. Behaviour (deadly, rideable,
    /// safe) is derived from this in one place so rules stay consistent.
    /// </summary>
    public enum LaneType
    {
        Safe,   // grass — nothing kills you here
        Road,   // cars/trucks — contact kills
        Water,  // logs/lilypads — you must ride them or you drown
        Rail    // trains — telegraphed, then deadly
    }
}
