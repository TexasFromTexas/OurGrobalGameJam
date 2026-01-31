using UnityEngine;
using UnityEngine.Events;

namespace BetSystem
{
    public class BetManager : MonoBehaviour
    {
        [Header("Settings")]
        public int initialPlayerChips = 20;
        public int initialBet = 1;

        [Header("State")]
        public int playerChips;
        public int currentPot;
        public int currentMaxContribution;
        public int playerContributedThisRound;
        public int enemyContributedThisRound;

        [Header("Events")]
        public UnityEvent onChipsNegative; // Trigger reveal cards
        public UnityEvent onGameOver; // Trigger death
        public UnityEvent onGameStateChanged; // UI update

        private void Start()
        {
            playerChips = initialPlayerChips;
            StartRound();
        }

        public void StartRound()
        {
            currentMaxContribution = initialBet;
            playerContributedThisRound = initialBet;
            enemyContributedThisRound = initialBet;
            
            playerChips -= initialBet;
            currentPot = initialBet * 2; // Forced 1bb each

            CheckNegativeChips();
            onGameStateChanged?.Invoke();
        }

        #region Player Actions
        public void PlayerRaise() => Raise(true);
        public void PlayerCall() => Call(true);
        public void PlayerFold() => Fold(true);
        #endregion

        #region Enemy Actions
        public void EnemyRaise() => Raise(false);
        public void EnemyCall() => Call(false);
        public void EnemyFold() => Fold(false);
        #endregion

        private void Raise(bool isPlayer)
        {
            // Exponential raising: double the previous max stake
            currentMaxContribution *= 2;
            
            if (isPlayer)
            {
                int additional = currentMaxContribution - playerContributedThisRound;
                playerChips -= additional;
                playerContributedThisRound = currentMaxContribution;
            }
            else
            {
                // Enemy has infinite chips, just update contribution
                enemyContributedThisRound = currentMaxContribution;
            }

            UpdatePot();
            CheckNegativeChips();
            onGameStateChanged?.Invoke();
        }

        private void Call(bool isPlayer)
        {
            if (isPlayer)
            {
                int needed = enemyContributedThisRound - playerContributedThisRound;
                if (needed > 0)
                {
                    playerChips -= needed;
                    playerContributedThisRound = enemyContributedThisRound;
                }
            }
            else
            {
                // Enemy matches player
                enemyContributedThisRound = playerContributedThisRound;
            }

            UpdatePot();
            CheckNegativeChips();
            onGameStateChanged?.Invoke();
        }

        private void Fold(bool isPlayer)
        {
            // If player folds, enemy wins. If enemy folds, player wins.
            if (isPlayer) EnemyWin();
            else PlayerWin();
        }

        private void UpdatePot()
        {
            currentPot = playerContributedThisRound + enemyContributedThisRound;
        }

        public void PlayerWin()
        {
            playerChips += currentPot;
            EndSettlement();
        }

        public void EnemyWin()
        {
            // Pot lost
            EndSettlement();
        }

        private void EndSettlement()
        {
            currentPot = 0;
            if (playerChips < 0)
            {
                Debug.Log("Game Over: Player chips remain negative after settlement.");
                onGameOver?.Invoke();
            }
            else
            {
                StartRound();
            }
        }

        private void CheckNegativeChips()
        {
            if (playerChips < 0)
            {
                Debug.Log("Warning: Player chips are negative. Revealing all cards.");
                onChipsNegative?.Invoke();
            }
        }
    }
}
