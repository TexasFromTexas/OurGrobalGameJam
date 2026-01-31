using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace BetSystem
{
    public class EnemyBetAI : MonoBehaviour
    {
        public BetManager betManager;
        public CardDeckSystem cardDeckSystem;

        [Header("Settings")]
        public float minThinkingTime = 0.3f;
        public float maxThinkingTime = 0.4f;

        // Internal flag to prevent double-acting during coroutine
        private bool isThinking = false;

        private void Start()
        {
            if (betManager == null) betManager = GetComponent<BetManager>();
            if (cardDeckSystem == null) cardDeckSystem = FindFirstObjectByType<CardDeckSystem>();

            if (betManager != null)
            {
                betManager.onGameStateChanged.AddListener(CheckTurn);
                betManager.onNewRoundStarted.AddListener(OnNewRound);
                betManager.onPhaseChanged.AddListener(OnPhaseChanged);
            }

            if (cardDeckSystem != null)
            {
                // Wait for Cards to be dealt before acting
                cardDeckSystem.OnRoundStateChanged += OnCardRoundStateChanged;
            }

            // Initial check
           // CheckTurn(); // Don't check on Start, wait for Round/Cards.
        }

        private void OnDestroy()
        {
             if (cardDeckSystem != null)
             {
                 cardDeckSystem.OnRoundStateChanged -= OnCardRoundStateChanged;
             }
        }

        private void OnCardRoundStateChanged(bool isInRound)
        {
            if (isInRound)
            {
                // Cards Dealt, Game On -> Check if we need to act
                CheckTurn();
            }
            else
            {
                StopAllCoroutines();
                isThinking = false;
            }
        }

        private bool hasRaisedThisPhase = false;

        private void OnPhaseChanged(BetPhase phase)
        {
            hasRaisedThisPhase = false;
            CheckTurn();
        }

        private void OnNewRound()
        {
            isThinking = false;
            hasRaisedThisPhase = false;
            StopAllCoroutines();
            
            // User Request: Force wait 1 second then check to ensure cards are ready
            StartCoroutine(ForceFirstTurnCheck());
        }

        private IEnumerator ForceFirstTurnCheck()
        {
            yield return new WaitForSeconds(1.0f);
            Debug.Log("[EnemyAI] Force checking turn after 1s delay...");
            CheckTurn();
        }

        private void CheckTurn()
        {
            if (betManager == null || isThinking) return;
            if (cardDeckSystem == null || !cardDeckSystem.IsInRound) return; 

            // STRICT STATE CHECK
            if (betManager.turnState == TurnState.Dealing)
            {
                 // Strictly waiting for cards
                 return;
            }

            if (betManager.turnState != TurnState.EnemyTurn)
            {
               // Debug.Log($"[EnemyAI] Not my turn. State: {betManager.turnState}");
               return; 
            }

            // 2. Settlement / End Check
            if (betManager.currentPhase == BetPhase.Showdown) return;
            if (betManager.isSettlementLocked) return;

            // 3. Do we need to act?
            // "Enemy Turn" implies we need to act.
            // But we keep the double check logic just in case logic calls CheckTurn spuriously.
            // Actually, if it IS EnemyTurn, we MUST act.
            StartCoroutine(ThinkAndAct());
        }

        private IEnumerator ThinkAndAct()
        {
            isThinking = true;
            Debug.Log("[EnemyAI] Thinking started...");

            // Simulate processing time
            float delay = Random.Range(minThinkingTime, maxThinkingTime);
            yield return new WaitForSeconds(delay);

            try
            {
                // Double check state after delay (in case player folded or game ended)
                if (betManager.currentPhase == BetPhase.Showdown || betManager.isSettlementLocked)
                {
                    Debug.Log("[EnemyAI] Stop thinking: Phase ended or Locked.");
                    yield break;
                }

                MakeDecision();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[EnemyAI] Error during decision: {e.Message}\n{e.StackTrace}");
                // Fallback: Call to keep game moving?
                if (betManager != null) betManager.EnemyCall();
            }
            finally
            {
                isThinking = false;
                Debug.Log("[EnemyAI] Thinking finished. isThinking = false");
            }
        }

        private void MakeDecision()
        {
            if (Judge.Instance == null || cardDeckSystem == null)
            {
                // Fallback: Just Call if Judge missing
                betManager.EnemyCall();
                return;
            }

            // Gather Cards
            List<PlayingCard> enemyHand = GetCardsFromTransform(cardDeckSystem.enemyHandArea);
            List<GameObject> PublicCardFlip = new List<GameObject>();
            foreach (var item in cardDeckSystem.PublicCardObjects)
            {
                if (item.GetComponent<CardFaceController>()?._isShowingBack != false)
                {
                    PublicCardFlip.Add(item);
                }
            }

            List<PlayingCard> publicCards = GetCardsFromObjects(PublicCardFlip);
            List<PlayingCard> fullHand = new List<PlayingCard>(enemyHand);
            fullHand.AddRange(publicCards);

            // Judge AI Logic
            float raiseRate, callRate, foldRate;

            // Note: Judge.GetRateNow signature: (enemyCards, raise, "fold"(Call), "drop"(Fold))
            Judge.Instance.GetRateNow(fullHand, out raiseRate, out callRate, out foldRate);
            behaviorType behavior = Judge.Instance.GetAIBehavior(fullHand);

            switch (behavior)
            {
                case behaviorType.raise:
                    // Smart Logic: Limit to 1 raise per phase
                    if (hasRaisedThisPhase)
                    {
                        Debug.Log("Enemy desired Raise but already raised this phase. Falling back to Call.");
                        betManager.EnemyCall();
                    }
                    else
                    {
                        hasRaisedThisPhase = true;
                        betManager.EnemyRaise();
                    }
                    break;
                case behaviorType.fold:
                    betManager.EnemyFold();
                    break;
                case behaviorType.call:
                    betManager.EnemyCall();
                    break;
            }
        }

        // Helpers reused from Bridge
        private List<PlayingCard> GetCardsFromObjects(List<GameObject> c) => c.Select(x => x.GetComponent<CardDisplay>()?.cardData).Where(d => d != null).ToList();
        private List<PlayingCard> GetCardsFromTransform(Transform t) => t.GetComponentsInChildren<CardDisplay>().Select(x => x.cardData).Where(d => d != null).ToList();
    }
}
