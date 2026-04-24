using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;
using ChaiTea;

public class Simulation3_31 : MonoBehaviour
{
    Thread simThread;
    bool simulationRunning = false;
    bool hapticThreadStopped = true;

    [SerializeField]
    private GripperTool tool;
    [SerializeField]
    private HapticObject hapticObject;
    private ChaiTea.GenericObject chaiObj;
    [SerializeField]
    private Rigidbody unityRigidbody;

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

                tool.Tool.UpdateFromDevice();
                tool.Tool.ComputeInteractionForces();
                tool.Tool.ApplyToDevice();
            }
        }
        finally
        {
            hapticThreadStopped = true;
            tool.Tool.StopServo();
            HapticWorld.Instance.ResetWorld();
        }
    }
}