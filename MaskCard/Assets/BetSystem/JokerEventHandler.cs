using UnityEngine;
using System.Collections.Generic;
using BetSystem;

namespace BetSystem
{
    public class JokerEventHandler : MonoBehaviour
    {
        [Header("核心引用")]
        public BetCardSystemBridge bridge;
        public BetManager betManager;     // 筹码池系统引用
        public CardDeckSystem cardDeckSystem; // 牌库系统引用

        private void Start()
        {
            // 自动查找引用
            if (bridge == null) bridge = FindFirstObjectByType<BetCardSystemBridge>();
            if (betManager == null) betManager = FindFirstObjectByType<BetManager>();
            if (cardDeckSystem == null) cardDeckSystem = FindFirstObjectByType<CardDeckSystem>();

            // 打印引用状态（排查是否找到）
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
        /// 鬼牌触发：删牌 → 重新抽牌 → 清零筹码池（totalPot）
        /// </summary>
        public void OnJokerTriggered(bool playerHasJoker, bool enemyHasJoker)
        {
            Debug.Log($"[JokerEventHandler] 鬼牌特效触发！玩家拥有: {playerHasJoker}, 敌人拥有: {enemyHasJoker}");

            // 检查核心引用是否有效
            if (cardDeckSystem == null)
            {
                Debug.LogError("[JokerEventHandler] cardDeckSystem引用为空，无法执行删牌/抽牌！");
                return;
            }
            if (betManager == null)
            {
                Debug.LogError("[JokerEventHandler] betManager引用为空，无法清零筹码池！");
            }

            // 第一步：删除场上所有牌（玩家/敌人/公牌）
            DeleteAllCardsOnField();

            // 第二步：重新抽取一轮新牌（玩家2/敌人2/公牌5）
            DrawNewRoundCards();

            // 第三步：清零筹码池（totalPot）+ 触发UI更新
            ResetTotalPot();

            // 可选：鬼牌自定义逻辑（判输赢/重启回合）
            // betManager.PlayerWin();
            // betManager.StartRound();
        }

        /// <summary>
        /// 核心：清零筹码池（totalPot），并触发UI更新
        /// </summary>
        private void ResetTotalPot()
        {
            if (betManager == null) return;

            // 记录清零前的数值（便于日志排查）
            int oldPotValue = betManager.totalPot;
            // 清零筹码池
            betManager.totalPot = 0;

            // 触发UI更新事件（确保BetUIController的potText同步显示0）
            betManager.onGameStateChanged?.Invoke();

            Debug.Log($"[JokerEventHandler] 筹码池已清零！清零前：{oldPotValue} bb → 清零后：0 bb");
        }

        /// <summary>
        /// 删除场上所有牌（仅调用public成员）
        /// </summary>
        private void DeleteAllCardsOnField()
        {
            // 删除玩家手牌
            DeleteCardList(cardDeckSystem.playerCardObjects, "玩家手牌");
            // 删除敌人手牌
            DeleteCardList(cardDeckSystem.enemyCardObjects, "敌人手牌");
            // 删除公牌
            DeleteCardList(cardDeckSystem.PublicCardObjects, "公牌（含额外添加）");

            Debug.Log("[JokerEventHandler] 所有牌已删除！");
        }

        /// <summary>
        /// 通用删牌方法（仅操作public列表）
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

            // 复制列表避免遍历异常
            List<GameObject> tempList = new List<GameObject>(cardList);
            foreach (GameObject card in tempList)
            {
                if (card != null) Destroy(card);
            }

            cardList.Clear();
            Debug.Log($"[JokerEventHandler] 已清空{cardType}列表，共删除{tempList.Count}张");
        }

        /// <summary>
        /// 重新抽牌（仅调用CardDeckSystem的public方法）
        /// </summary>
        private void DrawNewRoundCards()
        {
            // 检查牌库数量（≥9张才抽牌）
            if (cardDeckSystem.cardDeck.Count < 9)
            {
                Debug.LogWarning($"[JokerEventHandler] 牌库剩余{cardDeckSystem.cardDeck.Count}张，不足9张，无法抽牌！");
                return;
            }

            // 1. 抽玩家手牌（2张）
            List<PlayingCard> playerHand = new List<PlayingCard>();
            cardDeckSystem.DrawCardsFromDeck(ref playerHand, 2);
            foreach (var card in playerHand)
            {
                cardDeckSystem.SpawnSingleCard(card, cardDeckSystem.playerHandArea, ref cardDeckSystem.playerCardObjects);
            }
            cardDeckSystem.RearrangePlayerHand(); // 已有public修饰

            // 2. 抽敌人手牌（2张）
            List<PlayingCard> enemyHand = new List<PlayingCard>();
            cardDeckSystem.DrawCardsFromDeck(ref enemyHand, 2);
            foreach (var card in enemyHand)
            {
                cardDeckSystem.SpawnSingleCard(card, cardDeckSystem.enemyHandArea, ref cardDeckSystem.enemyCardObjects);
            }
            cardDeckSystem.RearrangeEnemyHand(); // 已改为public

            // 3. 抽公牌（5张）
            List<PlayingCard> publicCards = new List<PlayingCard>();
            cardDeckSystem.DrawCardsFromDeck(ref publicCards, 5);
            foreach (var card in publicCards)
            {
                cardDeckSystem.SpawnSingleCard(card, cardDeckSystem.publicCardArea, ref cardDeckSystem.publicCardObjects);
            }
            cardDeckSystem.RearrangePublicCards(); // 已改为public

            Debug.Log("[JokerEventHandler] 重新抽牌完成！");
        }

        private void OnDestroy()
        {
            // 移除事件监听
            if (bridge != null)
            {
                bridge.onJokerDetected.RemoveListener(OnJokerTriggered);
            }
        }
    }
}