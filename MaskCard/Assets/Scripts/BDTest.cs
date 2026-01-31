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
    /// �����ƵĻ�ɫ������ֵд������
    /// ��������
    /// </summary>
    /// <param name="cards"></param>
    private void SetCardsName(List<PlayingCard> cards)
    {
        foreach (var item in cards)
        {
            string suitStr = item.suit switch
            {
                CardSuit.Spade => "����",
                CardSuit.Heart => "����",
                CardSuit.Club => "÷��",
                CardSuit.Diamond => "����",
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
            //�˴���Ҫ�жϹ������Ƿ��й���


            judge.GetResult(myCards, enemyCards, out myHandCards, out myHandsType, out enemyHandCards, out enemyHandsType, out win);
            //��ӡmyHandCards
            StringBuilder sb = new StringBuilder();
            foreach (var card in myHandCards)
            {
                sb.Append(card.cardName + " ");
            }
            Debug.Log("�ҵ������ǣ�" + myHandsType + "�������ǣ�" + sb.ToString());
            //��ӡenemyHandCards
            sb = new StringBuilder();
            foreach (var card in enemyHandCards)
            {
                sb.Append(card.cardName + " ");
            }
            Debug.Log("���˵������ǣ�" + enemyHandsType + "�������ǣ�" + sb.ToString());
            //��ӡ���
            if (win)
            {
                Debug.Log("��Ӯ�ˣ�");
            }
            else
            {
                Debug.Log("�����ˣ�");
            }
        }
    }
}