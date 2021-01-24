using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

/*
PathOSWindow.cs 
(Atiya Nova) 2021
 */

public class PathOSWindow : EditorWindow
{

    string[] tabLabels = { "Agent", "Manager", "Visualization", "Batching", "Profiles"};
    int tabSelection = 0;

    private PathOSProfileWindow profileWindow;
    private PathOSAgentBatchingWindow batchingWindow;
    private PathOSAgentWindow agentWindow;
    private PathOSManagerWindow managerWindow;

    private GameObject proxyAgent, proxyManager;
    private Vector2 scrollPos = Vector2.zero;

    [MenuItem("Window/PathOS")]
    public static void ShowWindow()
    {
        EditorWindow.GetWindow(typeof(PathOSWindow), false, "PathOS");
    }


    private void OnEnable()
    {
        //initializes the different windows
        profileWindow = new PathOSProfileWindow();
        batchingWindow = new PathOSAgentBatchingWindow();
        agentWindow = new PathOSAgentWindow();
        managerWindow = new PathOSManagerWindow();
    }

    //gizmo stuff from here https://stackoverflow.com/questions/37267021/unity-editor-script-visible-hidden-gizmos


    void OnGUI()
    {
        scrollPos = GUILayout.BeginScrollView(scrollPos, true, true);
        //warning incase gizmos aren't enabled
        EditorGUILayout.HelpBox("Please make sure to have Gizmos enabled", MessageType.Warning);

        //to instantiate the AI
        if (GUILayout.Button("Instantiate PathOS Agent and Manager"))
        {
            proxyAgent = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/PathOS/Prefabs/PathOS Agent.prefab") as GameObject;
            Instantiate(proxyAgent);

            proxyManager = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/PathOS/Prefabs/PathOS Manager.prefab") as GameObject;
            Instantiate(proxyManager);
        }

        // The tabs to alternate between specific menus
        GUILayout.BeginHorizontal();
        tabSelection = GUILayout.Toolbar(tabSelection, tabLabels);
        GUILayout.EndHorizontal();

        switch (tabSelection)
        {
            case 0:
                agentWindow.OnWindowOpen();
                break;
            case 1:
                managerWindow.OnWindowOpen();
                break;
            case 2:
                managerWindow.OnVisualizationOpen();
                break;
            case 3:
                batchingWindow.OnWindowOpen();
                break;
            case 4:
                profileWindow.OnWindowOpen();
                break;
        }
        GUILayout.EndScrollView();

    }
}
