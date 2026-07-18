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

            laneGenerator.ResetWorld();
            player.ResetToStart();
            cameraFollow.Begin();

            _abilityContext = new AbilityContext(player.transform);
            _activeAbility = economy.SelectedCharacter != null ? economy.SelectedCharacter.ability : null;
            _activeAbility?.OnRunStart(_abilityContext);

            State = GameState.Playing;
            OnRunStarted?.Invoke();
        }

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
