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

// 卡牌数据类
[Serializable]
public class PlayingCard
{
    public CardSuit suit;
    public CardRank rank;
    public string cardName;

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
    // 牌库相关
    private List<PlayingCard> mainDeck = new List<PlayingCard>();
    private List<PlayingCard> remainingDeck = new List<PlayingCard>();

    // 手牌/公共牌（存储实例化的卡牌GameObject）
    private List<GameObject> playerCardObjects = new List<GameObject>();
    private List<GameObject> enemyCardObjects = new List<GameObject>();
    private List<GameObject> publicCardObjects = new List<GameObject>();

    // UI引用
    [Header("UI组件")]
    public Button startRoundButton;
    public Button endRoundButton;
    public GameObject cardPrefab; // 拖入你创建的CardPrefab
    public Transform playerHandArea; // 拖入PlayerHandArea
    public Transform enemyHandArea; // 拖入EnemyHandArea
    public Transform publicCardArea; // 拖入PublicCardArea

    // 卡牌间距（可调，避免卡牌重叠）
    [Header("卡牌布局")]
    public float cardSpacing = 20f; // 卡牌之间的水平间距

    private void Start()
    {
        // 1. 初始化54张牌的主牌库
        InitializeMainDeck();
        // 2. 初始化剩余牌库（复制主牌库）
        remainingDeck = new List<PlayingCard>(mainDeck);
        // 3. 关键修复：首次洗牌！确保第一次抽卡是随机的
        ShuffleDeck(ref remainingDeck);

        // 绑定按钮事件
        startRoundButton.onClick.AddListener(StartNewRound);
        endRoundButton.onClick.AddListener(EndCurrentRound);

        // 初始禁用结束按钮
        endRoundButton.interactable = false;

        // 检查关键引用是否为空（排查用）
        CheckReferences();
    }

    // 检查场景赋值是否正确
    private void CheckReferences()
    {
        if (cardPrefab == null) Debug.LogError("请给CardPrefab字段拖入卡牌预制体！");
        if (playerHandArea == null) Debug.LogError("请给PlayerHandArea字段拖入玩家手牌区域！");
        if (enemyHandArea == null) Debug.LogError("请给EnemyHandArea字段拖入敌人手牌区域！");
        if (publicCardArea == null) Debug.LogError("请给PublicCardArea字段拖入公共牌区域！");
    }

    // 初始化54张牌的主牌库
    private void InitializeMainDeck()
    {
        mainDeck.Clear();

        // 添加4花色×13点数的52张常规牌
        foreach (CardSuit suit in Enum.GetValues(typeof(CardSuit)))
        {
            foreach (CardRank rank in Enum.GetValues(typeof(CardRank)))
            {
                if (rank == CardRank.Joker) continue;
                mainDeck.Add(new PlayingCard(suit, rank));
            }
        }

        // 添加2张鬼牌
        mainDeck.Add(new PlayingCard(CardSuit.Spade, CardRank.Joker));
        mainDeck.Add(new PlayingCard(CardSuit.Heart, CardRank.Joker));
    }

    // 开始新一轮抽牌
    private void StartNewRound()
    {
        // 清空上一轮的卡牌物体
        ClearAllCardObjects();

        // 检查剩余牌库是否足够抽9张（玩家2+公共5+敌人2）
        if (remainingDeck.Count < 9)
        {
            Debug.LogWarning("剩余牌库不足，无法完成抽牌！");
            return;
        }

        // 1. 抽2张到玩家手牌 → 实例化卡牌
        List<PlayingCard> playerHand = new List<PlayingCard>();
        DrawCardsToHand(ref playerHand, 2);
        SpawnCards(playerHand, playerHandArea, ref playerCardObjects);

        // 2. 抽5张公共牌 → 实例化卡牌
        List<PlayingCard> publicCards = new List<PlayingCard>();
        DrawCardsToHand(ref publicCards, 5);
        SpawnCards(publicCards, publicCardArea, ref publicCardObjects);

        // 3. 抽2张到敌人手牌 → 实例化卡牌
        List<PlayingCard> enemyHand = new List<PlayingCard>();
        DrawCardsToHand(ref enemyHand, 2);
        SpawnCards(enemyHand, enemyHandArea, ref enemyCardObjects);

        // 按钮状态切换
        endRoundButton.interactable = true;
        startRoundButton.interactable = false;
    }

    // 回合结束：回收卡牌+洗牌
    private void EndCurrentRound()
    {
        // 回收所有抽出去的牌（容错处理：防止组件缺失）
        remainingDeck.AddRange(GetCardDataFromObjects(playerCardObjects));
        remainingDeck.AddRange(GetCardDataFromObjects(enemyCardObjects));
        remainingDeck.AddRange(GetCardDataFromObjects(publicCardObjects));

        // 洗牌
        ShuffleDeck(ref remainingDeck);

        // 清空卡牌物体
        ClearAllCardObjects();

        // 按钮状态切换
        endRoundButton.interactable = false;
        startRoundButton.interactable = true;

        Debug.Log("回合结束，牌库已洗牌！剩余牌库数量：" + remainingDeck.Count);
    }

    // 容错处理：从卡牌物体获取数据（防止组件缺失报错）
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

    // 从剩余牌库抽取指定数量的牌
    private void DrawCardsToHand(ref List<PlayingCard> targetHand, int drawCount)
    {
        for (int i = 0; i < drawCount; i++)
        {
            if (remainingDeck.Count == 0) break;

            // 抽取第一张牌（牌库已洗牌，所以是随机的）
            PlayingCard drawnCard = remainingDeck[0];
            targetHand.Add(drawnCard);
            remainingDeck.RemoveAt(0);
        }
    }

    // Fisher-Yates洗牌算法（公平打乱牌库）
    private void ShuffleDeck(ref List<PlayingCard> deck)
    {
        for (int i = deck.Count - 1; i > 0; i--)
        {
            int randomIndex = UnityEngine.Random.Range(0, i + 1);
            // 交换两张牌的位置
            (deck[i], deck[randomIndex]) = (deck[randomIndex], deck[i]);
        }
        Debug.Log("牌库已洗牌，当前牌库数量：" + deck.Count);
    }

    // 实例化卡牌预制体并排列
    private void SpawnCards(List<PlayingCard> cardList, Transform parentArea, ref List<GameObject> cardObjectList)
    {
        // 容错：父物体为空则返回
        if (parentArea == null)
        {
            Debug.LogError($"卡牌父物体为空，无法生成卡牌！");
            return;
        }

        // 计算卡牌起始位置（让卡牌居中排列）
        float cardWidth = cardPrefab.GetComponent<RectTransform>().sizeDelta.x;
        float totalWidth = (cardList.Count - 1) * (cardWidth + cardSpacing);
        float startX = -totalWidth / 2;

        for (int i = 0; i < cardList.Count; i++)
        {
            // 实例化卡牌预制体
            GameObject cardObj = Instantiate(cardPrefab, parentArea);
            RectTransform cardRect = cardObj.GetComponent<RectTransform>();

            // 设置卡牌位置（水平排列，不重叠）
            cardRect.anchoredPosition = new Vector2(startX + i * (cardWidth + cardSpacing), 0);
            // 确保锚点居中（防止位置偏移）
            cardRect.anchorMin = new Vector2(0.5f, 0.5f);
            cardRect.anchorMax = new Vector2(0.5f, 0.5f);

            // 绑定卡牌数据
            CardDisplay cardDisplay = cardObj.GetComponent<CardDisplay>();
            if (cardDisplay == null)
            {
                cardDisplay = cardObj.AddComponent<CardDisplay>();
            }
            cardDisplay.cardData = cardList[i];

            // 显示卡牌名称（普通Text组件）
            Text cardText = cardObj.GetComponentInChildren<Text>();
            if (cardText != null)
            {
                cardText.text = cardList[i].cardName;
            }
            else
            {
                Debug.LogError($"卡牌预制体中找不到Text组件！请检查预制体是否包含Text子物体");
            }

            // 存入列表，方便后续清空
            cardObjectList.Add(cardObj);
        }
    }

    // 清空所有实例化的卡牌物体
    private void ClearAllCardObjects()
    {
        ClearCardObjects(ref playerCardObjects);
        ClearCardObjects(ref enemyCardObjects);
        ClearCardObjects(ref publicCardObjects);
    }

    // 清空指定区域的卡牌物体（容错：防止空物体报错）
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

// 卡牌显示组件（存储卡牌数据）
public class CardDisplay : MonoBehaviour
{
    public PlayingCard cardData; // 绑定卡牌数据
}