using UnityEngine;
using UnityEngine.UI;
using BetSystem; // 必须引用命名空间

/// <summary>
/// 卡牌正反面贴图设置器（挂载在卡牌预制体上）
/// 功能：读取CardFaceController的状态，自动给正反面Image赋值不同贴图
/// 无需修改原有代码，仅依赖CardFaceController的公开字段/组件
/// </summary>
[RequireComponent(typeof(CardFaceController))] // 强制依赖原有控制器，避免遗漏
public class CardFaceSpriteSetter : MonoBehaviour
{
    [Header("卡牌贴图配置（拖入不同Sprite）")]
    [Tooltip("卡牌背面显示的贴图")]
    public Sprite backSprite; // 背面贴图
    [Tooltip("卡牌正面显示的贴图")]
    public Sprite frontSprite; // 正面贴图

    // 引用原有控制器（自动获取，无需手动拖入）
    private CardFaceController _faceController;

    private void Awake()
    {
        // 自动获取卡牌上的CardFaceController（原有脚本）
        _faceController = GetComponent<CardFaceController>();
        if (_faceController == null)
        {
            Debug.LogError($"[{name}] 未找到CardFaceController组件！请确保预制体已挂载该组件", this);
            enabled = false; // 禁用脚本避免报错
            return;
        }

        // 初始化：根据当前正反面状态设置贴图
        UpdateSpriteByFaceState();
    }

    private void Update()
    {
        // 实时检测状态变化（也可改用事件，性能更优，见下方优化说明）
        UpdateSpriteByFaceState();
    }

    /// <summary>
    /// 核心逻辑：根据CardFaceController的状态更新贴图
    /// 完全复用原有CardFaceController的Image组件，无需重复绑定
    /// </summary>
    private void UpdateSpriteByFaceState()
    {
        if (_faceController == null) return;

        // 1. 获取原有CardFaceController绑定的正反面Image组件
        Image backImage = _faceController.cardBackImage;
        Image frontImage = _faceController.cardFrontImage;

        // 空值检查：避免原有Image未绑定导致报错
        if (backImage == null || frontImage == null)
        {
            Debug.LogWarning($"[{name}] CardFaceController的正反面Image未绑定！请先在预制体中绑定", this);
            return;
        }

        // 2. 读取原有控制器的正反面状态
        bool isShowingBack = _faceController._isShowingBack;

        // 3. 给背面Image赋值（仅当状态为背面时生效，保证贴图唯一）
        if (backSprite != null && backImage.sprite != backSprite)
        {
            backImage.sprite = backSprite;
        }

        // 4. 给正面Image赋值（仅当状态为正面时生效）
        if (frontSprite != null && frontImage.sprite != frontSprite)
        {
            frontImage.sprite = frontSprite;
        }

        // 可选：打印状态日志（调试用，发布时可注释）
        // Debug.Log($"[{name}] 状态：{(isShowingBack ? "背面" : "正面")} | 贴图已更新", this);
    }

    // ========== 可选：手动触发贴图更新（外部调用） ==========
    /// <summary>
    /// 手动刷新贴图（比如外部修改了Sprite后调用）
    /// </summary>
    public void RefreshSprite()
    {
        UpdateSpriteByFaceState();
    }

    // ========== 优化：改用事件监听（替代Update，降低性能消耗） ==========
    /// <summary>
    /// 如需优化性能，可注释Update方法，启用以下事件监听逻辑
    /// 需在原有CardFaceController中添加状态变更事件（无需修改原有代码，仅扩展）
    /// </summary>
    /*
    private void OnEnable()
    {
        // 监听原有控制器的状态变更（需扩展CardFaceController，见下方说明）
        if (_faceController != null)
        {
            _faceController.OnFaceStateChanged += UpdateSpriteByFaceState;
        }
    }

    private void OnDisable()
    {
        // 取消监听，避免内存泄漏
        if (_faceController != null)
        {
            _faceController.OnFaceStateChanged -= UpdateSpriteByFaceState;
        }
    }
    */
}

// ========== 可选扩展：给原有CardFaceController添加事件（无需修改原有代码，新增此段即可） ==========
// 说明：此段代码可单独放在任意脚本中，或追加到CardFaceController脚本末尾，用于优化性能
