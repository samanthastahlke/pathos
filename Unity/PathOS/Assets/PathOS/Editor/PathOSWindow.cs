using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

/*
 * Atiya Nova 2021
 */

public class PathOSWindow : EditorWindow
{

    string[] tabLabels = { "Agent", "Manager", "Batching" };
    int tabSelection = 0;

    [MenuItem("Window/PathOS")]
    public static void ShowWindow()
    {
        EditorWindow.GetWindow(typeof(PathOSWindow));
    }

    void OnGUI()
    {
        // The actual window code goes here
        GUILayout.BeginHorizontal();
        tabSelection = GUILayout.Toolbar(tabSelection, tabLabels);
        GUILayout.EndHorizontal();
    }
}
