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
        public TextMeshProUGUI phaseText;
        public TextMeshProUGUI statusText;

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
            if (chipsText) chipsText.text = $"Chips: {betManager.playerChips} bb";
            if (potText) potText.text = $"Pot: {betManager.totalPot} bb";
            if (phaseText) phaseText.text = $"Phase: {betManager.currentPhase}";
            
            // NEW: Disable Buttons if cannot afford (or All-in state)
            if (playerRaiseBtn) 
            {
                playerRaiseBtn.interactable = betManager.CanRaise();
            }

            if (playerCallBtn)
            {
                // Can always call (allows All-In) if you have any chips?
                // Or if needed == 0 (Check)?
                // User said: "disable if... chips negative". 
                // CanCall checks if chips > 0.
                bool interactable = betManager.CanCall();
                playerCallBtn.interactable = interactable;

                // Update text to indicate "All In" if funds low?
                // TextMeshProUGUI btnText = playerCallBtn.GetComponentInChildren<TextMeshProUGUI>();
                // if (btnText) 
                // {
                //      int needed = betManager.enemyContributedThisPhase - betManager.playerContributedThisPhase;
                //      if (needed > betManager.playerChips) btnText.text = "ALL IN"; 
                //      else btnText.text = "CALL";
                // }
            }

            if (statusText) 
            {
                // Show Acted status for debug
                string pStatus = betManager.playerActedThisPhase ? "[Act]" : "[Wait]";
                string eStatus = betManager.enemyActedThisPhase ? "[Act]" : "[Wait]";
                string allInStatus = betManager.isAllIn ? " [ALL-IN]" : "";

                statusText.text = $"Stake Level: {betManager.currentGlobalStake}\n" +
                                  $"P: {betManager.playerContributedThisPhase} {pStatus} | E: {betManager.enemyContributedThisPhase} {eStatus}" +
                                  $"{allInStatus}";
            }
        }
    }
}
