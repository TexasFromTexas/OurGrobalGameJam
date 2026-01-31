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

    public enum TurnState
    {
        None,
        PlayerTurn,
        EnemyTurn,
        Settlement,
        Dealing // NEW
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
        public bool isAllIn = false; 

        [Header("Round Logic")]
        public TurnState turnState = TurnState.None;

        [Header("Events")]
        public UnityEvent onChipsNegative; 
        public UnityEvent onGameOver;
        public UnityEvent onGameStateChanged;
        public UnityEvent<BetPhase> onPhaseChanged;
        public UnityEvent onNewRoundStarted;
        
        public UnityEvent<bool> onFold; 
        public UnityEvent onAllIn; 

        private IEnumerator Start()
        {
            yield return null;
            playerChips = initialPlayerChips;
            StartRound();
        }

        public void StartRound()
        {
            isSettlementLocked = false; 
            isAllIn = false; 
            currentPhase = BetPhase.Preflop;
            totalPot = 0;
            currentGlobalStake = initialBet;
            
            // STRICT STATE: Dealing first (Preflop dealing)
            turnState = TurnState.Dealing;
            Debug.Log($"[BetManager] Round Start. State: {turnState}");

            ResetPhaseState();
            
            // Initial forced bets
            if (playerChips >= initialBet) ApplyContribution(true, initialBet);
            ApplyContribution(false, initialBet); 

            playerActedThisPhase = false;
            enemyActedThisPhase = false;
            
            CheckNegativeChips();
            onGameStateChanged?.Invoke();
            onPhaseChanged?.Invoke(currentPhase);
            onNewRoundStarted?.Invoke(); 
        }
        public void StartBettingPhase()
        {
            if (currentPhase == BetPhase.Showdown) return;

            turnState = TurnState.EnemyTurn;
            Debug.Log($"[BetManager] Betting Started ({currentPhase}). State: {turnState}");
            
            // Trigger UI update if needed, or rely on GameStateChanged from callers?
            onGameStateChanged?.Invoke();
        }

        private void MoveToNextPhase()
        {
            if (currentPhase == BetPhase.Showdown) return;

            // NEW: If All-In, we skip betting rounds and go straight to Showdown?
            if (isAllIn)
            {
                currentPhase = BetPhase.Showdown;
            }
            else
            {
                currentPhase++;
            }

            ResetPhaseState();
            
            // New Phase -> Dealing State first
            turnState = TurnState.Dealing;
            Debug.Log($"[BetManager] Phase Changed to {currentPhase}. State: {turnState} (Waiting for Reveal)");
            
            onPhaseChanged?.Invoke(currentPhase);
            onGameStateChanged?.Invoke();
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
            if (turnState != TurnState.PlayerTurn) return false; 
            if (isAllIn) return false;
            int cost = (currentGlobalStake * 2) - playerContributedThisPhase;
            return playerChips >= cost; 
        }

        public bool CanCall()
        {
            if (turnState != TurnState.PlayerTurn) return false;
            if (isAllIn) return false;
            return playerChips > 0;
        }

        private void Raise(bool isPlayer)
        {
            MarkAsActed(isPlayer);
            int newStake = currentGlobalStake * 2;
            
            if (isPlayer)
            {
                if (turnState != TurnState.PlayerTurn) return; // Generic guard

                int needed = newStake - playerContributedThisPhase;
                if (needed > playerChips)
                {
                    Debug.LogWarning("Player Raise blocked: Insufficient chips.");
                    return;
                }
                
                ApplyContribution(true, needed);
                if (playerChips == 0) TriggerAllIn();
                
                // Player Raised -> Turn passes to Enemy
                turnState = TurnState.EnemyTurn;
                Debug.Log($"[BetManager] Player Raised. State: {turnState}");
            }
            else // Enemy Raise
            {
                if (turnState != TurnState.EnemyTurn) return; // Generic guard

                int maxStake = playerChips + playerContributedThisPhase;
                if (newStake > maxStake) newStake = maxStake;

                int needed = newStake - enemyContributedThisPhase;
                ApplyContribution(false, needed);
                currentGlobalStake = newStake;
                
                // Enemy Raised -> Turn passes to Player
                turnState = TurnState.PlayerTurn;
                Debug.Log($"[BetManager] Enemy Raised. State: {turnState}");
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
                if (turnState != TurnState.PlayerTurn) return;

                int needed = enemyContributedThisPhase - playerContributedThisPhase;
                // Debug Log for diagnostics
                Debug.Log($"[BetManager] Player Call. Chips: {playerChips}, Needed: {needed}, EnemyContrib: {enemyContributedThisPhase}, PlayerContrib: {playerContributedThisPhase}");

                if (needed > playerChips) { needed = playerChips; TriggerAllIn(); }
                if (needed > 0) ApplyContribution(true, needed);
                if (playerChips == 0) TriggerAllIn();
                
                // Player Called -> Logic to check phase end
                // If phase doesn't end, theoretically pass to Enemy logic (though usually phase ends)
                turnState = TurnState.EnemyTurn; 
                Debug.Log($"[BetManager] Player Called. State: {turnState}");
            }
            else
            {
                if (turnState != TurnState.EnemyTurn) return;

                int needed = playerContributedThisPhase - enemyContributedThisPhase;
                if (needed > 0) ApplyContribution(false, needed);
                
                // Enemy Called -> Turn passes to Player
                turnState = TurnState.PlayerTurn;
                Debug.Log($"[BetManager] Enemy Called. State: {turnState}");
            }

            CheckNegativeChips();
            TryMoveToNextPhase();
        }

        private void TriggerAllIn()
        {
            if (!isAllIn)
            {
                isAllIn = true;
                Debug.Log($"[BetManager] ALL IN Triggered! PlayerChips: {playerChips}, StackTrace: {System.Environment.StackTrace}");
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
