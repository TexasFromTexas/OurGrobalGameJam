using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// 覆盖层偷看公牌脚本（零侵入原公牌）
/// 新增：仅偷看已翻开牌后面的2张（不足则1张）
/// </summary>
public class PeekCardOverlayButton : MonoBehaviour
{
    [Header("核心引用")]
    public CardDeckSystem cardDeckSystem; // 拖入CardDeckManager
    public Button peekBtn; // 拖入“偷看公牌”按钮
    public GameObject peekCardOverlayPrefab; // 拖入制作好的覆盖层预制体

    [Header("视觉效果参数（可调整）")]
    public Vector2 peekOffset = new Vector2(0, 10); // 上移偏移量（y轴+10）
    [Range(0.5f, 1f)] public float peekAlpha = 0.8f; // 透明度（0.8=微微透明）

    private List<GameObject> _overlayList = new List<GameObject>(); // 存储生成的覆盖层
    private bool _isPeeking = false;

    private void Start()
    {
        // 容错检查
        if (cardDeckSystem == null)
        {
            Debug.LogError("请拖入CardDeckSystem！");
            return;
        }
        if (peekBtn == null)
        {
            Debug.LogError("请拖入偷看按钮！");
            return;
        }
        if (peekCardOverlayPrefab == null)
        {
            Debug.LogError("请拖入覆盖层预制体！");
            return;
        }

        peekBtn.onClick.AddListener(TogglePeek);
        UpdateButtonState();
    }

    private void Update()
    {
        UpdateButtonState();
    }

    /// <summary>
    /// 更新按钮状态：仅回合中+有可偷看的牌时可用
    /// </summary>
    private void UpdateButtonState()
    {
        if (cardDeckSystem == null || peekBtn == null) return;

        bool isInRound = cardDeckSystem.IsInRound;
        bool hasPeekableCards = HasPeekableCards(); // 新增：判断是否有可偷看的牌

        peekBtn.interactable = isInRound && hasPeekableCards;
        peekBtn.GetComponentInChildren<Text>().text = _isPeeking ? "摘下面具" : "露出狡诈的样子";
    }

    /// <summary>
    /// 新增：判断是否有可偷看的牌（已翻开牌后面有未翻开的牌）
    /// </summary>
    private bool HasPeekableCards()
    {
        List<GameObject> publicCards = cardDeckSystem.PublicCardObjects;
        if (publicCards.Count == 0) return false;

        // 找到最后一张已翻开的牌的索引
        int lastRevealedIndex = GetLastRevealedCardIndex();
        // 已翻开牌后面有未翻开的牌 → 可偷看
        return lastRevealedIndex < publicCards.Count - 1;
    }

    /// <summary>
    /// 新增：获取最后一张已翻开的公牌索引（核心逻辑）
    /// </summary>
    private int GetLastRevealedCardIndex()
    {
        List<GameObject> publicCards = cardDeckSystem.PublicCardObjects;
        int lastRevealedIndex = -1; // 默认-1（没有已翻开的牌）

        for (int i = 0; i < publicCards.Count; i++)
        {
            CardFaceController ctrl = publicCards[i].GetComponent<CardFaceController>();
            if (ctrl == null) continue;

            // 判断卡牌是否已翻开（非背面状态）
            // 注：通过CardFaceController的状态标记判断，更精准
            bool isRevealed = !ctrl.GetComponent<CardFaceController>()._isShowingBack;
            if (isRevealed)
            {
                lastRevealedIndex = i; // 更新最后一张已翻开的索引
            }
        }

        return lastRevealedIndex;
    }

    /// <summary>
    /// 切换偷看状态：生成/销毁覆盖层
    /// </summary>
    private void TogglePeek()
    {
        _isPeeking = !_isPeeking;

        if (_isPeeking)
        {
            CreatePeekOverlays(); // 生成覆盖层（仅偷看指定牌）
        }
        else
        {
            DestroyPeekOverlays(); // 销毁覆盖层
        }
    }

    /// <summary>
    /// 核心修改：仅生成已翻开牌后面的2张（不足则1张）的覆盖层
    /// </summary>
    private void CreatePeekOverlays()
    {
        List<GameObject> publicCards = cardDeckSystem.PublicCardObjects;
        if (publicCards.Count == 0) return;

        // 1. 找到最后一张已翻开的牌的索引
        int lastRevealedIndex = GetLastRevealedCardIndex();
        // 2. 确定偷看的起始索引（已翻开牌的下一张）
        int peekStartIndex = lastRevealedIndex + 1;
        // 3. 计算可偷看的牌数量（最多2张，最少1张）
        int availableCount = publicCards.Count - peekStartIndex;
        int peekCount = availableCount >= 2 ? 2 : (availableCount >= 1 ? 1 : 0);

        if (peekCount == 0)
        {
            Debug.LogWarning("没有可偷看的牌（已无未翻开的牌）！");
            _isPeeking = false; // 重置状态
            UpdateButtonState();
            return;
        }

        Debug.Log($"偷看范围：从索引{peekStartIndex}开始，共{peekCount}张牌");

        // 4. 仅生成指定范围的覆盖层
        for (int i = peekStartIndex; i < peekStartIndex + peekCount; i++)
        {
            if (i >= publicCards.Count) break; // 边界保护

            GameObject originCard = publicCards[i];
            CardFaceController originCtrl = originCard.GetComponent<CardFaceController>();
            if (originCtrl == null || originCtrl.GetCardText() == "未知卡牌") continue;

            // 生成覆盖层（带偏移+透明）
            GameObject overlay = Instantiate(peekCardOverlayPrefab, originCard.transform.parent);

            // 设置位置：原位置 + 偏移量
            Vector3 newPos = originCard.transform.position;
            newPos.x += peekOffset.x;
            newPos.y += peekOffset.y;
            overlay.transform.position = newPos;

            overlay.transform.rotation = originCard.transform.rotation;
            overlay.transform.localScale = originCard.transform.localScale;

            // 赋值牌面信息 + 设置透明
            Image overlayFront = overlay.GetComponentInChildren<Image>(true);
            Text overlayText = overlay.GetComponentInChildren<Text>(true);

            if (overlayFront != null)
            {
                overlayFront.sprite = originCtrl.GetCardFrontSprite();
                Color frontColor = overlayFront.color;
                frontColor.a = peekAlpha;
                overlayFront.color = frontColor;
                overlayFront.gameObject.SetActive(true);
            }
            if (overlayText != null)
            {
                overlayText.text = originCtrl.GetCardText();
                Color textColor = overlayText.color;
                textColor.a = peekAlpha + 0.1f; // 文字稍不透明
                overlayText.color = textColor;
                overlayText.gameObject.SetActive(true);
            }

            overlay.SetActive(true);
            _overlayList.Add(overlay);

            Debug.Log($"生成偷看覆盖层：{originCtrl.GetCardText()}（索引{i}）");
        }
    }

    /// <summary>
    /// 销毁所有覆盖层
    /// </summary>
    private void DestroyPeekOverlays()
    {
        foreach (var overlay in _overlayList)
        {
            if (overlay != null)
            {
                Destroy(overlay);
            }
        }
        _overlayList.Clear();
        Debug.Log("销毁所有偷看覆盖层");
    }

    /// <summary>
    /// 回合结束时自动销毁覆盖层
    /// </summary>
    private void OnRoundEnd()
    {
        _isPeeking = false;
        DestroyPeekOverlays();
    }

    private void OnDestroy()
    {
        DestroyPeekOverlays();
    }
}