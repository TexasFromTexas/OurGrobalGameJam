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

// 你修改后的点数枚举（带自定义数值，方便比较大小）
public enum CardRank
{
    Ace = 14,
    Two = 15,
    Three = 3, Four = 4, Five = 5, Six = 6, Seven = 7,
    Eight = 8, Nine = 9, Ten = 10, Jack = 11, Queen = 12, King = 13, Joker = 16
}

// 卡牌数据类（核心修改：适配自定义枚举值的名称生成）
[Serializable]
public class PlayingCard
{
    public CardSuit suit;
    public CardRank rank;
    public string cardName; // 卡牌文字信息（如"黑桃A"）
    public int rankValue; // 新增：存储枚举的数值，方便后续比较大小

    public PlayingCard(CardSuit suit, CardRank rank)
    {
        this.suit = suit;
        this.rank = rank;
        this.rankValue = (int)rank; // 记录数值，方便后续比较

        // 1. 处理花色文字
        string suitStr = suit switch
        {
            CardSuit.Spade => "黑桃",
            CardSuit.Heart => "红桃",
            CardSuit.Club => "梅花",
            CardSuit.Diamond => "方块",
            _ => ""
        };

        // 2. 核心修改：适配自定义枚举值的点数文字映射
        string rankStr = rank switch
        {
            CardRank.Ace => "A",       // 14 → A
            CardRank.Two => "2",       // 15 → 2
            CardRank.Three => "3",     // 3 → 3
            CardRank.Four => "4",      // 4 → 4
            CardRank.Five => "5",      // 5 → 5
            CardRank.Six => "6",       // 6 → 6
            CardRank.Seven => "7",     // 7 → 7
            CardRank.Eight => "8",     // 8 → 8
            CardRank.Nine => "9",      // 9 → 9
            CardRank.Ten => "10",      // 10 → 10
            CardRank.Jack => "J",      // 11 → J
            CardRank.Queen => "Q",     // 12 → Q
            CardRank.King => "K",      // 13 → K
            CardRank.Joker => "鬼牌",  // 16 → 鬼牌
            _ => ""
        };

        // 3. 生成最终卡牌名称（鬼牌不需要花色）
        if (rank == CardRank.Joker)
        {
            cardName = rankStr; // 鬼牌直接显示“鬼牌”
        }
        else
        {
            cardName = $"{suitStr}{rankStr}"; // 如“黑桃A”、“红桃2”
        }
    }
}

public class CardDeckSystem : MonoBehaviour
{
    // ========== 核心：唯一的54张牌库（队列式） ==========
    public List<PlayingCard> cardDeck = new List<PlayingCard>(); // 唯一牌库（初始54张，队列FIFO）

    // ========== 回合状态标记 ==========
    private bool isInRound = false; // 是否处于回合中（默认不在）
    public bool IsInRound => isInRound; // 供外部脚本获取回合状态

    // 手牌/公共牌（存储实例化的卡牌GameObject）
    private List<GameObject> playerCardObjects = new List<GameObject>();
    private List<GameObject> enemyCardObjects = new List<GameObject>();
    private List<GameObject> publicCardObjects = new List<GameObject>();

    // UI引用
    [Header("UI组件")]
    public Button startRoundButton;
    public Button endRoundButton;
    public GameObject cardPrefab; // 拖入卡牌预制体
    public Transform playerHandArea; // 拖入玩家手牌区域
    public Transform enemyHandArea; // 拖入敌人手牌区域
    public Transform publicCardArea; // 拖入公共牌区域

    // 卡牌布局参数（间距现在会真正生效）
    [Header("卡牌布局")]
    public float cardSpacing = 10f; // 调小这个值（如5/10）让卡牌更紧凑
    public float cardWidth = 80f; // 手动指定卡牌宽度（避免获取失败，可和预制体宽度一致）

    private void Start()
    {
        // 1. 初始化54张牌库
        InitializeCardDeck();
        // 2. 初始洗牌（随机队列顺序）
        ShuffleDeck(ref cardDeck);

        // 绑定按钮事件
        startRoundButton.onClick.AddListener(StartNewRound);
        endRoundButton.onClick.AddListener(EndCurrentRound);
        endRoundButton.interactable = false;

        // 检查关键引用
        CheckReferences();

        // 自动获取卡牌预制体宽度（备用）
        if (cardPrefab != null && cardWidth <= 0)
        {
            cardWidth = cardPrefab.GetComponent<RectTransform>().sizeDelta.x;
        }
    }

    // ========== 初始化54张唯一牌库 ==========
    private void InitializeCardDeck()
    {
        cardDeck.Clear();

        // 添加4花色×13点数的52张常规牌（适配新枚举）
        foreach (CardSuit suit in Enum.GetValues(typeof(CardSuit)))
        {
            // 按你定义的枚举顺序添加：Ace→Two→Three→...→King
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

        // 添加2张鬼牌，凑齐54张
        cardDeck.Add(new PlayingCard(CardSuit.Spade, CardRank.Joker));
        cardDeck.Add(new PlayingCard(CardSuit.Heart, CardRank.Joker));

        Debug.Log($"初始化完成：唯一牌库共{cardDeck.Count}张牌");
    }

    // ========== 检查引用是否完整（容错） ==========
    private void CheckReferences()
    {
        if (cardPrefab == null) Debug.LogError("请给CardPrefab字段拖入卡牌预制体！");
        if (playerHandArea == null) Debug.LogError("请给PlayerHandArea字段拖入玩家手牌区域！");
        if (enemyHandArea == null) Debug.LogError("请给EnemyHandArea字段拖入敌人手牌区域！");
        if (publicCardArea == null) Debug.LogError("请给PublicCardArea字段拖入公共牌区域！");
    }

    // ========== 开始新回合（标记回合中+抽初始牌） ==========
    private void StartNewRound()
    {
        // 清空上一轮卡牌
        ClearAllCardObjects();

        // 检查牌库是否足够抽9张（玩家2+公共5+敌人2）
        if (cardDeck.Count < 9)
        {
            Debug.LogWarning($"牌库剩余{cardDeck.Count}张，不足9张，无法开始新回合！");
            return;
        }

        // 标记为回合中
        isInRound = true;

        // 1. 抽2张到玩家手牌
        List<PlayingCard> playerHand = new List<PlayingCard>();
        DrawCardsFromDeck(ref playerHand, 2);
        foreach (var card in playerHand)
        {
            SpawnSingleCard(card, playerHandArea, ref playerCardObjects);
        }
        // 重新排列玩家手牌（关键：让间距生效）
        RearrangePlayerHand();

        // 2. 抽5张公共牌
        List<PlayingCard> publicCards = new List<PlayingCard>();
        DrawCardsFromDeck(ref publicCards, 5);
        foreach (var card in publicCards)
        {
            SpawnSingleCard(card, publicCardArea, ref publicCardObjects);
        }
        RearrangePublicCards();

        // 3. 抽2张到敌人手牌
        List<PlayingCard> enemyHand = new List<PlayingCard>();
        DrawCardsFromDeck(ref enemyHand, 2);
        foreach (var card in enemyHand)
        {
            SpawnSingleCard(card, enemyHandArea, ref enemyCardObjects);
        }
        RearrangeEnemyHand();

        // 按钮状态切换
        endRoundButton.interactable = true;
        startRoundButton.interactable = false;

        Debug.Log($"新回合开始：牌库剩余{cardDeck.Count}张，当前状态：回合中");
    }

    // ========== 结束回合（回收所有牌+标记回合外） ==========
    private void EndCurrentRound()
    {
        // 1. 收集所有抽出的牌
        List<PlayingCard> drawnCards = new List<PlayingCard>();
        drawnCards.AddRange(GetCardDataFromObjects(playerCardObjects));
        drawnCards.AddRange(GetCardDataFromObjects(enemyCardObjects));
        drawnCards.AddRange(GetCardDataFromObjects(publicCardObjects));

        // 2. 回收牌到牌库
        cardDeck.AddRange(drawnCards);
        Debug.Log($"回合结束：回收{drawnCards.Count}张牌，牌库当前共{cardDeck.Count}张");

        // 3. 洗牌
        ShuffleDeck(ref cardDeck);

        // 4. 清空卡牌物体
        ClearAllCardObjects();

        // 标记为回合外
        isInRound = false;

        // 按钮状态切换
        endRoundButton.interactable = false;
        startRoundButton.interactable = true;

        Debug.Log($"回合已结束，当前状态：回合外");
    }

    // ========== 公共方法：抽单张牌到玩家手牌（带回合校验） ==========
    public PlayingCard DrawOneCardToPlayerHand()
    {
        // 1. 校验是否在回合中
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

        // 3. 队列逻辑：从头部抽1张
        PlayingCard drawnCard = cardDeck[0];
        cardDeck.RemoveAt(0);

        // 4. 生成单张卡牌并重新排列所有手牌
        SpawnSingleCard(drawnCard, playerHandArea, ref playerCardObjects);
        RearrangePlayerHand(); // 关键：新增牌后重新排列，间距生效

        Debug.Log($"抽牌成功：{drawnCard.cardName}（数值：{drawnCard.rankValue}）已加入玩家手牌，牌库剩余{cardDeck.Count}张");
        return drawnCard;
    }

    // ========== 生成单张卡牌（删除了高亮组件相关代码） ==========
    private void SpawnSingleCard(PlayingCard cardData, Transform parentArea, ref List<GameObject> cardList)
    {
        if (parentArea == null || cardPrefab == null) return;

        // 实例化卡牌
        GameObject cardObj = Instantiate(cardPrefab, parentArea);
        RectTransform cardRect = cardObj.GetComponent<RectTransform>();
        CardDisplay cardDisplay = cardObj.GetComponent<CardDisplay>();

        // 绑定卡牌数据
        if (cardDisplay == null)
        {
            cardDisplay = cardObj.AddComponent<CardDisplay>();
        }
        cardDisplay.cardData = cardData;

        // 显示卡牌名称（现在会正确显示“A/2/3”，而非“14/15/3”）
        Text cardText = cardObj.GetComponentInChildren<Text>();
        if (cardText != null)
        {
            cardText.text = cardData.cardName;
        }
        else
        {
            Debug.LogError($"卡牌预制体中找不到Text组件！");
        }

        // 加入卡牌列表
        cardList.Add(cardObj);
    }

    // ========== 重新排列玩家手牌（核心：让间距生效） ==========
    private void RearrangePlayerHand()
    {
        int cardCount = playerCardObjects.Count;
        if (cardCount == 0 || playerHandArea == null) return;

        // 计算总宽度和起始位置（居中排列）
        float totalWidth = (cardCount - 1) * (cardWidth + cardSpacing);
        float startX = -totalWidth / 2;

        // 遍历所有手牌，重新设置位置
        for (int i = 0; i < cardCount; i++)
        {
            GameObject cardObj = playerCardObjects[i];
            RectTransform cardRect = cardObj.GetComponent<RectTransform>();
            if (cardRect == null) continue;

            // 设置位置（间距由cardSpacing控制）
            cardRect.anchoredPosition = new Vector2(startX + i * (cardWidth + cardSpacing), 0);
            cardRect.anchorMin = new Vector2(0.5f, 0.5f);
            cardRect.anchorMax = new Vector2(0.5f, 0.5f);
        }
    }

    // ========== 重新排列敌人手牌 ==========
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

    // ========== 重新排列公共牌 ==========
    private void RearrangePublicCards()
    {
        int cardCount = publicCardObjects.Count;
        if (cardCount == 0 || publicCardArea == null) return;

        float totalWidth = (cardCount - 1) * (cardWidth + cardSpacing);
        float startX = -totalWidth / 2;

        for (int i = 0; i < cardCount; i++)
        {
            GameObject cardObj = publicCardObjects[i];
            RectTransform cardRect = cardObj.GetComponent<RectTransform>();
            if (cardRect == null) continue;

            cardRect.anchoredPosition = new Vector2(startX + i * (cardWidth + cardSpacing), 0);
            cardRect.anchorMin = new Vector2(0.5f, 0.5f);
            cardRect.anchorMax = new Vector2(0.5f, 0.5f);
        }
    }

    // ========== 核心：从队列头部抽指定数量的牌 ==========
    private void DrawCardsFromDeck(ref List<PlayingCard> targetHand, int drawCount)
    {
        for (int i = 0; i < drawCount; i++)
        {
            if (cardDeck.Count == 0) break;

            // 队列FIFO：从头部抽牌
            PlayingCard drawnCard = cardDeck[0];
            targetHand.Add(drawnCard);
            cardDeck.RemoveAt(0);

            Debug.Log($"抽卡：{drawnCard.cardName}（数值：{drawnCard.rankValue}），牌库剩余{cardDeck.Count}张");
        }
    }

    // ========== 清空所有卡牌 ==========
    private void ClearAllCardObjects()
    {
        ClearCardObjects(ref playerCardObjects);
        ClearCardObjects(ref enemyCardObjects);
        ClearCardObjects(ref publicCardObjects);
    }

    // ========== 清空指定区域的卡牌 ==========
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

    // ========== 从卡牌物体中提取数据（容错） ==========
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

    // ========== Fisher-Yates洗牌算法 ==========
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

// 卡牌显示组件（保留）
public class CardDisplay : MonoBehaviour
{
    public PlayingCard cardData; // 绑定卡牌数据
}