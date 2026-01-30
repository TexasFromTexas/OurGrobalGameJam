using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class PublicCardRevealButton : MonoBehaviour
{
    [Header("核心引用")]
    public CardDeckSystem cardDeckSystem;
    public Button revealPublicCardBtn;

    private int revealedCardCount = 0;

    private void Start()
    {
        if (cardDeckSystem == null)
        {
            Debug.LogError("请给PublicCardRevealButton拖入CardDeckSystem！");
            return;
        }
        if (revealPublicCardBtn == null)
        {
            Debug.LogError("请给PublicCardRevealButton拖入解牌按钮！");
            return;
        }

        revealPublicCardBtn.onClick.AddListener(OnClickRevealPublicCard);
        revealPublicCardBtn.interactable = false;

        // 监听回合状态变化
        cardDeckSystem.OnRoundStateChanged += OnRoundStateChanged;
    }

    private void Update()
    {
        UpdateButtonInteractable();
    }

    private void UpdateButtonInteractable()
    {
        if (cardDeckSystem == null || revealPublicCardBtn == null) return;

        // 核心判断：增加日志，方便排查
        bool isInRound = cardDeckSystem.IsInRound;
        int publicCardCount = cardDeckSystem.PublicCardObjects.Count;
        bool hasUnrevealed = revealedCardCount < 5;

        Debug.Log($"按钮状态判断：回合中={isInRound}，公共牌数={publicCardCount}，未显示={hasUnrevealed}");

        // 放宽判断：只要回合中且有公共牌就允许点击（避免数量严格等于5的限制）
        revealPublicCardBtn.interactable = isInRound && publicCardCount > 0 && hasUnrevealed;
    }

    private void OnClickRevealPublicCard()
    {
        List<GameObject> publicCards = cardDeckSystem.PublicCardObjects;
        if (publicCards.Count == 0)
        {
            Debug.LogWarning("没有公共牌可显示！");
            return;
        }

        // 分阶段显示
        if (revealedCardCount == 0)
        {
            RevealCards(publicCards, 0, Mathf.Min(2, publicCards.Count - 1));
            revealedCardCount = 3;
        }
        else if (revealedCardCount == 3)
        {
            RevealCards(publicCards, 3, Mathf.Min(3, publicCards.Count - 1));
            revealedCardCount = 4;
        }
        else if (revealedCardCount == 4)
        {
            RevealCards(publicCards, 4, Mathf.Min(4, publicCards.Count - 1));
            revealedCardCount = 5;
        }
    }

    private void RevealCards(List<GameObject> cards, int start, int end)
    {
        for (int i = start; i <= end; i++)
        {
            if (i >= cards.Count) break;
            CardFaceController faceCtrl = cards[i].GetComponent<CardFaceController>();
            if (faceCtrl != null)
            {
                faceCtrl.ShowFrontFace();
            }
        }
    }

    private void OnRoundStateChanged(bool isInRound)
    {
        if (isInRound)
        {
            // 新回合：重置状态，延迟0.5秒刷新（确保公共牌已生成）
            revealedCardCount = 0;
            Invoke(nameof(UpdateButtonInteractable), 0.5f);
            Debug.Log("新回合开始，重置解牌状态");
        }
        else
        {
            revealPublicCardBtn.interactable = false;
        }
    }

    private void OnDestroy()
    {
        if (cardDeckSystem != null)
        {
            cardDeckSystem.OnRoundStateChanged -= OnRoundStateChanged;
        }
    }
}