using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 卡牌选中效果：点击时Y轴在原始基础上增加指定偏移，再次点击恢复
/// 挂载到Card根节点上
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class CardSelectEffect : MonoBehaviour, IPointerClickHandler
{
    [Header("选中偏移配置")]
    [Tooltip("选中时Y轴在原始位置上增加的像素值（正数=向上浮起）")]
    public float selectYAddValue = 20f; // 推荐值：15-30像素（可根据需求调整）
    [Tooltip("是否允许多张卡牌同时选中")]
    public bool allowMultiSelect = false;

    [Header("状态（只读）")]
    [SerializeField] private bool isSelected = false; // 当前是否选中

    private RectTransform _cardRect; // 卡牌的RectTransform
    private Vector2 _originalAnchoredPos; // 记录卡牌初始的锚定位置（包含原始Y轴）
    private static CardSelectEffect _lastSelectedCard; // 上一个选中的卡牌（单选中模式用）

    private void Awake()
    {
        // 获取卡牌的RectTransform（UI卡牌核心组件）
        _cardRect = GetComponent<RectTransform>();
        if (_cardRect == null)
        {
            Debug.LogError($"卡牌{gameObject.name}缺少RectTransform组件！");
            enabled = false;
            return;
        }

        // 记录初始位置（关键：保存原始的Y轴基础值）
        _originalAnchoredPos = _cardRect.anchoredPosition;
    }

    /// <summary>
    /// 鼠标点击卡牌时触发（UI点击核心接口）
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        // 仅响应鼠标左键点击
        if (eventData.button != PointerEventData.InputButton.Left) return;

        // 切换选中状态
        isSelected = !isSelected;

        if (isSelected)
        {
            // 单选中模式：取消上一张选中卡牌的状态
            if (!allowMultiSelect && _lastSelectedCard != null && _lastSelectedCard != this)
            {
                _lastSelectedCard.ResetToOriginalPos();
            }

            // 核心：选中时Y轴 = 原始Y轴 + 设定的偏移值
            SetCardSelectPos();

            // 记录当前选中的卡牌
            _lastSelectedCard = this;
        }
        else
        {
            // 取消选中：恢复到原始Y轴位置
            ResetToOriginalPos();
        }
    }

    /// <summary>
    /// 设置选中状态：Y轴在原始基础上增加偏移值
    /// </summary>
    
   private void SetCardSelectPos()
    {
        Vector2 selectedPos = new Vector2(
            _originalAnchoredPos.x,
            Mathf.Clamp(_originalAnchoredPos.y + selectYAddValue, _originalAnchoredPos.y - 50, _originalAnchoredPos.y + 50)
        );
        _cardRect.anchoredPosition = selectedPos;
    }

    /// <summary>
    /// 重置到原始位置（恢复初始Y轴）
    /// </summary>
    public void ResetToOriginalPos()
    {
        isSelected = false;
        _cardRect.anchoredPosition = _originalAnchoredPos;
        Debug.Log($"卡牌{gameObject.name}取消选中，Y轴恢复到{_originalAnchoredPos.y}");
    }

    // 场景切换/卡牌销毁时清理状态
    private void OnDestroy()
    {
        if (_lastSelectedCard == this)
        {
            _lastSelectedCard = null;
        }
    }

    // 可选：点击空白区域取消所有选中（需挂载到Canvas）
    /*
    private void Update()
    {
        if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject())
        {
            if (_lastSelectedCard != null)
            {
                _lastSelectedCard.ResetToOriginalPos();
                _lastSelectedCard = null;
            }
        }
    }
    */
}