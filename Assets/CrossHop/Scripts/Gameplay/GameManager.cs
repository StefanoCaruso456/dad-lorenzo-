using System;
using CrossHop.Characters;
using CrossHop.Economy;
using UnityEngine;

namespace CrossHop.Gameplay
{
    public enum GameState { Menu, Playing, GameOver }

    /// <summary>
    /// Top-level run orchestrator. Wires the gameplay systems together, tracks run
    /// state and score, applies the selected character's ability, and banks coins
    /// on death. Deliberately thin — real logic lives in the systems it coordinates.
    /// </summary>
    public sealed class GameManager : MonoBehaviour
    {
        [Header("Systems")]
        [SerializeField] private LaneGenerator laneGenerator;
        [SerializeField] private PlayerController player;
        [SerializeField] private CameraFollow cameraFollow;
        [SerializeField] private EconomyManager economy;

        [Header("World")]
        [Tooltip("World loaded when the selected character has none (also the very first launch).")]
        [SerializeField] private WorldTheme fallbackWorld;

        [Header("Scoring")]
        [Tooltip("Coins awarded per row of distance, before ability modifiers.")]
        [SerializeField] private int coinsPerRow = 1;

        public GameState State { get; private set; } = GameState.Menu;
        public int Score { get; private set; }

        public event Action OnRunStarted;
        public event Action<int, int, DeathCause> OnRunEnded; // score, coinsEarned, cause

        private CharacterAbility _activeAbility;
        private AbilityContext _abilityContext;

        private void OnEnable()
        {
            player.OnRowAdvanced += HandleRowAdvanced;
            player.OnDied += HandleDeath;
        }

        private void OnDisable()
        {
            player.OnRowAdvanced -= HandleRowAdvanced;
            player.OnDied -= HandleDeath;
        }

        public void StartRun()
        {
            Score = 0;

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

            _abilityContext = new AbilityContext(player.transform);
            _activeAbility = character != null ? character.ability : null;
            _activeAbility?.OnRunStart(_abilityContext);

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

        private void HandleDeath(DeathCause cause)
        {
            if (State != GameState.Playing) return;
            State = GameState.GameOver;
            cameraFollow.Stop();
            _activeAbility?.OnRunEnd(_abilityContext);

            int baseCoins = Score * coinsPerRow;
            int earned = _activeAbility != null ? _activeAbility.ModifyCoinReward(baseCoins) : baseCoins;

            economy.AddCoins(earned);
            economy.ReportScore(Score);
            economy.Flush();

            OnRunEnded?.Invoke(Score, earned, cause);
        }
    }
}
