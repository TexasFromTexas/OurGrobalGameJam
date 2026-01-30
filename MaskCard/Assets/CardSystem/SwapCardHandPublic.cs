using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 手牌↔公牌交换功能（最终修复版）
/// 解决：公牌背面时交换数值不同步、显示和实际数值不一致问题
/// </summary>
public class SwapCardHandPublic : MonoBehaviour
{
    [Header("核心配置")]
    public CardDeckSystem cardDeckSystem; // 拖入CardDeckManager
    public Button swapTriggerBtn; // 交换触发按钮
    public Color selectHighlightColor = new Color(1, 1, 0, 0.5f); // 选中高亮色

    [Header("调试参数（只读）")]
    [SerializeField, ReadOnly] private bool _isSwapMode = false;
    [SerializeField, ReadOnly] private GameObject _selectedHandCard = null;
    [SerializeField, ReadOnly] private GameObject _selectedPublicCard = null;
    private bool _isSwapping = false;
    private Color _originHandCardColor;

    // 只读特性
    public class ReadOnlyAttribute : PropertyAttribute { }

    private void Start()
    {
        if (cardDeckSystem == null)
        {
            Debug.LogError("[SwapCard] 请拖入CardDeckSystem！");
            return;
        }
        if (swapTriggerBtn == null)
        {
            Debug.LogError("[SwapCard] 请拖入交换触发按钮！");
            return;
        }

        swapTriggerBtn.onClick.AddListener(ToggleSwapMode);
        SetCardClickable(false);
        UpdateSwapButtonText();

        // 初始化公牌数据源：确保所有公牌的显示缓存和cardData一致
        InitPublicCardDataSync();
    }

    /// <summary>
    /// 初始化公牌数据同步：避免背面公牌缓存旧值
    /// </summary>
    private void InitPublicCardDataSync()
    {
        if (cardDeckSystem.PublicCardObjects == null) return;

        foreach (var publicCard in cardDeckSystem.PublicCardObjects)
        {
            if (publicCard == null) continue;

            CardDisplay cd = publicCard.GetComponent<CardDisplay>();
            CardFaceController cfc = publicCard.GetComponent<CardFaceController>();

            if (cd != null && cfc != null)
            {
                // 强制绑定公牌的显示数据到cardData（核心：消除缓存）
                cfc.bindingCardData = cd.cardData;
            }
        }
    }

    private void UpdateSwapButtonText()
    {
        Text btnText = swapTriggerBtn.GetComponentInChildren<Text>();
        if (btnText != null)
        {
            btnText.text = _isSwapMode ? "取消交换" : "交换手牌公牌";
        }
    }

    private void ToggleSwapMode()
    {
        _isSwapMode = !_isSwapMode;

        if (_isSwapMode)
        {
            ResetSelectState();
            SetCardClickable(true);
            Debug.Log("[SwapCard] 进入交换模式：先选手牌，再选公牌");
        }
        else
        {
            ResetSelectState();
            SetCardClickable(false);
            Debug.Log("[SwapCard] 退出交换模式");
        }
    }

    private void SetCardClickable(bool isClickable)
    {
        // 处理手牌
        if (cardDeckSystem.playerCardObjects != null)
        {
            foreach (var handCard in cardDeckSystem.playerCardObjects)
            {
                if (handCard == null) continue;

                Button btn = handCard.GetComponent<Button>();
                if (btn == null)
                {
                    btn = handCard.AddComponent<Button>();
                    btn.transition = Selectable.Transition.None;
                }

                btn.onClick.RemoveAllListeners();
                if (isClickable)
                {
                    btn.onClick.AddListener(() => SelectHandCard(handCard));
                }
                btn.interactable = isClickable;
            }
        }

        // 处理公牌
        if (cardDeckSystem.PublicCardObjects != null)
        {
            foreach (var publicCard in cardDeckSystem.PublicCardObjects)
            {
                if (publicCard == null) continue;

                Button btn = publicCard.GetComponent<Button>();
                if (btn == null)
                {
                    btn = publicCard.AddComponent<Button>();
                    btn.transition = Selectable.Transition.None;
                }

                btn.onClick.RemoveAllListeners();
                if (isClickable)
                {
                    btn.onClick.AddListener(() => SelectPublicCard(publicCard));
                }
                btn.interactable = isClickable;
            }
        }
    }

    private void SelectHandCard(GameObject handCard)
    {
        if (_isSwapping || _selectedPublicCard != null)
        {
            Debug.LogWarning("[SwapCard] 请先取消当前选择！");
            return;
        }

        if (_selectedHandCard != null)
        {
            ResetHandCardHighlight(_selectedHandCard);
        }

        _selectedHandCard = handCard;
        SetHandCardHighlight(handCard);
        Debug.Log($"[SwapCard] 选中手牌：{handCard.name}（数值：{GetCardName(handCard)}）");
    }

    private void SelectPublicCard(GameObject publicCard)
    {
        if (_isSwapping || _selectedHandCard == null || _selectedPublicCard != null)
        {
            Debug.LogWarning("[SwapCard] 请先选中1张手牌！");
            return;
        }

        _selectedPublicCard = publicCard;
        TriggerCardValueSwap();
    }

    /// <summary>
    /// 核心修复：交换数值+同步公牌显示缓存
    /// </summary>
    private void TriggerCardValueSwap()
    {
        if (_isSwapping || _selectedHandCard == null || _selectedPublicCard == null) return;

        _isSwapping = true;
        Debug.Log("[SwapCard] 开始交换数值...");

        // 1. 获取核心组件
        CardDisplay handCD = _selectedHandCard.GetComponent<CardDisplay>();
        CardDisplay publicCD = _selectedPublicCard.GetComponent<CardDisplay>();
        CardFaceController publicCFC = _selectedPublicCard.GetComponent<CardFaceController>();

        if (handCD == null || publicCD == null)
        {
            Debug.LogError("[SwapCard] 卡牌缺少CardDisplay组件！");
            _isSwapping = false;
            return;
        }
        if (handCD.cardData == null || publicCD.cardData == null)
        {
            Debug.LogError("[SwapCard] 卡牌数据为空！");
            _isSwapping = false;
            return;
        }

        // 2. 保存原始数值（避免赋值覆盖）
        PlayingCard handOriginalValue = handCD.cardData;
        PlayingCard publicOriginalValue = publicCD.cardData;

        // 3. 交换核心数值（唯一数据源）
        handCD.cardData = publicOriginalValue;
        publicCD.cardData = handOriginalValue;

        // 4. 关键修复：同步背面公牌的显示缓存
        if (publicCFC != null)
        {
            // 强制将公牌的显示数据绑定到最新的cardData
            publicCFC.bindingCardData = publicCD.cardData;
            // 如果公牌是背面状态，标记其需要刷新显示
            if (publicCFC._isShowingBack)
            {
                publicCFC.NeedRefreshDisplay = true;
            }
        }

        // 5. 同步视觉文字（立即更新可见数值）
        UpdateCardText(_selectedHandCard, handCD.cardData.cardName);
        UpdateCardText(_selectedPublicCard, publicCD.cardData.cardName);

        // 6. 验证交换结果
        Debug.Log($"[SwapCard] 数值交换完成：\n" +
                  $"手牌 {_selectedHandCard.name} → 数值：{handCD.cardData.cardName}\n" +
                  $"公牌 {_selectedPublicCard.name} → 数值：{publicCD.cardData.cardName}\n" +
                  $"公牌显示缓存同步：{publicCFC != null && publicCFC.bindingCardData == publicCD.cardData}");

        ResetSelectState();
        ToggleSwapMode();
        _isSwapping = false;
    }

    /// <summary>
    /// 获取卡牌数值名称
    /// </summary>
    private string GetCardName(GameObject card)
    {
        CardDisplay cd = card.GetComponent<CardDisplay>();
        return cd != null && cd.cardData != null ? cd.cardData.cardName : "未知数值";
    }

    /// <summary>
    /// 更新卡牌文字显示
    /// </summary>
    private void UpdateCardText(GameObject card, string text)
    {
        Text cardText = card.GetComponentInChildren<Text>();
        if (cardText != null)
        {
            cardText.text = text;
        }
    }

    private void SetHandCardHighlight(GameObject card)
    {
        Image img = card.GetComponent<Image>();
        if (img == null) return;

        _originHandCardColor = img.color;
        img.color = selectHighlightColor;
    }

    private void ResetHandCardHighlight(GameObject card)
    {
        Image img = card.GetComponent<Image>();
        if (img == null) return;

        img.color = _originHandCardColor;
    }

    private void ResetSelectState()
    {
        if (_selectedHandCard != null)
        {
            ResetHandCardHighlight(_selectedHandCard);
            _selectedHandCard = null;
        }
        _selectedPublicCard = null;
        _isSwapping = false;
    }

    private void OnDestroy()
    {
        if (swapTriggerBtn != null)
        {
            swapTriggerBtn.onClick.RemoveAllListeners();
        }
        SetCardClickable(false);
        ResetSelectState();
    }
}

#if UNITY_EDITOR

[CustomPropertyDrawer(typeof(SwapCardHandPublic.ReadOnlyAttribute))]
public class SwapCardReadOnlyDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        GUI.enabled = false;
        EditorGUI.PropertyField(position, property, label);
        GUI.enabled = true;
    }
}
#endif