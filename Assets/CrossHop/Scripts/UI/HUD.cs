using CrossHop.Economy;
using CrossHop.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CrossHop.UI
{
    /// <summary>
    /// The in-run heads-up display and game-over screen. This is the consumer side of
    /// data flow #1: the gameplay/economy systems already raise events — the HUD binds
    /// to them and draws, never polling save data. The only per-frame read is the live
    /// score (a single int) while playing. Every reference is null-guarded so a missing
    /// wire degrades gracefully instead of throwing.
    /// </summary>
    public sealed class HUD : MonoBehaviour
    {
        [Header("Systems")]
        [SerializeField] private GameManager gameManager;
        [SerializeField] private EconomyManager economy;

        [Header("Playing")]
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private TMP_Text coinText;

        [Header("Game over")]
        [SerializeField] private GameObject gameOverPanel;
        [SerializeField] private TMP_Text finalScoreText;
        [SerializeField] private TMP_Text bestScoreText;
        [SerializeField] private TMP_Text earnedText;
        [SerializeField] private Button retryButton;

        private void OnEnable()
        {
            if (economy != null) economy.OnCoinsChanged += HandleCoinsChanged;
            if (gameManager != null)
            {
                gameManager.OnRunStarted += HandleRunStarted;
                gameManager.OnRunEnded += HandleRunEnded;
            }
            if (retryButton != null) retryButton.onClick.AddListener(HandleRetry);
            if (gameOverPanel != null) gameOverPanel.SetActive(false);
        }

        // Initial draw in Start so every manager's Awake has run (avoids load-order NREs).
        private void Start()
        {
            if (economy != null) HandleCoinsChanged(economy.Coins);
        }

        private void OnDisable()
        {
            if (economy != null) economy.OnCoinsChanged -= HandleCoinsChanged;
            if (gameManager != null)
            {
                gameManager.OnRunStarted -= HandleRunStarted;
                gameManager.OnRunEnded -= HandleRunEnded;
            }
            if (retryButton != null) retryButton.onClick.RemoveListener(HandleRetry);
        }

        private void Update()
        {
            if (gameManager != null && gameManager.State == GameState.Playing && scoreText != null)
                scoreText.text = gameManager.Score.ToString();
        }

        private void HandleCoinsChanged(int total)
        {
            if (coinText != null) coinText.text = total.ToString();
        }

        private void HandleRunStarted()
        {
            if (gameOverPanel != null) gameOverPanel.SetActive(false);
            if (scoreText != null) scoreText.text = "0";
        }

        private void HandleRunEnded(int score, int earned, DeathCause cause)
        {
            if (gameOverPanel != null) gameOverPanel.SetActive(true);
            if (finalScoreText != null) finalScoreText.text = $"Score  {score}";
            if (bestScoreText != null && economy != null) bestScoreText.text = $"Best  {economy.BestScore}";
            if (earnedText != null) earnedText.text = $"+{earned}";
        }

        private void HandleRetry()
        {
            if (gameManager != null) gameManager.Restart();
        }
    }
}
