using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace BetSystem
{
    public class BetUIController : MonoBehaviour
    {
        public BetManager betManager;

        [Header("UI Elements")]
        public TextMeshProUGUI chipsText;
        public TextMeshProUGUI potText;
        public TextMeshProUGUI playerStatusText;
        public TextMeshProUGUI enemyStatusText;

        [Header("Player Buttons")]
        public Button playerRaiseBtn;
        public Button playerCallBtn;
        public Button playerFoldBtn;

        [Header("Enemy Buttons")]
        public Button enemyRaiseBtn;
        public Button enemyCallBtn;
        public Button enemyFoldBtn;

        [Header("Manual Win Control")]
        public Button winPlayerBtn;
        public Button winEnemyBtn;

        private void Start()
        {
            if (betManager == null) betManager = FindFirstObjectByType<BetManager>();

            // Player actions
            if (playerRaiseBtn) playerRaiseBtn.onClick.AddListener(betManager.PlayerRaise);
            if (playerCallBtn) playerCallBtn.onClick.AddListener(betManager.PlayerCall);
            if (playerFoldBtn) playerFoldBtn.onClick.AddListener(betManager.PlayerFold);

            // Enemy actions
            if (enemyRaiseBtn) enemyRaiseBtn.onClick.AddListener(betManager.EnemyRaise);
            if (enemyCallBtn) enemyCallBtn.onClick.AddListener(betManager.EnemyCall);
            if (enemyFoldBtn) enemyFoldBtn.onClick.AddListener(betManager.EnemyFold);

            // Win control
            if (winPlayerBtn) winPlayerBtn.onClick.AddListener(betManager.PlayerWin);
            if (winEnemyBtn) winEnemyBtn.onClick.AddListener(betManager.EnemyWin);

            betManager.onGameStateChanged.AddListener(UpdateUI);
            UpdateUI();
        }

        public void UpdateUI()
        {
            if (chipsText) chipsText.text = $"Player: {betManager.playerChips} bb";
            if (potText) potText.text = $"Pot: {betManager.currentPot} bb";
            
            if (playerStatusText) playerStatusText.text = $"P Contrib: {betManager.playerContributedThisRound}";
            if (enemyStatusText) enemyStatusText.text = $"E Contrib: {betManager.enemyContributedThisRound}";

            // Optional: Toggle interactability based on turn (not requested yet, but good practice)
        }
    }
}
