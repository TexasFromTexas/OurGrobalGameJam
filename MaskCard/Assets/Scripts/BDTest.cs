using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text;

public class BDTest : MonoBehaviour
{
    Judge judge;
    public List<PlayingCard> myCards;
    public List<PlayingCard> enemyCards;
    public List<PlayingCard> publicCards;

    List<PlayingCard> myHandCards;
    PokerHandType myHandsType;
    List<PlayingCard> enemyHandCards;
    PokerHandType enemyHandsType;
    bool win;
    // Start is called before the first frame update
    void Start()
    {
        judge = Judge.Instance;
        SetCardsName(enemyCards);
        SetCardsName(myCards);
        SetCardsName(publicCards);
        //  myCards = new List<PlayingCard>();
        //  myCards.Add(new PlayingCard(CardSuit.Spade, CardRank.Ace));
        //  myCards.Add(new PlayingCard(CardSuit.Spade, CardRank.King));
        //
        //  enemyCards = new List<PlayingCard>();
        //  enemyCards.Add(new PlayingCard(CardSuit.Spade, CardRank.Ace));
        //  enemyCards.Add(new PlayingCard(CardSuit.Heart, CardRank.Nine));
        //
        //  publicCards = new List<PlayingCard>();
        //  publicCards.Add(new PlayingCard(CardSuit.Spade, CardRank.Queen));
        //  publicCards.Add(new PlayingCard(CardSuit.Spade, CardRank.Two));
        //  publicCards.Add(new PlayingCard(CardSuit.Spade, CardRank.Ten));
        //  publicCards.Add(new PlayingCard(CardSuit.Spade, CardRank.Nine));
        //  publicCards.Add(new PlayingCard(CardSuit.Spade, CardRank.Eight));

        myCards.AddRange(publicCards);
        enemyCards.AddRange(publicCards);
    }

    /// <summary>
    /// 根据牌的花色和牌面值写出牌名
    /// 仅测试用
    /// </summary>
    /// <param name="cards"></param>
    private void SetCardsName(List<PlayingCard> cards)
    {
        foreach (var item in cards)
        {
            string suitStr = item.suit switch
            {
                CardSuit.Spade => "黑桃",
                CardSuit.Heart => "红桃",
                CardSuit.Club => "梅花",
                CardSuit.Diamond => "方块",
                _ => ""
            };
            string rankStr = item.rank switch
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
            item.cardName = $"{suitStr}{rankStr}";
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            //此处需要判断公牌中是否有鬼牌


            judge.GetResult(myCards, enemyCards, out myHandCards, out myHandsType, out enemyHandCards, out enemyHandsType, out win);
            //打印myHandCards
            StringBuilder sb = new StringBuilder();
            foreach (var card in myHandCards)
            {
                sb.Append(card.cardName + " ");
            }
            Debug.Log("我的牌型是：" + myHandsType + "，牌型是：" + sb.ToString());
            //打印enemyHandCards
            sb = new StringBuilder();
            foreach (var card in enemyHandCards)
            {
                sb.Append(card.cardName + " ");
            }
            Debug.Log("敌人的牌型是：" + enemyHandsType + "，牌型是：" + sb.ToString());
            //打印结果
            if (win)
            {
                Debug.Log("我赢了！");
            }
            else
            {
                Debug.Log("我输了！");
            }
        }
    }
}