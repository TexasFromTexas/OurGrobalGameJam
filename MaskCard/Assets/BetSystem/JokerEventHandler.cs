using UnityEngine;

namespace BetSystem
{
    public class JokerEventHandler : MonoBehaviour
    {
        public BetCardSystemBridge bridge;
        public BetManager betManager;

        private void Start()
        {
            // 自动查找引用（如果未拖拽）
            if (bridge == null) bridge = FindFirstObjectByType<BetCardSystemBridge>();
            if (betManager == null) betManager = FindFirstObjectByType<BetManager>();

            // 订阅事件
            if (bridge != null)
            {
                bridge.onJokerDetected.AddListener(OnJokerTriggered);
            }
        }

        /// <summary>
        /// 当鬼牌事件被触发后，执行此逻辑
        /// </summary>
        /// <param name="playerHasJoker">玩家/公牌是否有鬼牌</param>
        /// <param name="enemyHasJoker">敌人/公牌是否有鬼牌</param>
        public void OnJokerTriggered(bool playerHasJoker, bool enemyHasJoker)
        {
            Debug.Log($"[JokerEventHandler] 鬼牌特效触发！玩家拥有: {playerHasJoker}, 敌人拥有: {enemyHasJoker}");

            // ============================================
            // 在下方编写你的特殊逻辑
            // ============================================

            // 示例：这里可以播放动画、扣血、或者直接判负等
            // ...


            // ============================================
            // 重要：逻辑执行完毕后，必须手动结束回合！
            // ============================================
            // 如果你想让玩家直接赢：
            // betManager.PlayerWin();

            // 如果你想让敌人赢：
            // betManager.EnemyWin();

            // 或者你想强制流局重开：
            // betManager.StartRound();
        }

        private void OnDestroy()
        {
            if (bridge != null)
            {
                bridge.onJokerDetected.RemoveListener(OnJokerTriggered);
            }
        }
    }
}
