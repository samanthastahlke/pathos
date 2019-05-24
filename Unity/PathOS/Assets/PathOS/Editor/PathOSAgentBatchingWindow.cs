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

    private const int pathDisplayLength = 32;
    private GUIStyle errorStyle = new GUIStyle();

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

    [SerializeField]
    private Vector3 startLocation;

    [SerializeField]
    private string loadPrefabFile = "--";

    private string shortPrefabFile;

    private bool validPrefabFile = false;

    private List<PathOSAgent> instantiatedAgents = new List<PathOSAgent>();
    private List<PathOSAgent> existingSceneAgents = new List<PathOSAgent>();

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
    private string loadHeuristicsFile = "--";

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

        if (loadHeuristicsFile == "")
            loadHeuristicsFile = "--";

        if (loadPrefabFile == "")
            loadPrefabFile = "--";

        TruncateFilepath(loadHeuristicsFile, ref shortHeuristicsFile);
        TruncateFilepath(loadPrefabFile, ref shortPrefabFile);

        errorStyle.normal.textColor = Color.red;
        CheckPrefabFile();

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

        //If simultaneous simulation is selected, draw the prefab selection utility.
        if(simultaneous)
        {
            startLocation = EditorGUILayout.Vector3Field("Starting location: ", startLocation);

            EditorGUILayout.LabelField("Prefab to use: ", shortPrefabFile);

            if (GUILayout.Button("Select Prefab..."))
            {
                loadPrefabFile = EditorUtility.OpenFilePanel("Select Prefab...",
                    Application.dataPath, "prefab");

                TruncateFilepath(loadPrefabFile, ref shortPrefabFile);
                CheckPrefabFile();
            }

            if (!validPrefabFile)
            {
                EditorGUILayout.LabelField("Error! You must select a Unity prefab" +
                    " with the PathOSAgent component.", errorStyle);
            }

            if(GUILayout.Button("Test Instantiation"))
            {
                FindSceneAgents();
                SetSceneAgentsActive(false);
                InstantiateAgents(4);
            }

            if(GUILayout.Button("Test Removal"))
            {
                DeleteInstantiatedAgents(4);
                SetSceneAgentsActive(true);
            }

        }

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

                    TruncateFilepath(loadHeuristicsFile, ref shortHeuristicsFile);
                }

                break;
        }

        //Apply new heuristic values to the agent.
        if (GUILayout.Button("Apply to agent"))
            ApplyHeuristics();

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
                    ApplyHeuristics();

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

    private void ApplyHeuristics()
    {
        GrabAgentReference();

        if (null == agentReference)
            return;

        Undo.RecordObject(agentReference, "Set Agent Heuristics");

        SetHeuristics(agentReference);
    }

    private void TruncateFilepath(string longPath, ref string shortPath)
    {
        shortPath = longPath.Substring(
                    Mathf.Max(0, longPath.Length - pathDisplayLength));

        if (longPath.Length > pathDisplayLength)
            shortPath = "..." + shortPath;
    }

    private void SetHeuristics(PathOSAgent agent)
    {
        switch (heuristicMode)
        {
            case HeuristicMode.FIXED:

                SyncFixedLookup();

                foreach (PathOS.HeuristicScale scale in agent.heuristicScales)
                {
                    scale.scale = fixedLookup[scale.heuristic];
                }

                agent.experienceScale = fixedExp;
                break;

            case HeuristicMode.RANGE:

                SyncRangeLookup();

                foreach (PathOS.HeuristicScale scale in agent.heuristicScales)
                {
                    PathOS.FloatRange range = rangeLookup[scale.heuristic];
                    scale.scale = Random.Range(range.min, range.max);
                }

                agent.experienceScale = Random.Range(rangeExp.min, rangeExp.max);
                break;

            case HeuristicMode.LOAD:
                break;
        }
    }

    private void CheckPrefabFile()
    {
        string loadPrefabFileLocal = GetLocalPrefabFile();
        validPrefabFile = AssetDatabase.LoadAssetAtPath<PathOSAgent>(loadPrefabFileLocal);
    }

    private string GetLocalPrefabFile()
    {
        if (loadPrefabFile.Length < Application.dataPath.Length)
            return "";

        //PrefabUtility needs paths relative to the project folder.
        //Application.dataPath gives us the project folder + "/Assets".
        //We need our string to start with "Assets".
        //Ergo, we split the string starting at the length of the data path - 6.
        return loadPrefabFile.Substring(Application.dataPath.Length - 6);
    }

    private void FindSceneAgents()
    {
        existingSceneAgents.Clear();
        existingSceneAgents.AddRange(FindObjectsOfType<PathOSAgent>());
    }

    private void SetSceneAgentsActive(bool active)
    {
        for(int i = 0; i < existingSceneAgents.Count; ++i)
        {
            existingSceneAgents[i].gameObject.SetActive(active);
        }
    }

    private void DeleteInstantiatedAgents(int count)
    {
        if (count > instantiatedAgents.Count)
            count = instantiatedAgents.Count;

        for(int i = 0; i < count; ++i)
        {
            Object.DestroyImmediate(instantiatedAgents[instantiatedAgents.Count - 1].gameObject);
            instantiatedAgents.RemoveAt(instantiatedAgents.Count - 1);
        }
    }

    private void InstantiateAgents(int count)
    {
        if (!validPrefabFile)
            return;

        PathOSAgent prefab = AssetDatabase.LoadAssetAtPath<PathOSAgent>(GetLocalPrefabFile());

        if (null == prefab)
            return;

        for (int i = 0; i < count; ++i)
        {
            GameObject newAgent = PrefabUtility.InstantiatePrefab(prefab.gameObject) as GameObject;
            newAgent.transform.position = startLocation;
            instantiatedAgents.Add(newAgent.GetComponent<PathOSAgent>());
        }
    }
}
