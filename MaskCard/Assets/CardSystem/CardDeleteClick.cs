using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 卡牌点击删除脚本（自动添加到卡牌，无需手动配置）
/// </summary>
public class CardDeleteClick : MonoBehaviour, IPointerClickHandler
{
    private CardDeleteButton cardDeleteButton;

    private void Awake()
    {
        // 自动找到CardDeleteButton实例（挂载在CardEffectManager上）
        cardDeleteButton = FindAnyObjectByType<CardDeleteButton>();
    }

    // 卡牌点击触发删除
    public void OnPointerClick(PointerEventData eventData)
    {
        if (cardDeleteButton != null && cardDeleteButton.IsSelectingCardToDelete)
        {
            cardDeleteButton.DeletePlayerCard(gameObject);
        }
    }
}