using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

/*
 * Atiya Nova 2021
 */

public class PathOSWindow : EditorWindow
{

    string[] tabLabels = { "Agent", "Manager", "Batching", "Profiles"};
    int tabSelection = 0;

    private PathOSProfileWindow profileWindow;
    private PathOSAgentBatchingWindow batchingWindow;

    [MenuItem("Window/PathOS")]
    public static void ShowWindow()
    {
        EditorWindow.GetWindow(typeof(PathOSWindow));
    }


    private void OnEnable()
    {
        //initializes the different windows
        profileWindow = new PathOSProfileWindow();
        batchingWindow = new PathOSAgentBatchingWindow();
    }
    void OnGUI()
    {
        // The actual window code goes here
        GUILayout.BeginHorizontal();
        tabSelection = GUILayout.Toolbar(tabSelection, tabLabels);
        GUILayout.EndHorizontal();

        switch (tabSelection)
        {
            case 0:

                if (GUILayout.Button("Import Profiles..."))
                {
                }
                break;
            case 1:
                if (GUILayout.Button("oop Profiles..."))
                {
                }
                break;
            case 2:
                batchingWindow.OnWindowOpen();
                break;
            case 3:
                profileWindow.OnWindowOpen();
                break;
        }

    }
}
