using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;
using ChaiTea;

public class SimulationG : MonoBehaviour
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

    private void Start()
    {
        // 在主线程中缓存 ODEWorld 实例
        odeWorldInstance = ODEWorld.Instance;

        // 启动模拟线程
        simThread = new Thread(UpdateHaptics);
        simThread.Start();
    }

    private void OnDestroy()
    {
        // 停止模拟线程
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
        // 执行主线程中的任务队列
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
        odeclock clock = new odeclock();
        clock.Reset();

        try
        {
            while (simulationRunning)
            {
                // 停止模拟时钟
                clock.Stop();

                // 获取时间间隔（秒）
                double timeInterval = Mathf.Clamp((float)clock.GetCurrentTimeSeconds(), 0.0001f, 0.001f);

                // 重启模拟时钟
                clock.Reset();
                clock.Start();

                // 计算全局参考帧
                EnqueueMainThreadAction(() => HapticWorld.Instance.world.ComputeGlobalPositions(true));

                // 更新工具的位置和方向
                tool.Tool.UpdateFromDevice();

                // 计算交互力
                tool.Tool.ComputeInteractionForces();

                // 将力发送到触觉设备
                tool.Tool.ApplyToDevice();

                // 更新物理模拟
                if (odeWorldInstance != null)
                {
                    EnqueueMainThreadAction(() => odeWorldInstance.odeworld.UpdateDynamics(timeInterval));
                }
            }
        }
        finally
        {
            hapticThreadStopped = true;
            tool.Tool.StopServo();

            // 重置世界状态
            EnqueueMainThreadAction(() => HapticWorld.Instance.ResetWorld());
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
        }
    }
}