using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class PublicCardRevealButton : MonoBehaviour
{
    [Header("核心引用")]
    public CardDeckSystem cardDeckSystem;
    public Button revealPublicCardBtn;

    // 阶段定义：0=初始，1=翻3张阶段，2=翻1张阶段，3=翻1张阶段，4=完成
    private int _currentPhase = 0;
    // 每个阶段需要翻的牌数（严格3→1→1）
    private readonly int[] _phaseRevealCounts = { 0, 3, 1, 1 };

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
        // 监听公牌删除事件（需CardDeckSystem支持）
        if (cardDeckSystem != null)
        {
            cardDeckSystem.OnPublicCardRemoved += OnPublicCardRemoved;
        }
    }

    private void Update()
    {
        UpdateButtonInteractable();
    }

    private void UpdateButtonInteractable()
    {
        if (cardDeckSystem == null || revealPublicCardBtn == null) return;

        bool isInRound = cardDeckSystem.IsInRound;
        int publicCardCount = cardDeckSystem.PublicCardObjects?.Count ?? 0;
        bool hasUnrevealed = _currentPhase < _phaseRevealCounts.Length - 1; // 阶段未完成

        revealPublicCardBtn.interactable = isInRound && publicCardCount > 0 && hasUnrevealed;
    }

    private void OnClickRevealPublicCard()
    {
        if (_currentPhase >= _phaseRevealCounts.Length - 1)
        {
            Debug.LogWarning("所有阶段翻牌已完成！");
            return;
        }

        // 获取当前阶段需要翻的牌数（严格3→1→1）
        int needRevealCount = _phaseRevealCounts[_currentPhase + 1];
        Debug.Log($"当前阶段：{_currentPhase}，需要翻牌数：{needRevealCount}");

        // 翻对应数量的未翻开牌（动态找，不依赖索引）
        int actualRevealed = RevealUnrevealedCards(needRevealCount);
        Debug.Log($"实际翻牌数：{actualRevealed}");

        // 只要翻了至少1张，就推进阶段（即使未翻满，避免因删除导致无牌可翻的情况）
        if (actualRevealed > 0)
        {
            _currentPhase++;
            Debug.Log($"阶段推进到：{_currentPhase}");
        }
        else
        {
            Debug.LogWarning("没有找到未翻开的公牌，无法推进阶段！");
        }

        UpdateButtonInteractable();
    }

    /// <summary>
    /// 核心：动态找未翻开的牌，翻指定数量（不依赖索引，删除后仍生效）
    /// </summary>
    private int RevealUnrevealedCards(int count)
    {
        if (cardDeckSystem == null || cardDeckSystem.PublicCardObjects == null) return 0;

        int revealed = 0;
        foreach (var publicCard in cardDeckSystem.PublicCardObjects)
        {
            if (publicCard == null || revealed >= count) break;

            CardFaceController faceCtrl = publicCard.GetComponent<CardFaceController>();
            if (faceCtrl != null && faceCtrl._isShowingBack) // 仅翻未翻开的牌
            {
                faceCtrl.ShowFrontFace();
                revealed++;
                Debug.Log($"翻牌：{publicCard.name}（原状态：背面）");
            }
        }
        return revealed;
    }

    /// <summary>
    /// 公牌删除时：仅更新显示，不影响阶段推进
    /// </summary>
    private void OnPublicCardRemoved(GameObject removedCard)
    {
        if (removedCard == null) return;
        Debug.Log($"公牌被删除：{removedCard.name}，阶段不受影响");
        // 删除后刷新按钮状态
        UpdateButtonInteractable();
    }

    private void OnRoundStateChanged(bool isInRound)
    {
        if (isInRound)
        {
            // 新回合重置阶段
            _currentPhase = 0;
            Invoke(nameof(UpdateButtonInteractable), 0.5f);
            Debug.Log("新回合开始，重置翻牌阶段为初始状态");
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
            cardDeckSystem.OnPublicCardRemoved -= OnPublicCardRemoved;
        }
    }
}