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
        public float minThinkingTime = 1.0f;
        public float maxThinkingTime = 2.0f;
        
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
            }
            
            // Initial check
            CheckTurn();
        }

        private void OnNewRound()
        {
            isThinking = false;
            StopAllCoroutines();
        }

        private void CheckTurn()
        {
            if (betManager == null || isThinking) return;

            // 1. Basic Turn Check
            // We only act if the Player has already acted in this phase.
            // (Assumes Player goes first logic)
            if (!betManager.playerActedThisPhase) return;

            // 2. Settlement / End Check
            if (betManager.currentPhase == BetPhase.Showdown) return;
            if (betManager.isSettlementLocked) return;

            // 3. Do we need to act?
            // - If we haven't acted yet this phase
            // - OR if player raised (we contributed less)
            bool needToAct = !betManager.enemyActedThisPhase 
                             || (betManager.enemyContributedThisPhase < betManager.playerContributedThisPhase);

            if (needToAct)
            {
                StartCoroutine(ThinkAndAct());
            }
        }

        private IEnumerator ThinkAndAct()
        {
            isThinking = true;
            
            // Simulate processing time
            float delay = Random.Range(minThinkingTime, maxThinkingTime);
            yield return new WaitForSeconds(delay);

            // Double check state after delay (in case player folded or game ended)
            if (betManager.currentPhase == BetPhase.Showdown || betManager.isSettlementLocked) 
            {
                isThinking = false;
                yield break;
            }

            MakeDecision();
            
            isThinking = false;
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
            List<PlayingCard> publicCards = GetCardsFromObjects(cardDeckSystem.PublicCardObjects);
            
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
                   betManager.EnemyRaise();
                   break;
               case behaviorType.fold:
                   betManager.EnemyFold();
                   break;
               case behaviorType.drop:
                   betManager.EnemyCall();
                   break;
           }
        }

        // Helpers reused from Bridge
        private List<PlayingCard> GetCardsFromObjects(List<GameObject> c) => c.Select(x => x.GetComponent<CardDisplay>()?.cardData).Where(d => d != null).ToList();
        private List<PlayingCard> GetCardsFromTransform(Transform t) => t.GetComponentsInChildren<CardDisplay>().Select(x => x.cardData).Where(d => d != null).ToList();
    }
}
