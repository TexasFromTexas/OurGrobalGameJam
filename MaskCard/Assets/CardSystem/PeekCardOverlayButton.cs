using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using BetSystem;

/// <summary>
/// 覆盖层偷看公牌脚本（克隆体为原牌子物体 + 随机偷看 + 标记不重复）
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
    public Vector2 peekOffset = new Vector2(0, 10); // 相对于原牌的像素偏移（显示在牌上方）
    public float dissolveDuration = 1.2f; // 背面Dissolve渐变时长（和删除卡牌一致）
    public int maxPeekCount = 2; // 每次最多偷看的牌数
    private const string DISSOLVE_PROPERTY_NAME = "_Dissolve"; // 匹配Shader的_Dissolve属性

    private List<GameObject> _overlayList = new List<GameObject>();
    private List<Material> _backMaterialInstances = new List<Material>(); // 保存克隆体背面材质实例
    private List<GameObject> _peekedCards = new List<GameObject>(); // 记录偷看过的牌（标记）

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

        // 加mouseAction判空，避免空引用
        if (mouseAction != null)
        {
            peekBtn.onClick.AddListener(() => StartCoroutine(mouseAction.BeginUseMask(peekBtn.gameObject, () => PeekRandomCards())));
        }
        else
        {
            peekBtn.onClick.AddListener(PeekRandomCards);
            Debug.LogWarning("未找到MouseAction，直接绑定偷看逻辑！");
        }
        UpdateButtonState();
    }

    private void Update()
    {
        UpdateButtonState();
    }

    /// <summary>
    /// 仅当有未偷看过的牌且筹码足够时可点击
    /// </summary>
    private void UpdateButtonState()
    {
        if (cardDeckSystem == null || peekBtn == null) return;

        bool isInRound = cardDeckSystem.IsInRound;
        bool hasPeekableCards = HasUnpeekedUnrevealedCards(); // 有未偷看过且未翻开的牌
        bool costCondition = betManager != null && betManager.playerChips >= betManager.costPeekCard;

        peekBtn.interactable = isInRound && hasPeekableCards && costCondition;

        // 按钮文本固定为"露出狡诈的样子"（加判空）
        Text btnText = peekBtn.GetComponentInChildren<Text>();
        if (btnText != null)
        {
            btnText.text = "露出狡诈的样子";
        }
    }

    /// <summary>
    /// 判断是否有「未偷看过且未翻开」的公牌
    /// </summary>
    private bool HasUnpeekedUnrevealedCards()
    {
        List<GameObject> publicCards = cardDeckSystem.PublicCardObjects;
        if (publicCards == null || publicCards.Count == 0) return false;

        var availableCards = publicCards.Where(card =>
            card != null &&
            !_peekedCards.Contains(card) &&
            card.GetComponent<CardFaceController>()?._isShowingBack == true
        ).ToList();

        return availableCards.Count > 0;
    }

    /// <summary>
    /// 核心逻辑：随机选择未偷看过的牌进行偷看
    /// </summary>
    private void PeekRandomCards()
    {
        // 扣筹码检查
        if (betManager != null && !betManager.TrySpendChips(betManager.costPeekCard))
        {
            Debug.LogWarning($"筹码不足！无法偷看。需要: {betManager.costPeekCard}");
            return;
        }

        CreateRandomPeekOverlays();
    }

    /// <summary>
    /// 核心修改：克隆体作为原牌的子物体生成
    /// </summary>
    private void CreateRandomPeekOverlays()
    {
        List<GameObject> publicCards = cardDeckSystem.PublicCardObjects;
        if (publicCards == null || publicCards.Count == 0) return;

        var availableCards = publicCards.Where(card =>
            card != null &&
            !_peekedCards.Contains(card) &&
            card.GetComponent<CardFaceController>()?._isShowingBack == true
        ).ToList();

        if (availableCards.Count == 0)
        {
            Debug.LogWarning("没有可偷看的新牌！");
            UpdateButtonState();
            return;
        }

        int peekCount = Mathf.Min(maxPeekCount, availableCards.Count);
        var randomCards = availableCards.OrderBy(_ => Random.value).Take(peekCount).ToList();

        foreach (var originCard in randomCards)
        {
            _peekedCards.Add(originCard);

            CardFaceController originCtrl = originCard.GetComponent<CardFaceController>();
            if (originCtrl == null || originCtrl.GetCardText() == "未知卡牌") continue;

            RectTransform originCardRect = originCard.GetComponent<RectTransform>();
            if (originCardRect == null) continue;

            // ========== 核心修改1：克隆体父物体改为原牌的Transform ==========
            GameObject overlay = Instantiate(peekCardOverlayPrefab, originCard.transform);
            RectTransform overlayRect = overlay.GetComponent<RectTransform>();
            if (overlayRect == null)
            {
                Destroy(overlay);
                continue;
            }

            // ========== 核心修改2：子物体RectTransform适配 ==========
            // 1. 匹配原牌的尺寸（和原牌一样大）
            overlayRect.sizeDelta = originCardRect.sizeDelta;
            // 2. 锚点设为中心（相对于原牌居中）
            overlayRect.anchorMin = new Vector2(0.5f, 0.5f);
            overlayRect.anchorMax = new Vector2(0.5f, 0.5f);
            overlayRect.pivot = new Vector2(0.5f, 0.5f);
            // 3. 偏移：基于原牌中心向上偏移peekOffset（显示在牌上方）
            overlayRect.anchoredPosition = peekOffset;
            // 4. 匹配旋转和缩放（和原牌一致）
            overlayRect.rotation = originCardRect.rotation;
            overlayRect.localScale = originCardRect.localScale;

            // ========== 原有逻辑：初始化背面/正面状态 ==========
            Transform overlayCardBack = overlay.transform.Find("CardBack");
            Transform overlayCardFront = overlay.transform.Find("CardFront");
            if (overlayCardBack == null || overlayCardFront == null)
            {
                Debug.LogError("克隆体缺少CardBack/CardFront子物体！");
                Destroy(overlay);
                continue;
            }

            overlayCardBack.gameObject.SetActive(true);
            overlayCardFront.gameObject.SetActive(false);

            Image overlayBackImage = overlayCardBack.GetComponent<Image>();
            if (overlayBackImage == null || overlayBackImage.material == null)
            {
                Debug.LogWarning("克隆体CardBack缺少Image或材质！");
                Destroy(overlay);
                continue;
            }

            Material backInstance = new Material(overlayBackImage.material);
            backInstance.SetFloat(DISSOLVE_PROPERTY_NAME, -1f);
            overlayBackImage.material = backInstance;
            _backMaterialInstances.Add(backInstance);

            // 同步克隆体正面内容
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

            // 启动背面Dissolve渐变
            StartCoroutine(AnimateBackDissolve(overlayBackImage, backInstance, overlayCardFront.gameObject));

            overlay.SetActive(true);
            _overlayList.Add(overlay);
        }

        Debug.Log($"随机偷看了{randomCards.Count}张牌，克隆体已作为原牌子物体生成");
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

            backMaterial.SetFloat(DISSOLVE_PROPERTY_NAME, currentValue);
            backImage.material = backMaterial;

            yield return null;
        }

        backMaterial.SetFloat(DISSOLVE_PROPERTY_NAME, targetValue);
        backImage.material = backMaterial;
        frontObject.SetActive(true);
    }

    /// <summary>
    /// 重置偷看标记（供新增公牌/新回合时调用）
    /// </summary>
    public void ResetPeekState()
    {
        _peekedCards.Clear();
        DestroyPeekOverlays();
        UpdateButtonState();
        Debug.Log("重置偷看状态：已清除偷看标记，按钮可重新点击");
    }

    /// <summary>
    /// 新增公牌后刷新状态（兼容原有调用）
    /// </summary>
    public void RefreshPeekButtonState()
    {
        ResetPeekState();
    }

    /// <summary>
    /// 销毁克隆体和材质
    /// </summary>
    private void DestroyPeekOverlays()
    {
        foreach (var mat in _backMaterialInstances)
        {
            if (mat != null) Destroy(mat);
        }
        _backMaterialInstances.Clear();

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