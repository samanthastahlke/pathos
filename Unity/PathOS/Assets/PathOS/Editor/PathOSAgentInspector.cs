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

    private SerializedProperty experienceScale;
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

    private Dictionary<Heuristic, string> heuristicLabels;

    private void OnEnable()
    {
        agent = (PathOSAgent)target;
        serial = new SerializedObject(agent);

        experienceScale = serial.FindProperty("experienceScale");
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

        heuristicLabels = new Dictionary<Heuristic, string>();

        foreach(HeuristicScale curScale in agent.heuristicScales)
        {
            string label = curScale.heuristic.ToString();

            label = label.Substring(0, 1).ToUpper() + label.Substring(1).ToLower();
            heuristicLabels.Add(curScale.heuristic, label);
        }
    }

    public override void OnInspectorGUI()
    {
        serial.Update();

        EditorGUILayout.LabelField("General", EditorStyles.boldLabel);

        EditorGUILayout.PropertyField(memoryRef);
        EditorGUILayout.PropertyField(eyeRef);

        EditorGUILayout.PropertyField(freezeAgent);
        EditorGUILayout.PropertyField(verboseDebugging);

        EditorGUILayout.LabelField("Player Characteristics", EditorStyles.boldLabel);

        EditorGUILayout.PropertyField(experienceScale);

        for(int i = 0; i < agent.heuristicScales.Count; ++i)
        {
            agent.heuristicScales[i].scale = EditorGUILayout.Slider(
                 heuristicLabels[agent.heuristicScales[i].heuristic],
                 agent.heuristicScales[i].scale, 0.0f, 1.0f);
        }

        EditorGUILayout.LabelField("Navigation", EditorStyles.boldLabel);

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
