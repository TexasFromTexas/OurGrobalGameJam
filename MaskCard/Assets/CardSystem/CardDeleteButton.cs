using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 手牌+公牌删除脚本（带按钮配置+仅删除已翻开公牌）
/// 挂载到CardEffectManager → 拖入CardDeckSystem和删除按钮即可
/// </summary>
public class CardDeleteButton : MonoBehaviour
{
    [Header("核心引用")]
    public CardDeckSystem cardDeckSystem; // 拖入CardDeckManager
    public Button deleteCardBtn; // 拖入你的删除按钮

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
        if (deleteCardBtn != null)
        {
            deleteCardBtn.onClick.AddListener(ToggleDeleteMode);
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
        deleteCardBtn.interactable = cardDeckSystem.IsInRound
                                 && cardDeckSystem.PlayerCardCount > 0
                                 && cardDeckSystem.PublicCardObjects.Count > 0;
    }

    #region 对外接口
    public void EnterDeleteMode()
    {
        if (_isDeleteMode) return;
        _isDeleteMode = true;
        _selectedHandCard = null;
        _selectedPublicCard = null;
        Debug.Log("进入删除模式：先选手牌，再选【已翻开】的公牌"); // 提示更新
    }

    public void ExitDeleteMode()
    {
        _isDeleteMode = false;
        _selectedHandCard = null;
        _selectedPublicCard = null;
        Debug.Log("退出删除模式");
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
            // ========== 核心修改：判断公牌是否已翻开 ==========
            CardFaceController cardFace = publicCard.GetComponent<CardFaceController>();
            if (cardFace == null)
            {
                Debug.LogWarning($"公牌{publicCard.name}缺少CardFaceController组件，无法判断是否翻开！");
                return;
            }
            // 仅允许选中已翻开的公牌（_isShowingBack=false → 正面/已翻开）
            if (cardFace._isShowingBack)
            {
                Debug.LogWarning($"无法选中：公牌{publicCard.name}未翻开（背面），仅可删除已翻开的公牌！");
                return;
            }

            // 只有已翻开的公牌才会被选中
            _selectedPublicCard = publicCard;
            Debug.Log($"选中待删除公牌（已翻开）：{publicCard.GetComponent<CardDisplay>().cardData.cardName}");
            if (_selectedHandCard != null) DeleteSelectedCards();
        }
    }
    #endregion

    #region 核心删除逻辑
    private void DeleteSelectedCards()
    {
        if (_selectedHandCard == null || _selectedPublicCard == null || cardDeckSystem == null) return;

        // ========== 处理手牌：先判断是否为鬼牌 ==========
        CardDisplay handCardDisplay = _selectedHandCard.GetComponent<CardDisplay>();
        if (handCardDisplay != null && handCardDisplay.cardData != null && handCardDisplay.cardData.rank == CardRank.Joker)
        {
            // 手牌是鬼牌：洗回牌堆，不删除
            cardDeckSystem.ReturnJokerToDeck(_selectedHandCard, cardDeckSystem.playerCardObjects);
            Debug.Log("选中的手牌是鬼牌，已洗回牌堆");
        }
        else
        {
            // 非鬼牌：正常删除
            if (cardDeckSystem.playerCardObjects.Contains(_selectedHandCard))
            {
                cardDeckSystem.RemovePlayerCard(_selectedHandCard);
                Debug.Log($"删除手牌：{_selectedHandCard.GetComponent<CardDisplay>().cardData.cardName}");
            }
        }

        // ========== 处理公牌：先判断是否为鬼牌 ==========
        CardDisplay publicCardDisplay = _selectedPublicCard.GetComponent<CardDisplay>();
        if (publicCardDisplay != null && publicCardDisplay.cardData != null && publicCardDisplay.cardData.rank == CardRank.Joker)
        {
            // 公牌是鬼牌：洗回牌堆，不删除
            cardDeckSystem.ReturnJokerToDeck(_selectedPublicCard, cardDeckSystem.PublicCardObjects);
            Debug.Log("选中的公牌是鬼牌，已洗回牌堆");
        }
        else
        {
            // 非鬼牌：正常删除
            if (cardDeckSystem.PublicCardObjects.Contains(_selectedPublicCard))
            {
                cardDeckSystem.RemovePublicCard(_selectedPublicCard);
                Debug.Log($"删除公牌（已翻开）：{_selectedPublicCard.GetComponent<CardDisplay>().cardData.cardName}");
            }
        }

        cardDeckSystem.RearrangePlayerHand();
        ExitDeleteMode();
        if (deleteCardBtn != null) deleteCardBtn.GetComponentInChildren<Text>().text = "表现生气的样子";
        Debug.Log($"操作完成！剩余手牌：{cardDeckSystem.PlayerCardCount}，剩余公牌：{cardDeckSystem.PublicCardObjects.Count}");
    }
    #endregion

    #region 辅助方法
    public bool IsDeleteMode => _isDeleteMode;
    public void ClearSelectedCards()
    {
        _selectedHandCard = null;
        _selectedPublicCard = null;
    }
    #endregion
}