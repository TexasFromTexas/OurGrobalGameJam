using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

[ExecuteInEditMode]
public class CardFaceController : MonoBehaviour
{
    [Header("卡牌正反面引用")]
    public Image cardBackImage; // 卡牌背面（暗牌）
    public Image cardFrontImage; // 卡牌正面（明牌）
    public Text cardText; // 卡牌文字（正面，可选：留空则自动查找）

    [Header("当前状态（只读）")]
    [SerializeField, ReadOnly]
    private string currentCardState = "未初始化";

    private bool _isShowingBack = false;
    private Text _autoFoundCardText; // 自动找到的文字组件

    private void Awake()
    {
        // ❶ 把自动查找移到Awake()，确保在ShowBackFace之前就初始化
        if (cardText == null)
        {
            _autoFoundCardText = GetComponentInChildren<Text>(true); // 包含禁用的组件
        }
    }

    private void Update()
    {
        UpdateStateDisplay();
    }

    public void ShowBackFace()
    {
        // 控制图片显示
        if (cardBackImage != null) cardBackImage.gameObject.SetActive(true);
        if (cardFrontImage != null) cardFrontImage.gameObject.SetActive(false);

        // ❷ 强制再次查找（防止Awake没找到）
        if (cardText == null && _autoFoundCardText == null)
        {
            _autoFoundCardText = GetComponentInChildren<Text>(true);
        }

        // 控制文字显示（双保险）
        if (cardText != null)
            cardText.gameObject.SetActive(false);
        else if (_autoFoundCardText != null)
            _autoFoundCardText.gameObject.SetActive(false);

        _isShowingBack = true;
    }

    public void ShowFrontFace()
    {
        // 控制图片显示
        if (cardBackImage != null) cardBackImage.gameObject.SetActive(false);
        if (cardFrontImage != null) cardFrontImage.gameObject.SetActive(true);

        // 强制再次查找
        if (cardText == null && _autoFoundCardText == null)
        {
            _autoFoundCardText = GetComponentInChildren<Text>(true);
        }

        // 控制文字显示（双保险）
        if (cardText != null)
            cardText.gameObject.SetActive(true);
        else if (_autoFoundCardText != null)
            _autoFoundCardText.gameObject.SetActive(true);

        _isShowingBack = false;
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