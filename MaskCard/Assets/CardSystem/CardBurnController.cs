using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// 卡牌燃烧删除控制器（适配UI Image）
/// 功能：删除卡牌时触发燃烧溶解动画，完成后销毁物体
/// </summary>
public class CardBurnController : MonoBehaviour
{
    [Header("燃烧动画配置")]
    public float burnDuration = 1.2f; // 燃烧动画时长
    private Material _instanceMaterial;
    private Image _cardImage;

    private void Awake()
    {
        _cardImage = GetComponent<Image>();
        if (_cardImage.material != null)
        {
            // 创建材质实例（确保每张卡牌的材质独立）
            _instanceMaterial = new Material(_cardImage.material);
            _cardImage.material = _instanceMaterial;
            // 初始化Dissolve参数（完全不溶解）
            _instanceMaterial.SetFloat("_Dissolve", -1f);
        }
    }

    /// <summary>
    /// 触发燃烧动画（外部调用）
    /// </summary>
    public IEnumerator StartBurnAnimation()
    {
        if (_instanceMaterial == null) yield break;

        // 平滑过渡Dissolve到1（完全溶解）
        float elapsed = 0f;
        while (elapsed < burnDuration)
        {
            elapsed += Time.deltaTime;
            float dissolveValue = Mathf.Lerp(-1f, 1f, elapsed / burnDuration);
            _instanceMaterial.SetFloat("_Dissolve", dissolveValue);
            yield return null;
        }

        // 动画结束后销毁卡牌
        Destroy(gameObject);
    }

    // 外部调用接口
    public void TriggerBurnAndDestroy()
    {
        StartCoroutine(StartBurnAnimation());
    }

    // 销毁时释放材质实例
    private void OnDestroy()
    {
        if (_instanceMaterial != null)
        {
            Destroy(_instanceMaterial);
        }
    }
}