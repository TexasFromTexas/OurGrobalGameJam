using UnityEngine;
using UnityEngine.UI;
using BetSystem;

/// <summary>
/// 手牌+公牌删除脚本（空引用修复版）
/// </summary>
public class CardDeleteButton : MonoBehaviour
{
    [Header("核心引用")]
    public CardDeckSystem cardDeckSystem;
    public Button deleteCardBtn;
    public BetManager betManager;
    public MouseAction mouseAction;

    [Header("Dissolve渐变配置")]
    public float dissolveDuration = 1.2f;

    private bool _isDeleteMode = false;
    private GameObject _selectedHandCard = null;
    private GameObject _selectedPublicCard = null;

    private void Start()
    {
        // 核心引用容错
        if (cardDeckSystem == null)
        {
            Debug.LogError("请拖入CardDeckSystem！");
            enabled = false;
            return;
        }
        if (deleteCardBtn == null)
        {
            Debug.LogError("请拖入deleteCardBtn！");
            enabled = false;
            return;
        }

        // 可选引用容错
        if (betManager == null)
        {
            betManager = FindFirstObjectByType<BetManager>();
            if (betManager == null) Debug.LogWarning("未找到BetManager，删除不扣筹码！");
        }

        if (mouseAction == null)
        {
            mouseAction = FindFirstObjectByType<MouseAction>();
            if (mouseAction == null) Debug.LogWarning("未找到MouseAction，使用直接点击绑定！");
        }

        // 按钮点击绑定（加判空）
        if (mouseAction != null)
        {
            deleteCardBtn.onClick.AddListener(() => StartCoroutine(mouseAction.BeginUseMask(deleteCardBtn.gameObject, () => ToggleDeleteMode())));
        }
        else
        {
            deleteCardBtn.onClick.AddListener(ToggleDeleteMode);
        }

        UpdateButtonInteractable();
      
    }

    private void Update()
    {
        if (deleteCardBtn != null)
        {
            UpdateButtonInteractable();
        }
    }

   

    private void ToggleDeleteMode()
    {
        if (_isDeleteMode)
        {
            ExitDeleteMode();
            
        }
        else
        {
            EnterDeleteMode();
           
        }
    }

    private void UpdateButtonInteractable()
    {
        bool hasPlayerCards = cardDeckSystem.playerCardObjects != null && cardDeckSystem.PlayerCardCount > 0;
        bool hasPublicCards = cardDeckSystem.PublicCardObjects != null && cardDeckSystem.PublicCardObjects.Count > 0;
        bool basicCondition = cardDeckSystem.IsInRound && hasPlayerCards && hasPublicCards;

        bool costCondition = true;
        if (betManager != null)
        {
            costCondition = betManager.playerChips >= betManager.costDeleteCard;
        }

        deleteCardBtn.interactable = basicCondition && costCondition;
    }

    #region 对外接口
    public void EnterDeleteMode()
    {
        if (_isDeleteMode) return;
        _isDeleteMode = true;
        _selectedHandCard = null;
        _selectedPublicCard = null;

        int cost = betManager != null ? betManager.costDeleteCard : 0;
        Debug.Log($"进入删除模式：需要消耗 {cost} 筹码");
    }

    public void ExitDeleteMode()
    {
        _isDeleteMode = false;

        if (_selectedHandCard != null)
        {
            CardDeleteClick handDel = _selectedHandCard.GetComponent<CardDeleteClick>();
            if (handDel != null) handDel.ResetDissolveToDefault();
        }

        if (_selectedPublicCard != null)
        {
            CardDeleteClick pubDel = _selectedPublicCard.GetComponent<CardDeleteClick>();
            if (pubDel != null) pubDel.ResetDissolveToDefault();
        }

        _selectedHandCard = null;
        _selectedPublicCard = null;
        Debug.Log("退出删除模式，已重置Dissolve");
    }

    public void SelectHandCardToDelete(GameObject handCard)
    {
        if (!_isDeleteMode || handCard == null || cardDeckSystem.playerCardObjects == null) return;

        if (cardDeckSystem.playerCardObjects.Contains(handCard))
        {
            _selectedHandCard = handCard;
            CardDisplay cardDisplay = handCard.GetComponent<CardDisplay>();
            string cardName = cardDisplay?.cardData?.cardName ?? "未知卡牌";
            Debug.Log($"选中待删除手牌：{cardName}");

            if (_selectedPublicCard != null) DeleteSelectedCards();
        }
    }

    public void SelectPublicCardToDelete(GameObject publicCard)
    {
        if (!_isDeleteMode || publicCard == null || cardDeckSystem.PublicCardObjects == null) return;

        if (cardDeckSystem.PublicCardObjects.Contains(publicCard))
        {
            CardFaceController cardFace = publicCard.GetComponent<CardFaceController>();
            if (cardFace == null)
            {
                Debug.LogWarning($"公牌{publicCard.name}缺少CardFaceController！");
                return;
            }

            if (cardFace._isShowingBack)
            {
                Debug.LogWarning($"公牌{publicCard.name}未翻开，无法选中！");
                return;
            }

            CardDisplay cardDisplay = publicCard.GetComponent<CardDisplay>();
            string cardName = cardDisplay?.cardData?.cardName ?? "未知卡牌";
            Debug.Log($"选中待删除公牌：{cardName}");

            _selectedPublicCard = publicCard;
            if (_selectedHandCard != null) DeleteSelectedCards();
        }
    }
    #endregion

    #region 核心删除逻辑
    private void DeleteSelectedCards()
    {
        if (_selectedHandCard == null || _selectedPublicCard == null) return;

        if (betManager != null && !betManager.TrySpendChips(betManager.costDeleteCard))
        {
            Debug.LogWarning($"筹码不足！需要{betManager.costDeleteCard}，拥有{betManager.playerChips}");
            ExitDeleteMode();
           
            return;
        }

        // 处理手牌
        CardDisplay handCardDisplay = _selectedHandCard.GetComponent<CardDisplay>();
        if (handCardDisplay != null && handCardDisplay.cardData != null && handCardDisplay.cardData.rank == CardRank.Joker)
        {
            if (cardDeckSystem.playerCardObjects != null)
            {
                cardDeckSystem.ReturnJokerToDeck(_selectedHandCard, cardDeckSystem.playerCardObjects);
                CardDeleteClick handDel = _selectedHandCard.GetComponent<CardDeleteClick>();
                if (handDel != null) handDel.ResetDissolveToDefault();
                Debug.Log("手牌是鬼牌，已洗回牌堆并重置Dissolve");
            }
        }
        else
        {
            if (cardDeckSystem.playerCardObjects != null && cardDeckSystem.playerCardObjects.Contains(_selectedHandCard))
            {
                TriggerCardDissolveDelete(_selectedHandCard);
                cardDeckSystem.playerCardObjects.Remove(_selectedHandCard);
                string cardName = handCardDisplay?.cardData?.cardName ?? "未知卡牌";
                Debug.Log($"触发手牌Dissolve删除：{cardName}");
            }
        }

        // 处理公牌
        CardDisplay publicCardDisplay = _selectedPublicCard.GetComponent<CardDisplay>();
        if (publicCardDisplay != null && publicCardDisplay.cardData != null && publicCardDisplay.cardData.rank == CardRank.Joker)
        {
            if (cardDeckSystem.PublicCardObjects != null)
            {
                cardDeckSystem.ReturnJokerToDeck(_selectedPublicCard, cardDeckSystem.PublicCardObjects);
                CardDeleteClick pubDel = _selectedPublicCard.GetComponent<CardDeleteClick>();
                if (pubDel != null) pubDel.ResetDissolveToDefault();
                Debug.Log("公牌是鬼牌，已洗回牌堆并重置Dissolve");
            }
        }
        else
        {
            if (cardDeckSystem.PublicCardObjects != null && cardDeckSystem.PublicCardObjects.Contains(_selectedPublicCard))
            {
                TriggerCardDissolveDelete(_selectedPublicCard);
                cardDeckSystem.PublicCardObjects.Remove(_selectedPublicCard);
                string cardName = publicCardDisplay?.cardData?.cardName ?? "未知卡牌";
                Debug.Log($"触发公牌Dissolve删除：{cardName}");
            }
        }

        ExitDeleteMode();
       

        int handCount = cardDeckSystem.playerCardObjects != null ? cardDeckSystem.PlayerCardCount : 0;
        int publicCount = cardDeckSystem.PublicCardObjects != null ? cardDeckSystem.PublicCardObjects.Count : 0;
        Debug.Log($"删除完成！剩余手牌：{handCount}，剩余公牌：{publicCount}");
    }
    #endregion

    #region 触发Dissolve删除
    private void TriggerCardDissolveDelete(GameObject targetCard)
    {
        if (targetCard == null) return;

        CardDeleteClick deleteClick = targetCard.GetComponent<CardDeleteClick>();
        if (deleteClick != null)
        {
            deleteClick.ResetDissolveToDefault();
            deleteClick.TriggerDissolveDelete(dissolveDuration);
        }
        else
        {
            Debug.LogWarning($"卡牌{targetCard.name}缺少CardDeleteClick，直接销毁！");
            Destroy(targetCard);
        }
    }
    #endregion

    #region 辅助方法
    public bool IsDeleteMode => _isDeleteMode;

    public void ClearSelectedCards()
    {
        if (_selectedHandCard != null)
        {
            CardDeleteClick handDel = _selectedHandCard.GetComponent<CardDeleteClick>();
            if (handDel != null) handDel.ResetDissolveToDefault();
        }

        if (_selectedPublicCard != null)
        {
            CardDeleteClick pubDel = _selectedPublicCard.GetComponent<CardDeleteClick>();
            if (pubDel != null) pubDel.ResetDissolveToDefault();
        }

        _selectedHandCard = null;
        _selectedPublicCard = null;
    }
    #endregion
}