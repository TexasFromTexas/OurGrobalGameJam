using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using System.Linq; 

namespace BetSystem
{
    public class BetCardSystemBridge : MonoBehaviour
    {
        public BetManager betManager;
        public CardDeckSystem cardDeckSystem;

        [Header("Joker Logic")]
        public bool stopShowdownIfJokerFound = true; 
        public UnityEvent<bool, bool> onJokerDetected; // (hasPlayerJoker, hasEnemyJoker)

        private void Start()
        {
            if (betManager == null) betManager = GetComponent<BetManager>();
            if (cardDeckSystem == null) cardDeckSystem = FindFirstObjectByType<CardDeckSystem>();

            if (betManager != null)
            {
                betManager.onChipsNegative.AddListener(RevealAllCommunityCards);
                betManager.onPhaseChanged.AddListener(OnPhaseChanged);
                betManager.onNewRoundStarted.AddListener(OnBettingRoundRestarted);
                
                // NEW: Listen for Fold to check Jokers
                betManager.onFold.AddListener(OnFoldCheckJokers);
            }
        }

        private void OnFoldCheckJokers(bool isPlayerFolded)
        {
            // When someone folds, we check for *revealed* Jokers immediately.
            // Note: Hand cards of the folder are likely hidden. Public cards might be revealed.
            CheckAndTriggerJokerLogic(true); 
            // 'true' here means we want to lock settlement if found. 
            // If Check returns true (Joker found), it handles the locking.
        }

        private void OnBettingRoundRestarted()
        {
            if (cardDeckSystem == null) return;
            if (cardDeckSystem.IsInRound)
            {
                if (cardDeckSystem.endRoundButton && cardDeckSystem.endRoundButton.interactable)
                    cardDeckSystem.endRoundButton.onClick.Invoke();
            }

            if (cardDeckSystem.startRoundButton)
            {
                if (cardDeckSystem.startRoundButton.interactable)
                    cardDeckSystem.startRoundButton.onClick.Invoke();
            }
        }

        private void OnPhaseChanged(BetPhase phase)
        {
            switch (phase)
            {
                case BetPhase.Flop:
                    RevealCommunityCards(3);
                    break;
                case BetPhase.Turn:
                    RevealCommunityCards(4);
                    break;
                case BetPhase.River:
                    RevealCommunityCards(5);
                    break;
                case BetPhase.Showdown:
                    RevealAllCommunityCards();
                    PerformShowdown();
                    break;
            }
        }

        private void PerformShowdown()
        {
            if (cardDeckSystem == null) return;

            // 1. Reveal Enemy Cards (Showdown always reveals all)
            RevealCardsInTransform(cardDeckSystem.enemyHandArea);

            // 2. Check for Jokers (Showdown means everything is revealed, so we can check everything)
            // Note: Since we just called RevealCardsInTransform, they are now "revealed".
            bool jokerFound = CheckAndTriggerJokerLogic(stopShowdownIfJokerFound);
            
            if (jokerFound && stopShowdownIfJokerFound)
            {
                return; // Stop standard Judge
            }

            // 3. Collect cards for Judge
            List<PlayingCard> playerHand = GetCardsFromObjects(cardDeckSystem.playerCardObjects);
            List<PlayingCard> publicCards = GetCardsFromObjects(cardDeckSystem.PublicCardObjects);
            List<PlayingCard> enemyHand = GetCardsFromTransform(cardDeckSystem.enemyHandArea);

            List<PlayingCard> playerFullHand = new List<PlayingCard>(playerHand);
            playerFullHand.AddRange(publicCards);
            List<PlayingCard> enemyFullHand = new List<PlayingCard>(enemyHand);
            enemyFullHand.AddRange(publicCards);

            // 4. Call Judge Logic
            if (Judge.Instance != null)
            {
                List<PlayingCard> pBest;
                PokerHandType pType;
                List<PlayingCard> eBest;
                PokerHandType eType;
                bool playerWins;

                Judge.Instance.GetResult(playerFullHand, enemyFullHand, out pBest, out pType, out eBest, out eType, out playerWins);

                if (playerWins) betManager.PlayerWin();
                else betManager.EnemyWin();
            }
        }

        /// <summary>
        /// Checks all cards in play. Returns true if a REVEALED Joker is found.
        /// If found and shouldLock is true, locks the BetManager settlement.
        /// </summary>
        private bool CheckAndTriggerJokerLogic(bool shouldLock)
        {
            if (cardDeckSystem == null) return false;

            bool playerHasRevealedJoker = HasRevealedJoker(cardDeckSystem.playerCardObjects);
            bool publicHasRevealedJoker = HasRevealedJoker(cardDeckSystem.PublicCardObjects);
            bool enemyHasRevealedJoker = HasRevealedJoker(cardDeckSystem.enemyCardObjects); // Uses list directly if available, or helper

            // Note: enemyCardObjects logic in CardDeckSystem might not be exposed as a public list if private. 
            // In CardDeckSystem.cs it is private List<GameObject> enemyCardObjects. 
            // But we can get them via Transform if needed.
            if (cardDeckSystem.enemyHandArea != null)
            {
                 // Re-check via transform to be safe if list is private/empty
                 enemyHasRevealedJoker = HasRevealedJokerInTransform(cardDeckSystem.enemyHandArea);
            }

            // Determine ownership for the event
            // Public Joker counts for... both? or triggers event with true, true? 
            // Usually Joker ownership matters. If Public has it, both "have" it in a sense.
            // Let's pass: 
            // PlayerParam = PlayerHandHas OR PublicHas
            // EnemyParam = EnemyHandHas OR PublicHas
            
            bool pEvent = playerHasRevealedJoker || publicHasRevealedJoker;
            bool eEvent = enemyHasRevealedJoker || publicHasRevealedJoker;

            if (pEvent || eEvent)
            {
                Debug.Log($"Revealed Joker Detected! P:{pEvent}, E:{eEvent}");
                
                if (shouldLock)
                {
                    betManager.isSettlementLocked = true;
                }
                
                onJokerDetected?.Invoke(pEvent, eEvent);
                return true;
            }

            return false;
        }

        private bool HasRevealedJoker(List<GameObject> cards)
        {
            if (cards == null) return false;
            foreach (var go in cards)
            {
                if (IsJokerAndRevealed(go)) return true;
            }
            return false;
        }

        private bool HasRevealedJokerInTransform(Transform parent)
        {
            if (parent == null) return false;
            foreach (Transform t in parent)
            {
                if (IsJokerAndRevealed(t.gameObject)) return true;
            }
            return false;
        }

        private bool IsJokerAndRevealed(GameObject go)
        {
            if (go == null) return false;

            CardDisplay display = go.GetComponent<CardDisplay>();
            CardFaceController face = go.GetComponent<CardFaceController>();

            if (display != null && face != null)
            {
                bool isJoker = display.cardData != null && display.cardData.rank == CardRank.Joker;
                bool isRevealed = !face._isShowingBack; // _isShowingBack is public per previous view

                return isJoker && isRevealed;
            }
            return false;
        }

        // Helpers
        private void RevealCardsInTransform(Transform parent)
        {
            if (parent == null) return;
            CardFaceController[] faces = parent.GetComponentsInChildren<CardFaceController>();
            foreach (var f in faces) f.ShowFrontFace();
        }

        public void RevealCommunityCards(int totalToReveal)
        {
            if (cardDeckSystem == null) return;
            List<GameObject> publicCards = cardDeckSystem.PublicCardObjects;
            if (publicCards == null) return;

            for (int i = 0; i < totalToReveal; i++)
            {
                if (i < publicCards.Count)
                {
                    CardFaceController face = publicCards[i].GetComponent<CardFaceController>();
                    if (face != null) face.ShowFrontFace();
                }
            }
        }
        
        public void RevealAllCommunityCards() => RevealCommunityCards(5);

        private List<PlayingCard> GetCardsFromObjects(List<GameObject> c) // ... simplified helper
        {
             return c.Select(x => x.GetComponent<CardDisplay>()?.cardData).Where(d => d != null).ToList();
        }
        private List<PlayingCard> GetCardsFromTransform(Transform t)
        {
             return t.GetComponentsInChildren<CardDisplay>().Select(x => x.cardData).Where(d => d != null).ToList();
        }
    }
}
