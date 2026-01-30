using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 独立的抽牌按钮脚本：点击按钮给玩家抽1张牌
/// </summary>
public class DrawOneCardButton : MonoBehaviour
{
    // 引用牌库系统（在Inspector中拖入CardDeckManager物体）
    [Header("核心引用")]
    public CardDeckSystem cardDeckSystem;
    // 抽牌按钮（在Inspector中拖入你的抽牌按钮）
    public Button drawOneCardBtn;

    private void Start()
    {
        // 容错：检查引用是否为空
        if (cardDeckSystem == null)
        {
            Debug.LogError("请给DrawOneCardButton脚本的cardDeckSystem字段拖入CardDeckManager物体！");
            return;
        }
        if (drawOneCardBtn == null)
        {
            Debug.LogError("请给DrawOneCardButton脚本的drawOneCardBtn字段拖入抽牌按钮！");
            return;
        }

        // 绑定按钮点击事件：点击就抽1张牌
        drawOneCardBtn.onClick.AddListener(OnClickDrawOneCard);
        // 初始设置按钮状态（回合外不可点击）
        UpdateButtonInteractable();
    }

    // 每帧检测回合状态，更新按钮是否可点击（可选，优化体验）
    private void Update()
    {
        UpdateButtonInteractable();
    }

    /// <summary>
    /// 根据回合状态更新按钮是否可点击
    /// </summary>
    private void UpdateButtonInteractable()
    {
        if (cardDeckSystem == null || drawOneCardBtn == null) return;
        // 只有回合中且牌库不为空时，按钮才可用
        drawOneCardBtn.interactable = cardDeckSystem.IsInRound && cardDeckSystem.GetComponent<CardDeckSystem>().cardDeck.Count > 0;
    }

    /// <summary>
    /// 按钮点击回调：抽1张牌到玩家手牌
    /// </summary>
    private void OnClickDrawOneCard()
    {
        PlayingCard newCard = cardDeckSystem.DrawOneCardToPlayerHand();
        // 可选：抽牌后的额外逻辑（比如提示玩家）
        if (newCard != null)
        {
            Debug.Log($"你抽到了：{newCard.cardName}");
            // 这里可以加UI提示、音效等逻辑
        }
    }
}