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
        
        // Split Events as requested
        public UnityEvent onPublicJokerRevealed; // Triggered during Showdown/Win Check if public has joker
        public UnityEvent<bool, bool> onHandJokerRevealed; // (hasPlayerHandJoker, hasEnemyHandJoker) - Triggered anytime

        private void Start()
        {
            if (betManager == null) betManager = GetComponent<BetManager>();
            if (cardDeckSystem == null) cardDeckSystem = FindFirstObjectByType<CardDeckSystem>();

            if (betManager != null)
            {
                betManager.onChipsNegative.AddListener(RevealAllCommunityCards);
                betManager.onPhaseChanged.AddListener(OnPhaseChanged);
                betManager.onNewRoundStarted.AddListener(OnBettingRoundRestarted);
            }
        }

        private void Update()
        {
            // "Anytime" check for Hand Jokers
            // We check every frame (or could throttle) if any HAND card is a revealed Joker.
            CheckHandJokersAnytime();
        }

        private void CheckHandJokersAnytime()
        {
            if (cardDeckSystem == null) return;
            
            // Note: Reuse HasRevealedJoker helper
            bool playerHandHas = HasRevealedJoker(cardDeckSystem.playerCardObjects);
            
            bool enemyHandHas = false;
            // Check enemy list or transform
            if (cardDeckSystem.enemyCardObjects != null && cardDeckSystem.enemyCardObjects.Count > 0)
            {
                 enemyHandHas = HasRevealedJoker(cardDeckSystem.enemyCardObjects);
            }
            else if (cardDeckSystem.enemyHandArea != null)
            {
                 enemyHandHas = HasRevealedJokerInTransform(cardDeckSystem.enemyHandArea);
            }

            if (playerHandHas || enemyHandHas)
            {
                // Prevent spamming? 
                // The user said "Trigger... anytime". Usually this means "When it happens".
                // If we trigger every frame, the handler (JokerEventHandler) re-re-re-re-restarts the round every frame.
                // WE MUST LOCK IT.
                if (!betManager.isSettlementLocked) 
                {
                    Debug.Log($"[Anytime Check] Hand Joker Detected! P:{playerHandHas} E:{enemyHandHas}");
                    betManager.isSettlementLocked = true; // Lock to prevent multi-trigger
                    onHandJokerRevealed?.Invoke(playerHandHas, enemyHandHas);
                }
            }
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
                    CheckAndSkipIfRevealedJoker(3); // Turn is index 3 (4th card)
                    RevealCommunityCards(4);
                    break;
                case BetPhase.River:
                    CheckAndSkipIfRevealedJoker(4); // River is index 4 (5th card)
                    RevealCommunityCards(5);
                    break;
                case BetPhase.Showdown:
                    RevealAllCommunityCards();
                    PerformShowdown();
                    break;
            }
        }

        private void CheckAndSkipIfRevealedJoker(int cardIndex)
        {
            if (cardDeckSystem == null || cardDeckSystem.PublicCardObjects == null) return;
            if (cardIndex >= cardDeckSystem.PublicCardObjects.Count) return;

            GameObject cardObj = cardDeckSystem.PublicCardObjects[cardIndex];
            
            // Check if this specific card is a Revealed Joker
            if (IsJokerAndRevealed(cardObj))
            {
                Debug.Log($"[Skip Logic] Phase Card [{cardIndex}] is a Revealed Joker! Skipping phase...");
                // We restart a coroutine or just call SkipPhase?
                // Calling SkipPhase immediately here might be risky if we are inside the Event recursion?
                // BetManager calls onPhaseChanged -> Bridge calls SkipPhase -> BetManager updates Phase -> onPhaseChanged...
                // This is recursion, but it's finite (Turn -> River -> Showdown). 
                // So calling SkipPhase directly is fine as long as we don't block.
                betManager.SkipPhase();
            }
        }

        private void PerformShowdown()
        {
            if (cardDeckSystem == null) return;

            // 1. Reveal Enemy Cards
            RevealCardsInTransform(cardDeckSystem.enemyHandArea);

            // 2. Check for Public Joker (Win Judgment Trigger)
            bool publicJokerFound = HasRevealedJoker(cardDeckSystem.PublicCardObjects);
            
            if (publicJokerFound)
            {
                Debug.Log("[Showdown] Public Joker Found! Triggering Event.");
                if (stopShowdownIfJokerFound)
                {
                    betManager.isSettlementLocked = true; 
                    onPublicJokerRevealed?.Invoke();
                    return; // Stop standard Judge
                }
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
