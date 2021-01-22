using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class PathOSManagerWindow : EditorWindow
{

    /* Basic Settings */
    [SerializeField]
    private PathOSManager managerReference;
    private OGLogManager logManagerReference;
    private OGLogVisualizer logVisualizerReference;
    private Editor currentManagerEditor, currentLogEditor, currentVisEditor;
    private void OnEnable()
    {

    }

    public void OnWindowOpen()
    {
        managerReference = EditorGUILayout.ObjectField("Manager Reference: ", managerReference, typeof(PathOSManager), true)
            as PathOSManager;

        if (managerReference != null)
        {
            EditorGUILayout.Space();

            Editor managerEditor = Editor.CreateEditor(managerReference);

            if (currentManagerEditor != null)
            {
                DestroyImmediate(currentManagerEditor);
            }

            currentManagerEditor = managerEditor;

            // Shows the created Editor beneath CustomEditor
            currentManagerEditor.OnInspectorGUI();
        }
    }

    public void OnVisualizationOpen()
    {
        managerReference = EditorGUILayout.ObjectField("Manager Reference: ", managerReference, typeof(PathOSManager), true)
            as PathOSManager;

         if (managerReference != null)
         {
            logManagerReference = managerReference.GetComponent<OGLogManager>();
            logVisualizerReference = managerReference.GetComponent<OGLogVisualizer>();

            EditorGUILayout.Space();

            Editor managerEditor = Editor.CreateEditor(logManagerReference);
            Editor visualizerEditor = Editor.CreateEditor(logVisualizerReference);

            if (currentLogEditor != null)
            {
                DestroyImmediate(currentLogEditor);
            }

            if (currentVisEditor != null)
            {
                DestroyImmediate(currentVisEditor);
            }


            currentLogEditor = managerEditor;
            currentVisEditor = visualizerEditor;

            // Shows the created Editor beneath CustomEditor
            currentLogEditor.OnInspectorGUI();
            currentVisEditor.OnInspectorGUI();
        }
    }
}
