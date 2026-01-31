using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 敌人血条系统：初始4点血，删除包含Joker的卡牌时扣1点血
/// 挂载到空物体（如EnemyHealthManager）上
/// </summary>
public class EnemyHealthSystem : MonoBehaviour
{
    [Header("基础血量配置")]
    [Tooltip("敌人初始血量（默认4点）")]
    public int initialHealth = 4;
    [Tooltip("当前敌人血量（只读）")]
    [SerializeField] private int currentHealth;

    [Header("血条UI引用")]
    [Tooltip("显示血量数值的文本（可选）")]
    public Text healthText;
    [Tooltip("血条填充图片（可选，需设置Fill Method为Horizontal）")]
    public Image healthFillImage;

    // 单例模式：方便其他脚本快速调用扣血方法
    public static EnemyHealthSystem Instance;

    private void Awake()
    {
        // 单例初始化（确保场景中只有一个敌人血条管理器）
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // 初始化血量
        currentHealth = initialHealth;
        // 更新血条UI显示
        UpdateHealthUI();
    }

    /// <summary>
    /// 敌人扣血方法（每次扣1点，不低于0）
    /// </summary>
    public void TakeDamage()
    {
        // 扣1点血，确保血量不小于0
        currentHealth = Mathf.Max(currentHealth - 1, 0);
        Debug.Log($"敌人扣1点血，当前血量：{currentHealth}");

        // 更新血条UI
        UpdateHealthUI();

        // 可选：血量为0时触发敌人死亡逻辑
        if (currentHealth <= 0)
        {
            OnEnemyDie();
        }
    }

    /// <summary>
    /// 更新血条UI显示
    /// </summary>
    private void UpdateHealthUI()
    {
        // 1. 更新血量文本（如果引用了文本）
        if (healthText != null)
        {
            healthText.text = $"敌人血量：{currentHealth}/{initialHealth}";
        }

        // 2. 更新血条填充图片（如果引用了图片）
        if (healthFillImage != null)
        {
            // 计算填充比例（当前血量/初始血量）
            float fillRatio = (float)currentHealth / initialHealth;
            healthFillImage.fillAmount = fillRatio;
        }
    }

    /// <summary>
    /// 敌人死亡逻辑（可选扩展）
    /// </summary>
    private void OnEnemyDie()
    {
        Debug.Log("敌人血量为0，死亡！");
        // 可添加：敌人死亡动画、游戏胜利逻辑、血条隐藏等
        // healthFillImage.gameObject.SetActive(false);
        // healthText.gameObject.SetActive(false);
    }

    /// <summary>
    /// 重置敌人血量（可选，用于重新开始游戏）
    /// </summary>
    public void ResetHealth()
    {
        currentHealth = initialHealth;
        UpdateHealthUI();
        Debug.Log("敌人血量已重置");
    }
}