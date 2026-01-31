using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

[ExecuteInEditMode]
public class CardFaceController : MonoBehaviour
{
    [Header("卡牌正反面引用")]
    public Image cardBackImage; // 卡牌背面（暗牌）
    public Image cardFrontImage; // 卡牌正面（明牌）
    public Text cardText; // 卡牌文字（正面）

    [Header("当前状态（只读）")]
    [SerializeField, ReadOnly]
    private string currentCardState = "未初始化";

    public bool _isShowingBack = true; // Default to TRUE (Face Down)
    private Text _autoFoundCardText;
    private CardDisplay _cardDisplay; // 存储卡牌固定数据
    public PlayingCard bindingCardData; // 绑定的核心数据（唯一数据源）
    public bool NeedRefreshDisplay = false; // 是否需要刷新显示

    private void Awake()
    {
        // 获取卡牌固定数据（生成时就确定的牌面属性）
        _cardDisplay = GetComponent<CardDisplay>();
        if (cardText == null)
        {
            _autoFoundCardText = GetComponentInChildren<Text>(true);
        }
        
        // Force Apply Initial State (Face Down)
        ApplyFaceState();
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

    // ========== 原有方法（不动） ==========
    public void ShowBackFace()
    {
        _isShowingBack = true;
        // Debug.Log($"[CardFaceController] ShowBACKFace on {gameObject.name}");
        ApplyFaceState();
    }

    public void ShowFrontFace()
    {
        _isShowingBack = false;
        
        // DEBUG: Trace who is revealing the card
        if (gameObject.GetComponentInParent<CardDeckSystem>() != null || transform.parent.name.Contains("Public"))
        {
             Debug.Log($"[CardFaceController] ShowFrontFace called on {gameObject.name}. Stack: {System.Environment.StackTrace}");
        }

        ApplyFaceState();
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