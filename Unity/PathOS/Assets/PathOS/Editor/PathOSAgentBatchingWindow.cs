using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class PathOSAgentBatchingWindow : EditorWindow
{
    //Used to identify preferences string by Unity.
    private const string editorPrefsID = "PathOSAgentBatching";

    /* Style Constraints */
    private const float shortLabelWidth = 24.0f;
    private const float shortFloatfieldWidth = 40.0f;

    private const int filepathLength = 32;

    /* Basic Settings */
    [SerializeField]
    private PathOSAgent agentReference;

    [SerializeField]
    private bool hasAgent;

    [SerializeField]
    private int agentID;

    [SerializeField]
    private int numAgents;

    //TODO: Simulating multiple agents simultaneously.
    [SerializeField]
    private bool simultaneous = false;

    //Max number of agents to be simulated simultaneously.
    private const int MAX_AGENTS_SIMULTANEOUS = 8;

    [SerializeField]
    private float timeScale = 1.0f;

    public enum HeuristicMode
    {
        FIXED = 0,
        RANGE,
        LOAD
    };

    private string[] heuristicModeLabels =
    {
        "Fixed Values",
        "Random Within Range",
        "Load from File"
    };

    /* Behaviour Customization */
    [SerializeField]
    private HeuristicMode heuristicMode;

    private Dictionary<PathOS.Heuristic, string> heuristicLabels =
        new Dictionary<PathOS.Heuristic, string>();

    [SerializeField]
    private List<PathOS.HeuristicScale> fixedHeuristics =
        new List<PathOS.HeuristicScale>();

    private Dictionary<PathOS.Heuristic, float> fixedLookup =
        new Dictionary<PathOS.Heuristic, float>();

    [SerializeField]
    private float fixedExp;

    [SerializeField]
    private List<PathOS.HeuristicRange> rangeHeuristics =
        new List<PathOS.HeuristicRange>();

    private Dictionary<PathOS.Heuristic, PathOS.FloatRange> rangeLookup =
        new Dictionary<PathOS.Heuristic, PathOS.FloatRange>();

    [SerializeField]
    private PathOS.FloatRange rangeExp;

    //TODO: Implementation for loading a series of agents from a file.
    [SerializeField]
    private string loadHeuristicsFile;

    [SerializeField]
    private string shortHeuristicsFile;

    /* Simulation Controls */
    private bool simulationActive = false;
    private bool triggerFrame = false;
    private bool previousPlaystate = false;
    private int agentsLeft = 0;

    [MenuItem("Window/PathOS Agent Batching")]
    public static void ShowWindow()
    {
        EditorWindow window = EditorWindow.GetWindow(typeof(PathOSAgentBatchingWindow), true, 
            "PathOS Agent Batching");

        window.minSize = new Vector2(420.0f, 690.0f);
    }

    private void OnEnable()
    {
        //Load saved settings.
        string prefsData = EditorPrefs.GetString(editorPrefsID, JsonUtility.ToJson(this, false));
        JsonUtility.FromJsonOverwrite(prefsData, this);

        //Re-establish agent reference, if it has been nullified.
        //This can happen when switching into Playmode.
        //Otherwise, re-grab the agent's instance ID.
        if (hasAgent)
        {
            if(agentReference != null)
                agentID = agentReference.GetInstanceID();
            else
                agentReference = EditorUtility.InstanceIDToObject(agentID) as PathOSAgent;
        }

        hasAgent = agentReference != null;

        //Build the heuristic lookups.
        foreach (PathOS.Heuristic heuristic in 
            System.Enum.GetValues(typeof(PathOS.Heuristic)))
        {
            fixedLookup.Add(heuristic, 0.0f);
            rangeLookup.Add(heuristic, new PathOS.FloatRange { min = 0.0f, max = 1.0f });
        }

        System.Array heuristics = System.Enum.GetValues(typeof(PathOS.Heuristic));

        //Check that we have the correct number of heuristics.
        //(Included to future-proof against changes to the list).
        if (fixedHeuristics.Count != heuristics.Length)
        {
            fixedHeuristics.Clear();
            foreach(PathOS.Heuristic heuristic in heuristics)
            {
                fixedHeuristics.Add(new PathOS.HeuristicScale(heuristic, 0.0f));
            }
        }

        if (rangeHeuristics.Count != heuristics.Length)
        {
            rangeHeuristics.Clear();
            foreach(PathOS.Heuristic heuristic in heuristics)
            {
                rangeHeuristics.Add(new PathOS.HeuristicRange(heuristic));
            }
        }

        foreach (PathOS.Heuristic heuristic in heuristics)
        {
            string label = heuristic.ToString();

            label = label.Substring(0, 1).ToUpper() + label.Substring(1).ToLower();
            heuristicLabels.Add(heuristic, label);
        }

        Repaint();
    }

    private void OnDisable()
    {
        //Save settings to the editor.
        string prefsData = JsonUtility.ToJson(this, false);
        EditorPrefs.SetString(editorPrefsID, prefsData);     
    }

    private void OnDestroy()
    {
        //Reset the timescale.
        Time.timeScale = 1.0f;
    }

    private void OnGUI()
    {
        EditorGUI.BeginChangeCheck();

        GrabAgentReference();
        agentReference = EditorGUILayout.ObjectField("Agent Reference: ", agentReference, typeof(PathOSAgent), true)
            as PathOSAgent;

        //Update agent ID if the user has selected a new object reference.
        if(EditorGUI.EndChangeCheck())
        {
            hasAgent = agentReference != null;

            if (hasAgent)
                agentID = agentReference.GetInstanceID();        
        }

        numAgents = EditorGUILayout.IntField("Number of agents: ", numAgents);

        simultaneous = EditorGUILayout.Toggle("Simulate Simultaneously", simultaneous);

        timeScale = EditorGUILayout.Slider("Timescale: ", timeScale, 1.0f, 8.0f);

        heuristicMode = (HeuristicMode)GUILayout.SelectionGrid(
            (int)heuristicMode, heuristicModeLabels, heuristicModeLabels.Length);

        switch(heuristicMode)
        {
            case HeuristicMode.FIXED:

                if (GUILayout.Button("Load from Agent"))
                    LoadHeuristicsFromAgent();

                fixedExp = EditorGUILayout.Slider("Experience Scale",
                    fixedExp, 0.0f, 1.0f);

                for (int i = 0; i < fixedHeuristics.Count; ++i)
                {
                    fixedHeuristics[i].scale = EditorGUILayout.Slider(
                        heuristicLabels[fixedHeuristics[i].heuristic],
                        fixedHeuristics[i].scale, 0.0f, 1.0f);
                }

                break;

            case HeuristicMode.RANGE:

                EditorGUILayout.BeginHorizontal();

                EditorGUILayout.MinMaxSlider("Experience Scale",
                    ref rangeExp.min, ref rangeExp.max, 0.0f, 1.0f);

                rangeExp.min = EditorGUILayout.FloatField(
                    RoundFloatfield(rangeExp.min), GUILayout.Width(shortFloatfieldWidth));

                EditorGUILayout.LabelField("<->", GUILayout.Width(shortLabelWidth));

                rangeExp.max = EditorGUILayout.FloatField(
                    RoundFloatfield(rangeExp.max), GUILayout.Width(shortFloatfieldWidth));

                EditorGUILayout.EndHorizontal();

                for (int i = 0; i < rangeHeuristics.Count; ++i)
                {
                    EditorGUILayout.BeginHorizontal();

                    EditorGUILayout.MinMaxSlider(
                        heuristicLabels[rangeHeuristics[i].heuristic],
                        ref rangeHeuristics[i].range.min,
                        ref rangeHeuristics[i].range.max,
                        0.0f, 1.0f);

                    rangeHeuristics[i].range.min = EditorGUILayout.FloatField(
                        RoundFloatfield(rangeHeuristics[i].range.min), 
                        GUILayout.Width(shortFloatfieldWidth));

                    EditorGUILayout.LabelField("<->", GUILayout.Width(shortLabelWidth));

                    rangeHeuristics[i].range.max = EditorGUILayout.FloatField(
                        RoundFloatfield(rangeHeuristics[i].range.max), 
                        GUILayout.Width(shortFloatfieldWidth));

                    EditorGUILayout.EndHorizontal();
                }

                break;

            case HeuristicMode.LOAD:

                EditorGUILayout.LabelField("File to load: ", shortHeuristicsFile);

                if (GUILayout.Button("Select CSV..."))
                {
                    loadHeuristicsFile = EditorUtility.OpenFilePanel("Select CSV...",
                        Application.dataPath, "csv");

                    shortHeuristicsFile = loadHeuristicsFile.Substring(
                    Mathf.Max(0, loadHeuristicsFile.Length - filepathLength));

                    if (loadHeuristicsFile.Length > filepathLength)
                        shortHeuristicsFile = "..." + shortHeuristicsFile;
                }

                break;
        }

        //Apply new heuristic values to the agent.
        if (GUILayout.Button("Apply to agent"))
            SetHeuristics();

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

    //Used to truncate floating-point values in input fields.
    private float RoundFloatfield(float val)
    {
        return Mathf.Round(val * 1000.0f) / 1000.0f;
    }

    private void Update()
    {
        previousPlaystate = EditorApplication.isPlaying;

        if (simulationActive)
        {
            if(triggerFrame)
            {
                EditorApplication.isPlaying = true;
                Time.timeScale = timeScale;
                triggerFrame = false;
            }
            else if (!EditorApplication.isPlaying)
            {
                if (agentsLeft == 0)
                    simulationActive = false;
                else
                {
                    SetHeuristics();

                    //We need to wait one frame to ensure Unity
                    //saves the changes to the agent's heuristic values
                    //in the undo stack.
                    triggerFrame = true;
                    --agentsLeft;
                }
            }
        }
    }

    //Grab fixed heuristic values from the agent reference specified.
    private void LoadHeuristicsFromAgent()
    {
        if(null == agentReference)
            return;

        foreach(PathOS.HeuristicScale scale in agentReference.heuristicScales)
        {
            fixedLookup[scale.heuristic] = scale.scale;
        }

        foreach(PathOS.HeuristicScale scale in fixedHeuristics)
        {
            scale.scale = fixedLookup[scale.heuristic];
        }

        fixedExp = agentReference.experienceScale;
    }

    private void SyncFixedLookup()
    {
        foreach(PathOS.HeuristicScale scale in fixedHeuristics)
        {
            fixedLookup[scale.heuristic] = scale.scale;
        }        
    }

    private void SyncRangeLookup()
    {
        foreach (PathOS.HeuristicRange range in rangeHeuristics)
        {
            rangeLookup[range.heuristic] = range.range;
        }
    }

    private void GrabAgentReference()
    {
        if(hasAgent && null == agentReference)
            agentReference = EditorUtility.InstanceIDToObject(agentID) as PathOSAgent;
    }

    private void SetHeuristics()
    {
        GrabAgentReference();

        if (null == agentReference)
            return;

        Undo.RecordObject(agentReference, "Set Agent Heuristics");

        switch(heuristicMode)
        {
            case HeuristicMode.FIXED:

                SyncFixedLookup();

                foreach(PathOS.HeuristicScale scale in agentReference.heuristicScales)
                {
                    scale.scale = fixedLookup[scale.heuristic];
                }

                agentReference.experienceScale = fixedExp;
                break;

            case HeuristicMode.RANGE:

                SyncRangeLookup();

                foreach(PathOS.HeuristicScale scale in agentReference.heuristicScales)
                {
                    PathOS.FloatRange range = rangeLookup[scale.heuristic];
                    scale.scale = Random.Range(range.min, range.max);
                }

                agentReference.experienceScale = Random.Range(rangeExp.min, rangeExp.max);
                break;

            case HeuristicMode.LOAD:
                break;
        }
    }
}
