using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// 卡牌悬停效果：高亮边框 + 伸出/放大
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class CardHoverEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    // 悬停配置
    [Header("悬停效果参数")]
    public Color highlightColor = Color.yellow; // 高亮边框颜色
    public float hoverOffsetZ = 50f; // 伸出距离（Z轴，越大越靠前）
    public float hoverScale = 1.1f; // 悬停放大比例
    public float smoothTime = 0.1f; // 过渡平滑时间

    // 私有变量
    private RectTransform rectTrans;
    private Outline cardOutline; // 边框组件
    private Vector3 originalLocalPos; // 原始本地位置
    private Vector3 originalLocalScale; // 原始本地缩放
    private Vector3 targetLocalPos; // 目标位置
    private Vector3 targetLocalScale; // 目标缩放
    private Vector3 velocityPos = Vector3.zero;
    private Vector3 velocityScale = Vector3.zero;

    private void Awake()
    {
        // 获取组件
        rectTrans = GetComponent<RectTransform>();
        // 自动添加Outline组件（如果没有）
        cardOutline = GetComponent<Outline>();
        if (cardOutline == null)
        {
            cardOutline = gameObject.AddComponent<Outline>();
        }

        // 初始化
        originalLocalPos = rectTrans.localPosition;
        originalLocalScale = rectTrans.localScale;
        // 默认隐藏边框
        cardOutline.effectColor = Color.clear;
        cardOutline.effectDistance = new Vector2(2, -2); // 边框大小

        // 初始目标为原始状态
        targetLocalPos = originalLocalPos;
        targetLocalScale = originalLocalScale;
    }

    private void Update()
    {
        // 平滑过渡到目标位置/缩放
        rectTrans.localPosition = Vector3.SmoothDamp(rectTrans.localPosition, targetLocalPos, ref velocityPos, smoothTime);
        rectTrans.localScale = Vector3.SmoothDamp(rectTrans.localScale, targetLocalScale, ref velocityScale, smoothTime);
    }

    // 鼠标进入卡牌
    public void OnPointerEnter(PointerEventData eventData)
    {
        // 高亮边框
        cardOutline.effectColor = highlightColor;
        // 伸出+放大
        targetLocalPos = originalLocalPos + new Vector3(0, 0, -hoverOffsetZ); // Z轴越小越靠前（UI的Z轴反向）
        targetLocalScale = originalLocalScale * hoverScale;
    }

    // 鼠标离开卡牌
    public void OnPointerExit(PointerEventData eventData)
    {
        // 恢复边框
        cardOutline.effectColor = Color.clear;
        // 恢复位置+缩放
        targetLocalPos = originalLocalPos;
        targetLocalScale = originalLocalScale;
    }

    // 供外部调用：重置卡牌位置（比如重新排列时）
    public void ResetCardTransform()
    {
        originalLocalPos = rectTrans.localPosition;
        targetLocalPos = originalLocalPos;
        targetLocalScale = originalLocalScale;
    }
}