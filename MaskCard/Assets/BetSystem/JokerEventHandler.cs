using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI; // 新增：用于更新卡牌Text显示
using BetSystem;

namespace BetSystem
{
    public class JokerEventHandler : MonoBehaviour
    {
        [Header("核心引用")]
        public BetCardSystemBridge bridge;
        public BetManager betManager;
        public CardDeckSystem cardDeckSystem;

        private void Start()
        {
            // 自动查找引用
            if (bridge == null) bridge = FindFirstObjectByType<BetCardSystemBridge>();
            if (betManager == null) betManager = FindFirstObjectByType<BetManager>();
            if (cardDeckSystem == null) cardDeckSystem = FindFirstObjectByType<CardDeckSystem>();

            // 打印引用状态
            Debug.Log($"[JokerEventHandler] 引用状态：bridge={bridge != null}, betManager={betManager != null}, cardDeckSystem={cardDeckSystem != null}");

            // 订阅鬼牌事件
            if (bridge != null)
            {
                bridge.onJokerDetected.AddListener(OnJokerTriggered);
                Debug.Log("[JokerEventHandler] 已订阅鬼牌事件");
            }
            else
            {
                Debug.LogError("[JokerEventHandler] 未找到BetCardSystemBridge，无法订阅事件！");
            }
        }

        /// <summary>
        /// 鬼牌触发完整逻辑：删牌 → 重新抽牌 → 清零筹码池 → 鬼牌替换公牌
        /// </summary>
        public void OnJokerTriggered(bool playerHasJoker, bool enemyHasJoker)
        {
            Debug.Log($"[JokerEventHandler] 鬼牌特效触发！玩家拥有: {playerHasJoker}, 敌人拥有: {enemyHasJoker}");

            // 检查核心引用
            if (cardDeckSystem == null)
            {
                Debug.LogError("[JokerEventHandler] cardDeckSystem引用为空，无法执行核心逻辑！");
                return;
            }
            if (betManager == null)
            {
                Debug.LogError("[JokerEventHandler] betManager引用为空，无法清零筹码池！");
            }

            // 1. 删除场上所有牌
            DeleteAllCardsOnField();

            // 2. 重新抽取一轮新牌（玩家2/敌人2/公牌5）
            betManager.StartRound();

            // 3. 清零筹码池
            ResetTotalPot();

            // 4. 鬼牌替换公牌（核心新增逻辑）
            ReplaceJokerWithPublicCard();
        }

        #region 新增：鬼牌替换公牌核心逻辑
        /// <summary>
        /// 执行鬼牌替换公牌：手牌鬼牌 ↔ 公牌第四/五张（仅交换数据）
        /// </summary>
        private void ReplaceJokerWithPublicCard()
        {
            // 步骤1：找到手牌中的鬼牌（玩家/敌人手牌）
            GameObject jokerInHand = FindJokerInHand();
            if (jokerInHand == null)
            {
                Debug.Log("[JokerEventHandler] 玩家/敌人手牌中未找到鬼牌，无需替换！");
                return;
            }

            // 步骤2：找到公牌中要交换的目标牌（第四张→第五张，优先非鬼牌）
            GameObject targetPublicCard = FindTargetPublicCard();
            if (targetPublicCard == null)
            {
                Debug.LogWarning("[JokerEventHandler] 公牌数量不足/无可用交换牌，替换失败！");
                return;
            }

            // 步骤3：交换两张牌的数值（仅数据，不换物体）
            SwapCardData(jokerInHand, targetPublicCard);

            Debug.Log("[JokerEventHandler] 鬼牌与公牌替换完成！");
        }

        /// <summary>
        /// 查找玩家/敌人手牌中的鬼牌（优先玩家，再敌人）
        /// </summary>
        /// <returns>鬼牌物体，无则返回null</returns>
        private GameObject FindJokerInHand()
        {
            // 先查玩家手牌
            foreach (GameObject cardObj in cardDeckSystem.playerCardObjects)
            {
                if (IsCardJoker(cardObj))
                {
                    Debug.Log("[JokerEventHandler] 找到玩家手牌中的鬼牌！");
                    return cardObj;
                }
            }

            // 再查敌人手牌
            foreach (GameObject cardObj in cardDeckSystem.enemyCardObjects)
            {
                if (IsCardJoker(cardObj))
                {
                    Debug.Log("[JokerEventHandler] 找到敌人手牌中的鬼牌！");
                    return cardObj;
                }
            }

            return null;
        }

        /// <summary>
        /// 判断一张卡牌是否是鬼牌
        /// </summary>
        private bool IsCardJoker(GameObject cardObj)
        {
            if (cardObj == null) return false;

            CardDisplay cardDisplay = cardObj.GetComponent<CardDisplay>();
            return cardDisplay != null && cardDisplay.cardData != null && cardDisplay.cardData.rank == CardRank.Joker;
        }

        /// <summary>
        /// 查找公牌中要交换的目标牌：优先第四张（索引3），非鬼牌则用；否则第五张（索引4）
        /// </summary>
        /// <returns>目标公牌物体，无则返回null</returns>
        private GameObject FindTargetPublicCard()
        {
            List<GameObject> publicCards = cardDeckSystem.PublicCardObjects;

            // 检查公牌数量是否≥4张
            if (publicCards.Count < 4)
            {
                Debug.LogWarning($"[JokerEventHandler] 公牌数量仅{publicCards.Count}张，不足4张！");
                return null;
            }

            // 优先第四张（索引3）
            GameObject fourthPublicCard = publicCards[3];
            if (!IsCardJoker(fourthPublicCard))
            {
                Debug.Log("[JokerEventHandler] 选择公牌第四张作为交换目标（非鬼牌）");
                return fourthPublicCard;
            }

            // 第四张是鬼牌，选第五张（索引4）
            if (publicCards.Count >= 5)
            {
                GameObject fifthPublicCard = publicCards[4];
                Debug.Log("[JokerEventHandler] 公牌第四张是鬼牌，选择第五张作为交换目标");
                return fifthPublicCard;
            }

            // 无可用牌
            return null;
        }

        /// <summary>
        /// 交换两张卡牌的数值（仅PlayingCard数据，不换物体位置），并更新UI显示
        /// </summary>
        private void SwapCardData(GameObject cardA, GameObject cardB)
        {
            if (cardA == null || cardB == null) return;

            CardDisplay displayA = cardA.GetComponent<CardDisplay>();
            CardDisplay displayB = cardB.GetComponent<CardDisplay>();

            if (displayA == null || displayB == null || displayA.cardData == null || displayB.cardData == null)
            {
                Debug.LogError("[JokerEventHandler] 卡牌缺少CardDisplay或cardData，交换失败！");
                return;
            }

            // 保存A的原始数据
            PlayingCard tempData = new PlayingCard(displayA.cardData.suit, displayA.cardData.rank);
            tempData.cardName = displayA.cardData.cardName;
            tempData.rankValue = displayA.cardData.rankValue;

            // A ← B的数据
            displayA.cardData.suit = displayB.cardData.suit;
            displayA.cardData.rank = displayB.cardData.rank;
            displayA.cardData.cardName = displayB.cardData.cardName;
            displayA.cardData.rankValue = displayB.cardData.rankValue;

            // B ← 原始A的数据
            displayB.cardData.suit = tempData.suit;
            displayB.cardData.rank = tempData.rank;
            displayB.cardData.cardName = tempData.cardName;
            displayB.cardData.rankValue = tempData.rankValue;

            // 更新UI显示（卡牌上的文字）
            UpdateCardText(cardA, displayA.cardData.cardName);
            UpdateCardText(cardB, displayB.cardData.cardName);

            Debug.Log($"[JokerEventHandler] 交换完成：{cardA.name} ↔ {cardB.name}");
        }

        /// <summary>
        /// 更新卡牌上的Text显示
        /// </summary>
        private void UpdateCardText(GameObject cardObj, string newText)
        {
            Text cardText = cardObj.GetComponentInChildren<Text>();
            if (cardText != null)
            {
                cardText.text = newText;
            }
            else
            {
                Debug.LogError($"[JokerEventHandler] 卡牌{cardObj.name}未找到Text组件，UI更新失败！");
            }
        }
        #endregion

        #region 原有逻辑（删牌/抽牌/清零筹码池）
        /// <summary>
        /// 清零筹码池（totalPot）+ 触发UI更新
        /// </summary>
        private void ResetTotalPot()
        {
            if (betManager == null) return;

            int oldPotValue = betManager.totalPot;
            betManager.totalPot = 2;
            betManager.onGameStateChanged?.Invoke();

            Debug.Log($"[JokerEventHandler] 筹码池已清零！清零前：{oldPotValue} bb → 清零后：0 bb");
        }

        /// <summary>
        /// 删除场上所有牌
        /// </summary>
        private void DeleteAllCardsOnField()
        {
            if (cardDeckSystem == null) return;

            // 处理玩家手牌（鬼牌洗回，非鬼牌删除）
            cardDeckSystem.ProcessCards_JokerReturnToDeck(cardDeckSystem.playerCardObjects, "玩家手牌");
            // 处理敌人手牌（鬼牌洗回，非鬼牌删除）
            cardDeckSystem.ProcessCards_JokerReturnToDeck(cardDeckSystem.enemyCardObjects, "敌人手牌");
            // 处理公牌（鬼牌洗回，非鬼牌删除）
            cardDeckSystem.ProcessCards_JokerReturnToDeck(cardDeckSystem.PublicCardObjects, "公牌");

            Debug.Log("[JokerEventHandler] 所有牌处理完成：鬼牌洗回牌堆，非鬼牌已删除！");
        }

        /// <summary>
        /// 通用删牌方法
        /// </summary>
        private void DeleteCardList(List<GameObject> cardList, string cardType)
        {
            if (cardList == null)
            {
                Debug.LogError($"[JokerEventHandler] {cardType}列表为空引用！");
                return;
            }

            Debug.Log($"[JokerEventHandler] 开始删除{cardType}：数量={cardList.Count}");

            if (cardList.Count == 0) return;

            List<GameObject> tempList = new List<GameObject>(cardList);
            foreach (GameObject card in tempList)
            {
                if (card != null) Destroy(card);
            }

            cardList.Clear();
            Debug.Log($"[JokerEventHandler] 已清空{cardType}列表，共删除{tempList.Count}张");
        }

        /// <summary>
        /// 重新抽牌
        /// </summary>
     
        #endregion

        private void OnDestroy()
        {
            if (bridge != null)
            {
                bridge.onJokerDetected.RemoveListener(OnJokerTriggered);
            }
        }
    }
}