using UnityEngine;

public class SmoothParallaxMouseEffect : MonoBehaviour
{
    [Header("Parallax Layers")]
    public Transform backgroundLayer;  // 背景层
    public Transform bossLayer;        // Boss层
    public Transform cardsLayer;       // 卡牌层

    [Header("Movement Settings")]
    public float mouseSensitivity = 0.5f;           // 鼠标敏感度
    public float backLayerMultiplier = 0.3f;        // 背景层移动倍数 (最小)
    public float bossLayerMultiplier = 0.6f;        // Boss层移动倍数 (中等)
    public float cardsLayerMultiplier = 1.0f;       // 卡牌层移动倍数 (最大)
    public float smoothSpeed = 5f;                  // 平滑移动速度

    [Header("Screen Boundaries")]
    public float leftBoundary = -200f;              // 左边界
    public float rightBoundary = 200f;              // 右边界

    private Vector3 originalBackPosition;
    private Vector3 originalBossPosition;
    private Vector3 originalCardsPosition;

    private Vector3 targetBackPosition;
    private Vector3 targetBossPosition;
    private Vector3 targetCardsPosition;

    private void Start()
    {
        // 保存初始位置
        if (backgroundLayer != null) originalBackPosition = backgroundLayer.position;
        if (bossLayer != null) originalBossPosition = bossLayer.position;
        if (cardsLayer != null) originalCardsPosition = cardsLayer.position;

        // 初始化目标位置
        targetBackPosition = originalBackPosition;
        targetBossPosition = originalBossPosition;
        targetCardsPosition = originalCardsPosition;
    }

    void Update()
    {
        // 获取鼠标在屏幕坐标系的位置
        Vector3 mousePos = Input.mousePosition;

        // 计算鼠标相对于屏幕中心的位置 (-1 到 1)
        float normalizedMouseX = (mousePos.x - Screen.width / 2f) / (Screen.width / 2f);

        // 检查鼠标位置
        bool isMouseOnLeftSide = normalizedMouseX < -0.4f;  // 鼠标在屏幕左侧20%范围内
        bool isMouseOnRightSide = normalizedMouseX > 0.4f;  // 鼠标在屏幕右侧20%范围内

        if (isMouseOnLeftSide)
        {
            // 鼠标在左侧时，图层向右移动（相反方向）
            float offset = Mathf.Abs(normalizedMouseX) * mouseSensitivity;  // 取绝对值确保正值

            // 计算目标位置（向右移动，幅度：Back < Boss < Cards）
            if (backgroundLayer != null)
            {
                targetBackPosition = originalBackPosition;
                targetBackPosition.x = Mathf.Clamp(originalBackPosition.x + offset * backLayerMultiplier,
                                                  leftBoundary, rightBoundary);
            }

            if (bossLayer != null)
            {
                targetBossPosition = originalBossPosition;
                targetBossPosition.x = Mathf.Clamp(originalBossPosition.x + offset * bossLayerMultiplier,
                                                  leftBoundary, rightBoundary);
            }

            if (cardsLayer != null)
            {
                targetCardsPosition = originalCardsPosition;
                targetCardsPosition.x = Mathf.Clamp(originalCardsPosition.x + offset * cardsLayerMultiplier,
                                                  leftBoundary, rightBoundary);
            }
        }
        else if (isMouseOnRightSide)
        {
            // 鼠标在右侧时，图层向左移动（相反方向）
            float offset = Mathf.Abs(normalizedMouseX) * mouseSensitivity;  // 取绝对值确保正值

            // 计算目标位置（向左移动，幅度：Back < Boss < Cards）
            if (backgroundLayer != null)
            {
                targetBackPosition = originalBackPosition;
                targetBackPosition.x = Mathf.Clamp(originalBackPosition.x - offset * backLayerMultiplier,
                                                  leftBoundary, rightBoundary);
            }

            if (bossLayer != null)
            {
                targetBossPosition = originalBossPosition;
                targetBossPosition.x = Mathf.Clamp(originalBossPosition.x - offset * bossLayerMultiplier,
                                                  leftBoundary, rightBoundary);
            }

            if (cardsLayer != null)
            {
                targetCardsPosition = originalCardsPosition;
                targetCardsPosition.x = Mathf.Clamp(originalCardsPosition.x - offset * cardsLayerMultiplier,
                                                  leftBoundary, rightBoundary);
            }
        }
        else
        {
            // 鼠标在中间区域时，目标位置恢复为原始位置
            targetBackPosition = originalBackPosition;
            targetBossPosition = originalBossPosition;
            targetCardsPosition = originalCardsPosition;
        }

        // 平滑移动到目标位置
        if (backgroundLayer != null)
            backgroundLayer.position = Vector3.Lerp(backgroundLayer.position, targetBackPosition,
                                                   Time.deltaTime * smoothSpeed);

        if (bossLayer != null)
            bossLayer.position = Vector3.Lerp(bossLayer.position, targetBossPosition,
                                             Time.deltaTime * smoothSpeed);

        if (cardsLayer != null)
            cardsLayer.position = Vector3.Lerp(cardsLayer.position, targetCardsPosition,
                                              Time.deltaTime * smoothSpeed);
    }
}