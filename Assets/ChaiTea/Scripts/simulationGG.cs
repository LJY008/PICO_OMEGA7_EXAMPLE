using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;
using ChaiTea;

public class SimulationGG : MonoBehaviour
{
    private Thread simThread;
    private bool simulationRunning = false;
    private bool hapticThreadStopped = true;

    [SerializeField]
    private GripperTool tool;

    // 用于主线程执行的任务队列
    private readonly Queue<System.Action> mainThreadActions = new Queue<System.Action>();

    // 缓存 ODEWorld 实例，避免在后台线程中访问 Unity API
    private ODEWorld odeWorldInstance;

    // 假设我们有一个Chai3d的对象实例
    [SerializeField]
    private GenericObject chai3dObject;

    // 对应的Unity GameObject
    [SerializeField]
    private GameObject unityObject;

    [SerializeField]
    private ODEGenericBody Object;

    private void Start()
    {
        Debug.Log("SimulationGG: Starting simulation...");

        // 在主线程中缓存 ODEWorld 实例
        odeWorldInstance = ODEWorld.Instance;
        if (odeWorldInstance == null)
        {
            Debug.LogError("SimulationGG: Failed to get ODEWorld instance.");
            return;
        }
        Debug.Log("SimulationGG: Successfully cached ODEWorld instance.");

        // 启动模拟线程
        simThread = new Thread(UpdateHaptics);
        simThread.Start();
        Debug.Log("SimulationGG: Simulation thread started.");
    }

    private void OnDestroy()
    {
        Debug.Log("SimulationGG: Stopping simulation...");

        // 停止模拟线程
        simulationRunning = false;

        // 清空主线程任务队列
        lock (mainThreadActions)
        {
            mainThreadActions.Clear();
            Debug.Log("SimulationGG: Cleared main thread action queue.");
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
            Debug.LogError("SimulationGG: Simulation thread did not stop properly.");
        }
        else
        {
            Debug.Log("SimulationGG: Simulation thread stopped successfully.");
        }

        simThread.Join();
        Debug.Log("SimulationGG: Simulation thread joined.");
    }

    private void Update()
    {
        // 执行主线程中的任务队列
        lock (mainThreadActions)
        {
            while (mainThreadActions.Count > 0)
            {
                try
                {
                    mainThreadActions.Dequeue().Invoke();
                }
                catch (Exception e)
                {
                    Debug.LogError($"SimulationGG: Error executing main thread action: {e.Message}");
                }
            }
        }
    }

    private void UpdateHaptics()
    {
        Debug.Log("SimulationGG: Entering UpdateHaptics loop...");
        simulationRunning = true;
        hapticThreadStopped = false;
        odeclock clock = new odeclock();
        clock.Reset();

        try
        {
            while (simulationRunning)
            {
                Debug.Log("SimulationGG: Starting a new simulation step...");

                // 停止模拟时钟
                clock.Stop();
                Debug.Log("SimulationGG: Stopped simulation clock.");

                // 获取时间间隔（秒）
                double timeInterval = Mathf.Clamp((float)clock.GetCurrentTimeSeconds(), 0.0001f, 0.001f);
                Debug.Log($"SimulationGG: Time interval for this step: {timeInterval} seconds.");

                // 重启模拟时钟
                clock.Reset();
                clock.Start();
                Debug.Log("SimulationGG: Restarted simulation clock.");

                

                // 计算全局参考帧
                EnqueueMainThreadAction(() =>
                {
                    HapticWorld.Instance.world.ComputeGlobalPositions(true);
                    Debug.Log("SimulationGG: Computed global positions in HapticWorld.");
                });

                // 更新工具的位置和方向
                tool.Tool.UpdateFromDevice();
                Debug.Log("SimulationGG: Updated tool position and orientation from device.");

                // 计算交互力
                tool.Tool.ComputeInteractionForces();
                Debug.Log("SimulationGG: Computed interaction forces.");

                // 将力发送到触觉设备
                tool.Tool.ApplyToDevice();
                Debug.Log("SimulationGG: Applied forces to the haptic device.");

                // 更新物理模拟
                if (odeWorldInstance != null)
                {
                    EnqueueMainThreadAction(() =>
                    {
                        odeWorldInstance.odeworld.UpdateDynamics(timeInterval);
                        Debug.Log("SimulationGG: Updated physics dynamics with time interval.");
                    });
                }

                // 获取Chai3d对象的信息并同步到Unity
                if (chai3dObject != null && unityObject != null)
                {
                    GetChai3dObjectInfoAndSyncToUnity(chai3dObject, unityObject);
                    Debug.Log("SimulationGG: Synchronized Chai3d object info to Unity.");
                }

                Debug.Log("SimulationGG: Completed one simulation step.");
            }
        }
        finally
        {
            hapticThreadStopped = true;
            tool.Tool.StopServo();
            Debug.Log("SimulationGG: Stopped servo on the tool.");

            // 重置世界状态
            EnqueueMainThreadAction(() =>
            {
                HapticWorld.Instance.ResetWorld();
                Debug.Log("SimulationGG: Reset HapticWorld state.");
            });
        }
    }

    /// <summary>
    /// 将任务添加到主线程执行队列
    /// </summary>
    /// <param name="action">需要在主线程中执行的任务</param>
    private void EnqueueMainThreadAction(System.Action action)
    {
        lock (mainThreadActions)
        {
            mainThreadActions.Enqueue(action);
            Debug.Log("SimulationGG: Enqueued an action to the main thread.");
        }
    }

    /// <summary>
    /// 获取Chai3d对象的信息并同步到Unity
    /// </summary>
    /// <param name="chai3dObj">Chai3d对象</param>
    /// <param name="unityObj">对应的Unity对象</param>
    private void GetChai3dObjectInfoAndSyncToUnity(GenericObject chai3dObj, GameObject unityObj)
    {
        // 获取Chai3d对象的位置和旋转
        Vector3 position = chai3dObj.GetLocalPosition();
        Quaternion rotation = chai3dObj.GetRotation();
        Debug.Log($"SimulationGG: Retrieved Chai3d object position: {position}, rotation: {rotation}.");

        // 将这些值封装成一个动作并在主线程中执行
        EnqueueMainThreadAction(() =>
        {
            unityObj.transform.position = position;
            unityObj.transform.rotation = rotation;
            Debug.Log($"SimulationGG: Updated Unity object position to {position} and rotation to {rotation}.");
        });
    }
}