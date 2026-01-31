using UnityEngine;
using System.Collections.Generic;

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

            // Simulate clicking the UI buttons to leverage existing logic without modifying code
            
            // 1. If a round is currently active, end it first (recycle cards)
            if (cardDeckSystem.IsInRound)
            {
                if (cardDeckSystem.endRoundButton != null && cardDeckSystem.endRoundButton.interactable)
                {
                    cardDeckSystem.endRoundButton.onClick.Invoke();
                    Debug.Log("BetCardSystemBridge: Auto-clicked EndRoundButton");
                }
            }

            // 2. Start a new round (deal cards)
            if (cardDeckSystem.startRoundButton != null)
            {
                // Note: EndRoundButton click usually makes startRoundButton interactable immediately
                if (cardDeckSystem.startRoundButton.interactable)
                {
                    cardDeckSystem.startRoundButton.onClick.Invoke();
                    Debug.Log("BetCardSystemBridge: Auto-clicked StartRoundButton");
                }
                else
                {
                    // If not interactable yet (maybe deck empty?), force try or log warning
                    // But if deck is empty, cardDeckSystem handles it gracefully anyway
                    Debug.LogWarning("BetCardSystemBridge: StartRoundButton not interactable, cannot start card round.");
                }
            }
        }

        private void OnPhaseChanged(BetPhase phase)
        {
            switch (phase)
            {
                case BetPhase.Preflop:
                    // Usually implies new round, handled by OnBettingRoundRestarted
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
                    // Optionally reveal enemy cards here?
                    // RevealEnemyHand();
                    break;
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
