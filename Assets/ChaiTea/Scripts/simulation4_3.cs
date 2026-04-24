using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;
using ChaiTea;

public class Simulation4_3 : MonoBehaviour
{
    Thread simThread;
    bool simulationRunning = false;
    bool hapticThreadStopped = true;

    [SerializeField]
    private GripperTool tool; // 触觉设备工具
    [SerializeField]
    private HapticObject hapticObject; // 可交互物体
    private ChaiTea.GenericObject chaiObj; // CHAI Tea 对象
    [SerializeField]
    private Rigidbody unityRigidbody; // 物体的刚体

    private Queue<System.Action> mainThreadActions = new Queue<System.Action>();

    private void Start()
    {
        simThread = new Thread(UpdateHaptics);
        simThread.Start();
        if (hapticObject != null)
        {
            chaiObj = hapticObject.Get();
        }
        else
        {
            Debug.LogError("HapticObject is not assigned.");
        }
    }

    private void OnDestroy()
    {
        simulationRunning = false;

        // 清空主线程任务队列
        lock (mainThreadActions)
        {
            mainThreadActions.Clear();
        }

        // 等待线程结束
        int timeout = 5000; // 5秒
        while (!hapticThreadStopped && timeout > 0)
        {
            Thread.Sleep(5);
            timeout -= 5;
        }

        if (!hapticThreadStopped)
        {
            Debug.LogError("Simulation thread did not stop properly.");
        }

        simThread.Join();
    }

    private void Update()
    {
        lock (mainThreadActions)
        {
            while (mainThreadActions.Count > 0)
            {
                mainThreadActions.Dequeue().Invoke();
            }
        }
    }

    private void UpdateHaptics()
    {
        simulationRunning = true;
        hapticThreadStopped = false;
        try
        {
            while (simulationRunning)
            {
                // Compute global reference frames for each object
                HapticWorld.Instance.world.ComputeGlobalPositions(true);

                Vector3 localPosition = Vector3.zero;
                Quaternion rotation = Quaternion.identity;

                lock (mainThreadActions)
                {
                    mainThreadActions.Enqueue(() =>
                    {
                        if (unityRigidbody != null)
                        {
                            localPosition = unityRigidbody.position;
                            rotation = unityRigidbody.rotation;
                        }
                    });
                }

                while (mainThreadActions.Count > 0)
                {
                    Thread.Sleep(1);
                }

                if (chaiObj != null)
                {
                    chaiObj.SetLocalPosition(localPosition);
                    chaiObj.SetRotation(rotation);
                }

                // 更新触觉设备的状态
                tool.Tool.UpdateFromDevice();

                // 计算交互力并应用到设备
                tool.Tool.ComputeInteractionForces();
                tool.Tool.ApplyToDevice();

                // 检测接触并应用力到物体
                //ApplyForceToObjects();
            }
        }
        finally
        {
            hapticThreadStopped = true;
            tool.Tool.StopServo();
            HapticWorld.Instance.ResetWorld();
        }
    }

    /// <summary>
    /// 应用力到物体
    /// </summary>
    //private void ApplyForceToObjects()
    //{
    //    // 假设 tool.Tool 提供了两个方法：
    //    // - GetLeftGripperPosition() 返回左侧夹爪的位置
    //    // - GetRightGripperPosition() 返回右侧夹爪的位置
    //    // - GetForceMagnitude() 返回当前力的大小

    //    Vector3 leftPosition = tool.Tool.GetLeftGripperPosition();
    //    Vector3 rightPosition = tool.Tool.GetRightGripperPosition();
    //    float forceMagnitude = tool.Tool.GetForceMagnitude();

    //    // 左侧夹爪的射线检测
    //    RaycastHit hitLeft;
    //    if (Physics.Raycast(leftPosition, (rightPosition - leftPosition).normalized, out hitLeft, 0.1f))
    //    {
    //        Rigidbody rb = hitLeft.collider.GetComponent<Rigidbody>();
    //        if (rb != null)
    //        {
    //            // 计算力的方向（从左侧夹爪指向接触点）
    //            Vector3 forceDirection = (hitLeft.point - leftPosition).normalized;
    //            rb.AddForceAtPosition(forceDirection * forceMagnitude, hitLeft.point, ForceMode.Force);
    //            Debug.Log($"Applied force {forceDirection * forceMagnitude} at position {hitLeft.point}.");
    //        }
    //    }

    //    // 右侧夹爪的射线检测
    //    RaycastHit hitRight;
    //    if (Physics.Raycast(rightPosition, (leftPosition - rightPosition).normalized, out hitRight, 0.1f))
    //    {
    //        Rigidbody rb = hitRight.collider.GetComponent<Rigidbody>();
    //        if (rb != null)
    //        {
    //            // 计算力的方向（从右侧夹爪指向接触点）
    //            Vector3 forceDirection = (hitRight.point - rightPosition).normalized;
    //            rb.AddForceAtPosition(forceDirection * forceMagnitude, hitRight.point, ForceMode.Force);
    //            Debug.Log($"Applied force {forceDirection * forceMagnitude} at position {hitRight.point}.");
    //        }
    //    }
    //}
}