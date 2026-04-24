using ChaiTea;
using System;
using UnityEngine;

public class ODEWorld : MonoBehaviour
{
    // 单例实例
    private static ODEWorld instance;

    // 全局访问点
    public static ODEWorld Instance
    {
        get
        {
            if (instance == null)
            {
                // 查找场景中已存在的 ODEWorld 实例
                instance = FindObjectOfType<ODEWorld>();

                // 如果没有找到，则创建一个新的实例
                if (instance == null)
                {
                    GameObject obj = new GameObject("ODEWorld");
                    instance = obj.AddComponent<ODEWorld>();
                    Debug.Log("Created a new ODEWorld instance.");
                }
                else
                {
                    Debug.Log("Found existing ODEWorld instance.");
                }
            }
            return instance;
        }
    }

    public ChaiTea.ODEworld odeworld;

    [SerializeField]
    private Vector3 gravity = new Vector3(0, -9.81f, 0); // 默认重力值

    [SerializeField]
    double linearDamping = 0.01;

    [SerializeField]
    double angularDamping = 0.01;

    private void Awake()
    {
        // 确保只有一个实例存在
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject); // 防止场景切换时销毁
            Debug.Log("ODEWorld instance initialized in Awake.");
        }
        else if (instance != this)
        {
            Destroy(gameObject);
            return;
        }

        // 初始化 ODEWorld 对象
        odeworld = new ChaiTea.ODEworld(HapticWorld.Instance.world);
        HapticWorld.Instance.world.AddChild(odeworld);
        odeworld.SetGravity(gravity);
        odeworld.SetLinearDamping(linearDamping);
        odeworld.SetAngularDamping(angularDamping);

        Debug.Log("ODEWorld has been loaded and initialized.");
    }

    public ChaiTea.ODEworld GetODEworld()
    {
        return odeworld;
    }
}