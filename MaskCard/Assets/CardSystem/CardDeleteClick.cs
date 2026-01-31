using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// 卡牌点击删除脚本（最终版：适配大写_Dissolve + CardFront子物体 + 无延迟 + 重置Dissolve）
/// </summary>
public class CardDeleteClick : MonoBehaviour, IPointerClickHandler
{
    private CardDeleteButton cardDeleteButton;
    private Image _cardImage; // 指向CardFront的Image
    private Material _instanceMaterial;
    // 核心修正：匹配Shader中的大写Dissolve（Unity规范是_Dissolve）
    private const string DISSOLVE_PROPERTY_NAME = "_Dissolve";

    private void Awake()
    {
        // 1. 找到删除按钮逻辑
        cardDeleteButton = FindAnyObjectByType<CardDeleteButton>();

        // 2. 关键：找到子物体CardFront的Image组件
        Transform cardFrontTrans = transform.Find("CardFront");
        if (cardFrontTrans != null)
        {
            _cardImage = cardFrontTrans.GetComponent<Image>();
            if (_cardImage == null)
            {
                Debug.LogError($"卡牌{gameObject.name}的CardFront子物体缺少Image组件！");
            }
        }
        else
        {
            Debug.LogError($"卡牌{gameObject.name}未找到CardFront子物体！请检查预制体结构");
        }

        // 3. 初始化材质并重置Dissolve为-1
        ResetDissolveToDefault();
    }

    // 卡牌点击触发选中
    public void OnPointerClick(PointerEventData eventData)
    {
        if (cardDeleteButton != null && cardDeleteButton.IsDeleteMode)
        {
            if (IsPlayerHandCard())
            {
                cardDeleteButton.SelectHandCardToDelete(gameObject);
            }
            else if (IsPublicCard())
            {
                cardDeleteButton.SelectPublicCardToDelete(gameObject);
            }
        }
    }

    /// <summary>
    /// 核心：Dissolve从-1渐变到1，完成后删除并重置
    /// </summary>
    public IEnumerator AnimateDissolveToDestroy(float duration = 1.2f)
    {
        // 双重校验：确保CardFront的Image和材质存在
        if (_instanceMaterial == null || _cardImage == null)
        {
            Debug.LogWarning($"卡牌{gameObject.name}的CardFront材质/Image为空，直接删除");
            Destroy(gameObject);
            yield break;
        }

        float elapsedTime = 0f;
        float startValue = -1f; // 强制初始值为-1
        float targetValue = 1f;

        // 立即设置初始值，无延迟生效
        _instanceMaterial.SetFloat(DISSOLVE_PROPERTY_NAME, startValue);
        _cardImage.material = _instanceMaterial; // 强制刷新材质

        // 平滑渐变（防止帧率异常导致数值超界）
        while (elapsedTime < duration)
        {
            elapsedTime = Mathf.Min(elapsedTime + Time.deltaTime, duration);
            float currentValue = Mathf.Lerp(startValue, targetValue, elapsedTime / duration);

            // 写入大写_Dissolve属性（匹配Shader）
            _instanceMaterial.SetFloat(DISSOLVE_PROPERTY_NAME, currentValue);
            _cardImage.material = _instanceMaterial;

            // 调试日志：可查看每帧数值变化
            Debug.Log($"卡牌{gameObject.name} CardFront Dissolve值：{currentValue}");
            yield return null;
        }

        // 确保最终值为1
        _instanceMaterial.SetFloat(DISSOLVE_PROPERTY_NAME, targetValue);
        _cardImage.material = _instanceMaterial;

        // 删除前重置Dissolve为-1
        ResetDissolveToDefault();

        // 销毁材质和卡牌（延迟0.1秒确保重置生效）
        Destroy(_instanceMaterial);
        Destroy(gameObject, 0.1f);
    }

    /// <summary>
    /// 外部调用接口
    /// </summary>
    public void TriggerDissolveDelete(float duration = 1.2f)
    {
        // 停止已有协程，避免重复执行导致延迟
        StopAllCoroutines();
        StartCoroutine(AnimateDissolveToDestroy(duration));
    }

    /// <summary>
    /// 重置Dissolve为默认值-1（核心适配CardFront）
    /// </summary>
    public void ResetDissolveToDefault()
    {
        if (_cardImage != null)
        {
            // 重新实例化CardFront的材质，避免共享材质污染
            if (_cardImage.material != null)
            {
                _instanceMaterial = new Material(_cardImage.material);
            }
            else
            {
                Debug.LogWarning($"卡牌{gameObject.name}的CardFront无绑定材质！");
                return;
            }

            // 写入大写_Dissolve属性，重置为-1
            _instanceMaterial.SetFloat(DISSOLVE_PROPERTY_NAME, -1f);
            _cardImage.material = _instanceMaterial;
            Debug.Log($"卡牌{gameObject.name} CardFront Dissolve已重置为-1");
        }
    }

    // 判断是否为玩家手牌
    private bool IsPlayerHandCard()
    {
        if (cardDeleteButton == null || cardDeleteButton.cardDeckSystem == null)
            return false;
        return cardDeleteButton.cardDeckSystem.playerCardObjects.Contains(gameObject);
    }

    // 判断是否为公牌
    private bool IsPublicCard()
    {
        if (cardDeleteButton == null || cardDeleteButton.cardDeckSystem == null)
            return false;
        return cardDeleteButton.cardDeckSystem.PublicCardObjects.Contains(gameObject);
    }

    // 销毁时强制重置Dissolve
    private void OnDestroy()
    {
        ResetDissolveToDefault();
        if (_instanceMaterial != null)
        {
            Destroy(_instanceMaterial);
        }
    }

    // 禁用时重置（防止复用卡牌时Dissolve异常）
    private void OnDisable()
    {
        ResetDissolveToDefault();
    }
}