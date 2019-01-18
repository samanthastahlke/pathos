using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using PathOS;

/*
PathOSAgentInspector.cs 
PathOSAgentInspector (c) Nine Penguins (Samantha Stahlke) 2018
*/

[CustomEditor(typeof(PathOSAgent))]
public class PathOSAgentInspector : Editor
{
    private PathOSAgent agent;
    private SerializedObject serial;

    private SerializedProperty heuristicList;

    private SerializedProperty memoryRef;
    private SerializedProperty eyeRef;

    private SerializedProperty freezeAgent;
    private SerializedProperty verboseDebugging;

    private SerializedProperty routeComputeTime;
    private SerializedProperty perceptionComputeTime;
    private SerializedProperty exploreDegrees;
    private SerializedProperty invisibleExploreDegrees;
    private SerializedProperty lookDegrees;
    private SerializedProperty lookTime;
    private SerializedProperty visitThreshold;
    private SerializedProperty exploreSimilarityThreshold;
    private SerializedProperty forgetTime;

    private void OnEnable()
    {
        agent = (PathOSAgent)target;
        serial = new SerializedObject(agent);

        heuristicList = serial.FindProperty("heuristicScales");

        memoryRef = serial.FindProperty("memory");
        eyeRef = serial.FindProperty("eyes");

        freezeAgent = serial.FindProperty("freezeAgent");
        verboseDebugging = serial.FindProperty("verboseDebugging");

        routeComputeTime = serial.FindProperty("routeComputeTime");
        perceptionComputeTime = serial.FindProperty("perceptionComputeTime");
        exploreDegrees = serial.FindProperty("exploreDegrees");
        invisibleExploreDegrees = serial.FindProperty("invisibleExploreDegrees");
        lookDegrees = serial.FindProperty("lookDegrees");
        lookTime = serial.FindProperty("lookTime");
        visitThreshold = serial.FindProperty("visitThreshold");
        exploreSimilarityThreshold = serial.FindProperty("exploreSimilarityThreshold");
        forgetTime = serial.FindProperty("forgetTime");

        agent.RefreshHeuristicList();
    }

    public override void OnInspectorGUI()
    {
        serial.Update();

        EditorGUILayout.PropertyField(memoryRef);
        EditorGUILayout.PropertyField(eyeRef);

        EditorGUILayout.PropertyField(freezeAgent);
        EditorGUILayout.PropertyField(verboseDebugging);

        for(int i = 0; i < agent.heuristicScales.Count; ++i)
        {
            agent.heuristicScales[i].scale = EditorGUILayout.Slider(
                 agent.heuristicScales[i].heuristic.ToString(),
                 agent.heuristicScales[i].scale, 0.0f, 1.0f);
        }

        EditorGUILayout.PropertyField(routeComputeTime);
        EditorGUILayout.PropertyField(perceptionComputeTime);
        EditorGUILayout.PropertyField(exploreDegrees);
        EditorGUILayout.PropertyField(invisibleExploreDegrees);
        EditorGUILayout.PropertyField(lookDegrees);
        EditorGUILayout.PropertyField(lookTime);
        EditorGUILayout.PropertyField(visitThreshold);
        EditorGUILayout.PropertyField(exploreSimilarityThreshold);
        EditorGUILayout.PropertyField(forgetTime);

        serial.ApplyModifiedProperties();

        if (GUI.changed)
        {
            EditorUtility.SetDirty(agent);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        }
    }
}
