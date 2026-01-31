using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 卡牌贴图控制器（挂载在卡牌预制体上）
/// 功能：检测正反面状态，自动切换对应的贴图
/// </summary>
[RequireComponent(typeof(Image))] // 强制挂载Image组件，避免遗漏
public class CardSpriteController : MonoBehaviour
{
    [Header("卡牌贴图配置")]
    [Tooltip("卡牌正面的贴图（Sprite类型）")]
    public Sprite frontSprite; // 正面贴图
    [Tooltip("卡牌背面的贴图（Sprite类型）")]
    public Sprite backSprite; // 背面贴图

    [Header("状态控制（只读）")]
    [SerializeField] private bool isShowingBack = true; // 默认显示背面
    private Image cardImage; // 卡牌的主Image组件（用于显示贴图）

    private void Awake()
    {
        // 自动获取挂载在当前物体上的Image组件
        cardImage = GetComponent<Image>();
        if (cardImage == null)
        {
            Debug.LogError($"[{name}] 未找到Image组件！请确保卡牌预制体有Image组件", this);
            return;
        }

        // 初始化：默认显示背面贴图
        UpdateCardSprite();
    }

    /// <summary>
    /// 核心方法：根据当前状态更新贴图
    /// </summary>
    private void UpdateCardSprite()
    {
        if (cardImage == null) return;

        // 检测状态，赋值对应贴图
        if (isShowingBack)
        {
            // 显示背面：赋值背面贴图
            if (backSprite != null)
            {
                cardImage.sprite = backSprite;
                Debug.Log($"[{name}] 切换为背面，贴图已更新", this);
            }
            else
            {
                Debug.LogWarning($"[{name}] 背面贴图未绑定！", this);
            }
        }
        else
        {
            // 显示正面：赋值正面贴图
            if (frontSprite != null)
            {
                cardImage.sprite = frontSprite;
                Debug.Log($"[{name}] 切换为正面，贴图已更新", this);
            }
            else
            {
                Debug.LogWarning($"[{name}] 正面贴图未绑定！", this);
            }
        }
    }

    #region 外部调用方法（切换正反面）
    /// <summary>
    /// 切换到卡牌正面（外部调用）
    /// </summary>
    public void ShowFront()
    {
        isShowingBack = false;
        UpdateCardSprite();
    }

    /// <summary>
    /// 切换到卡牌背面（外部调用）
    /// </summary>
    public void ShowBack()
    {
        isShowingBack = true;
        UpdateCardSprite();
    }

    /// <summary>
    /// 切换正反面状态（外部调用，切换当前显示）
    /// </summary>
    public void ToggleFace()
    {
        isShowingBack = !isShowingBack;
        UpdateCardSprite();
    }
    #endregion

    #region 状态查询方法（获取当前正反面）
    /// <summary>
    /// 查询当前是否显示背面
    /// </summary>
    /// <returns>true=背面，false=正面</returns>
    public bool IsShowingBack()
    {
        return isShowingBack;
    }

    /// <summary>
    /// 查询当前是否显示正面
    /// </summary>
    /// <returns>true=正面，false=背面</returns>
    public bool IsShowingFront()
    {
        return !isShowingBack;
    }
    #endregion
}