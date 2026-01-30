using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

// 花色枚举
public enum CardSuit
{
    Spade, Heart, Club, Diamond
}

// 点数枚举
public enum CardRank
{
    Ace, Two, Three, Four, Five, Six, Seven,
    Eight, Nine, Ten, Jack, Queen, King, Joker
}

// 卡牌数据类（保留原有逻辑，仅优化注释）
[Serializable]
public class PlayingCard
{
    public CardSuit suit;
    public CardRank rank;
    public string cardName; // 卡牌文字信息（如"黑桃A"）

    public PlayingCard(CardSuit suit, CardRank rank)
    {
        this.suit = suit;
        this.rank = rank;

        if (rank == CardRank.Joker)
        {
            cardName = "鬼牌";
        }
        else
        {
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
                CardRank.Jack => "J",
                CardRank.Queen => "Q",
                CardRank.King => "K",
                _ => ((int)rank + 1).ToString()
            };

            cardName = $"{suitStr}{rankStr}";
        }
    }
}

public class CardDeckSystem : MonoBehaviour
{
    // ========== 核心修改：唯一的54张牌库（队列式） ==========
    public List<PlayingCard> cardDeck = new List<PlayingCard>(); // 唯一牌库（初始54张，队列FIFO）

    // ========== 新增：回合状态标记 ==========
    private bool isInRound = false; // 是否处于回合中（默认不在）
    // 提供公共属性供外部脚本获取回合状态（比如控制抽卡按钮灰显）
    public bool IsInRound => isInRound;

    // 手牌/公共牌（存储实例化的卡牌GameObject，保留原有）
    private List<GameObject> playerCardObjects = new List<GameObject>();
    private List<GameObject> enemyCardObjects = new List<GameObject>();
    private List<GameObject> publicCardObjects = new List<GameObject>();

    // UI引用（保留原有）
    [Header("UI组件")]
    public Button startRoundButton;
    public Button endRoundButton;
    public GameObject cardPrefab; // 拖入你创建的CardPrefab
    public Transform playerHandArea; // 拖入PlayerHandArea
    public Transform enemyHandArea; // 拖入EnemyHandArea
    public Transform publicCardArea; // 拖入PublicCardArea

    // 卡牌布局（保留原有）
    [Header("卡牌布局")]
    public float cardSpacing = 20f; // 卡牌之间的水平间距

    private void Start()
    {
        // 1. 初始化唯一的54张牌库（包含52张常规牌+2张鬼牌）
        InitializeCardDeck();
        // 2. 初始洗牌（让队列初始为随机顺序）
        ShuffleDeck(ref cardDeck);

        // 绑定按钮事件（保留原有）
        startRoundButton.onClick.AddListener(StartNewRound);
        endRoundButton.onClick.AddListener(EndCurrentRound);
        endRoundButton.interactable = false;

        // 检查关键引用（保留原有）
        CheckReferences();
    }

    // ========== 核心修改：初始化唯一的54张牌库 ==========
    private void InitializeCardDeck()
    {
        cardDeck.Clear();

        // 添加4花色×13点数的52张常规牌
        foreach (CardSuit suit in Enum.GetValues(typeof(CardSuit)))
        {
            foreach (CardRank rank in Enum.GetValues(typeof(CardRank)))
            {
                if (rank == CardRank.Joker) continue;
                cardDeck.Add(new PlayingCard(suit, rank));
            }
        }

        // 添加2张鬼牌，凑齐54张
        cardDeck.Add(new PlayingCard(CardSuit.Spade, CardRank.Joker));
        cardDeck.Add(new PlayingCard(CardSuit.Heart, CardRank.Joker));

        Debug.Log($"初始化完成：唯一牌库共{cardDeck.Count}张牌");
    }

    // 检查场景赋值（保留原有）
    private void CheckReferences()
    {
        if (cardPrefab == null) Debug.LogError("请给CardPrefab字段拖入卡牌预制体！");
        if (playerHandArea == null) Debug.LogError("请给PlayerHandArea字段拖入玩家手牌区域！");
        if (enemyHandArea == null) Debug.LogError("请给EnemyHandArea字段拖入敌人手牌区域！");
        if (publicCardArea == null) Debug.LogError("请给PublicCardArea字段拖入公共牌区域！");
    }

    // 开始新一轮抽卡（保留原有功能，新增回合状态标记）
    private void StartNewRound()
    {
        // 清空上一轮的卡牌物体
        ClearAllCardObjects();

        // 检查牌库是否足够抽9张（玩家2+公共5+敌人2）
        if (cardDeck.Count < 9)
        {
            Debug.LogWarning($"牌库剩余{cardDeck.Count}张，不足9张，无法开始新回合！");
            return;
        }

        // ========== 新增：标记为“回合中” ==========
        isInRound = true;

        // 1. 从队列头部抽2张到玩家手牌（队列FIFO）
        List<PlayingCard> playerHand = new List<PlayingCard>();
        DrawCardsFromDeck(ref playerHand, 2);
        SpawnCards(playerHand, playerHandArea, ref playerCardObjects);

        // 2. 从队列头部抽5张公共牌
        List<PlayingCard> publicCards = new List<PlayingCard>();
        DrawCardsFromDeck(ref publicCards, 5);
        SpawnCards(publicCards, publicCardArea, ref publicCardObjects);

        // 3. 从队列头部抽2张到敌人手牌
        List<PlayingCard> enemyHand = new List<PlayingCard>();
        DrawCardsFromDeck(ref enemyHand, 2);
        SpawnCards(enemyHand, enemyHandArea, ref enemyCardObjects);

        // 按钮状态切换
        endRoundButton.interactable = true;
        startRoundButton.interactable = false;

        Debug.Log($"新回合抽卡完成：牌库剩余{cardDeck.Count}张牌，当前状态：回合中");
    }

    // ========== 核心修改：回合结束→所有牌洗回牌库 + 标记回合结束 ==========
    private void EndCurrentRound()
    {
        // 1. 收集所有已抽出的牌（玩家手牌+敌人手牌+公共牌）
        List<PlayingCard> drawnCards = new List<PlayingCard>();
        drawnCards.AddRange(GetCardDataFromObjects(playerCardObjects));
        drawnCards.AddRange(GetCardDataFromObjects(enemyCardObjects));
        drawnCards.AddRange(GetCardDataFromObjects(publicCardObjects));

        // 2. 把抽出的牌全部放回牌库（队列尾部，也可直接加在列表末尾）
        cardDeck.AddRange(drawnCards);
        Debug.Log($"回合结束：回收{drawnCards.Count}张牌，牌库当前共{cardDeck.Count}张");

        // 3. 洗牌（打乱整个牌库，恢复队列随机性）
        ShuffleDeck(ref cardDeck);

        // 4. 清空卡牌物体
        ClearAllCardObjects();

        // ========== 新增：标记为“回合外” ==========
        isInRound = false;

        // 按钮状态切换
        endRoundButton.interactable = false;
        startRoundButton.interactable = true;

        Debug.Log($"回合已结束，当前状态：回合外");
    }

    // ========== 核心修改：抽单张牌方法增加回合状态校验 ==========
    /// <summary>
    /// 从牌库队列头部抽1张到玩家手牌（自动生成UI）
    /// </summary>
    /// <returns>抽到的卡牌数据（牌库空/回合外则返回null）</returns>
    public PlayingCard DrawOneCardToPlayerHand()
    {
        // 1. 新增：校验是否在回合中
        if (!isInRound)
        {
            Debug.LogWarning("当前不在回合中，禁止抽卡！请先点击“开始回合”");
            return null;
        }

        // 2. 检查牌库是否为空
        if (cardDeck.Count == 0)
        {
            Debug.LogWarning("牌库已空，无法抽牌！");
            return null;
        }

        // 3. 队列逻辑：从牌库头部抽1张
        PlayingCard drawnCard = cardDeck[0];
        cardDeck.RemoveAt(0);

        // 4. 生成这张牌的UI到玩家手牌区域（复用原有SpawnCards逻辑）
        List<PlayingCard> tempList = new List<PlayingCard>() { drawnCard };
        SpawnCards(tempList, playerHandArea, ref playerCardObjects);

        Debug.Log($"抽牌成功：{drawnCard.cardName} 已加入玩家手牌，牌库剩余{cardDeck.Count}张");
        return drawnCard;
    }

    // ========== 核心修改：从队列头部抽卡（FIFO） ==========
    private void DrawCardsFromDeck(ref List<PlayingCard> targetHand, int drawCount)
    {
        for (int i = 0; i < drawCount; i++)
        {
            if (cardDeck.Count == 0) break;

            // 队列逻辑：从头部（索引0）抽牌（先进先出）
            PlayingCard drawnCard = cardDeck[0];
            targetHand.Add(drawnCard);
            cardDeck.RemoveAt(0); // 移除队列头部的牌

            Debug.Log($"抽卡：{drawnCard.cardName}，牌库剩余{cardDeck.Count}张");
        }
    }

    // 容错：从卡牌物体获取数据（保留原有）
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

    // Fisher-Yates洗牌算法（保留原有）
    private void ShuffleDeck(ref List<PlayingCard> deck)
    {
        for (int i = deck.Count - 1; i > 0; i--)
        {
            int randomIndex = UnityEngine.Random.Range(0, i + 1);
            (deck[i], deck[randomIndex]) = (deck[randomIndex], deck[i]);
        }
        Debug.Log($"牌库已洗牌，当前数量：{deck.Count}");
    }

    // 实例化卡牌预制体（保留原有）
    private void SpawnCards(List<PlayingCard> cardList, Transform parentArea, ref List<GameObject> cardObjectList)
    {
        if (parentArea == null)
        {
            Debug.LogError($"卡牌父物体为空，无法生成卡牌！");
            return;
        }

        // 计算卡牌起始位置（居中排列）
        float cardWidth = cardPrefab.GetComponent<RectTransform>().sizeDelta.x;
        float totalWidth = (cardList.Count - 1) * (cardWidth + cardSpacing);
        float startX = -totalWidth / 2;

        for (int i = 0; i < cardList.Count; i++)
        {
            // 实例化卡牌
            GameObject cardObj = Instantiate(cardPrefab, parentArea);
            RectTransform cardRect = cardObj.GetComponent<RectTransform>();

            // 设置位置（水平排列）
            cardRect.anchoredPosition = new Vector2(startX + i * (cardWidth + cardSpacing), 0);
            cardRect.anchorMin = new Vector2(0.5f, 0.5f);
            cardRect.anchorMax = new Vector2(0.5f, 0.5f);

            // 绑定卡牌数据
            CardDisplay cardDisplay = cardObj.GetComponent<CardDisplay>();
            if (cardDisplay == null)
            {
                cardDisplay = cardObj.AddComponent<CardDisplay>();
            }
            cardDisplay.cardData = cardList[i];

            // 显示卡牌文字信息
            Text cardText = cardObj.GetComponentInChildren<Text>();
            if (cardText != null)
            {
                cardText.text = cardList[i].cardName;
            }
            else
            {
                Debug.LogError($"卡牌预制体中找不到Text组件！");
            }

            // 存入列表
            cardObjectList.Add(cardObj);
        }
    }

    // 清空所有卡牌物体（保留原有）
    private void ClearAllCardObjects()
    {
        ClearCardObjects(ref playerCardObjects);
        ClearCardObjects(ref enemyCardObjects);
        ClearCardObjects(ref publicCardObjects);
    }

    // 清空指定区域卡牌（保留原有）
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
}

// 卡牌显示组件（保留原有）
public class CardDisplay : MonoBehaviour
{
    public PlayingCard cardData; // 绑定卡牌数据
}