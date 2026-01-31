using UnityEngine;
using UnityEngine.UI;
using BetSystem;

/// <summary>
/// 抽取额外公牌脚本（新增：通知偷看脚本刷新）
/// </summary>
public class AddPublicCardButton : MonoBehaviour
{
    [Header("核心引用")]
    public CardDeckSystem cardDeckSystem;
    public Button addPublicCardBtn;
    // 新增：引用偷看脚本
    public PeekCardOverlayButton peekCardScript;

    public BetManager betManager;

    // 🔴 核心修改1：删除_isClicked变量（这是单次限制的根源）
    // private bool _isClicked = false;

    private void Start()
    {
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

        if (betManager == null) betManager = FindFirstObjectByType<BetManager>();

        addPublicCardBtn.onClick.AddListener(OnClickAddPublicCard);
        cardDeckSystem.OnRoundStateChanged += OnRoundStateChanged;
        UpdateButtonInteractable();
    }

    private void Update()
    {
        UpdateButtonInteractable();
    }

    private void UpdateButtonInteractable()
    {
        if (cardDeckSystem == null || addPublicCardBtn == null) return;

        bool isInRound = cardDeckSystem.IsInRound;
        bool hasCardsInDeck = cardDeckSystem.cardDeck.Count > 0;

        bool costCondition = true;
        if (betManager != null)
        {
            costCondition = betManager.playerChips >= betManager.costAddPublic;
        }

        // 🔴 核心修改2：移除!_isClicked条件（解除单次点击限制）
        bool canClick = isInRound && hasCardsInDeck && costCondition;

        addPublicCardBtn.interactable = canClick;

        Text btnText = addPublicCardBtn.GetComponentInChildren<Text>();
        if (btnText != null)
        {
            // 🔴 核心修改3：按钮文本固定（不再根据_isClicked切换）
            // 可根据需求修改文本内容，比如显示抽取成本
            btnText.text = $"花费{betManager?.costAddPublic ?? 0}筹码抽公牌";
            // 如果想保留原有文本，改为：btnText.text = "装出和善的样子";
        }
        else
        {
            Debug.LogWarning("多抽公牌按钮缺少Text子组件！");
        }
    }

    private void OnClickAddPublicCard()
    {
        // Cost Check
        if (betManager != null)
        {
            if (!betManager.TrySpendChips(betManager.costAddPublic))
            {
                Debug.LogWarning($"筹码不足！无法抽取额外公牌。需要: {betManager.costAddPublic}");
                return;
            }
        }

        bool success = cardDeckSystem.DrawExtraPublicCard();
        if (success)
        {
            // 🔴 核心修改4：删除_isClicked = true（不再限制后续点击）
            Debug.Log("额外公牌抽取成功！");

            // 新增：通知偷看脚本刷新状态
            if (peekCardScript != null)
            {
                peekCardScript.RefreshPeekButtonState();
            }
            else
            {
                Debug.LogWarning("未绑定PeekCardOverlayButton脚本！");
            }
        }
        else
        {
            Debug.Log("额外公牌抽取失败！");
        }
        UpdateButtonInteractable();
    }

    private void OnRoundStateChanged(bool isInRound)
    {
        if (isInRound)
        {
            // 🔴 核心修改5：删除_isClicked重置（变量已移除）
            Invoke(nameof(UpdateButtonInteractable), 0.1f);
        }
        else
        {
            addPublicCardBtn.interactable = false;
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