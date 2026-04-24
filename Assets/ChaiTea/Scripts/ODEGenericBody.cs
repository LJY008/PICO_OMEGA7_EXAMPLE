using ChaiTea;
using UnityEngine;

public class ODEGenericBody : MonoBehaviour
{
    private ChaiTea.ODEgenericbody body;
    public ChaiTea.ODEgenericbody Body { get { return body; } }

    private ODEWorld odeWorld; // 引用 ODEWorld 实例
    private HapticBox hapticBox;

    [SerializeField]
    double Mass = 0.01;

    private void Awake()
    {
        // 如果未通过 Inspector 赋值，则尝试自动查找 ODEWorld 实例
        if (odeWorld == null)
        {
            odeWorld = FindObjectOfType<ODEWorld>();
            if (odeWorld == null)
            {
                Debug.LogError("ODEWorld instance not found in the scene.");
                return;
            }
        }

        // 创建 ODEGenericBody 并绑定到 ODEWorld
        body = new ChaiTea.ODEgenericbody(odeWorld.GetODEworld());
        if (body == null || !body.Initialized)
        {
            Debug.LogError("Failed to initialize ODEgenericbody.");
            return;
        }

        Debug.Log("ODEGenericBody has been loaded and initialized.");

        // 获取 HapticBox 组件并获取 obj
        hapticBox = GetComponent<HapticBox>();
        if (hapticBox == null)
        {
            Debug.LogError("HapticBox component not found on this GameObject.");
            return;
        }

        // 获取 HapticBox 的 ShapeBox 对象并设置到 ODEGenericBody
        ChaiTea.ShapeBox shapeBox = hapticBox.Get();
        if (shapeBox == null)
        {
            Debug.LogError("Failed to retrieve ShapeBox from HapticBox.");
            return;
        }

        // 设置 HapticBox 的形状到 ODEGenericBody
        body.SetImageModel(shapeBox);

        // 创建动态盒子
        body.CreateDynamicBox(
            this.transform.localScale.x,
            this.transform.localScale.y,
            this.transform.localScale.z
        );

        // 设置质量
        body.SetMass(Mass);
        body.EnableDynamic();
        //Vector3 planePosition = new Vector3(0, 0, 0);
        //// 设置局部位置
        ////body.ODESetLocalPosition(planePosition
        //////this.transform.localPosition
        //////this.transform.localPosition.x,
        //////this.transform.localPosition.y,
        //////this.transform.localPosition.z
        ////);
        ////SyncPositionToUnity();
        //Vec3 planeNormal = new Vec3(0, 1, 0);
        //body.CreateStaticPlane(planePosition, planeNormal);
    }

    /// <summary>
    /// 将 ODE 物体的位置同步到 Unity 的 Transform
    /// </summary>
    //private void SyncPositionToUnity()
    //{
    //    Vec3 localPosition = body.GetLocalPosition(); // 假设 GetLocalPosition 返回 Vec3 类型
    //    transform.localPosition = new Vector3((float)localPosition.x, (float)localPosition.y, (float)localPosition.z);
    //    Debug.Log($"Synchronized position to Unity: {transform.localPosition}");
    //}
}



