using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class PathOSManagerWindow : EditorWindow
{

    /* Basic Settings */
    [SerializeField]
    private PathOSManager managerReference;

    private void OnEnable()
    {

    }

    public void OnWindowOpen()
    {

        managerReference = EditorGUILayout.ObjectField("Manager Reference: ", managerReference, typeof(PathOSManager), true)
            as PathOSManager;

    }
}
