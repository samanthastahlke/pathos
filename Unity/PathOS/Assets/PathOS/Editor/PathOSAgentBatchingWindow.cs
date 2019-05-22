using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class PathOSAgentBatchingWindow : EditorWindow
{
    private const string editorPrefsID = "PathOSAgentBatching";

    [SerializeField]
    private PathOSAgent agentReference;

    [SerializeField]
    private int numAgents;

    private bool simulationActive = false;
    private int agentsLeft = 0;

    [MenuItem("Window/PathOS Agent Batching")]
    public static void ShowWindow()
    {
        EditorWindow.GetWindow(typeof(PathOSAgentBatchingWindow), true, "PathOS Agent Batching");
    }

    private void OnEnable()
    {
        string prefsData = EditorPrefs.GetString(editorPrefsID, JsonUtility.ToJson(this, false));
        JsonUtility.FromJsonOverwrite(prefsData, this);

        if (null == agentReference)
            agentReference = FindObjectOfType<PathOSAgent>();
    }

    private void OnDisable()
    {
        string prefsData = JsonUtility.ToJson(this, false);
        EditorPrefs.SetString(editorPrefsID, prefsData);
    }

    private void OnGUI()
    {
        agentReference = EditorGUILayout.ObjectField("Agent Reference: ", agentReference, typeof(PathOSAgent), true)
            as PathOSAgent;

        numAgents = EditorGUILayout.IntField("Number of agents to simulate: ", numAgents);

        if(GUILayout.Button("Start"))
        {
            simulationActive = true;
            agentsLeft = numAgents;
        }

        if(GUILayout.Button("Stop"))
        {
            simulationActive = false;
            EditorApplication.isPlaying = false;
        }        
    }

    private void Update()
    {
        if (simulationActive)
        {
            if (!EditorApplication.isPlaying)
            {
                if (agentsLeft == 0)
                    simulationActive = false;
                else
                {
                    EditorApplication.isPlaying = true;
                    --agentsLeft;
                }
            }
        }
    }
}
