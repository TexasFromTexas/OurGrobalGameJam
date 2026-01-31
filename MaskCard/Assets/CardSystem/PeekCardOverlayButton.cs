using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;
using BetSystem;

/// <summary>
/// 覆盖层偷看公牌脚本（全新逻辑：克隆体背面Dissolve渐变消失，露出正面）
/// </summary>
public class PeekCardOverlayButton : MonoBehaviour
{
    [Header("核心引用")]
    public CardDeckSystem cardDeckSystem;
    public Button peekBtn;
    public GameObject peekCardOverlayPrefab; // 需和原卡牌结构一致（包含CardBack/CardFront）

    public BetManager betManager;
    public MouseAction mouseAction;

    [Header("视觉效果参数（可调整）")]
    public Vector2 peekOffset = new Vector2(0, 10); // UI像素偏移（显示在牌上方）
    public float dissolveDuration = 1.2f; // 背面Dissolve渐变时长（和删除卡牌一致）
    private const string DISSOLVE_PROPERTY_NAME = "_Dissolve"; // 匹配Shader的_Dissolve属性

    private List<GameObject> _overlayList = new List<GameObject>();
    private List<Material> _backMaterialInstances = new List<Material>(); // 保存克隆体背面材质实例
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
        mouseAction = FindFirstObjectByType<MouseAction>();

        peekBtn.onClick.AddListener(() => StartCoroutine(mouseAction.BeginUseMask(peekBtn.gameObject, () => TogglePeek())));
        UpdateButtonState();
    }

    private void Update()
    {
        UpdateButtonState();
    }

    private void UpdateButtonState()
    {
        if (cardDeckSystem == null || peekBtn == null) return;

        bool isInRound = cardDeckSystem.IsInRound;
        bool hasPeekableCards = HasPeekableCards();
        bool costCondition = true;

        if (betManager != null && !_isPeeking)
        {
            costCondition = betManager.playerChips >= betManager.costPeekCard;
        }

        peekBtn.interactable = isInRound && hasPeekableCards && costCondition;

<<<<<<< Updated upstream
=======
        // 文本切换（保留你的逻辑）
        // 文本切换（已移除，无需检测Text组件）
        /*
>>>>>>> Stashed changes
        Text btnText = peekBtn.GetComponentInChildren<Text>();
        if (btnText != null)
        {
            btnText.text = _isPeeking ? "摘下面具" : "露出狡诈的样子";
        }
    }

    private bool HasPeekableCards()
    {
        List<GameObject> publicCards = cardDeckSystem.PublicCardObjects;
        if (publicCards == null || publicCards.Count == 0) return false;

        int lastRevealedIndex = GetLastRevealedCardIndex();
        bool hasUnrevealedAfter = lastRevealedIndex < publicCards.Count - 1;
        bool hasAnyUnrevealed = HasAnyUnrevealedCards();

        return hasUnrevealedAfter || (lastRevealedIndex == -1 && hasAnyUnrevealed);
    }

    private bool HasAnyUnrevealedCards()
    {
        List<GameObject> publicCards = cardDeckSystem.PublicCardObjects;
        if (publicCards == null || publicCards.Count == 0) return false;

        foreach (var card in publicCards)
        {
            if (card == null) continue;
            CardFaceController ctrl = card.GetComponent<CardFaceController>();
            if (ctrl != null && ctrl._isShowingBack)
            {
                return true;
            }
        }
        return false;
    }

    private int GetLastRevealedCardIndex()
    {
        List<GameObject> publicCards = cardDeckSystem.PublicCardObjects;
        if (publicCards == null || publicCards.Count == 0) return -1;

        int lastRevealedIndex = -1;
        for (int i = 0; i < publicCards.Count; i++)
        {
            GameObject card = publicCards[i];
            if (card == null) continue;

            CardFaceController ctrl = card.GetComponent<CardFaceController>();
            if (ctrl == null) continue;

            bool isRevealed = !ctrl._isShowingBack;
            if (isRevealed)
            {
                lastRevealedIndex = i;
            }
        }
        return lastRevealedIndex;
    }

    public void RefreshPeekButtonState()
    {
        _isPeeking = false;
        DestroyPeekOverlays();
        UpdateButtonState();
        Debug.Log("新增公牌后，刷新偷看按钮状态");
    }

    private void TogglePeek()
    {
        bool targetState = !_isPeeking;

        if (targetState)
        {
            if (betManager != null && !betManager.TrySpendChips(betManager.costPeekCard))
            {
                Debug.LogWarning($"筹码不足！无法偷看。需要: {betManager.costPeekCard}");
                return;
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
    /// 核心逻辑：生成克隆体 → 初始化背面状态 → 启动Dissolve渐变
    /// </summary>
    private void CreatePeekOverlays()
    {
        List<GameObject> publicCards = cardDeckSystem.PublicCardObjects;
        if (publicCards == null || publicCards.Count == 0) return;

        int lastRevealedIndex = GetLastRevealedCardIndex();
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

            // ========== 1. 克隆体位置与尺寸匹配（保留原有精准逻辑） ==========
            RectTransform originCardRect = originCard.GetComponent<RectTransform>();
            if (originCardRect == null) continue;

            GameObject overlay = Instantiate(peekCardOverlayPrefab, originCardRect.parent);
            RectTransform overlayRect = overlay.GetComponent<RectTransform>();
            if (overlayRect == null)
            {
                Destroy(overlay);
                continue;
            }

            overlayRect.sizeDelta = originCardRect.sizeDelta;
            overlayRect.anchorMin = originCardRect.anchorMin;
            overlayRect.anchorMax = originCardRect.anchorMax;
            overlayRect.pivot = originCardRect.pivot;
            overlayRect.anchoredPosition = new Vector2(
                originCardRect.anchoredPosition.x + peekOffset.x,
                originCardRect.anchoredPosition.y + peekOffset.y
            );
            overlayRect.rotation = originCardRect.rotation;
            overlayRect.localScale = originCardRect.localScale;

            // ========== 2. 初始化克隆体状态：显示背面，隐藏正面 ==========
            Transform overlayCardBack = overlay.transform.Find("CardBack");
            Transform overlayCardFront = overlay.transform.Find("CardFront");
            if (overlayCardBack == null || overlayCardFront == null)
            {
                Debug.LogError("克隆体缺少CardBack/CardFront子物体！");
                Destroy(overlay);
                continue;
            }

            // 初始状态：显示背面，隐藏正面
            overlayCardBack.gameObject.SetActive(true);
            overlayCardFront.gameObject.SetActive(false);

            // ========== 3. 克隆体背面材质实例化（避免共享影响其他卡牌） ==========
            Image overlayBackImage = overlayCardBack.GetComponent<Image>();
            if (overlayBackImage == null || overlayBackImage.material == null)
            {
                Debug.LogWarning("克隆体CardBack缺少Image或材质！");
                Destroy(overlay);
                continue;
            }

            // 实例化背面材质（仅当前克隆体使用）
            Material backInstance = new Material(overlayBackImage.material);
            backInstance.SetFloat(DISSOLVE_PROPERTY_NAME, -1f); // 初始Dissolve为-1（完全不透明）
            overlayBackImage.material = backInstance;
            _backMaterialInstances.Add(backInstance);

            // ========== 4. 同步克隆体正面内容与原卡牌一致 ==========
            Image overlayFrontImage = overlayCardFront.GetComponent<Image>();
            Text overlayFrontText = overlayCardFront.GetComponentInChildren<Text>(true);
            if (overlayFrontImage != null)
            {
                overlayFrontImage.sprite = originCtrl.GetCardFrontSprite();
            }
            if (overlayFrontText != null)
            {
                overlayFrontText.text = originCtrl.GetCardText();
            }

            // ========== 5. 启动背面Dissolve渐变协程 ==========
            StartCoroutine(AnimateBackDissolve(overlayBackImage, backInstance, overlayCardFront.gameObject));

            overlay.SetActive(true);
            _overlayList.Add(overlay);
        }
    }

    /// <summary>
    /// 协程：背面材质Dissolve从-1渐变到1，完成后显示正面
    /// </summary>
    private IEnumerator AnimateBackDissolve(Image backImage, Material backMaterial, GameObject frontObject)
    {
        float elapsedTime = 0f;
        float startValue = -1f;
        float targetValue = 1f;

        while (elapsedTime < dissolveDuration)
        {
            elapsedTime = Mathf.Min(elapsedTime + Time.deltaTime, dissolveDuration);
            float currentValue = Mathf.Lerp(startValue, targetValue, elapsedTime / dissolveDuration);

            // 更新背面材质的Dissolve值
            backMaterial.SetFloat(DISSOLVE_PROPERTY_NAME, currentValue);
            backImage.material = backMaterial;

            yield return null;
        }

        // 渐变完成：确保Dissolve为1，显示正面
        backMaterial.SetFloat(DISSOLVE_PROPERTY_NAME, targetValue);
        backImage.material = backMaterial;
        frontObject.SetActive(true);
    }

    private void DestroyPeekOverlays()
    {
        // 销毁材质实例（避免内存泄漏）
        foreach (var mat in _backMaterialInstances)
        {
            if (mat != null) Destroy(mat);
        }
        _backMaterialInstances.Clear();

        // 销毁克隆体
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