using UnityEngine;
using UnityEngine.Events;
using System.Collections;

namespace BetSystem
{
    /// <summary>
    /// ÅÆ¾Ö½×¶ÎÃ¶¾Ù
    /// </summary>
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
        public bool isAllIn = false; // New All-In State

        [Header("Events")]
        public UnityEvent onChipsNegative; 
        public UnityEvent onGameOver;
        public UnityEvent onGameStateChanged;
        public UnityEvent<BetPhase> onPhaseChanged;
        public UnityEvent onNewRoundStarted;
        
        // NEW: Fold event to allow external checks before settlement
        public UnityEvent<bool> onFold; // param: isPlayerFolded
        public UnityEvent onAllIn; // Trigger when All-In happens

        private IEnumerator Start()
        {
            yield return null;
            playerChips = initialPlayerChips;
            StartRound();
        }

        // NEW: Turn Tracking
        public bool isPlayerTurn = false; // Default to false (Enemy starts)

        public void StartRound()
        {
            isSettlementLocked = false; 
            isAllIn = false; // Reset All-In
            currentPhase = BetPhase.Preflop;
            totalPot = 0;
            currentGlobalStake = initialBet;
            
            // Turn Logic: Match starts with Player Blind, Enemy Blind.
            // But User wants "Enemy First".
            // So on Round Start, isPlayerTurn = false.
            isPlayerTurn = false;
            
            ResetPhaseState();
            
            // Initial forced bets (Blinds)
            if (playerChips >= initialBet) ApplyContribution(true, initialBet);
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

        // Helper for UI
        public bool CanRaise()
        {
            if (!isPlayerTurn) return false; // Not your turn
            if (isAllIn) return false;
            int cost = (currentGlobalStake * 2) - playerContributedThisPhase;
            return playerChips >= cost; 
        }

        public bool CanCall()
        {
            if (!isPlayerTurn) return false; // Not your turn
            if (isAllIn) return false;
            return playerChips > 0;
        }

        private void Raise(bool isPlayer)
        {
            MarkAsActed(isPlayer);
            
            // Calculate proposed new stake
            int newStake = currentGlobalStake * 2;
            
            if (isPlayer)
            {
                int needed = newStake - playerContributedThisPhase;
                if (needed > playerChips)
                {
                    Debug.LogWarning("Player Raise blocked: Insufficient chips.");
                    return;
                }
                
                ApplyContribution(true, needed);
                if (playerChips == 0) TriggerAllIn();
                
                // Player Raised -> Turn passes to Enemy
                isPlayerTurn = false;
            }
            else // Enemy Raise
            {
                int maxStake = playerChips + playerContributedThisPhase;
                if (newStake > maxStake) newStake = maxStake;

                int needed = newStake - enemyContributedThisPhase;
                ApplyContribution(false, needed);
                currentGlobalStake = newStake;
                
                // Enemy Raised -> Turn passes to Player
                isPlayerTurn = true;
            }
            
            if (isPlayer) currentGlobalStake = newStake;

            CheckNegativeChips();
            onGameStateChanged?.Invoke();
        }

        private void Call(bool isPlayer)
        {
            MarkAsActed(isPlayer);

            if (isPlayer)
            {
                int needed = enemyContributedThisPhase - playerContributedThisPhase;
                
                // Smart All-In Logic
                if (needed > playerChips)
                {
                    needed = playerChips; 
                    TriggerAllIn();
                }
                
                if (needed > 0) ApplyContribution(true, needed);
                if (playerChips == 0) TriggerAllIn();
                
                // Player Called -> If Phase ENDS, turn resetting is handled in MoveToNextPhase.
                // If Phase continues (e.g. check-check?), usually means end.
                // But generally, Player action ends turn.
                isPlayerTurn = false; // Pass back to Enemy (or Phase Change will reset)
            }
            else
            {
                int needed = playerContributedThisPhase - enemyContributedThisPhase;
                if (needed > 0) ApplyContribution(false, needed);
                
                // Enemy Called -> Turn passes to Player
                isPlayerTurn = true;
            }

            CheckNegativeChips();
            TryMoveToNextPhase();
        }

        private void TriggerAllIn()
        {
            if (!isAllIn)
            {
                isAllIn = true;
                Debug.Log("ALL IN Triggered!");
                onAllIn?.Invoke();
            }
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

        public void SkipPhase()
        {
            Debug.Log("Forcing Phase Skip (Joker Logic)");
            MoveToNextPhase();
        }

        private void MoveToNextPhase()
        {
            if (currentPhase == BetPhase.Showdown) return;

            // NEW: If All-In, we skip betting rounds and go straight to Showdown?
            // "Forced skip to open cards" -> Showdown.
            // But we might want to reveal Flop/Turn/River visually step by step? 
            // The simple interpretation is: Force Phase = Showdown.
            // BUT usually you want to see the cards dealt.
            // Let's just advance phase. If All-In, logic elsewhere (like Bridge) might auto-open cards?
            // Or here, we just fast forward?
            // Let's try: If All-In, we still advance phase by phase, but AUTOPLAY (no betting allowed).
            // But User said "Forced skip to open cards". 
            // Let's jump to Showdown if All-In to be compliant with request.
            
            if (isAllIn)
            {
                currentPhase = BetPhase.Showdown;
            }
            else
            {
                currentPhase++;
            }

            ResetPhaseState();
            
            // New Phase -> Enemy Starts
            isPlayerTurn = false;
            
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
