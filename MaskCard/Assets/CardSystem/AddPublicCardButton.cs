using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 抽取额外公牌脚本（独立挂载在CardEffectManager上）
/// 功能：按钮触发，从牌库多抽一张牌作为公牌并明牌，支持文本切换
/// </summary>
public class AddPublicCardButton : MonoBehaviour
{
    [Header("核心引用")]
    public CardDeckSystem cardDeckSystem; // 拖入CardDeckManager物体
    public Button addPublicCardBtn; // 拖入“多抽公牌”按钮

    // 新增：标记是否已抽取过额外公牌（控制文本切换+限制次数）
    private bool _isClicked = false;

    private void Start()
    {
        // 容错检查（和现有脚本逻辑一致）
        if (cardDeckSystem == null)
        {
            Debug.LogError("请给AddPublicCardButton拖入CardDeckSystem！");
            return;
        }
        if (addPublicCardBtn == null)
        {
            Debug.LogError("请给AddPublicCardButton拖入多抽公牌按钮！");
            return;
        }

        // 绑定按钮点击事件
        addPublicCardBtn.onClick.AddListener(OnClickAddPublicCard);
        // 监听回合状态变化，重置抽取状态（关键：新回合恢复初始文本）
        cardDeckSystem.OnRoundStateChanged += OnRoundStateChanged;
        // 初始禁用按钮（仅回合中且牌库有牌时可用）
        UpdateButtonInteractable();
    }

    private void Update()
    {
        // 每帧更新按钮状态+文本
        UpdateButtonInteractable();
    }

    /// <summary>
    /// 更新按钮可点击状态 + 文本切换
    /// </summary>
    private void UpdateButtonInteractable()
    {
        if (cardDeckSystem == null || addPublicCardBtn == null) return;

        // 基础可点击条件：回合中+牌库有牌+未抽取过
        bool isInRound = cardDeckSystem.IsInRound;
        bool hasCardsInDeck = cardDeckSystem.cardDeck.Count > 0;
        bool canClick = isInRound && hasCardsInDeck && !_isClicked;

        // 更新按钮可点击状态
        addPublicCardBtn.interactable = canClick;

        // ========== 核心：文本切换 ==========
        Text btnText = addPublicCardBtn.GetComponentInChildren<Text>();
        if (btnText != null)
        {
            if (_isClicked)
            {
                btnText.text = "摘下面具"; // 点击后文本
            }
            else
            {
                btnText.text = "装出和善的样子"; // 点击前文本
            }
        }
        else
        {
            Debug.LogWarning("多抽公牌按钮缺少Text子组件！");
        }
    }

    /// <summary>
    /// 按钮点击回调：抽取额外公牌并明牌
    /// </summary>
    private void OnClickAddPublicCard()
    {
        bool success = cardDeckSystem.DrawExtraPublicCard();
        if (success)
        {
            _isClicked = true; // 标记为已抽取，切换文本
            Debug.Log("额外公牌抽取成功！");
        }
        else
        {
            Debug.Log("额外公牌抽取失败！");
        }
        // 点击后立即更新按钮状态和文本
        UpdateButtonInteractable();
    }

    /// <summary>
    /// 回合状态变化时重置（新回合恢复初始状态）
    /// </summary>
    private void OnRoundStateChanged(bool isInRound)
    {
        if (isInRound)
        {
            // 新回合开始：重置抽取状态，恢复初始文本
            _isClicked = false;
            Invoke(nameof(UpdateButtonInteractable), 0.1f); // 延迟刷新，确保状态同步
        }
        else
        {
            // 回合结束：禁用按钮
            addPublicCardBtn.interactable = false;
        }
    }

    /// <summary>
    /// 防止内存泄漏，移除监听
    /// </summary>
    private void OnDestroy()
    {
        if (cardDeckSystem != null)
        {
            cardDeckSystem.OnRoundStateChanged -= OnRoundStateChanged;
        }
    }
}