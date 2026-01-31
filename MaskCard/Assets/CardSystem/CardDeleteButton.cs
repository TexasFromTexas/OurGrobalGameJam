using UnityEngine;
using UnityEngine.UI;
using BetSystem;

/// <summary>
/// 手牌+公牌删除脚本（适配小写dissolve+无延迟+重置Dissolve）
/// </summary>
public class CardDeleteButton : MonoBehaviour
{
    [Header("核心引用")]
    public CardDeckSystem cardDeckSystem;
    public Button deleteCardBtn;
    public BetManager betManager;
    public MouseAction mouseAction;

    [Header("Dissolve渐变配置")]
    public float dissolveDuration = 1.2f; // 渐变时长

    private bool _isDeleteMode = false;
    private GameObject _selectedHandCard = null;
    private GameObject _selectedPublicCard = null;

    private void Start()
    {
        if (cardDeckSystem == null)
        {
            Debug.LogError("请拖入CardDeckSystem！");
            return;
        }
        if (betManager == null) betManager = FindFirstObjectByType<BetManager>();
        mouseAction = FindFirstObjectByType<MouseAction>();

        if (deleteCardBtn != null)
        {
           // deleteCardBtn.onClick.AddListener(ToggleDeleteMode);
            deleteCardBtn.onClick.AddListener(() => StartCoroutine(mouseAction.BeginUseMask(deleteCardBtn.gameObject, () => ToggleDeleteMode())));
            UpdateButtonInteractable();
        }
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
            deleteCardBtn.GetComponentInChildren<Text>().text = "删除卡牌";
        }
        else
        {
            EnterDeleteMode();
            deleteCardBtn.GetComponentInChildren<Text>().text = "取消删除";
        }
    }

    private void UpdateButtonInteractable()
    {
        bool basicCondition = cardDeckSystem.IsInRound
                                 && cardDeckSystem.PlayerCardCount > 0
                                 && cardDeckSystem.PublicCardObjects.Count > 0;

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
        // 退出模式时重置选中卡牌的Dissolve
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
        if (!_isDeleteMode || handCard == null) return;
        if (cardDeckSystem.playerCardObjects.Contains(handCard))
        {
            _selectedHandCard = handCard;
            Debug.Log($"选中待删除手牌：{handCard.GetComponent<CardDisplay>().cardData.cardName}");
            if (_selectedPublicCard != null) DeleteSelectedCards();
        }
    }

    public void SelectPublicCardToDelete(GameObject publicCard)
    {
        if (!_isDeleteMode || publicCard == null) return;
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

            _selectedPublicCard = publicCard;
            Debug.Log($"选中待删除公牌：{publicCard.GetComponent<CardDisplay>().cardData.cardName}");
            if (_selectedHandCard != null) DeleteSelectedCards();
        }
    }
    #endregion

    #region 核心删除逻辑
    private void DeleteSelectedCards()
    {
        if (_selectedHandCard == null || _selectedPublicCard == null || cardDeckSystem == null) return;

        // 筹码检查
        if (betManager != null)
        {
            if (!betManager.TrySpendChips(betManager.costDeleteCard))
            {
                Debug.LogWarning($"筹码不足！需要{betManager.costDeleteCard}，拥有{betManager.playerChips}");
                ExitDeleteMode();
                deleteCardBtn.GetComponentInChildren<Text>().text = "删除卡牌";
                return;
            }
        }

        // 处理手牌
        CardDisplay handCardDisplay = _selectedHandCard.GetComponent<CardDisplay>();
        if (handCardDisplay != null && handCardDisplay.cardData != null && handCardDisplay.cardData.rank == CardRank.Joker)
        {
            cardDeckSystem.ReturnJokerToDeck(_selectedHandCard, cardDeckSystem.playerCardObjects);
            // 鬼牌洗回时重置Dissolve
            CardDeleteClick handDel = _selectedHandCard.GetComponent<CardDeleteClick>();
            if (handDel != null) handDel.ResetDissolveToDefault();
            Debug.Log("手牌是鬼牌，已洗回牌堆并重置Dissolve");
        }
        else
        {
            if (cardDeckSystem.playerCardObjects.Contains(_selectedHandCard))
            {
                TriggerCardDissolveDelete(_selectedHandCard);
                cardDeckSystem.playerCardObjects.Remove(_selectedHandCard);
                Debug.Log($"触发手牌Dissolve删除：{handCardDisplay.cardData.cardName}");
            }
        }

        // 处理公牌
        CardDisplay publicCardDisplay = _selectedPublicCard.GetComponent<CardDisplay>();
        if (publicCardDisplay != null && publicCardDisplay.cardData != null && publicCardDisplay.cardData.rank == CardRank.Joker)
        {
            cardDeckSystem.ReturnJokerToDeck(_selectedPublicCard, cardDeckSystem.PublicCardObjects);
            // 鬼牌洗回时重置Dissolve
            CardDeleteClick pubDel = _selectedPublicCard.GetComponent<CardDeleteClick>();
            if (pubDel != null) pubDel.ResetDissolveToDefault();
            Debug.Log("公牌是鬼牌，已洗回牌堆并重置Dissolve");
        }
        else
        {
            if (cardDeckSystem.PublicCardObjects.Contains(_selectedPublicCard))
            {
                TriggerCardDissolveDelete(_selectedPublicCard);
                cardDeckSystem.PublicCardObjects.Remove(_selectedPublicCard);
                Debug.Log($"触发公牌Dissolve删除：{publicCardDisplay.cardData.cardName}");
            }
        }

        ExitDeleteMode();
        if (deleteCardBtn != null) deleteCardBtn.GetComponentInChildren<Text>().text = "删除卡牌";
        Debug.Log($"删除完成！剩余手牌：{cardDeckSystem.PlayerCardCount}，剩余公牌：{cardDeckSystem.PublicCardObjects.Count}");
    }
    #endregion

    #region 触发Dissolve删除
    private void TriggerCardDissolveDelete(GameObject targetCard)
    {
        if (targetCard == null) return;

        CardDeleteClick deleteClick = targetCard.GetComponent<CardDeleteClick>();
        if (deleteClick != null)
        {
            // 先重置再触发，避免数值异常
            deleteClick.ResetDissolveToDefault();
            deleteClick.TriggerDissolveDelete(dissolveDuration);
        }
        else
        {
            Debug.LogWarning($"卡牌{targetCard.name}缺少CardDeleteClick，直接删除");
            Destroy(targetCard);
        }
    }
    #endregion

    #region 辅助方法
    public bool IsDeleteMode => _isDeleteMode;
    public void ClearSelectedCards()
    {
        // 清空选中时重置Dissolve
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