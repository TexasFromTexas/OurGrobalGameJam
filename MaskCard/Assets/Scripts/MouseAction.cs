using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class MouseAction : MonoBehaviour
{
    public GameObject canvas_ButArea;

    /// <summary>
    /// 鼠标点击物体并拖动，直到在遮罩内鼠标再次触发，触发finalAct
    /// </summary>
    /// <param name="obj"></param>
    /// <param name="finalAct"></param>
    /// <returns></returns>
    public IEnumerator BeginUseMask(GameObject obj, Action finalAct)
    {
        // 验证输入参数
        if (obj == null)
        {
            Debug.LogWarning("MouseAction: obj is null, cannot proceed with BeginUseSwapMask");
            yield break;
        }

        if (canvas_ButArea == null)
        {
            Debug.LogError("MouseAction: canvas_ButArea is not assigned!");
            yield break;
        }

        yield return null;

        // 复制一个obj的物体，并让他的位置与鼠标的位置一致（Z轴不变）
        Vector3 mousePosition = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, obj.transform.position.z));
        GameObject clonedObj = Instantiate(obj, mousePosition, obj.transform.rotation);

        // 设置克隆对象的父对象为当前脚本所在的游戏对象，便于统一管理
        clonedObj.transform.SetParent(obj.transform);
        clonedObj.transform.localScale = Vector3.one;

        bool isAtAreaAndClic = false;


        float screenHeightPercentage = Input.mousePosition.y / Screen.height;

        GameObject instantiatedCanvas = Instantiate(canvas_ButArea); //显示触发区域

        while (!isAtAreaAndClic)
        {
            //同步面具位置
            mousePosition = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, obj.transform.position.z));
            clonedObj.transform.position = mousePosition;


            screenHeightPercentage = Input.mousePosition.y / Screen.height;


            if (screenHeightPercentage <= 0.3f && Input.GetMouseButtonDown(0))
            {
                // 执行最终动作
                finalAct?.Invoke();

                // 清理实例化的对象
                if (clonedObj != null)
                {
                    Destroy(clonedObj);
                }
                if (instantiatedCanvas != null)
                {
                    Destroy(instantiatedCanvas);
                }
                isAtAreaAndClic = true;
                yield break;
            }
            yield return null;
        }
    }
}