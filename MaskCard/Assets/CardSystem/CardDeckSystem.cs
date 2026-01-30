using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// 花色枚举
public enum CardSuit
{
    Spade, Heart, Club, Diamond
}

// 点数枚举
public enum CardRank
{
    Ace = 14,
    Two = 15,
    Three = 3, Four = 4, Five = 5, Six = 6, Seven = 7,
    Eight = 8, Nine = 9, Ten = 10, Jack = 11, Queen = 12, King = 13, Joker = 16
}

[Serializable]
public class PlayingCard
{
    public CardSuit suit;
    public CardRank rank;
    public string cardName;
    public int rankValue;

    public PlayingCard(CardSuit suit, CardRank rank)
    {
        this.suit = suit;
        this.rank = rank;
        this.rankValue = (int)rank;

        string suitStr = suit switch
        {
            CardSuit.Spade => "黑桃",
            CardSuit.Heart => "红桃",
            CardSuit.Club => "梅花",
            CardSuit.Diamond => "方块",
            _ => ""
        };

        string rankStr = rank switch
        {
            CardRank.Ace => "A",
            CardRank.Two => "2",
            CardRank.Three => "3",
            CardRank.Four => "4",
            CardRank.Five => "5",
            CardRank.Six => "6",
            CardRank.Seven => "7",
            CardRank.Eight => "8",
            CardRank.Nine => "9",
            CardRank.Ten => "10",
            CardRank.Jack => "J",
            CardRank.Queen => "Q",
            CardRank.King => "K",
            CardRank.Joker => "Joker",
            _ => ""
        };

        if (rank == CardRank.Joker)
        {
            cardName = rankStr;
        }
        else
        {
            cardName = $"{suitStr}{rankStr}";
        }
    }
}

public class CardDeckSystem : MonoBehaviour
{
    // ========== 核心数据 ==========
    public List<PlayingCard> cardDeck = new List<PlayingCard>();
    private bool isInRound = false;
    public bool IsInRound => isInRound;

    // 供外部获取玩家手牌数量
    public int PlayerCardCount => playerCardObjects.Count;

    // 事件：回合状态变化时触发
    public Action<bool> OnRoundStateChanged;

    private List<GameObject> playerCardObjects = new List<GameObject>();
    private List<GameObject> enemyCardObjects = new List<GameObject>();
    private List<GameObject> publicCardObjects = new List<GameObject>();
    public List<GameObject> PublicCardObjects => publicCardObjects;

    // UI引用（已删除deleteCardButton）
    [Header("UI组件")]
    public Button startRoundButton;
    public Button endRoundButton;
    public GameObject cardPrefab;
    public Transform playerHandArea;
    public Transform enemyHandArea;
    public Transform publicCardArea;

    [Header("卡牌布局")]
    public float cardSpacing = 10f; // 玩家/敌人手牌间距
    public float cardWidth = 80f;
    public float publicCardSpacing = 15f; // 公共牌专属间距

    private void Start()
    {
        InitializeCardDeck();
        ShuffleDeck(ref cardDeck);

        startRoundButton.onClick.AddListener(StartNewRound);
        endRoundButton.onClick.AddListener(EndCurrentRound);
        endRoundButton.interactable = false;

        CheckReferences();

        if (cardPrefab != null && cardWidth <= 0)
        {
            cardWidth = cardPrefab.GetComponent<RectTransform>().sizeDelta.x;
        }
    }

    private void InitializeCardDeck()
    {
        cardDeck.Clear();

        foreach (CardSuit suit in Enum.GetValues(typeof(CardSuit)))
        {
            CardRank[] normalRanks = new CardRank[]
            {
                CardRank.Ace, CardRank.Two, CardRank.Three, CardRank.Four, CardRank.Five,
                CardRank.Six, CardRank.Seven, CardRank.Eight, CardRank.Nine, CardRank.Ten,
                CardRank.Jack, CardRank.Queen, CardRank.King
            };
            foreach (CardRank rank in normalRanks)
            {
                cardDeck.Add(new PlayingCard(suit, rank));
            }
        }

        cardDeck.Add(new PlayingCard(CardSuit.Spade, CardRank.Joker));
        cardDeck.Add(new PlayingCard(CardSuit.Heart, CardRank.Joker));

        Debug.Log($"初始化完成：唯一牌库共{cardDeck.Count}张牌");
    }

    private void CheckReferences()
    {
        if (cardPrefab == null) Debug.LogError("请给CardPrefab字段拖入卡牌预制体！");
        if (playerHandArea == null) Debug.LogError("请给PlayerHandArea字段拖入玩家手牌区域！");
        if (enemyHandArea == null) Debug.LogError("请给EnemyHandArea字段拖入敌人手牌区域！");
        if (publicCardArea == null) Debug.LogError("请给PublicCardArea字段拖入公共牌区域！");
    }

    private void StartNewRound()
    {
        ClearAllCardObjects();

        if (cardDeck.Count < 9)
        {
            Debug.LogWarning($"牌库剩余{cardDeck.Count}张，不足9张，无法开始新回合！");
            return;
        }

        isInRound = true;
        OnRoundStateChanged?.Invoke(true); // 触发回合开始事件

        List<PlayingCard> playerHand = new List<PlayingCard>();
        DrawCardsFromDeck(ref playerHand, 2);
        foreach (var card in playerHand)
        {
            SpawnSingleCard(card, playerHandArea, ref playerCardObjects);
        }
        RearrangePlayerHand();

        List<PlayingCard> publicCards = new List<PlayingCard>();
        DrawCardsFromDeck(ref publicCards, 5);
        foreach (var card in publicCards)
        {
            SpawnSingleCard(card, publicCardArea, ref publicCardObjects);
        }
        RearrangePublicCards();

        List<PlayingCard> enemyHand = new List<PlayingCard>();
        DrawCardsFromDeck(ref enemyHand, 2);
        foreach (var card in enemyHand)
        {
            SpawnSingleCard(card, enemyHandArea, ref enemyCardObjects);
        }
        RearrangeEnemyHand();

        endRoundButton.interactable = true;
        startRoundButton.interactable = false;

        Debug.Log($"新回合开始：牌库剩余{cardDeck.Count}张，当前状态：回合中");
    }

    private void EndCurrentRound()
    {
        List<PlayingCard> drawnCards = new List<PlayingCard>();
        drawnCards.AddRange(GetCardDataFromObjects(playerCardObjects));
        drawnCards.AddRange(GetCardDataFromObjects(enemyCardObjects));
        drawnCards.AddRange(GetCardDataFromObjects(publicCardObjects));

        cardDeck.AddRange(drawnCards);
        Debug.Log($"回合结束：回收{drawnCards.Count}张牌，牌库当前共{cardDeck.Count}张");

        ShuffleDeck(ref cardDeck);
        ClearAllCardObjects();

        isInRound = false;
        OnRoundStateChanged?.Invoke(false); // 触发回合结束事件

        endRoundButton.interactable = false;
        startRoundButton.interactable = true;

        Debug.Log($"回合已结束，当前状态：回合外");
    }

    public PlayingCard DrawOneCardToPlayerHand()
    {
        if (!isInRound)
        {
            Debug.LogWarning("当前不在回合中，禁止抽卡！请先点击“开始回合”");
            return null;
        }

        if (cardDeck.Count == 0)
        {
            Debug.LogWarning("牌库已空，无法抽牌！");
            return null;
        }

        PlayingCard drawnCard = cardDeck[0];
        cardDeck.RemoveAt(0);

        SpawnSingleCard(drawnCard, playerHandArea, ref playerCardObjects);
        RearrangePlayerHand();

        Debug.Log($"抽牌成功：{drawnCard.cardName}（数值：{drawnCard.rankValue}）已加入玩家手牌，牌库剩余{cardDeck.Count}张");
        return drawnCard;
    }

    private void SpawnSingleCard(PlayingCard cardData, Transform parentArea, ref List<GameObject> cardList)
    {
        if (parentArea == null || cardPrefab == null) return;

        GameObject cardObj = Instantiate(cardPrefab, parentArea);
        RectTransform cardRect = cardObj.GetComponent<RectTransform>();
        CardDisplay cardDisplay = cardObj.GetComponent<CardDisplay>();

        if (cardDisplay == null)
        {
            cardDisplay = cardObj.AddComponent<CardDisplay>();
        }
        cardDisplay.cardData = cardData;

        Text cardText = cardObj.GetComponentInChildren<Text>();
        if (cardText != null)
        {
            cardText.text = cardData.cardName;
        }
        else
        {
            Debug.LogError($"卡牌预制体中找不到Text组件！");
        }

        // 自动添加点击删除脚本
        if (!cardObj.GetComponent<CardDeleteClick>())
        {
            cardObj.AddComponent<CardDeleteClick>();
        }

        // 自动添加卡牌正反面控制脚本
        CardFaceController faceController = cardObj.GetComponent<CardFaceController>();
        if (faceController == null)
        {
            faceController = cardObj.AddComponent<CardFaceController>();
        }

        // ===================== 核心修改：区分公共牌/其他牌 =====================
        // 判断当前生成的卡牌是否是公共牌（通过父区域对比）
        if (parentArea == publicCardArea)
        {
            // 公共牌：强制设置为暗面
            faceController.ShowBackFace();
            Debug.Log($"生成公共牌：{cardData.cardName} → 暗面");
        }
        else
        {
            // 玩家牌/敌人牌：强制设置为亮面
            faceController.ShowFrontFace();
            Debug.Log($"生成玩家/敌人牌：{cardData.cardName} → 亮面");
        }
        // =====================================================================

        cardList.Add(cardObj);
    }

    // ========== 新增：供CardDeleteSystem调用的接口 ==========
    public void RemovePlayerCard(GameObject cardToDelete)
    {
        if (playerCardObjects.Contains(cardToDelete))
        {
            playerCardObjects.Remove(cardToDelete);
            Destroy(cardToDelete);
        }
    }

    // ========== 改为公共方法：供CardDeleteSystem调用 ==========
    public void RearrangePlayerHand()
    {
        int cardCount = playerCardObjects.Count;
        if (cardCount == 0 || playerHandArea == null) return;

        float totalWidth = (cardCount - 1) * (cardWidth + cardSpacing);
        float startX = -totalWidth / 2;

        for (int i = 0; i < cardCount; i++)
        {
            GameObject cardObj = playerCardObjects[i];
            RectTransform cardRect = cardObj.GetComponent<RectTransform>();
            if (cardRect == null) continue;

            cardRect.anchoredPosition = new Vector2(startX + i * (cardWidth + cardSpacing), 0);
            cardRect.anchorMin = new Vector2(0.5f, 0.5f);
            cardRect.anchorMax = new Vector2(0.5f, 0.5f);
        }
    }

    private void RearrangeEnemyHand()
    {
        int cardCount = enemyCardObjects.Count;
        if (cardCount == 0 || enemyHandArea == null) return;

        float totalWidth = (cardCount - 1) * (cardWidth + cardSpacing);
        float startX = -totalWidth / 2;

        for (int i = 0; i < cardCount; i++)
        {
            GameObject cardObj = enemyCardObjects[i];
            RectTransform cardRect = cardObj.GetComponent<RectTransform>();
            if (cardRect == null) continue;

            cardRect.anchoredPosition = new Vector2(startX + i * (cardWidth + cardSpacing), 0);
            cardRect.anchorMin = new Vector2(0.5f, 0.5f);
            cardRect.anchorMax = new Vector2(0.5f, 0.5f);
        }
    }

    private void RearrangePublicCards()
    {
        int cardCount = publicCardObjects.Count;
        if (cardCount == 0 || publicCardArea == null) return;

        float totalWidth = (cardCount - 1) * (cardWidth + publicCardSpacing);
        float startX = -totalWidth / 2;

        for (int i = 0; i < cardCount; i++)
        {
            GameObject cardObj = publicCardObjects[i];
            RectTransform cardRect = cardObj.GetComponent<RectTransform>();
            if (cardRect == null) continue;

            cardRect.anchoredPosition = new Vector2(startX + i * (cardWidth + publicCardSpacing), 0);
            cardRect.anchorMin = new Vector2(0.5f, 0.5f);
            cardRect.anchorMax = new Vector2(0.5f, 0.5f);
        }
    }

    private void DrawCardsFromDeck(ref List<PlayingCard> targetHand, int drawCount)
    {
        for (int i = 0; i < drawCount; i++)
        {
            if (cardDeck.Count == 0) break;

            PlayingCard drawnCard = cardDeck[0];
            targetHand.Add(drawnCard);
            cardDeck.RemoveAt(0);

            Debug.Log($"抽卡：{drawnCard.cardName}（数值：{drawnCard.rankValue}），牌库剩余{cardDeck.Count}张");
        }
    }

    private void ClearAllCardObjects()
    {
        ClearCardObjects(ref playerCardObjects);
        ClearCardObjects(ref enemyCardObjects);
        ClearCardObjects(ref publicCardObjects);
    }

    private void ClearCardObjects(ref List<GameObject> cardObjectList)
    {
        foreach (GameObject cardObj in cardObjectList)
        {
            if (cardObj != null)
            {
                Destroy(cardObj);
            }
        }
        cardObjectList.Clear();
    }

    private List<PlayingCard> GetCardDataFromObjects(List<GameObject> cardObjects)
    {
        List<PlayingCard> dataList = new List<PlayingCard>();
        foreach (GameObject go in cardObjects)
        {
            if (go == null) continue;
            CardDisplay display = go.GetComponent<CardDisplay>();
            if (display != null && display.cardData != null)
            {
                dataList.Add(display.cardData);
            }
        }
        return dataList;
    }

    private void ShuffleDeck(ref List<PlayingCard> deck)
    {
        for (int i = deck.Count - 1; i > 0; i--)
        {
            int randomIndex = UnityEngine.Random.Range(0, i + 1);
            (deck[i], deck[randomIndex]) = (deck[randomIndex], deck[i]);
        }
        Debug.Log($"牌库已洗牌，当前数量：{deck.Count}");
    }
}

public class CardDisplay : MonoBehaviour
{
    public PlayingCard cardData;
}