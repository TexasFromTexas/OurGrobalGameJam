using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class DepthLayer
{
    public Transform layer;
    [Header("倍数，倍数1则移动最大为Boundary")]
    public float multiplier = 0.2f;
}

public class SmoothParallaxMouseEffect : MonoBehaviour
{
    [Header("自定义层级")]
    public List<DepthLayer> layers;

    [Header("Movement Settings")]
    public float mouseSensitivity = 0.5f;           // 鼠标敏感度
    public float smoothSpeed = 5f;                  // 平滑移动速度
    public float edgeThreshold = 0.2f;              // 边缘触发阈值 (默认20%)

    [Header("Screen Boundaries")]
    public float leftBoundary = -200f;              // 左边界
    public float rightBoundary = 200f;              // 右边界

    private List<Vector3> originalPositions;
    private List<Vector3> targetPositions;

    private void Start()
    {
        // 初始化位置列表
        originalPositions = new List<Vector3>();
        targetPositions = new List<Vector3>();

        // 保存每个层级的初始位置
        foreach (var layer in layers)
        {
            if (layer.layer != null)
            {
                originalPositions.Add(layer.layer.position);
                targetPositions.Add(layer.layer.position);
            }
            else
            {
                originalPositions.Add(Vector3.zero);
                targetPositions.Add(Vector3.zero);
            }
        }
    }

    void Update()
    {
        if (layers == null || layers.Count == 0) return;

        // 获取鼠标在屏幕坐标系的位置
        Vector3 mousePos = Input.mousePosition;

        // 计算鼠标相对于屏幕中心的位置 (-1 到 1)
        float normalizedMouseX = (mousePos.x - Screen.width / 2f) / (Screen.width / 2f);

        // 检查鼠标位置 - 使用配置的边缘阈值
        bool isMouseOnLeftSide = normalizedMouseX < -edgeThreshold;  // 鼠标在屏幕左侧边缘区域内
        bool isMouseOnRightSide = normalizedMouseX > edgeThreshold;  // 鼠标在屏幕右侧边缘区域内

        for (int i = 0; i < layers.Count; i++)
        {
            var currentLayer = layers[i];
            if (currentLayer.layer == null) continue;

            if (isMouseOnLeftSide)
            {
                // 鼠标在左侧时，图层向右移动（相反方向）
                float offset = Mathf.Abs(normalizedMouseX) * mouseSensitivity;  // 取绝对值确保正值

                // 计算目标位置（向右移动）
                Vector3 targetPos = originalPositions[i];
                targetPos.x = Mathf.Clamp(originalPositions[i].x + offset * currentLayer.multiplier,
                                          leftBoundary, rightBoundary);
                targetPositions[i] = targetPos;
            }
            else if (isMouseOnRightSide)
            {
                // 鼠标在右侧时，图层向左移动（相反方向）
                float offset = Mathf.Abs(normalizedMouseX) * mouseSensitivity;  // 取绝对值确保正值

                // 计算目标位置（向左移动）
                Vector3 targetPos = originalPositions[i];
                targetPos.x = Mathf.Clamp(originalPositions[i].x - offset * currentLayer.multiplier,
                                          leftBoundary, rightBoundary);
                targetPositions[i] = targetPos;
            }
            else
            {
                // 鼠标在中间区域时，目标位置恢复为原始位置
                targetPositions[i] = originalPositions[i];
            }

            // 平滑移动到目标位置
            currentLayer.layer.position = Vector3.Lerp(currentLayer.layer.position,
                                                      targetPositions[i],
                                                      Time.deltaTime * smoothSpeed);
        }
    }
}