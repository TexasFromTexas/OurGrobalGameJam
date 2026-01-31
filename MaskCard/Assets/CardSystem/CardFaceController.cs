using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

[ExecuteInEditMode]
public class CardFaceController : MonoBehaviour
{
    [Header("卡牌正反面引用")]
    public Image cardBackImage; // 卡牌背面（暗牌）
    public Image cardFrontImage; // 卡牌正面（明牌）
    public Text cardText; // 卡牌文字（正面）

    [Header("翻牌动画配置")]
    public Animation cardAnimation; // 卡牌根节点的Animation组件（拖入）
    public string flipAnimationName = "Flip"; // 翻牌动画名称（匹配你的动画文件）
    public bool waitForAnimationFinish = true; // 是否等待动画完成后再显示正面（更自然）

    [Header("当前状态（只读）")]
    [SerializeField, ReadOnly]
    private string currentCardState = "未初始化";

    public bool _isShowingBack = true; // Default to TRUE (Face Down)
    private Text _autoFoundCardText;
    private CardDisplay _cardDisplay; // 存储卡牌固定数据
    public PlayingCard bindingCardData; // 绑定的核心数据（唯一数据源）
    public bool NeedRefreshDisplay = false; // 是否需要刷新显示

    // 新增：初始化锁，避免初始时播放动画
    private bool _isInitialized = false;

    private void Awake()
    {
        // 获取卡牌固定数据（生成时就确定的牌面属性）
        _cardDisplay = GetComponent<CardDisplay>();
        if (cardText == null)
        {
            _autoFoundCardText = GetComponentInChildren<Text>(true);
        }

        // 自动查找Animation组件（如果未手动拖入）
        if (cardAnimation == null)
        {
            cardAnimation = GetComponent<Animation>();
            if (cardAnimation == null)
            {
                Debug.LogWarning($"卡牌{gameObject.name}缺少Animation组件！请添加并绑定Flip动画");
            }
        }

        // 强制应用初始状态（仅显示背面，不播放动画）
        _isInitialized = false; // 初始化锁关闭，避免触发动画
        ApplyFaceState();
        _isInitialized = true; // 初始化完成，后续切换才播放动画
    }

    // ========== 新增：获取牌面固定信息 ==========
    /// <summary>
    /// 获取卡牌固定的牌面文字（比如“黑桃A”）
    /// </summary>
    public string GetCardText()
    {
        if (_cardDisplay != null && _cardDisplay.cardData != null)
        {
            return _cardDisplay.cardData.cardName;
        }
        else if (cardText != null)
        {
            return cardText.text;
        }
        else if (_autoFoundCardText != null)
        {
            return _autoFoundCardText.text;
        }
        return "未知卡牌";
    }

    /// <summary>
    /// 获取卡牌正面图片
    /// </summary>
    public Sprite GetCardFrontSprite()
    {
        return cardFrontImage != null ? cardFrontImage.sprite : null;
    }

    // ========== 原有方法（修改：添加初始化锁） ==========
    public void ShowBackFace()
    {
        _isShowingBack = true;
        ApplyFaceState();
    }

    public void ShowFrontFace()
    {
        // 仅当初始化完成 + 当前是背面时，才播放翻牌动画
        if (_isInitialized && _isShowingBack)
        {
            PlayFlipAnimation();
        }
        else
        {
            // 初始化中/已是正面，直接应用状态（不播放动画）
            _isShowingBack = false;
            ApplyFaceState();
        }

        // DEBUG: Trace who is revealing the card
        if (gameObject.GetComponentInParent<CardDeckSystem>() != null || transform.parent.name.Contains("Public"))
        {
            Debug.Log($"[CardFaceController] ShowFrontFace called on {gameObject.name}. Stack: {System.Environment.StackTrace}");
        }
    }

    // ========== 核心新增：播放翻牌动画（添加初始化锁） ==========
    /// <summary>
    /// 播放Flip翻牌动画，完成后切换到正面
    /// </summary>
    private void PlayFlipAnimation()
    {
        // 初始化未完成时，不播放动画
        if (!_isInitialized)
        {
            _isShowingBack = false;
            ApplyFaceState();
            return;
        }

        if (cardAnimation == null)
        {
            // 无动画组件，直接切换状态
            _isShowingBack = false;
            ApplyFaceState();
            Debug.LogWarning($"卡牌{gameObject.name}无Animation组件，直接显示正面");
            return;
        }

        // 查找Flip动画剪辑
        AnimationClip flipClip = cardAnimation.GetClip(flipAnimationName);
        if (flipClip == null)
        {
            // 无对应动画，直接切换状态
            _isShowingBack = false;
            ApplyFaceState();
            Debug.LogWarning($"卡牌{gameObject.name}未找到Flip动画：{flipAnimationName}，直接显示正面");
            return;
        }

        // 停止其他动画，播放翻牌动画
        cardAnimation.Stop();
        cardAnimation.Play(flipAnimationName);

        // 根据配置，选择是否等待动画完成后显示正面
        if (waitForAnimationFinish)
        {
            StartCoroutine(WaitForFlipAnimationThenShowFront(flipClip.length));
        }
        else
        {
            // 立即切换状态（动画和显示同步）
            _isShowingBack = false;
            ApplyFaceState();
        }
    }

    /// <summary>
    /// 协程：等待翻牌动画完成后，再显示卡牌正面
    /// </summary>
    private IEnumerator WaitForFlipAnimationThenShowFront(float animationDuration)
    {
        // 动画播放期间，保持背面显示
        _isShowingBack = true;
        ApplyFaceState();

        // 等待动画完成（可微调时间，比如乘以0.8，让显示时机更自然）
        yield return new WaitForSeconds(animationDuration * 0.8f);

        // 动画接近完成时，切换到正面
        _isShowingBack = false;
        ApplyFaceState();

        // 等待动画完全结束
        yield return new WaitForSeconds(animationDuration * 0.2f);
    }

    private void ApplyFaceState()
    {
        bool shouldShowFront = !_isShowingBack;
        if (cardBackImage != null) cardBackImage.gameObject.SetActive(!shouldShowFront);
        if (cardFrontImage != null) cardFrontImage.gameObject.SetActive(shouldShowFront);

        if (cardText == null && _autoFoundCardText == null)
        {
            _autoFoundCardText = GetComponentInChildren<Text>(true);
        }
        if (cardText != null)
            cardText.gameObject.SetActive(shouldShowFront);
        else if (_autoFoundCardText != null)
            _autoFoundCardText.gameObject.SetActive(shouldShowFront);

        // 更新状态显示
        UpdateStateDisplay();
    }

    private void UpdateStateDisplay()
    {
        currentCardState = _isShowingBack ? "背面（暗牌）" : "正面（明牌）";
    }

    public class ReadOnlyAttribute : PropertyAttribute { }
}

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(CardFaceController.ReadOnlyAttribute))]
public class ReadOnlyDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        GUI.enabled = false;
        EditorGUI.PropertyField(position, property, label);
        GUI.enabled = true;
    }
}
#endif