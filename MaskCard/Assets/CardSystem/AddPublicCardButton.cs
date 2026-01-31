using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 抽取额外公牌脚本（新增：通知偷看脚本刷新）
/// </summary>
public class AddPublicCardButton : MonoBehaviour
{
    [Header("核心引用")]
    public CardDeckSystem cardDeckSystem;
    public Button addPublicCardBtn;
    // 新增：引用偷看脚本
    public PeekCardOverlayButton peekCardScript;

    private bool _isClicked = false;

    private void Start()
    {
        if (cardDeckSystem == null)
        {
            Debug.LogError("请给AddPublicCardButton拖入CardDeckSystem！");
            return;
        }
        if (addPublicCardBtn == null)
        {
            Debug.LogError("请给AddPublicCardButton拖入多抽公牌按钮！");
            return;
        }

        addPublicCardBtn.onClick.AddListener(OnClickAddPublicCard);
        cardDeckSystem.OnRoundStateChanged += OnRoundStateChanged;
        UpdateButtonInteractable();
    }

    private void Update()
    {
        UpdateButtonInteractable();
    }

    private void UpdateButtonInteractable()
    {
        if (cardDeckSystem == null || addPublicCardBtn == null) return;

        bool isInRound = cardDeckSystem.IsInRound;
        bool hasCardsInDeck = cardDeckSystem.cardDeck.Count > 0;
        bool canClick = isInRound && hasCardsInDeck && !_isClicked;

        addPublicCardBtn.interactable = canClick;

        Text btnText = addPublicCardBtn.GetComponentInChildren<Text>();
        if (btnText != null)
        {
            btnText.text = _isClicked ? "摘下面具" : "装出和善的样子";
        }
        else
        {
            Debug.LogWarning("多抽公牌按钮缺少Text子组件！");
        }
    }

    private void OnClickAddPublicCard()
    {
        bool success = cardDeckSystem.DrawExtraPublicCard();
        if (success)
        {
            _isClicked = true;
            Debug.Log("额外公牌抽取成功！");

            // 新增：通知偷看脚本刷新状态
            if (peekCardScript != null)
            {
                peekCardScript.RefreshPeekButtonState();
            }
            else
            {
                Debug.LogWarning("未绑定PeekCardOverlayButton脚本！");
            }
        }
        else
        {
            Debug.Log("额外公牌抽取失败！");
        }
        UpdateButtonInteractable();
    }

    private void OnRoundStateChanged(bool isInRound)
    {
        if (isInRound)
        {
            _isClicked = false;
            Invoke(nameof(UpdateButtonInteractable), 0.1f);
        }
        else
        {
            addPublicCardBtn.interactable = false;
        }
    }

    private void OnDestroy()
    {
        if (cardDeckSystem != null)
        {
            cardDeckSystem.OnRoundStateChanged -= OnRoundStateChanged;
        }
    }
}