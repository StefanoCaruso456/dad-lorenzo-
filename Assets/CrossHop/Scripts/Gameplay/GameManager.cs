using System;
using CrossHop.Characters;
using CrossHop.Economy;
using UnityEngine;

namespace CrossHop.Gameplay
{
    public enum GameState { Menu, Playing, GameOver }

    /// <summary>
    /// Top-level run orchestrator. Wires the gameplay systems together, owns the active
    /// ability's per-run <see cref="AbilityRuntime"/>, tracks score and collected coins,
    /// and banks the reward on death. Deliberately thin — real logic lives in the systems
    /// it coordinates and in the ability runtimes it drives.
    /// </summary>
    public sealed class GameManager : MonoBehaviour
    {
        [Header("Systems")]
        [SerializeField] private LaneGenerator laneGenerator;
        [SerializeField] private PlayerController player;
        [SerializeField] private CameraFollow cameraFollow;
        [SerializeField] private EconomyManager economy;
        [Tooltip("Optional — field coins & the Coin Magnet ability. Leave empty to disable field coins.")]
        [SerializeField] private CoinField coinField;

        [Header("World")]
        [Tooltip("World loaded when the selected character has none (also the very first launch).")]
        [SerializeField] private WorldTheme fallbackWorld;

        [Header("Scoring")]
        [Tooltip("Coins awarded per row of distance, before ability modifiers.")]
        [SerializeField] private int coinsPerRow = 1;

        public GameState State { get; private set; } = GameState.Menu;
        public int Score { get; private set; }
        public int CoinsThisRun => _runCoins;

        public event Action OnRunStarted;
        public event Action<int, int, DeathCause> OnRunEnded; // score, coinsEarned, cause

        private AbilityRuntime _ability;
        private int _runCoins;

        private void OnEnable()
        {
            player.OnRowAdvanced += HandleRowAdvanced;
            player.OnDied += HandleDeath;
            if (coinField != null) coinField.OnCoinCollected += HandleCoinCollected;
        }

        private void OnDisable()
        {
            player.OnRowAdvanced -= HandleRowAdvanced;
            player.OnDied -= HandleDeath;
            if (coinField != null) coinField.OnCoinCollected -= HandleCoinCollected;
        }

        private void Update()
        {
            if (State == GameState.Playing) _ability?.Tick(Time.deltaTime);
        }

        public void StartRun()
        {
            Score = 0;
            _runCoins = 0;

            // Data flow: selected character -> its world -> configure the generator.
            CharacterData character = economy.SelectedCharacter;
            WorldTheme world = character != null && character.defaultWorld != null
                ? character.defaultWorld
                : fallbackWorld;

            if (world == null)
            {
                Debug.LogError("[GameManager] No world to load — assign a fallbackWorld or give the selected character a defaultWorld.");
                return;
            }

            laneGenerator.Configure(world);
            laneGenerator.ResetWorld();
            player.ResetToStart();
            cameraFollow.Begin();
            if (coinField != null) coinField.Begin();

            // Spin up the ability's run-scoped runtime and apply its hooks.
            _ability = character != null && character.ability != null ? character.ability.CreateRuntime() : null;
            var context = new AbilityContext(player.transform, coinField, laneGenerator.Grid.cellSize);
            _ability?.OnRunStart(context);

            HopProfile hop = HopProfile.Default;
            _ability?.ModifyHopProfile(ref hop);
            player.SetHopProfile(hop);
            player.DeathGuard = _ability != null ? _ability.TryAbsorbDeath : null;

            State = GameState.Playing;
            OnRunStarted?.Invoke();
        }

        /// <summary>Restart into a fresh run (user flow: the game-over "Retry" button).</summary>
        public void Restart() => StartRun();

        private void HandleRowAdvanced(int furthestRow)
        {
            if (State != GameState.Playing) return;
            Score = furthestRow;
        }

        private void HandleCoinCollected(int value)
        {
            if (State == GameState.Playing) _runCoins += value;
        }

        private void HandleDeath(DeathCause cause)
        {
            if (State != GameState.Playing) return;
            State = GameState.GameOver;

            cameraFollow.Stop();
            if (coinField != null) coinField.Stop();
            _ability?.OnRunEnd();
            player.DeathGuard = null;

            int baseCoins = Score * coinsPerRow + _runCoins;
            int earned = _ability != null ? _ability.ModifyCoinReward(baseCoins) : baseCoins;
            _ability = null;

            economy.AddCoins(earned);
            economy.ReportScore(Score);
            economy.Flush();

            OnRunEnded?.Invoke(Score, earned, cause);
        }
    }
}
