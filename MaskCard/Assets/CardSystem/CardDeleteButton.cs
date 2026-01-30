using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 独立的删除卡牌按钮脚本：和抽牌按钮脚本用法完全一致
/// 挂载到CardEffectManager空物体 → 拖入CardDeckSystem和删除按钮即可
/// </summary>
public class CardDeleteButton : MonoBehaviour
{
    [Header("核心引用")]
    public CardDeckSystem cardDeckSystem; // 拖入CardDeckManager物体
    public Button deleteCardBtn; // 拖入你的删除卡牌按钮

    // 选牌删除模式标记
    private bool isSelectingCardToDelete = false;

    private void Start()
    {
        // 容错：检查引用是否为空（和抽牌脚本逻辑一致）
        if (cardDeckSystem == null)
        {
            Debug.LogError("请给CardDeleteButton脚本的cardDeckSystem字段拖入CardDeckManager物体！");
            return;
        }
        if (deleteCardBtn == null)
        {
            Debug.LogError("请给CardDeleteButton脚本的deleteCardBtn字段拖入删除卡牌按钮！");
            return;
        }

        // 绑定按钮点击事件
        deleteCardBtn.onClick.AddListener(OnClickToggleDeleteMode);
        // 初始设置按钮状态（回合外不可点击）
        UpdateButtonInteractable();
    }

    // 每帧检测回合状态，更新按钮是否可点击（和抽牌脚本一致）
    private void Update()
    {
        UpdateButtonInteractable();
    }

    /// <summary>
    /// 根据回合状态更新按钮是否可点击
    /// </summary>
    private void UpdateButtonInteractable()
    {
        if (cardDeckSystem == null || deleteCardBtn == null) return;
        // 只有回合中且玩家有手牌时，按钮才可用
        deleteCardBtn.interactable = cardDeckSystem.IsInRound && cardDeckSystem.PlayerCardCount > 0;
    }

    /// <summary>
    /// 按钮点击回调：切换选牌删除模式
    /// </summary>
    private void OnClickToggleDeleteMode()
    {
        isSelectingCardToDelete = !isSelectingCardToDelete;

        if (isSelectingCardToDelete)
        {
            deleteCardBtn.GetComponentInChildren<Text>().text = "取消删除";
            Debug.Log("进入选牌删除模式：点击任意玩家手牌卡牌即可删除");
        }
        else
        {
            deleteCardBtn.GetComponentInChildren<Text>().text = "删除卡牌";
            Debug.Log("退出选牌删除模式");
        }
    }

    /// <summary>
    /// 供卡牌点击脚本调用：删除指定卡牌
    /// </summary>
    public void DeletePlayerCard(GameObject cardToDelete)
    {
        if (!isSelectingCardToDelete || cardDeckSystem == null) return;

        // 调用牌库系统移除卡牌
        cardDeckSystem.RemovePlayerCard(cardToDelete);
        // 重新排列剩余手牌
        cardDeckSystem.RearrangePlayerHand();
        // 退出选牌模式
        isSelectingCardToDelete = false;
        deleteCardBtn.GetComponentInChildren<Text>().text = "删除卡牌";

        Debug.Log($"删除卡牌：{cardToDelete.GetComponent<CardDisplay>().cardData.cardName}，剩余手牌数：{cardDeckSystem.PlayerCardCount}");
    }

    /// <summary>
    /// 供卡牌点击脚本获取选牌模式状态
    /// </summary>
    public bool IsSelectingCardToDelete => isSelectingCardToDelete;
}