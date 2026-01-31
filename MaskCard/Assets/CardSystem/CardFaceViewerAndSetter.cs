using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

/// <summary>
/// 卡牌正反面状态查看器+贴图设置器（挂载在卡牌预制体上）
/// 功能1：读取CardFaceController状态，在Inspector显示正反面信息
/// 功能2：根据状态自动给正反面Image赋值不同贴图
/// 无需修改原有CardFaceController代码
/// </summary>
[ExecuteInEditMode] // 编辑模式下也能实时显示状态
[RequireComponent(typeof(CardFaceController))]
public class CardFaceViewerAndSetter : MonoBehaviour
{
    [Header("=== 贴图配置 ===")]
    [Tooltip("卡牌背面要显示的贴图（Sprite类型）")]
    public Sprite backSprite;
    [Tooltip("卡牌正面要显示的贴图（Sprite类型）")]
    public Sprite frontSprite;

    [Header("=== 实时状态查看（只读）===")]
    [SerializeField, Space(5)]
    public string currentFaceState = "未初始化"; // 供Inspector查看的状态文本
    [SerializeField]
    public bool isShowingBack = false; // 同步CardFaceController的状态

    // 引用原有正反面控制器
    private CardFaceController _faceController;
    // 标记是否初始化完成（避免重复执行）
    private bool _isInited = false;

    private void Awake()
    {
        Init();
    }

    private void OnEnable()
    {
        Init();
    }

    // 编辑模式下修改参数后触发（比如拖入Sprite）
    private void OnValidate()
    {
        Init();
        UpdateFaceStateAndSprite();
    }

    private void Update()
    {
        // 运行/编辑模式都实时更新状态和贴图
        if (!_isInited) Init();
        UpdateFaceStateAndSprite();
    }

    /// <summary>
    /// 初始化：获取CardFaceController引用
    /// </summary>
    private void Init()
    {
        if (_isInited) return;

        _faceController = GetComponent<CardFaceController>();
        if (_faceController == null)
        {
            currentFaceState = "错误：未找到CardFaceController！";
            isShowingBack = false;
            _isInited = false;
            return;
        }

        _isInited = true;
        currentFaceState = "已初始化，等待状态更新";
    }

    /// <summary>
    /// 核心逻辑：同步状态 + 更新贴图
    /// </summary>
    private void UpdateFaceStateAndSprite()
    {
        if (_faceController == null) return;

        // 1. 同步CardFaceController的状态（供Inspector查看）
        isShowingBack = _faceController._isShowingBack;
        currentFaceState = isShowingBack ? "当前状态：背面（暗牌）" : "当前状态：正面（明牌）";

        // 2. 给正反面Image赋值对应贴图（复用CardFaceController的Image组件）
        if (_faceController.cardBackImage != null && backSprite != null)
        {
            _faceController.cardBackImage.sprite = backSprite;
        }
        if (_faceController.cardFrontImage != null && frontSprite != null)
        {
            _faceController.cardFrontImage.sprite = frontSprite;
        }
    }

    // ========== 可选：手动切换正反面（方便测试） ==========
    [ContextMenu("切换为正面")] // 右键菜单可直接调用
    public void ManualShowFront()
    {
        if (_faceController != null)
        {
            _faceController.ShowFrontFace();
            UpdateFaceStateAndSprite();
        }
    }

    [ContextMenu("切换为背面")]
    public void ManualShowBack()
    {
        if (_faceController != null)
        {
            _faceController.ShowBackFace();
            UpdateFaceStateAndSprite();
        }
    }
}

// ========== 可选：美化Inspector显示（让状态更醒目） ==========
#if UNITY_EDITOR
[CustomEditor(typeof(CardFaceViewerAndSetter))]
public class CardFaceViewerAndSetterEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // 绘制原有字段
        base.OnInspectorGUI();

        CardFaceViewerAndSetter targetScript = (CardFaceViewerAndSetter)target;

        // 醒目显示当前状态
        GUILayout.Space(10);
        GUIStyle stateStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 14,
            fontStyle = FontStyle.Bold,
            normal = { textColor = targetScript.isShowingBack ? Color.blue : Color.red }
        };
        GUILayout.Label(targetScript.currentFaceState, stateStyle);
    }
}
#endif