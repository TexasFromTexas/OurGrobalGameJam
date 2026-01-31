using UnityEngine;
using BetSystem;

namespace BetSystem
{
    public class JokerEventTemplate : MonoBehaviour
    {
        public BetCardSystemBridge bridge;

        private void Start()
        {
            if (bridge == null) bridge = FindFirstObjectByType<BetCardSystemBridge>();

            if (bridge != null)
            {
                // 订阅手牌鬼牌事件
                bridge.onHandJokerRevealed.AddListener(OnHandJokerDetected);
                
                // 订阅公牌鬼牌事件
                bridge.onPublicJokerRevealed.AddListener(OnPublicJokerDetected);
            }
        }

        // 当手牌里有鬼牌时
        private void OnHandJokerDetected(bool playerHasJoker, bool enemyHasJoker)
        {
            // 在这里写逻辑...
        }

        // 当公牌有鬼牌时
        private void OnPublicJokerDetected()
        {
            // 在这里写逻辑...
        }

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
