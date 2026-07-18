namespace CrossHop.Characters.Abilities
{
    using UnityEngine;

    /// <summary>
    /// Pixel's <b>Coin Magnet</b>: each frame, pulls nearby field coins toward the
    /// player so they're collected without a detour. Radius and speed are authored in
    /// grid cells so the feel is independent of world scale.
    /// </summary>
    [CreateAssetMenu(fileName = "CoinMagnet", menuName = "CrossHop/Abilities/Coin Magnet")]
    public sealed class CoinMagnetAbility : CharacterAbility
    {
        [Tooltip("Pull radius, in grid cells.")]
        [Min(0.5f)] public float radiusCells = 3f;
        [Tooltip("Pull speed, in grid cells per second.")]
        [Min(0.5f)] public float pullSpeedCells = 8f;

        public override AbilityRuntime CreateRuntime() => new Runtime(radiusCells, pullSpeedCells);

        private sealed class Runtime : AbilityRuntime
        {
            private readonly float _radiusCells, _pullSpeedCells;
            public Runtime(float radiusCells, float pullSpeedCells)
            {
                _radiusCells = radiusCells;
                _pullSpeedCells = pullSpeedCells;
            }

            public override void Tick(float deltaTime)
            {
                if (Context.CoinField == null || Context.Player == null) return;
                float cell = Context.CellSize;
                Context.CoinField.PullToward(
                    Context.Player.position,
                    _radiusCells * cell,
                    _pullSpeedCells * cell,
                    deltaTime);
            }
        }
    }
}
