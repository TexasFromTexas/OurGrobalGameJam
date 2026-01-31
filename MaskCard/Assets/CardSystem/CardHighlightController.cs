using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// 卡牌高亮控制器（挂载在卡牌预制体上）
/// 功能：选中/悬停时触发边框高亮，取消时恢复
/// </summary>
public class CardHighlightController : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
{
    [Header("高亮配置")]
    public Color highlightColor = Color.yellow; // 高亮颜色
    public float highlightIntensity = 8f; // 高亮强度
    private Color originalGlowColor;
    private float originalGlowIntensity;
    private MaterialPropertyBlock _mpb;
    private Image _cardImage;

    private void Awake()
    {
        _cardImage = GetComponent<Image>();
        _mpb = new MaterialPropertyBlock();
        if (_cardImage.material != null)
        {
            // 保存初始材质参数（避免修改共享材质）
            originalGlowColor = _cardImage.material.GetColor("_GlowColor");
            originalGlowIntensity = _cardImage.material.GetFloat("_GlowIntensity");
        }
    }

    /// <summary>
    /// 触发高亮
    /// </summary>
    public void EnableHighlight()
    {
        if (_cardImage == null || _cardImage.material == null) return;

        _cardImage.GetPropertyBlock(_mpb);
        _mpb.SetColor("_GlowColor", highlightColor);
        _mpb.SetFloat("_GlowIntensity", highlightIntensity);
        _cardImage.SetPropertyBlock(_mpb);
    }

    /// <summary>
    /// 关闭高亮
    /// </summary>
    public void DisableHighlight()
    {
        if (_cardImage == null || _cardImage.material == null) return;

        _cardImage.GetPropertyBlock(_mpb);
        _mpb.SetColor("_GlowColor", originalGlowColor);
        _mpb.SetFloat("_GlowIntensity", originalGlowIntensity);
        _cardImage.SetPropertyBlock(_mpb);
    }

    // 鼠标悬停触发高亮
    public void OnPointerEnter(PointerEventData eventData)
    {
        EnableHighlight();
    }

    // 鼠标离开取消高亮
    public void OnPointerExit(PointerEventData eventData)
    {
        DisableHighlight();
    }

    // 选中卡牌触发高亮
    public void OnSelect(BaseEventData eventData)
    {
        EnableHighlight();
    }

    // 取消选中取消高亮
    public void OnDeselect(BaseEventData eventData)
    {
        DisableHighlight();
    }
}