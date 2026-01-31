using UnityEngine;
using UnityEngine.Events;
using System.Collections;

namespace BetSystem
{
    public enum BetPhase
    {
        Preflop,
        Flop,
        Turn,
        River,
        Showdown
    }

    public class BetManager : MonoBehaviour
    {
        [Header("Settings")]
        public int initialPlayerChips = 20;
        public int initialBet = 1;

        [Header("Ability Costs")]
        public int costDeleteCard = 5;
        public int costSwapCard = 5;
        public int costAddPublic = 5;
        public int costDrawCard = 5;
        public int costPeekCard = 5;

        [Header("State")]
        public int playerChips;
        public int totalPot;
        
        [Header("Round Logic")]
        public int currentGlobalStake; 
        public int playerContributedThisPhase;
        public int enemyContributedThisPhase;
        public BetPhase currentPhase;

        public bool playerActedThisPhase;
        public bool enemyActedThisPhase;

        // NEW: Locking mechanism for Joker interception
        public bool isSettlementLocked = false; 

        [Header("Events")]
        public UnityEvent onChipsNegative; 
        public UnityEvent onGameOver;
        public UnityEvent onGameStateChanged;
        public UnityEvent<BetPhase> onPhaseChanged;
        public UnityEvent onNewRoundStarted;
        
        // NEW: Fold event to allow external checks before settlement
        public UnityEvent<bool> onFold; // param: isPlayerFolded

        private IEnumerator Start()
        {
            yield return null;
            playerChips = initialPlayerChips;
            StartRound();
        }

        public void StartRound()
        {
            isSettlementLocked = false; // Reset lock
            currentPhase = BetPhase.Preflop;
            totalPot = 0;
            currentGlobalStake = initialBet;
            ResetPhaseState();
            
            ApplyContribution(true, initialBet);
            ApplyContribution(false, initialBet);
            playerActedThisPhase = false;
            enemyActedThisPhase = false;

            CheckNegativeChips();
            onGameStateChanged?.Invoke();
            onPhaseChanged?.Invoke(currentPhase);
            onNewRoundStarted?.Invoke(); 
        }

        #region Generic Ability API
        /// <summary>
        /// Attempts to spend chips. Returns true if successful.
        /// </summary>
        /// <param name="amount">Amount to spend</param>
        /// <returns>True if chips were deducted, False if insufficient funds</returns>
        public bool TrySpendChips(int amount)
        {
            if (playerChips >= amount)
            {
                playerChips -= amount;
                CheckNegativeChips(); 
                onGameStateChanged?.Invoke();
                return true;
            }
            return false;
        }
        #endregion

        #region Actions
        public void PlayerRaise() => Raise(true);
        public void PlayerCall() => Call(true);
        public void PlayerFold() => Fold(true);

        public void EnemyRaise() => Raise(false);
        public void EnemyCall() => Call(false);
        public void EnemyFold() => Fold(false);
        #endregion

        private void Raise(bool isPlayer)
        {
            MarkAsActed(isPlayer);
            currentGlobalStake *= 2;
            
            if (isPlayer)
            {
                int needed = currentGlobalStake - playerContributedThisPhase;
                if (needed > 0) ApplyContribution(true, needed);
            }
            else
            {
                int needed = currentGlobalStake - enemyContributedThisPhase;
                if (needed > 0) ApplyContribution(false, needed);
            }
            
            CheckNegativeChips();
            onGameStateChanged?.Invoke();
        }

        private void Call(bool isPlayer)
        {
            MarkAsActed(isPlayer);

            if (isPlayer)
            {
                int needed = enemyContributedThisPhase - playerContributedThisPhase;
                if (needed > 0) ApplyContribution(true, needed);
            }
            else
            {
                int needed = playerContributedThisPhase - enemyContributedThisPhase;
                if (needed > 0) ApplyContribution(false, needed);
            }

            CheckNegativeChips();
            TryMoveToNextPhase();
        }

        private void Fold(bool isPlayer)
        {
            // Fire event first to allow interception
            onFold?.Invoke(isPlayer);

            // If an external script (like Bridge) locked the settlement, we stop here.
            if (isSettlementLocked)
            {
                Debug.Log("Fold settlement intercepted/locked by external logic (e.g. Joker).");
                return;
            }

            if (isPlayer) EnemyWin();
            else PlayerWin();
        }

        private void MarkAsActed(bool isPlayer)
        {
            if (isPlayer) playerActedThisPhase = true;
            else enemyActedThisPhase = true;
        }

        private void TryMoveToNextPhase()
        {
            bool contributionsEqual = playerContributedThisPhase == enemyContributedThisPhase;
            bool bothActed = playerActedThisPhase && enemyActedThisPhase;

            if (contributionsEqual && bothActed)
            {
                MoveToNextPhase();
            }
            else
            {
                onGameStateChanged?.Invoke();
            }
        }

        private void ApplyContribution(bool isPlayer, int amount)
        {
            if (isPlayer)
            {
                playerChips -= amount;
                playerContributedThisPhase += amount;
            }
            else
            {
                enemyContributedThisPhase += amount;
            }
            totalPot += amount;
        }

        private void ResetPhaseState()
        {
            playerContributedThisPhase = 0;
            enemyContributedThisPhase = 0;
            playerActedThisPhase = false;
            enemyActedThisPhase = false;
        }

        private void MoveToNextPhase()
        {
            if (currentPhase == BetPhase.Showdown) return;

            currentPhase++;
            ResetPhaseState();
            
            Debug.Log($"Transitioning to {currentPhase}. Stake remains {currentGlobalStake}");
            onPhaseChanged?.Invoke(currentPhase);
            onGameStateChanged?.Invoke();
        }

        public void PlayerWin()
        {
            playerChips += totalPot;
            EndSettlement();
        }

        public void EnemyWin()
        {
            EndSettlement();
        }

        private void EndSettlement()
        {
            if (playerChips < 0)
            {
                onGameOver?.Invoke();
            }
            else
            {
                Debug.Log("Round Ended. Restarting...");
                StartRound();
            }
        }

        private void CheckNegativeChips()
        {
            if (playerChips < 0) onChipsNegative?.Invoke();
        }
    }
}
