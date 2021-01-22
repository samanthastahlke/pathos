using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class PathOSAgentWindow : EditorWindow
{
    /* Basic Settings */
    [SerializeField]
    private PathOSAgent agentReference;
    private PathOSAgentMemory memoryReference;
    private PathOSAgentEyes eyeReference;
    private PathOSAgentRenderer rendererReference;

    private Editor currentTransformEditor, currentAgentEditor, currentMemoryEditor, currentEyeEditor, currentRendererEditor;

    private void OnEnable()
    {

    }

    public void OnWindowOpen()
    {

        agentReference = EditorGUILayout.ObjectField("Agent Reference: ", agentReference, typeof(PathOSAgent), true)
            as PathOSAgent;

        if (agentReference != null)
        {
            memoryReference = agentReference.GetComponent<PathOSAgentMemory>();
            eyeReference = agentReference.GetComponent<PathOSAgentEyes>();
            rendererReference = agentReference.GetComponent<PathOSAgentRenderer>();

            EditorGUILayout.Space();
            Editor agentEditor = Editor.CreateEditor(agentReference);
            Editor memoryEditor = Editor.CreateEditor(memoryReference);
            Editor eyeEditor = Editor.CreateEditor(eyeReference);
            Editor rendererEditor = Editor.CreateEditor(rendererReference);
            Editor transformEditor = Editor.CreateEditor(agentReference.gameObject.transform);

            // If there isn't a Transform currently selected then destroy the existing editor
            if (currentAgentEditor != null)
            {
                DestroyImmediate(currentAgentEditor);
            }

            if (currentMemoryEditor != null)
            {
                DestroyImmediate(currentMemoryEditor);
            }

            if (currentEyeEditor != null)
            {
                DestroyImmediate(currentEyeEditor);
            }

            if (currentRendererEditor != null)
            {
                DestroyImmediate(currentRendererEditor);
            }

            if (currentTransformEditor != null)
            {
                DestroyImmediate(currentTransformEditor);
            }

            currentAgentEditor = agentEditor;
            currentMemoryEditor = memoryEditor;
            currentEyeEditor = eyeEditor;
            currentRendererEditor = rendererEditor;
            currentTransformEditor = transformEditor;

            // Shows the created Editor beneath CustomEditor
            currentTransformEditor.OnInspectorGUI();
            currentAgentEditor.OnInspectorGUI();
            currentMemoryEditor.OnInspectorGUI();
            currentEyeEditor.OnInspectorGUI();
            currentRendererEditor.OnInspectorGUI();

        }
    }
}
