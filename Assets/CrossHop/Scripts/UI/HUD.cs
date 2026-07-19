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
    /// score (a single int) while playing.
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
            economy.OnCoinsChanged += HandleCoinsChanged;
            gameManager.OnRunStarted += HandleRunStarted;
            gameManager.OnRunEnded += HandleRunEnded;
            if (retryButton != null) retryButton.onClick.AddListener(HandleRetry);

            HandleCoinsChanged(economy.Coins);
            if (gameOverPanel != null) gameOverPanel.SetActive(false);
        }

        private void OnDisable()
        {
            economy.OnCoinsChanged -= HandleCoinsChanged;
            gameManager.OnRunStarted -= HandleRunStarted;
            gameManager.OnRunEnded -= HandleRunEnded;
            if (retryButton != null) retryButton.onClick.RemoveListener(HandleRetry);
        }

        private void Update()
        {
            if (gameManager.State == GameState.Playing && scoreText != null)
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
            if (bestScoreText != null) bestScoreText.text = $"Best  {economy.BestScore}";
            if (earnedText != null) earnedText.text = $"+{earned}";
        }

        private void HandleRetry() => gameManager.Restart();
    }
}
