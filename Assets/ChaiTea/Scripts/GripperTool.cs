using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class GripperTool : MonoBehaviour
{
    private ChaiTea.ToolGripper tool;
    public ChaiTea.ToolGripper Tool { get { return tool; } }


    [SerializeField]
    private int deviceIndex = 0;

    [SerializeField]
    private double toolRadius = 0.05;

    [SerializeField]
    private double workspaceRadius = 1f;

    [SerializeField]
    private GameObject thumbproxyObject;

    [SerializeField]
    private GameObject thumbhipObject;

    [SerializeField]
    private GameObject fingerproxyObject;

    [SerializeField]
    private GameObject fingerhipObject;
    [SerializeField]
    private bool showProxy = true;
    [SerializeField]
    private bool showHIP = true;

    private void Awake()
    {
        tool = new ChaiTea.ToolGripper(HapticWorld.Instance.world, deviceIndex);
        HapticWorld.Instance.world.AddChild(tool);
        tool.SetToolRadius(toolRadius);
        tool.SetWorkspaceRadius(workspaceRadius);
        tool.WaitForSmallForce(true);

    }

    private void Start()
    {
        Debug.Log("Starting Servo");
        tool.StartServo();
    }


    private void Update()
    {
        Vector3 thumbproxyPosition = tool.GetProxyPosition().thumbPosition;
        thumbproxyObject.transform.position = thumbproxyPosition;

        Vector3 thumbhipPosition = tool.GetDevicePosition().thumbPosition;
        thumbhipObject.transform.position = thumbhipPosition;

        Vector3 fingerproxyPosition = tool.GetProxyPosition().fingerPosition;
        fingerproxyObject.transform.position = fingerproxyPosition;

        Vector3 fingerhipPosition = tool.GetDevicePosition().fingerPosition;
        fingerhipObject.transform.position = fingerhipPosition;


    }


    private void OnApplicationQuit()
    {
        //tool.StopServo();
    }

    //We cannot modify objects in OnValidate, so we do a workaround.
    //Solution from here https://forum.unity.com/threads/sendmessage-cannot-be-called-during-awake-checkconsistency-or-onvalidate-can-we-suppress.537265/#post-8157614
#if UNITY_EDITOR
    private void OnValidate() => UnityEditor.EditorApplication.update += _OnValidate;
    private void _OnValidate()
    {
        UnityEditor.EditorApplication.update -= _OnValidate;
        if (this == null || !UnityEditor.EditorUtility.IsDirty(this)) return;

        thumbproxyObject.transform.localScale = new Vector3((float)toolRadius * 2, (float)toolRadius * 2, (float)toolRadius * 2);
        thumbhipObject.transform.localScale = new Vector3((float)toolRadius * 2, (float)toolRadius * 2, (float)toolRadius * 2);
        fingerproxyObject.transform.localScale = new Vector3((float)toolRadius * 2, (float)toolRadius * 2, (float)toolRadius * 2);
        fingerhipObject.transform.localScale = new Vector3((float)toolRadius * 2, (float)toolRadius * 2, (float)toolRadius * 2);

        thumbproxyObject.SetActive(showProxy);
        thumbhipObject.SetActive(showHIP);
        fingerproxyObject.SetActive(showProxy);
        fingerhipObject.SetActive(showHIP);

        UnityEditor.EditorUtility.SetDirty(thumbproxyObject);
        UnityEditor.EditorUtility.SetDirty(thumbhipObject);
        UnityEditor.EditorUtility.SetDirty(fingerproxyObject);
        UnityEditor.EditorUtility.SetDirty(fingerhipObject);
        //ModifyComponentsAndMarkThemAsDirty();
    }
#endif
}