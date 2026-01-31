using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using BetSystem;

namespace BetSystem
{
    public class JokerEventTemplate : MonoBehaviour
    {
        public BetCardSystemBridge bridge;
        public BetManager betManager;
        public CardDeckSystem cardDeckSystem;

        private void Start()
        {
            if (bridge == null) bridge = FindFirstObjectByType<BetCardSystemBridge>();
            if (betManager == null) betManager = FindFirstObjectByType<BetManager>();
            if (cardDeckSystem == null) cardDeckSystem = FindFirstObjectByType<CardDeckSystem>();

            if (bridge != null)
            {
                // 订阅手牌鬼牌事件 (Anytime)
                bridge.onHandJokerRevealed.AddListener(OnHandJokerDetected);
                
                // 订阅公牌鬼牌事件 (Showdown)
                bridge.onPublicJokerRevealed.AddListener(OnPublicJokerDetected);
            }
        }

        // 1. 当手牌里有鬼牌时（任意时刻被翻开）
        private void OnHandJokerDetected(bool playerHasJoker, bool enemyHasJoker)
        {
            Debug.Log($"[JokerTemplate] 手牌鬼牌触发！玩家:{playerHasJoker}, 敌人:{enemyHasJoker}");
            
            if (cardDeckSystem == null || betManager == null) return;

            // 步骤1：找到手牌中已翻开的鬼牌
            GameObject jokerInHand = FindRevealedJokerInHand();
            if (jokerInHand == null)
            {
                Debug.LogWarning("[JokerTemplate] 触发事件但未找到已翻开的手牌鬼牌（可能被再次覆盖？）");
                // 解锁结算，防止游戏卡死
                betManager.isSettlementLocked = false;
                return;
            }

            // 步骤2：确定交换目标（固定替换河牌/第五张；如果是鬼牌，则替换转牌/第四张）
            GameObject targetPublicCard = GetSwapTargetPublicCard();
            if (targetPublicCard == null)
            {
                Debug.LogWarning("[JokerTemplate] 无可用的公牌进行交换！");
                betManager.isSettlementLocked = false;
                return;
            }

            // 步骤3：执行交换（属性+位置）并立即翻开
            PerformSwapAndReveal(jokerInHand, targetPublicCard);

            // 步骤4：游戏继续（解锁结算锁定）
            // 规则：“直至被翻开前不动”... “若不是开牌判赢阶段则本轮游戏继续”
            // Hand Joker事件通常在BetManager锁定时触发（BetCardSystemBridge锁的），所以我们需要解锁。
            betManager.isSettlementLocked = false; 
            
            Debug.Log("[JokerTemplate] 手牌鬼牌处理完毕，游戏继续。");
        }

        // 2. 当公牌有鬼牌时（仅在Showdown判赢阶段触发）
        private void OnPublicJokerDetected()
        {
            Debug.Log("[JokerTemplate] 公牌鬼牌触发（Showdown）！");

            if (betManager == null || cardDeckSystem == null) return;

            // 规则：“如果判赢时鬼牌已被翻开，则判敌人赢”
            // 关键修正：必须先执行销毁/回收逻辑，再调用EnemyWin。
            // 因为 EnemyWin -> EndSettlement -> StartRound -> StartNewRound 会洗牌并抽新卡。
            // 如果先调用 EnemyWin，新卡生成后，我们再执行下面的销毁逻辑，就会把新发的卡也给销毁了！
            
            // 步骤1：销毁场上所有扑克牌（不再洗进牌堆，除了鬼牌）
            // 规则：“如果鬼牌将被销毁，则取消销毁，重新洗进牌堆”
            cardDeckSystem.ProcessCards_JokerReturnToDeck(cardDeckSystem.playerCardObjects, "玩家手牌");
            cardDeckSystem.ProcessCards_JokerReturnToDeck(cardDeckSystem.enemyCardObjects, "敌人手牌");
            cardDeckSystem.ProcessCards_JokerReturnToDeck(cardDeckSystem.PublicCardObjects, "公牌");
            
            Debug.Log("[JokerTemplate] 场上清理完成（鬼牌已回收）。准备结算并开启新回合。");

            // 步骤2：判敌人赢（这会触发筹码结算 -> 开启新回合）
            betManager.EnemyWin();

            Debug.Log("[JokerTemplate] 公牌鬼牌流程结束。");
        }

        #region Logic Helpers

        /// <summary>
        /// 查找手牌中已翻开的鬼牌
        /// </summary>
        private GameObject FindRevealedJokerInHand()
        {
            // 优先检查玩家
            foreach (var card in cardDeckSystem.playerCardObjects)
            {
                if (IsJokerAndRevealed(card)) return card;
            }
            // 检查敌人
            foreach (var card in cardDeckSystem.enemyCardObjects)
            {
                if (IsJokerAndRevealed(card)) return card;
            }
            return null;
        }

        private bool IsJokerAndRevealed(GameObject card)
        {
            if (card == null) return false;
            CardDisplay display = card.GetComponent<CardDisplay>();
            CardFaceController face = card.GetComponent<CardFaceController>();
            
            bool isJoker = display != null && display.cardData != null && display.cardData.rank == CardRank.Joker;
            bool isRevealed = face != null && !face._isShowingBack;
            
            return isJoker && isRevealed;
        }

        /// <summary>
        /// 获取交换目标的公牌：优先河牌（索引4），若为鬼牌则转牌（索引3）
        /// </summary>
        private GameObject GetSwapTargetPublicCard()
        {
            List<GameObject> publicCards = cardDeckSystem.PublicCardObjects;
            if (publicCards.Count < 5) return null; // 需要至少5张公牌

            GameObject riverCard = publicCards[4]; // 第五张
            
            // 检查河牌是否是鬼牌（无论翻开与否？根据图示“如果河牌也是鬼牌”，通常指卡牌本身）
            if (IsCardJoker(riverCard))
            {
                // 替换转牌
                Debug.Log("[JokerTemplate] 河牌是鬼牌，改为替换转牌。");
                return publicCards[3];
            }
            
            return riverCard;
        }

        private bool IsCardJoker(GameObject card)
        {
            if (card == null) return false;
            CardDisplay display = card.GetComponent<CardDisplay>();
            return display != null && display.cardData != null && display.cardData.rank == CardRank.Joker;
        }

        private void PerformSwapAndReveal(GameObject jokerCard, GameObject targetPublicCard)
        {
            // 1. 在逻辑列表中交换引用（关键：修复位置错乱）
            SwapCardsInLists(jokerCard, targetPublicCard);

            // 2. 物理/UI及其父物体交换
            SwapCardPosition(jokerCard, targetPublicCard);
            
            // 3. 数据交换（之前已实现）
            // SwapCardData(jokerCard, targetPublicCard); 
            // 修正：如果我们在逻辑列表里交换了引用，其实 cardData 也跟着引用走了。
            // 仔细想：JokerObj 去了 Public区，PublicObj 去了 Hand区。
            // 此时 JokerObj 还是 JokerData， PublicObj 还是 PublicData。
            // 也就是：Joker牌（物体+数据）整个变成了公牌。
            // 所以，不需要 SwapCardData！
            // 之前 SwapCardData 是为了让“位置不动，数据变了”。
            // 现在我们是“位置动了，物体动了，引用也动了”，那就不需要换数据了。
            // 除非... 用户的意思是“手里那张卡的位置变了公牌图，公牌的位置变了鬼牌图”？
            // 不，通常是“把鬼牌扔出去，把公牌拿回来”。
            // 所以物体交换了，数据就不用换了（因为物体带着数据）。
            
            // 但是检查之前的逻辑：Clone/Copy...
            // 如果我们物理交换了，JokerObj就在公牌区了。它显示的是Joker。
            // PublicObj在手牌区了。显示的是PublicCar。
            // 这就是我们要的效果。不需要 SwapCardData。
            // 如果执行 SwapCardData，那JokerObj（在公牌区）又变成了PublicData，PublicObj（在手牌区）变成了JokerData。
            // 结果：公牌区有一张长着Joker脸的普通公牌，手牌区有一张长着公牌脸的Joker。
            // 这是否是用户想要的？
            // 用户说：“固定替换...然后立刻翻开”。
            // 既然是“替换”，就是物体互换。
            // 所以我注释掉 SwapCardData。
            
            // 4. 立即翻开
            ForceCardShowFront(jokerCard);
            ForceCardShowFront(targetPublicCard);
            
            // 5. 刷新布局
            cardDeckSystem.RearrangePlayerHand();
            cardDeckSystem.RearrangeEnemyHand();
            cardDeckSystem.RearrangePublicCards();
        }

        private void SwapCardsInLists(GameObject cardA, GameObject cardB)
        {
             // 找到CardA所在的列表
             List<GameObject> listA = FindListContaining(cardA);
             List<GameObject> listB = FindListContaining(cardB);

             if (listA == null || listB == null)
             {
                 Debug.LogError("无法在CardDeckSystem的列表中找到卡牌，逻辑交换失败！");
                 return;
             }

             // 简单交换：Remove then Add? No, replace at index ensures order stability (if matters).
             // 手牌顺序通常无所谓，公牌顺序（索引）很重要（Turn/River）。
             
             int indexA = listA.IndexOf(cardA);
             int indexB = listB.IndexOf(cardB);

             if (indexA != -1 && indexB != -1)
             {
                 listA[indexA] = cardB;
                 listB[indexB] = cardA;
                 Debug.Log($"逻辑列表交换完成：{cardA.name} <-> {cardB.name}");
             }
        }

        private List<GameObject> FindListContaining(GameObject card)
        {
            if (cardDeckSystem.playerCardObjects.Contains(card)) return cardDeckSystem.playerCardObjects;
            if (cardDeckSystem.enemyCardObjects.Contains(card)) return cardDeckSystem.enemyCardObjects;
            if (cardDeckSystem.PublicCardObjects.Contains(card)) return cardDeckSystem.PublicCardObjects;
            return null;
        }

        // ================= 复用/搬运的底层辅助方法 =================

        private void SwapCardPosition(GameObject cardA, GameObject cardB)
        {
            if (cardA == null || cardB == null) return;
            RectTransform rectA = cardA.GetComponent<RectTransform>();
            RectTransform rectB = cardB.GetComponent<RectTransform>();
            
            Transform parentA = rectA.parent;
            // Vector2 anchoredPosA = rectA.anchoredPosition; // 不需要保留位置，Rearrange会重置
            // 只需要换父物体即可，位置由Rearrange接管
            
            Transform parentB = rectB.parent;

            rectA.SetParent(parentB);
            rectB.SetParent(parentA);
            
            // 归零位置防止飞出屏幕（Rearrange前）
            // rectA.anchoredPosition = Vector2.zero;
            // rectB.anchoredPosition = Vector2.zero;
        }

        private void UpdateCardText(GameObject cardObj, string text)
        {
            Text t = cardObj.GetComponentInChildren<Text>();
            if (t != null) t.text = text;
        }

        private void ForceCardShowFront(GameObject cardObj)
        {
            CardFaceController face = cardObj.GetComponent<CardFaceController>();
            if (face != null) face.ShowFrontFace();
        }

        #endregion

        private void OnDestroy()
        {
            if (bridge != null)
            {
                bridge.onHandJokerRevealed.RemoveListener(OnHandJokerDetected);
                bridge.onPublicJokerRevealed.RemoveListener(OnPublicJokerDetected);
            }
        }
    }
}
