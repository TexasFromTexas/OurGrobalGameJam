using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;



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
            return new PokerHandResult(PokerHandType.ThreeOfAKind, resultCards, threeRank*10+(int)resultCards[3].rank);
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