using UnityEngine;

public class HandSlotAutoDestroy : MonoBehaviour
{
    private void Update()
    {
        // 如果没有子物体（卡牌被移走或销毁），自动销毁自身
        if (transform.childCount == 0)
        {
            Destroy(gameObject);
        }
    }
}
