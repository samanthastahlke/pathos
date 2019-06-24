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

    private GUIStyle foldoutStyle = GUIStyle.none;
    private GUIStyle boldStyle = GUIStyle.none;

    private SerializedProperty experienceScale;
    private SerializedProperty heuristicList;

    private bool showPlayerCharacteristics = true;

    private SerializedProperty freezeAgent;

    private bool showNavCharacteristics = false;

    private SerializedProperty exploreDegrees;
    private SerializedProperty invisibleExploreDegrees;
    private SerializedProperty lookDegrees;
    private SerializedProperty visitThreshold;
    private SerializedProperty exploreThreshold;
    private SerializedProperty exploreTargetMargin;

    private Dictionary<Heuristic, string> heuristicLabels;

    private List<string> profileNames = new List<string>();
    private int profileIndex = 0;

    private void OnEnable()
    {
        agent = (PathOSAgent)target;
        serial = new SerializedObject(agent);

        experienceScale = serial.FindProperty("experienceScale");
        heuristicList = serial.FindProperty("heuristicScales");

        freezeAgent = serial.FindProperty("freezeAgent");

        exploreDegrees = serial.FindProperty("exploreDegrees");
        invisibleExploreDegrees = serial.FindProperty("invisibleExploreDegrees");
        lookDegrees = serial.FindProperty("lookDegrees");
        visitThreshold = serial.FindProperty("visitThreshold");
        exploreThreshold = serial.FindProperty("exploreThreshold");
        exploreTargetMargin = serial.FindProperty("exploreTargetMargin");

        agent.RefreshHeuristicList();

        heuristicLabels = new Dictionary<Heuristic, string>();

        foreach(HeuristicScale curScale in agent.heuristicScales)
        {
            string label = curScale.heuristic.ToString();

            label = label.Substring(0, 1).ToUpper() + label.Substring(1).ToLower();
            heuristicLabels.Add(curScale.heuristic, label);
        }

        if(null == PathOSProfileWindow.profiles)
            PathOSProfileWindow.profiles = PathOSProfileWindow.ReadPrefsData();
    }

    public override void OnInspectorGUI()
    {
        serial.Update();

        //Placed here since Unity seems to have issues with having these 
        //styles initialized on enable sometimes.
        foldoutStyle = EditorStyles.foldout;
        foldoutStyle.fontStyle = FontStyle.Bold;

        EditorGUILayout.LabelField("General", EditorStyles.boldLabel);

        EditorGUILayout.PropertyField(freezeAgent);

        showPlayerCharacteristics = EditorGUILayout.Foldout(
            showPlayerCharacteristics, "Player Characteristics", foldoutStyle);
       
        if(showPlayerCharacteristics)
        {
            EditorGUILayout.PropertyField(experienceScale);

            for (int i = 0; i < agent.heuristicScales.Count; ++i)
            {
                agent.heuristicScales[i].scale = EditorGUILayout.Slider(
                     heuristicLabels[agent.heuristicScales[i].heuristic],
                     agent.heuristicScales[i].scale, 0.0f, 1.0f);
            }

            boldStyle = EditorStyles.boldLabel;
            EditorGUILayout.LabelField("Load Values from Profile", boldStyle);

            profileNames.Clear();

            if (null == PathOSProfileWindow.profiles)
                PathOSProfileWindow.profiles = PathOSProfileWindow.ReadPrefsData();

            for(int i = 0; i < PathOSProfileWindow.profiles.Count; ++i)
            {
                profileNames.Add(PathOSProfileWindow.profiles[i].name);
            }

            if (profileNames.Count == 0)
                profileNames.Add("--");

            EditorGUILayout.BeginHorizontal();

            profileIndex = EditorGUILayout.Popup(profileIndex, profileNames.ToArray());

            if(GUILayout.Button("Apply Profile") 
                && profileIndex < PathOSProfileWindow.profiles.Count)
            {
                AgentProfile profile = PathOSProfileWindow.profiles[profileIndex];

                Dictionary<Heuristic, HeuristicRange> ranges = new Dictionary<Heuristic, HeuristicRange>();

                for(int i = 0; i < profile.heuristicRanges.Count; ++i)
                {
                    ranges.Add(profile.heuristicRanges[i].heuristic,
                        profile.heuristicRanges[i]);
                }

                Undo.RecordObject(agent, "Apply Agent Profile");
                for(int i = 0; i < agent.heuristicScales.Count; ++i)
                {
                    if(ranges.ContainsKey(agent.heuristicScales[i].heuristic))
                    {
                        HeuristicRange hr = ranges[agent.heuristicScales[i].heuristic];
                        agent.heuristicScales[i].scale = Random.Range(hr.range.min, hr.range.max);
                    }
                }

                agent.experienceScale = Random.Range(profile.expRange.min, profile.expRange.max);
            }

            EditorGUILayout.EndHorizontal();
        }

        showNavCharacteristics = EditorGUILayout.Foldout(
            showNavCharacteristics, "Navigation", foldoutStyle);

        if(showNavCharacteristics)
        {
            EditorGUILayout.PropertyField(exploreDegrees);
            EditorGUILayout.PropertyField(invisibleExploreDegrees);
            EditorGUILayout.PropertyField(lookDegrees);
            EditorGUILayout.PropertyField(visitThreshold);
            EditorGUILayout.PropertyField(exploreThreshold);
            EditorGUILayout.PropertyField(exploreTargetMargin);
        }
        
        serial.ApplyModifiedProperties();

        if (GUI.changed && !EditorApplication.isPlaying)
        {
            EditorUtility.SetDirty(agent);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        }
    }
}
