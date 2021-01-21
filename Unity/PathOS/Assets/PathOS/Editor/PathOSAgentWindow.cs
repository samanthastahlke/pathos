using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class PathOSAgentWindow : EditorWindow
{
    /* Basic Settings */
    [SerializeField]
    private PathOSAgent agentReference;

    private void OnEnable()
    {

    }

    public void OnWindowOpen()
    {

        agentReference = EditorGUILayout.ObjectField("Agent Reference: ", agentReference, typeof(PathOSAgent), true)
            as PathOSAgent;

    }
}
