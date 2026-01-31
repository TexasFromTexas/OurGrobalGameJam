using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// 卡牌高亮控制器（适配UI Image）
/// 功能：选中/悬停时触发边框高亮，取消时恢复
/// </summary>
public class CardHighlightController : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
{
    [Header("高亮配置")]
    public Color highlightColor = Color.yellow; // 高亮颜色
    public float highlightIntensity = 8f; // 高亮强度
    private Color originalGlowColor;
    private float originalGlowIntensity;
    private Material _instanceMaterial; // 材质实例（避免修改共享材质）
    private Image _cardImage;

    private void Awake()
    {
        _cardImage = GetComponent<Image>();
        if (_cardImage.material != null)
        {
            // 创建材质实例（确保每张卡牌的材质独立）
            _instanceMaterial = new Material(_cardImage.material);
            _cardImage.material = _instanceMaterial;
            // 保存初始参数
            originalGlowColor = _instanceMaterial.GetColor("_GlowColor");
            originalGlowIntensity = _instanceMaterial.GetFloat("_GlowIntensity");
        }
    }

    /// <summary>
    /// 触发高亮
    /// </summary>
    public void EnableHighlight()
    {
        if (_instanceMaterial == null) return;

        _instanceMaterial.SetColor("_GlowColor", highlightColor);
        _instanceMaterial.SetFloat("_GlowIntensity", highlightIntensity);
    }

    /// <summary>
    /// 关闭高亮
    /// </summary>
    public void DisableHighlight()
    {
        if (_instanceMaterial == null) return;

        _instanceMaterial.SetColor("_GlowColor", originalGlowColor);
        _instanceMaterial.SetFloat("_GlowIntensity", originalGlowIntensity);
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

    // 销毁时释放材质实例，避免内存泄漏
    private void OnDestroy()
    {
        if (_instanceMaterial != null)
        {
            Destroy(_instanceMaterial);
        }
    }
}