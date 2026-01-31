using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;


public enum behaviorType
{
    raise,
    drop,
    fold
}

/// <summary>
/// 牌型
/// </summary>
public enum PokerHandType
{
    /// <summary>
    /// 皇家同花顺
    /// </summary>
    RoyalFlush = 9,
    /// <summary>
    /// 同花顺
    /// </summary>
    StraightFlush = 8,
    /// <summary>
    /// 四条
    /// </summary>
    FourOfAKind = 7,
    /// <summary>
    /// 葫芦
    /// 分数为三张的*100+对的
    /// </summary>
    FullHouse = 6,
    /// <summary>
    /// 同花
    /// </summary>
    Flush = 5,
    /// <summary>
    /// 顺子
    /// </summary>
    Straight = 4,
    /// <summary>
    /// 三条
    /// </summary>
    ThreeOfAKind = 3,
    /// <summary>
    /// 两对
    /// </summary>
    TwoPair = 2,
    /// <summary>
    /// 一对
    /// </summary>
    OnePair = 1,
    /// <summary>
    /// 高牌
    /// </summary>
    HighCard = 0
}

public class Judge
{
    public static Judge Instance
    {
        get
        {
            if (instance == null)
            {
                instance = new Judge();
            }
            return instance;
        }
    }
    private static Judge instance;

    public Judge()
    {
        InitializeAIData();
    }


    /// <summary>
    /// 根据手牌列表，返回最佳手牌和手牌类型
    /// </summary>
    /// <param name="myCards">我的手牌+公牌</param>
    /// <param name="enemyCards">敌人的手牌+公牌</param>
    /// <param name="myHandCards">结算出的我的最佳手牌</param>
    /// <param name="myHandsType">结算出的我的手牌的类型</param>
    /// <param name="enemyHandCards">结算出的敌人的最佳手牌</param>
    /// <param name="enemyHandsType">结算出的敌人的手牌的类型</param>
    public void GetResult(List<PlayingCard> myCards, List<PlayingCard> enemyCards, out List<PlayingCard> myHandCards, out PokerHandType myHandsType, out List<PlayingCard> enemyHandCards, out PokerHandType enemyHandsType, out bool win)
    {
        win = false;
        myHandCards = GetBestCard(myCards);
        myHandsType = GetHandType(myHandCards);
        enemyHandCards = GetBestCard(enemyCards);
        enemyHandsType = GetHandType(enemyHandCards);
        win = CompareHands(myHandCards, enemyHandCards) >= 0;
    }

    /// <summary>
    /// 评估并返回最强的牌型组合
    /// </summary>
    /// <param name="cards">输入的牌集合</param>
    /// <returns>包含最强牌型和对应牌的列表</returns>
    public List<PlayingCard> GetBestCard(List<PlayingCard> cards)
    {
        if (cards == null || cards.Count < 5)
            return null; // 至少需要5张牌

        // 按降序排列牌
        List<PlayingCard> sortedCards = cards.OrderByDescending(c => c.rank).ToList();

        // 检查所有可能的5张牌组合，找出最强的
        var allCombinations = GetCombinations(sortedCards, 5);
        PokerHandResult bestResult = null;

        foreach (var combination in allCombinations)
        {
            var result = EvaluateSingleHand(combination);
            if (bestResult == null || IsBetterHand(result, bestResult))
            {
                bestResult = result;
            }
        }

        return bestResult?.Cards;
    }

    /// <summary>
    /// 评估单手5张牌的组合
    /// </summary>
    private PokerHandResult EvaluateSingleHand(List<PlayingCard> cards)
    {
        var sortedByRank = cards.OrderBy(c => c.rank).ToList();
        var rankGroups = cards.GroupBy(c => c.rank)
                             .OrderByDescending(g => g.Count())
                             .ThenByDescending(g => g.Key)
                             .ToList();
        var suits = cards.Select(c => c.suit).ToList();
        var ranks = cards.Select(c => c.rank).Distinct().OrderBy(r => r).ToList();


        bool isFlush = suits.Distinct().Count() == 1; // 判断是否同花
        bool isStraight = IsStraight(ranks);


        if (isFlush && isStraight)
        {
            if ((int)ranks.Max() == 14 && (int)ranks.Min() == 10) // A, K, Q, J, 10
                return new PokerHandResult(PokerHandType.RoyalFlush, cards, 14);
            else
                return new PokerHandResult(PokerHandType.StraightFlush, cards, (int)ranks.Max());
        }

        if (rankGroups[0].Count() == 4) // 四条
        {
            int fourRank = (int)rankGroups[0].Key;
            var kickers = cards.Where(c => (int)c.rank != fourRank).OrderByDescending(c => c.rank).Take(1).ToList();
            var resultCards = rankGroups[0].Concat(kickers).ToList();
            return new PokerHandResult(PokerHandType.FourOfAKind, resultCards, fourRank * 10 + (int)resultCards[4].rank);
        }

        if (rankGroups[0].Count() == 3 && rankGroups.Count >= 2 && rankGroups[1].Count() == 2) // 葫芦
        {
            int threeRank = (int)rankGroups[0].Key;
            int pairRank = (int)rankGroups[1].Key;
            var resultCards = cards.Where(c => (int)c.rank == threeRank || (int)c.rank == pairRank).ToList();
            return new PokerHandResult(PokerHandType.FullHouse, resultCards, threeRank * 1000 + pairRank * 10 + (int)resultCards[4].rank);
        }

        if (isFlush) // 同花
        {
            var flusCards = cards.OrderByDescending(c => c.rank).Take(5).ToList();
            return new PokerHandResult(PokerHandType.Flush, flusCards, (int)flusCards.First().rank * 10000 + (int)flusCards[1].rank * 1000 + (int)flusCards[2].rank * 100 + (int)flusCards[3].rank * 10 + (int)flusCards[4].rank);
        }

        if (isStraight) // 顺子
        {
            var straightCards = sortedByRank.Take(5).ToList(); // 简化处理
            int highestRank = (int)ranks.Max();
            return new PokerHandResult(PokerHandType.Straight, straightCards, highestRank);
        }

        if (rankGroups[0].Count() == 3) // 三条
        {
            int threeRank = (int)rankGroups[0].Key;
            var kickers = cards.Where(c => (int)c.rank != threeRank).OrderByDescending(c => c.rank).Take(2).ToList();
            var resultCards = rankGroups[0].Concat(kickers).ToList();
            return new PokerHandResult(PokerHandType.ThreeOfAKind, resultCards, threeRank * 10 + (int)resultCards[3].rank);
        }

        if (rankGroups[0].Count() == 2 && rankGroups.Count >= 2 && rankGroups[1].Count() == 2) // 两对
        {
            int firstPairRank = (int)rankGroups[0].Key;
            int secondPairRank = (int)rankGroups[1].Key;
            var kicker = cards.Where(c => (int)c.rank != firstPairRank && (int)c.rank != secondPairRank)
                             .OrderByDescending(c => c.rank).First();
            var resultCards = rankGroups[0].Concat(rankGroups[1]).Concat(new[] { kicker }).ToList();
            return new PokerHandResult(PokerHandType.TwoPair, resultCards, (int)firstPairRank * 100 + (int)secondPairRank * 10 + (int)kicker.rank);
        }

        if (rankGroups[0].Count() == 2) // 一对
        {
            int pairRank = (int)rankGroups[0].Key;
            var kickers = cards.Where(c => (int)c.rank != pairRank).OrderByDescending(c => c.rank).Take(3).ToList();
            var resultCards = rankGroups[0].Concat(kickers).ToList();
            return new PokerHandResult(PokerHandType.OnePair, resultCards, pairRank);
        }

        // 高牌
        var highCards = cards.OrderByDescending(c => c.rank).Take(5).ToList();
        return new PokerHandResult(PokerHandType.HighCard, highCards, (int)highCards.First().rank);
    }

    /// <summary>
    /// 判断是否为顺子
    /// </summary>
    private bool IsStraight(List<CardRank> ranks)
    {
        if (ranks.Count != 5) return false;

        for (int i = 1; i < ranks.Count; i++)
        {
            if (ranks[i] != ranks[i - 1] + 1)
                return false;
        }
        return true;
    }

    /// <summary>
    /// 生成组合
    /// </summary>
    private IEnumerable<List<T>> GetCombinations<T>(List<T> list, int length)
    {
        if (length == 0)
        {
            yield return new List<T>();
            yield break;
        }

        for (int i = 0; i <= list.Count - length; i++)
        {
            foreach (var tail in GetCombinations(list.Skip(i + 1).ToList(), length - 1))
            {
                yield return new List<T> { list[i] }.Concat(tail).ToList();
            }
        }
    }

    /// <summary>
    /// 比较两个手牌哪个更强
    /// </summary>
    private bool IsBetterHand(PokerHandResult hand1, PokerHandResult hand2)
    {
        if ((int)hand1.HandType != (int)hand2.HandType)
            return (int)hand1.HandType >= (int)hand2.HandType;

        // 相同类型比较分数
        return hand1.Score >= hand2.Score;
    }

    /// <summary>
    /// 获取手牌类型
    /// </summary>
    public PokerHandType GetHandType(List<PlayingCard> cards)
    {
        if (cards == null || cards.Count < 5)
            return PokerHandType.HighCard;

        // 按降序排列牌
        var sortedCards = cards.OrderByDescending(c => c.rank).ToList();

        // 检查所有可能的5张牌组合，找出最强的
        var allCombinations = GetCombinations(sortedCards, 5);
        PokerHandResult bestResult = null;

        foreach (var combination in allCombinations)
        {
            var result = EvaluateSingleHand(combination);
            if (bestResult == null || IsBetterHand(result, bestResult))
            {
                bestResult = result;
            }
        }

        return bestResult?.HandType ?? PokerHandType.HighCard;
    }

    /// <summary>
    /// 比较两手牌的强弱
    /// </summary>
    public int CompareHands(List<PlayingCard> hand1, List<PlayingCard> hand2)
    {
        var result1 = EvaluateBestHand(hand1);
        var result2 = EvaluateBestHand(hand2);

        if ((int)result1.HandType != (int)result2.HandType)
            return (int)result1.HandType - (int)result2.HandType;

        return result1.Score - result2.Score;
    }

    /// <summary>
    /// 评估最佳手牌（返回完整结果）
    /// </summary>
    private PokerHandResult EvaluateBestHand(List<PlayingCard> cards)
    {
        if (cards == null || cards.Count < 5)
            return new PokerHandResult(PokerHandType.HighCard, cards, 0);

        // 按降序排列牌
        var sortedCards = cards.OrderByDescending(c => c.rank).ToList();

        // 检查所有可能的5张牌组合，找出最强的
        var allCombinations = GetCombinations(sortedCards, 5);
        PokerHandResult bestResult = null;

        foreach (var combination in allCombinations)
        {
            var result = EvaluateSingleHand(combination);
            if (bestResult == null || IsBetterHand(result, bestResult))
            {
                bestResult = result;
            }
        }

        return bestResult ?? new PokerHandResult(PokerHandType.HighCard, cards, 0);
    }


    public behaviorType GetAIBehavior(List<PlayingCard> Cards)
    {
        float raiseRate = 0;
        float foldRate = 0;
        float dropRate = 0;

        GetRateNow(Cards, out raiseRate, out foldRate, out dropRate);
        dropRate = 1 - raiseRate - foldRate;


        float randomValue = Random.Range(0f, 1f);
        behaviorType behavior = behaviorType.fold;
        if (randomValue < raiseRate)
        {
            behavior = behaviorType.raise;
        }
        else if (randomValue < (raiseRate + foldRate))
        {
            behavior = behaviorType.fold;
        }
        else
        {
            behavior = behaviorType.drop;
        }
        Debug.Log($"本回合AI逻辑是：{behavior.ToString()}");
        return behavior;
    }
    /// <summary>
    /// 评估现有的牌组下电脑的行为概率
    /// </summary>
    /// <param name="enemyCards">包括手牌和已翻开的公牌</param>
    /// <param name="raiseRate">加注的概率</param>
    /// <param name="foldRate">跟注的概率</param>
    /// <param name="dropRate">弃牌的概率</param>
    public void GetRateNow(List<PlayingCard> enemyCards, out float raiseRate, out float foldRate, out float dropRate)
    {

        raiseRate = 0f;
        foldRate = 0f;
        dropRate = 0f;

        if (enemyCards == null || enemyCards.Count == 0)
        {
            // 如果没有牌，默认为高牌且保守策略
            var data = aiBehaviorDataDict.ContainsKey(PokerHandType.HighCard) ?
                      aiBehaviorDataDict[PokerHandType.HighCard] :
                      new AIBehaviorData { LessThanFiveRaiseRate = 0.05f, LessThanFiveFoldRate = 0.9f, LessThanFiveDropRate = 0.05f };
            raiseRate = data.LessThanFiveRaiseRate;
            foldRate = data.LessThanFiveFoldRate;
            dropRate = data.LessThanFiveDropRate;
            return;
        }

        // 获取当前牌型
        PokerHandType handType = GetHandTypeAdvanced(enemyCards);
        Debug.Log("本回合的牌型为：" + handType.ToString() + " 加注倍率:" + raiseRate + " 根注倍率:" + foldRate + " 弃牌倍率:" + dropRate);

        // 根据手牌数量选择对应的行为概率
        int cardCount = enemyCards.Count;

        if (cardCount < 5)
        {
            // 少于5张牌时的概率
            if (aiBehaviorDataDict.ContainsKey(handType))
            {
                var data = aiBehaviorDataDict[handType];
                raiseRate = data.LessThanFiveRaiseRate;
                foldRate = data.LessThanFiveFoldRate;
                dropRate = data.LessThanFiveDropRate;
            }
            else
            {
                // 如果找不到对应牌型，使用默认值
                var data = aiBehaviorDataDict.ContainsKey(PokerHandType.HighCard) ?
                          aiBehaviorDataDict[PokerHandType.HighCard] :
                          new AIBehaviorData { LessThanFiveRaiseRate = 0.05f, LessThanFiveFoldRate = 0.9f, LessThanFiveDropRate = 0.05f };
                raiseRate = data.LessThanFiveRaiseRate;
                foldRate = data.LessThanFiveFoldRate;
                dropRate = data.LessThanFiveDropRate;
            }
        }
        else
        {
            // 5张或更多牌时使用5张牌的概率
            if (aiBehaviorDataDict.ContainsKey(handType))
            {
                var data = aiBehaviorDataDict[handType];
                raiseRate = data.EqualFiveRaiseRate;
                foldRate = data.EqualFiveFoldRate;
                dropRate = data.EqualFiveDropRate;
            }
            else
            {
                // 如果找不到对应牌型，使用默认值
                var data = aiBehaviorDataDict.ContainsKey(PokerHandType.HighCard) ?
                          aiBehaviorDataDict[PokerHandType.HighCard] :
                          new AIBehaviorData { EqualFiveRaiseRate = 0.05f, EqualFiveFoldRate = 0.3f, EqualFiveDropRate = 0.65f };
                raiseRate = data.EqualFiveRaiseRate;
                foldRate = data.EqualFiveFoldRate;
                dropRate = data.EqualFiveDropRate;
            }
        }

        // 确保概率和为1
        float total = raiseRate + foldRate + dropRate;
        if (total > 0)
        {
            raiseRate /= total;
            foldRate /= total;
            dropRate /= total;
        }
    }

    /// <summary>
    /// AI逻辑判断，有可能不足5张牌也能判断潜在牌型
    /// </summary>
    private PokerHandType GetHandTypeAdvanced(List<PlayingCard> cards)
    {
        if (cards == null || cards.Count == 0)
            return PokerHandType.HighCard;

        // 按降序排列牌
        var sortedCards = cards.OrderByDescending(c => c.rank).ToList();

        // 分析牌的特征
        var rankGroups = cards.GroupBy(c => c.rank)
                             .OrderByDescending(g => g.Count())
                             .ToList();
        var suits = cards.Select(c => c.suit).ToList();
        var ranks = cards.Select(c => (int)c.rank).Distinct().OrderBy(r => r).ToList();

        // 统计各牌的数量
        int pairs = rankGroups.Count(g => g.Count() == 2);
        int threeOfAKind = rankGroups.Count(g => g.Count() == 3);
        int fourOfAKind = rankGroups.Count(g => g.Count() == 4);

        // 判断是否为同花
        bool isPotentialFlush = suits.GroupBy(s => s).Any(g => g.Count() >= 3); // 至少3张同花色

        // 判断是否为顺子潜力
        bool isPotentialStraight = CheckPotentialStraight(ranks);

        // 根据牌的特征判断最强的潜在牌型
        if (fourOfAKind > 0)
            return PokerHandType.FourOfAKind;
        else if (threeOfAKind > 0 && pairs > 0)
            return PokerHandType.FullHouse;
        else if (threeOfAKind > 0 && cards.Count == 4)
            return PokerHandType.FullHouse;
        else if (isPotentialFlush && isPotentialStraight)
            return PokerHandType.StraightFlush; // 简化处理
        else if (threeOfAKind > 0 && cards.Count == 4)
            return PokerHandType.ThreeOfAKind;
        else if (pairs >= 2)
            return PokerHandType.TwoPair;
        else if (pairs >= 1)
            return PokerHandType.OnePair;
        else if (isPotentialStraight)
            return PokerHandType.Straight;
        else if (isPotentialFlush)
            return PokerHandType.Flush;
        else
            return PokerHandType.HighCard;
    }

    /// <summary>
    /// 检查是否有顺子潜力
    /// </summary>
    private bool CheckPotentialStraight(List<int> ranks)
    {
        if (ranks.Count < 3) return false; // 至少需要3张牌才可能形成顺子

        // 检查是否可以通过添加牌形成顺子
        int consecutiveCount = 1;
        int gaps = 0;

        for (int i = 1; i < ranks.Count; i++)
        {
            if (ranks[i] == ranks[i - 1] + 1)
            {
                consecutiveCount++;
            }
            else if (ranks[i] == ranks[i - 1])
            {
                // 相同牌，跳过
                continue;
            }
            else
            {
                // 计算间隔
                gaps += ranks[i] - ranks[i - 1] - 1;
            }
        }

        // 如果连续牌数加上间隙数能达到顺子要求，则认为有顺子潜力
        return consecutiveCount + gaps >= 4; // 4张连续的牌有形成5张顺子的潜力
    }
    // 缓存AI行为数据
    private Dictionary<PokerHandType, AIBehaviorData> aiBehaviorDataDict;

    /// <summary>
    /// 初始化AI行为数据
    /// </summary>
    private void InitializeAIData()
    {
        if (aiBehaviorDataDict != null) return;

        aiBehaviorDataDict = new Dictionary<PokerHandType, AIBehaviorData>();

        // 加载CSV文件
        TextAsset csvText = Resources.Load<TextAsset>("敌人ai表");
        if (csvText != null)
        {
            ParseCSV(csvText.text);
        }
        else
        {
            Debug.LogError("找不到资源文件: Resources/敌人ai表.csv");
            // 使用默认值初始化
            SetDefaultValues();
        }
    }


    /// <summary>
    /// 解析CSV文件
    /// </summary>
    private void ParseCSV(string csvText)
    {
        string[] lines = csvText.Split('\n');

        // 跳过标题行
        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (string.IsNullOrEmpty(line)) continue;

            string[] values = line.Split(',');
            if (values.Length < 7) continue; // 确保有足够的列

            try
            {
                PokerHandType handType = (PokerHandType)System.Enum.Parse(typeof(PokerHandType), values[0]);

                AIBehaviorData data = new AIBehaviorData
                {
                    HandType = handType,
                    LessThanFiveRaiseRate = ParseFloat(values[1]),
                    LessThanFiveFoldRate = ParseFloat(values[2]),
                    LessThanFiveDropRate = ParseFloat(values[3]),
                    EqualFiveRaiseRate = ParseFloat(values[4]),
                    EqualFiveFoldRate = ParseFloat(values[5]),
                    EqualFiveDropRate = ParseFloat(values[6])
                };

                aiBehaviorDataDict[handType] = data;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"解析CSV行{i}时出错: {e.Message}, 行内容: {line}");
            }
        }
    }

    /// <summary>
    /// 设置默认值
    /// </summary>
    private void SetDefaultValues()
    {
        // 默认值：当找不到CSV文件时使用
        var defaultData = new[]
        {
            new AIBehaviorData { HandType = PokerHandType.HighCard, LessThanFiveRaiseRate = 0.05f, LessThanFiveFoldRate = 0.9f, LessThanFiveDropRate = 0.05f, EqualFiveRaiseRate = 0.05f, EqualFiveFoldRate = 0.3f, EqualFiveDropRate = 0.65f },
            new AIBehaviorData { HandType = PokerHandType.OnePair, LessThanFiveRaiseRate = 0.7f, LessThanFiveFoldRate = 0.25f, LessThanFiveDropRate = 0.05f, EqualFiveRaiseRate = 0.2f, EqualFiveFoldRate = 0.5f, EqualFiveDropRate = 0.3f },
            new AIBehaviorData { HandType = PokerHandType.TwoPair, LessThanFiveRaiseRate = 0.8f, LessThanFiveFoldRate = 0.15f, LessThanFiveDropRate = 0.05f, EqualFiveRaiseRate = 0.4f, EqualFiveFoldRate = 0.4f, EqualFiveDropRate = 0.2f },
            new AIBehaviorData { HandType = PokerHandType.ThreeOfAKind, LessThanFiveRaiseRate = 0.85f, LessThanFiveFoldRate = 0.1f, LessThanFiveDropRate = 0.05f, EqualFiveRaiseRate = 0.6f, EqualFiveFoldRate = 0.3f, EqualFiveDropRate = 0.1f },
            new AIBehaviorData { HandType = PokerHandType.Straight, LessThanFiveRaiseRate = 0.9f, LessThanFiveFoldRate = 0.08f, LessThanFiveDropRate = 0.02f, EqualFiveRaiseRate = 0.7f, EqualFiveFoldRate = 0.2f, EqualFiveDropRate = 0.1f },
            new AIBehaviorData { HandType = PokerHandType.Flush, LessThanFiveRaiseRate = 0.9f, LessThanFiveFoldRate = 0.08f, LessThanFiveDropRate = 0.02f, EqualFiveRaiseRate = 0.7f, EqualFiveFoldRate = 0.2f, EqualFiveDropRate = 0.1f },
            new AIBehaviorData { HandType = PokerHandType.FullHouse, LessThanFiveRaiseRate = 0.95f, LessThanFiveFoldRate = 0.04f, LessThanFiveDropRate = 0.01f, EqualFiveRaiseRate = 0.8f, EqualFiveFoldRate = 0.15f, EqualFiveDropRate = 0.05f },
            new AIBehaviorData { HandType = PokerHandType.FourOfAKind, LessThanFiveRaiseRate = 0.98f, LessThanFiveFoldRate = 0.01f, LessThanFiveDropRate = 0.01f, EqualFiveRaiseRate = 0.9f, EqualFiveFoldRate = 0.08f, EqualFiveDropRate = 0.02f },
            new AIBehaviorData { HandType = PokerHandType.StraightFlush, LessThanFiveRaiseRate = 0.99f, LessThanFiveFoldRate = 0.005f, LessThanFiveDropRate = 0.005f, EqualFiveRaiseRate = 0.95f, EqualFiveFoldRate = 0.04f, EqualFiveDropRate = 0.01f },
            new AIBehaviorData { HandType = PokerHandType.RoyalFlush, LessThanFiveRaiseRate = 1.0f, LessThanFiveFoldRate = 0.0f, LessThanFiveDropRate = 0.0f, EqualFiveRaiseRate = 1.0f, EqualFiveFoldRate = 0.0f, EqualFiveDropRate = 0.0f }
        };

        foreach (var data in defaultData)
        {
            aiBehaviorDataDict[data.HandType] = data;
        }
    }

    /// <summary>
    /// 解析浮点数，处理空值情况
    /// </summary>
    private float ParseFloat(string value)
    {
        if (string.IsNullOrEmpty(value.Trim()))
            return 0f;
        if (float.TryParse(value.Trim(), out float result))
            return result;
        return 0f;
    }
}

/// <summary>
/// 牌型评估结果类
/// 牌型 HandType、牌组 Cards、牌型值 Value
/// </summary>
public class PokerHandResult
{
    public PokerHandType HandType { get; set; }
    public List<PlayingCard> Cards { get; set; }
    public int Score { get; set; }  // 用于比较相同类型手牌的大小

    /// <summary>
    /// 创建一个 PokerHandResult 对象
    /// </summary>
    /// <param name="type"></param>
    /// <param name="cards"></param>
    /// <param name="score"></param>
    public PokerHandResult(PokerHandType type, List<PlayingCard> cards, int score = 0)
    {
        HandType = type;
        Cards = cards;
        Score = score;
    }
}

public class AIBehaviorData
{
    public PokerHandType HandType;
    public float LessThanFiveRaiseRate;
    public float LessThanFiveFoldRate;
    public float LessThanFiveDropRate;
    public float EqualFiveRaiseRate;
    public float EqualFiveFoldRate;
    public float EqualFiveDropRate;
}