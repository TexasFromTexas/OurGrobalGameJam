using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using BetSystem; // Added namespace

/// <summary>
/// 覆盖层偷看公牌脚本（修复：新增公牌后可用）
/// </summary>
public class PeekCardOverlayButton : MonoBehaviour
{
    [Header("核心引用")]
    public CardDeckSystem cardDeckSystem;
    public Button peekBtn;
    public GameObject peekCardOverlayPrefab;

    public BetManager betManager;

    [Header("视觉效果参数（可调整）")]
    public Vector2 peekOffset = new Vector2(0, 10);
    [Range(0.5f, 1f)] public float peekAlpha = 0.8f;

    private List<GameObject> _overlayList = new List<GameObject>();
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

        if (betManager == null) betManager = FindFirstObjectByType<BetManager>();

        peekBtn.onClick.AddListener(TogglePeek);
        UpdateButtonState();
    }

    private void Update()
    {
        UpdateButtonState();
    }

    /// <summary>
    /// 核心修复：更新按钮状态（补全逻辑漏洞）
    /// </summary>
    private void UpdateButtonState()
    {
        if (cardDeckSystem == null || peekBtn == null) return;

        bool isInRound = cardDeckSystem.IsInRound;
        bool hasPeekableCards = HasPeekableCards(); // 修复后的判断逻辑

        bool costCondition = true;
        if (betManager != null)
        {
            // Only require cost if we are NOT currently peeking (i.e. to Start peeking)
            // If we are already peeking, we can toggle off freely.
            if (!_isPeeking)
            {
                costCondition = betManager.playerChips >= betManager.costPeekCard;
            }
        }

        peekBtn.interactable = isInRound && hasPeekableCards && costCondition;

        // 文本切换（保留你的逻辑）
        Text btnText = peekBtn.GetComponentInChildren<Text>();
        if (btnText != null)
        {
            btnText.text = _isPeeking ? "摘下面具" : "露出狡诈的样子";
        }
        else
        {
            Debug.LogWarning("偷看按钮缺少Text子组件！");
        }
    }

    /// <summary>
    /// 核心修复：重构“是否有可偷看牌”的判断逻辑
    /// 支持两种场景：1.有已翻开牌且后面有未翻开牌 2.无已翻开牌但有未翻开牌
    /// </summary>
    private bool HasPeekableCards()
    {
        List<GameObject> publicCards = cardDeckSystem.PublicCardObjects;
        if (publicCards == null || publicCards.Count == 0) return false;

        int lastRevealedIndex = GetLastRevealedCardIndex();
        bool hasUnrevealedAfter = lastRevealedIndex < publicCards.Count - 1; // 已翻开牌后有未翻开牌
        bool hasAnyUnrevealed = HasAnyUnrevealedCards(); // 有任意未翻开牌

        // 只要满足一种场景就可偷看
        return hasUnrevealedAfter || (lastRevealedIndex == -1 && hasAnyUnrevealed);
    }

    /// <summary>
    /// 新增：判断是否有任意未翻开的公牌（解决新增公牌后无已翻开牌的情况）
    /// </summary>
    private bool HasAnyUnrevealedCards()
    {
        List<GameObject> publicCards = cardDeckSystem.PublicCardObjects;
        if (publicCards == null || publicCards.Count == 0) return false;

        foreach (var card in publicCards)
        {
            if (card == null) continue;
            CardFaceController ctrl = card.GetComponent<CardFaceController>();
            if (ctrl != null && ctrl._isShowingBack) // 未翻开（背面）
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// 核心修复：修正重复获取CardFaceController的错误
    /// </summary>
    private int GetLastRevealedCardIndex()
    {
        List<GameObject> publicCards = cardDeckSystem.PublicCardObjects;
        if (publicCards == null || publicCards.Count == 0) return -1;

        int lastRevealedIndex = -1;
        for (int i = 0; i < publicCards.Count; i++)
        {
            GameObject card = publicCards[i];
            if (card == null) continue;

            // 修复：直接使用已获取的ctrl，无需重复GetComponent
            CardFaceController ctrl = card.GetComponent<CardFaceController>();
            if (ctrl == null) continue;

            bool isRevealed = !ctrl._isShowingBack; // 非背面=已翻开
            if (isRevealed)
            {
                lastRevealedIndex = i;
            }
        }
        return lastRevealedIndex;
    }

    /// <summary>
    /// 新增：公共刷新方法（供新增公牌脚本调用）
    /// </summary>
    public void RefreshPeekButtonState()
    {
        _isPeeking = false;
        DestroyPeekOverlays();
        UpdateButtonState();
        Debug.Log("新增公牌后，刷新偷看按钮状态");
    }

    private void TogglePeek()
    {
        // Toggle Logic
        bool targetState = !_isPeeking;

        if (targetState) // Want to start peeking
        {
            // Cost Check
            if (betManager != null)
            {
                if (!betManager.TrySpendChips(betManager.costPeekCard))
                {
                    Debug.LogWarning($"筹码不足！无法偷看。需要: {betManager.costPeekCard}");
                    return;
                }
            }
        }

        _isPeeking = targetState;

        if (_isPeeking)
        {
            CreatePeekOverlays();
        }
        else
        {
            DestroyPeekOverlays();
        }
    }

    /// <summary>
    /// 优化：适配无已翻开牌时的偷看逻辑
    /// </summary>
    private void CreatePeekOverlays()
    {
        List<GameObject> publicCards = cardDeckSystem.PublicCardObjects;
        if (publicCards == null || publicCards.Count == 0) return;

        int lastRevealedIndex = GetLastRevealedCardIndex();
        // 无已翻开牌时，从第0张未翻开牌开始偷看
        int peekStartIndex = lastRevealedIndex == -1 ? 0 : lastRevealedIndex + 1;
        int availableCount = publicCards.Count - peekStartIndex;
        int peekCount = availableCount >= 2 ? 2 : (availableCount >= 1 ? 1 : 0);

        if (peekCount == 0)
        {
            Debug.LogWarning("没有可偷看的牌！");
            _isPeeking = false;
            UpdateButtonState();
            return;
        }

        for (int i = peekStartIndex; i < peekStartIndex + peekCount; i++)
        {
            if (i >= publicCards.Count) break;

            GameObject originCard = publicCards[i];
            CardFaceController originCtrl = originCard.GetComponent<CardFaceController>();
            if (originCtrl == null || originCtrl.GetCardText() == "未知卡牌") continue;

            GameObject overlay = Instantiate(peekCardOverlayPrefab, originCard.transform.parent);
            Vector3 newPos = originCard.transform.position;
            newPos.x += peekOffset.x;
            newPos.y += peekOffset.y;
            overlay.transform.position = newPos;

            overlay.transform.rotation = originCard.transform.rotation;
            overlay.transform.localScale = originCard.transform.localScale;

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
                textColor.a = peekAlpha + 0.1f;
                overlayText.color = textColor;
                overlayText.gameObject.SetActive(true);
            }

            overlay.SetActive(true);
            _overlayList.Add(overlay);
        }
    }

    private void DestroyPeekOverlays()
    {
        foreach (var overlay in _overlayList)
        {
            if (overlay != null) Destroy(overlay);
        }
        _overlayList.Clear();
    }

    private void OnDestroy()
    {
        DestroyPeekOverlays();
    }
}