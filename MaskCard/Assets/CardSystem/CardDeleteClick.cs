using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 卡牌点击删除脚本（适配双选删除：手牌+公牌）
/// 自动添加到卡牌，无需手动配置
/// </summary>
public class CardDeleteClick : MonoBehaviour, IPointerClickHandler
{
    private CardDeleteButton cardDeleteButton;

    private void Awake()
    {
        // 自动找到CardDeleteButton实例（挂载在CardEffectManager上）
        cardDeleteButton = FindAnyObjectByType<CardDeleteButton>();
    }

    // 卡牌点击触发选中（适配双选删除逻辑）
    public void OnPointerClick(PointerEventData eventData)
    {
        if (cardDeleteButton != null && cardDeleteButton.IsDeleteMode)
        {
            // 判断当前点击的是手牌还是公牌，调用对应的选中方法
            if (IsPlayerHandCard())
            {
                cardDeleteButton.SelectHandCardToDelete(gameObject);
            }
            else if (IsPublicCard())
            {
                cardDeleteButton.SelectPublicCardToDelete(gameObject);
            }
        }
    }

    /// <summary>
    /// 判断当前卡牌是否是玩家手牌
    /// </summary>
    private bool IsPlayerHandCard()
    {
        if (cardDeleteButton == null || cardDeleteButton.cardDeckSystem == null)
            return false;

        return cardDeleteButton.cardDeckSystem.playerCardObjects.Contains(gameObject);
    }

    /// <summary>
    /// 判断当前卡牌是否是公牌
    /// </summary>
    private bool IsPublicCard()
    {
        if (cardDeleteButton == null || cardDeleteButton.cardDeckSystem == null)
            return false;

        return cardDeleteButton.cardDeckSystem.PublicCardObjects.Contains(gameObject);
    }
}