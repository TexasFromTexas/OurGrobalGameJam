using UnityEngine;
using System.Collections.Generic;
using System.Linq; // Added for Linq

namespace BetSystem
{
    public class BetCardSystemBridge : MonoBehaviour
    {
        public BetManager betManager;
        public CardDeckSystem cardDeckSystem;

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

        private void OnBettingRoundRestarted()
        {
            if (cardDeckSystem == null) return;

            if (cardDeckSystem.IsInRound)
            {
                if (cardDeckSystem.endRoundButton != null && cardDeckSystem.endRoundButton.interactable)
                {
                    cardDeckSystem.endRoundButton.onClick.Invoke();
                }
            }

            if (cardDeckSystem.startRoundButton != null)
            {
                if (cardDeckSystem.startRoundButton.interactable)
                {
                    cardDeckSystem.startRoundButton.onClick.Invoke();
                }
            }
        }

        private void OnPhaseChanged(BetPhase phase)
        {
            switch (phase)
            {
                case BetPhase.Preflop:
                    break;
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

            // 1. Collect all cards
            List<PlayingCard> playerHand = GetCardsFromObjects(cardDeckSystem.playerCardObjects);
            List<PlayingCard> publicCards = GetCardsFromObjects(cardDeckSystem.PublicCardObjects);
            
            // Enemy cards are private, so we fetch from the UI container
            List<PlayingCard> enemyHand = GetCardsFromTransform(cardDeckSystem.enemyHandArea);

            // 2. Reveal Enemy Cards
            RevealCardsInTransform(cardDeckSystem.enemyHandArea);

            // 3. Prepare full hands
            List<PlayingCard> playerFullHand = new List<PlayingCard>(playerHand);
            playerFullHand.AddRange(publicCards);

            List<PlayingCard> enemyFullHand = new List<PlayingCard>(enemyHand);
            enemyFullHand.AddRange(publicCards);

            Debug.Log($"Showdown! Player Hand: {playerHand.Count}, Enemy Hand: {enemyHand.Count}, Public: {publicCards.Count}");

            // 4. Call Judge Logic
            if (Judge.Instance != null)
            {
                List<PlayingCard> pBest;
                PokerHandType pType;
                List<PlayingCard> eBest;
                PokerHandType eType;
                bool playerWins;

                Judge.Instance.GetResult(
                    playerFullHand, 
                    enemyFullHand, 
                    out pBest, 
                    out pType, 
                    out eBest, 
                    out eType, 
                    out playerWins
                );

                Debug.Log($"Judge Result: Player Type: {pType}, Enemy Type: {eType}. Player Wins? {playerWins}");

                // 5. Trigger Win/Loss
                // Note: Judge returns true for Win OR Tie (CompareHands >= 0). 
                // If strictly tie logic is needed, we would verify CompareHands result directly, 
                // but for now we give ties to Player based on ">= 0" logic in Judge.GetResult.
                if (playerWins)
                {
                    betManager.PlayerWin();
                }
                else
                {
                    betManager.EnemyWin();
                }
            }
            else
            {
                Debug.LogError("Judge.Instance is null! Cannot perform showdown.");
            }
        }

        private List<PlayingCard> GetCardsFromObjects(List<GameObject> cardObjs)
        {
            List<PlayingCard> result = new List<PlayingCard>();
            if (cardObjs == null) return result;

            foreach (var go in cardObjs)
            {
                if (go == null) continue;
                CardDisplay display = go.GetComponent<CardDisplay>();
                if (display != null && display.cardData != null)
                {
                    result.Add(display.cardData);
                }
            }
            return result;
        }

        private List<PlayingCard> GetCardsFromTransform(Transform parent)
        {
            List<PlayingCard> result = new List<PlayingCard>();
            if (parent == null) return result;

            CardDisplay[] displays = parent.GetComponentsInChildren<CardDisplay>();
            foreach (var d in displays)
            {
                if (d != null && d.cardData != null)
                {
                    result.Add(d.cardData);
                }
            }
            return result;
        }

        private void RevealCardsInTransform(Transform parent)
        {
            if (parent == null) return;
            CardFaceController[] faces = parent.GetComponentsInChildren<CardFaceController>();
            foreach (var f in faces)
            {
                f.ShowFrontFace();
            }
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

        public void RevealAllCommunityCards()
        {
            RevealCommunityCards(5);
        }
    }
}
