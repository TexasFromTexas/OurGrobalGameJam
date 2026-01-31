using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using BetSystem;
//111
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
                // Subscribe to both new events
                bridge.onPublicJokerRevealed.AddListener(() => OnJokerTriggered(false, false)); // Public trigger, params don't matter much for existing logic if it re-checks
                bridge.onHandJokerRevealed.AddListener(OnJokerTriggered);
                
                Debug.Log("[JokerEventHandler] 已订阅鬼牌事件 (Public & Hand)");
            }
            else
            {
                Debug.LogError("[JokerEventHandler] 未找到BetCardSystemBridge，无法订阅事件！");
            }
        }

        /// <summary>
        /// 鬼牌触发完整逻辑：删牌 → 重新抽牌 → 清零筹码池 → 鬼牌<位置+属性+状态>交换公牌
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

            // 1. 处理场上所有牌（鬼牌洗回，非鬼牌删除）
            DeleteAllCardsOnField();

            // 2. 重新抽取一轮新牌（修正：调用牌库的StartNewRound，不是筹码管理器）
            betManager.StartRound();

            // 3. 清零筹码池（修正：设为0，不是2）
            ResetTotalPot();

            // 4. 鬼牌<位置+属性+状态>交换公牌（核心重构逻辑）
            // ReplaceJokerWithPublicCard();
        }

        #region 核心重构：鬼牌<位置+属性+状态>交换公牌
        /// <summary>
        /// 执行鬼牌完整交换：位置互换 + 属性互换 + 强制正面朝上
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

            // 步骤3：交换两张牌的位置（父物体、坐标、锚点）
            SwapCardPosition(jokerInHand, targetPublicCard);

            // 步骤4：交换两张牌的属性（PlayingCard数据）
            SwapCardData(jokerInHand, targetPublicCard);

            // 步骤5：强制两张牌都正面朝上
            ForceCardShowFront(jokerInHand);
            ForceCardShowFront(targetPublicCard);

            // 步骤6：重新排列手牌和公牌（避免布局错乱）
            cardDeckSystem.RearrangePlayerHand();
            cardDeckSystem.RearrangePublicCards();

            Debug.Log("[JokerEventHandler] 鬼牌与公牌<位置+属性+状态>交换完成！");
        }

        /// <summary>
        /// 交换两张牌的位置（父物体、坐标、锚点）
        /// </summary>
        private void SwapCardPosition(GameObject cardA, GameObject cardB)
        {
            if (cardA == null || cardB == null) return;

            RectTransform rectA = cardA.GetComponent<RectTransform>();
            RectTransform rectB = cardB.GetComponent<RectTransform>();
            if (rectA == null || rectB == null)
            {
                Debug.LogError("[JokerEventHandler] 卡牌缺少RectTransform，位置交换失败！");
                return;
            }

            // 保存A的原始位置信息
            Transform parentA = rectA.parent;
            Vector2 anchoredPosA = rectA.anchoredPosition;
            Vector2 anchorMinA = rectA.anchorMin;
            Vector2 anchorMaxA = rectA.anchorMax;

            // 保存B的原始位置信息
            Transform parentB = rectB.parent;
            Vector2 anchoredPosB = rectB.anchoredPosition;
            Vector2 anchorMinB = rectB.anchorMin;
            Vector2 anchorMaxB = rectB.anchorMax;

            // 交换父物体和位置
            rectA.SetParent(parentB);
            rectA.anchoredPosition = anchoredPosB;
            rectA.anchorMin = anchorMinB;
            rectA.anchorMax = anchorMaxB;

            rectB.SetParent(parentA);
            rectB.anchoredPosition = anchoredPosA;
            rectB.anchorMin = anchorMinA;
            rectB.anchorMax = anchorMaxA;

            // 刷新布局（避免UI卡顿）
            rectA.ForceUpdateRectTransforms();
            rectB.ForceUpdateRectTransforms();

            Debug.Log($"[JokerEventHandler] 位置交换完成：{cardA.name} ↔ {cardB.name}");
        }

        /// <summary>
        /// 强制卡牌正面朝上
        /// </summary>
        private void ForceCardShowFront(GameObject cardObj)
        {
            if (cardObj == null) return;

            CardFaceController faceController = cardObj.GetComponent<CardFaceController>();
            if (faceController == null)
            {
                Debug.LogWarning($"[JokerEventHandler] 卡牌{cardObj.name}缺少CardFaceController，无法强制正面！");
                return;
            }

            // 强制显示正面（无论原本状态）
            faceController.ShowFrontFace();
            Debug.Log($"[JokerEventHandler] 卡牌{cardObj.name}已强制正面朝上");
        }
        #endregion

        #region 原有逻辑（保留+修正）
        /// <summary>
        /// 查找玩家/敌人手牌中的鬼牌（优先玩家，再敌人）
        /// </summary>
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
        /// 交换两张卡牌的属性（PlayingCard数据），并更新UI显示
        /// </summary>
        private void SwapCardData(GameObject cardA, GameObject cardB)
        {
            if (cardA == null || cardB == null) return;

            CardDisplay displayA = cardA.GetComponent<CardDisplay>();
            CardDisplay displayB = cardB.GetComponent<CardDisplay>();

            if (displayA == null || displayB == null || displayA.cardData == null || displayB.cardData == null)
            {
                Debug.LogError("[JokerEventHandler] 卡牌缺少CardDisplay或cardData，属性交换失败！");
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

            Debug.Log($"[JokerEventHandler] 属性交换完成：{cardA.name} ↔ {cardB.name}");
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

        /// <summary>
        /// 清零筹码池（修正：设为0，不是2）
        /// </summary>
        private void ResetTotalPot()
        {
            if (betManager == null) return;

            int oldPotValue = betManager.totalPot;
            betManager.totalPot = 0; // 核心修正：清零应为0
            betManager.onGameStateChanged?.Invoke();

            Debug.Log($"[JokerEventHandler] 筹码池已清零！清零前：{oldPotValue} bb → 清零后：0 bb");
        }

        /// <summary>
        /// 处理场上所有牌：鬼牌洗回牌堆，非鬼牌删除
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
        #endregion

        private void OnDestroy()
        {
            if (bridge != null)
            {
                bridge.onPublicJokerRevealed.RemoveListener(() => OnJokerTriggered(false, false));
                bridge.onHandJokerRevealed.RemoveListener(OnJokerTriggered);
            }
        }
    }
}