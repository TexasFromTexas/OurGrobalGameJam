using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 卡牌悬停高亮脚本（挂载到卡牌预制体上）
/// 鼠标进入卡牌时高亮，离开时恢复原色
/// </summary>
[RequireComponent(typeof(Image))] // 强制要求挂载Image组件（卡牌背景）
public class CardHoverHighlight : MonoBehaviour
{
    [Header("高亮设置")]
    [Tooltip("卡牌初始颜色（默认取Image的初始颜色）")]
    public Color normalColor = Color.white;

    [Tooltip("鼠标悬停时的高亮颜色")]
    public Color highlightColor = new Color(1.2f, 1.2f, 1.2f, 1f); // 轻微提亮

    [Tooltip("是否启用边框高亮（如果有边框Image）")]
    public bool useBorderHighlight = false;

    [Tooltip("边框Image组件（需手动拖入）")]
    public Image cardBorder;

    [Tooltip("边框初始颜色")]
    public Color borderNormalColor = Color.gray;

    [Tooltip("边框高亮颜色")]
    public Color borderHighlightColor = Color.yellow;

    private Image cardImage; // 卡牌背景Image组件
    private bool isInit = false; // 是否初始化完成

    private void Awake()
    {
        // 初始化组件
        InitComponent();
    }

    /// <summary>
    /// 初始化组件和默认颜色
    /// </summary>
    private void InitComponent()
    {
        // 获取卡牌背景Image
        cardImage = GetComponent<Image>();

        // 如果没设置初始颜色，就用Image当前的颜色
        if (normalColor == Color.white && cardImage != null)
        {
            normalColor = cardImage.color;
        }

        // 如果启用边框高亮，初始化边框颜色
        if (useBorderHighlight && cardBorder != null)
        {
            borderNormalColor = cardBorder.color;
        }

        isInit = true;
    }

    /// <summary>
    /// 鼠标进入卡牌（UI元素需开启Raycast Target）
    /// </summary>
    private void OnMouseEnter()
    {
        if (!isInit) return;
        Debug.Log("成功");
        // 背景高亮
        if (cardImage != null)
        {
            cardImage.color = highlightColor;
        }

        // 边框高亮（如果启用）
        if (useBorderHighlight && cardBorder != null)
        {
            cardBorder.color = borderHighlightColor;
        }
    }

    /// <summary>
    /// 鼠标离开卡牌
    /// </summary>
    private void OnMouseExit()
    {
        if (!isInit) return;

        // 恢复背景原色
        if (cardImage != null)
        {
            cardImage.color = normalColor;
        }

        // 恢复边框原色（如果启用）
        if (useBorderHighlight && cardBorder != null)
        {
            cardBorder.color = borderNormalColor;
        }
    }

    /// <summary>
    /// 防止场景运行时修改参数后失效
    /// </summary>
    private void OnValidate()
    {
        if (Application.isPlaying && isInit)
        {
            InitComponent();
        }
    }
}