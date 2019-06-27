using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using PathOS;

/*
PathOSAgentBatchingWindow.cs 
PathOSAgentBatchingWindow (c) Nine Penguins (Samantha Stahlke) 2019
*/
public class PathOSAgentBatchingWindow : EditorWindow
{
    //Used to identify preferences string by Unity.
    private const string editorPrefsID = "PathOSAgentBatching";

    private const int pathDisplayLength = 32;
    private GUIStyle errorStyle = new GUIStyle();
    private GUIStyle headerStyle = new GUIStyle();

    private static char[] commaSep = { ',' };

    /* Basic Settings */
    [SerializeField]
    private PathOSAgent agentReference;

    [SerializeField]
    private bool hasAgent;

    [SerializeField]
    private int agentID;

    [SerializeField]
    private int numAgents;

    [SerializeField]
    private float timeScale = 1.0f;

    /* For Simulataneous Simulation */
    [SerializeField]
    private bool simultaneousProperty = false;
    private bool simultaneous = false;

    [SerializeField]
    private Vector3 startLocation;

    [SerializeField]
    private string loadPrefabFile = "--";

    private string shortPrefabFile;

    private bool validPrefabFile = false;

    //Unity can lose our reference to the in-scene agent in between
    //edit mode and playmode. Here, we use the instance ID, which persists
    //between modes, to ensure we keep our reference during successive runs.
    [System.Serializable]
    private class RuntimeAgentReference
    {
        public PathOSAgent agent;
        public int instanceID;

        public RuntimeAgentReference(PathOSAgent agent)
        {
            instanceID = agent.GetInstanceID();
        }

        public void UpdateReference()
        {
            agent = EditorUtility.InstanceIDToObject(instanceID) as PathOSAgent;
        }
    }

    [SerializeField]
    private List<RuntimeAgentReference> instantiatedAgents = 
        new List<RuntimeAgentReference>();

    [SerializeField]
    private List<RuntimeAgentReference> existingSceneAgents = 
        new List<RuntimeAgentReference>();

    //Max number of agents to be simulated simultaneously.
    private const int MAX_AGENTS_SIMULTANEOUS = 8;

    /* Motive Configuration */
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

    private const string customProfile = "Custom...";

    [SerializeField]
    private string selectedProfile = customProfile;

    private List<string> profileNames = new List<string>();
    private int profileIndex = 0;

    [SerializeField]
    private PathOS.FloatRange rangeExp;

    [SerializeField]
    private string loadHeuristicsFile = "--";

    private string shortHeuristicsFile;

    private bool validHeuristicsFile;

    [System.Serializable]
    private class HeuristicSet
    {
        public float exp;
        public List<PathOS.HeuristicScale> scales = 
            new List<PathOS.HeuristicScale>();

        public Dictionary<PathOS.Heuristic, float> heuristics
            = new Dictionary<PathOS.Heuristic, float>();
    }

    private List<HeuristicSet> loadedHeuristics =
        new List<HeuristicSet>();

    private int loadAgentIndex = 0;

    /* Simulation Controls */
    private bool simulationActive = false;
    private bool triggerFrame = false;
    private bool cleanupWait = false;
    private bool cleanupFrame = false;
    private bool wasPlaying = false;
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

        //For heuristic ranges...
        if (rangeHeuristics.Count != heuristics.Length)
        {
            rangeHeuristics.Clear();
            foreach(PathOS.Heuristic heuristic in heuristics)
            {
                rangeHeuristics.Add(new PathOS.HeuristicRange(heuristic));
            }
        }

        //Agent profiles.
        if (null == PathOSProfileWindow.profiles)
            PathOSProfileWindow.ReadPrefsData();

        SyncProfileNames();

        //Labels for heuristic fields.
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

        PathOS.UI.TruncateStringHead(loadHeuristicsFile,
            ref shortHeuristicsFile, pathDisplayLength);
        PathOS.UI.TruncateStringHead(loadPrefabFile, 
            ref shortPrefabFile, pathDisplayLength);

        errorStyle.normal.textColor = Color.red;

        headerStyle.fontStyle = FontStyle.Bold;

        CheckPrefabFile();
        CheckHeuristicsFile();

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

        PlayerPrefs.SetInt(OGLogManager.overrideFlagId, 0);

        DeleteInstantiatedAgents(instantiatedAgents.Count);
        SetSceneAgentsActive(true);

        instantiatedAgents.Clear();
        existingSceneAgents.Clear();

        //Save settings to the editor.
        string prefsData = JsonUtility.ToJson(this, false);
        EditorPrefs.SetString(editorPrefsID, prefsData);
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("General", headerStyle);

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

        simultaneousProperty = EditorGUILayout.Toggle(
            "Simulate Simultaneously", simultaneousProperty);

        //If simultaneous simulation is selected, draw the prefab selection utility.
        if(simultaneousProperty)
        {
            startLocation = EditorGUILayout.Vector3Field("Starting location: ", startLocation);

            EditorGUILayout.LabelField("Prefab to use: ", shortPrefabFile);

            if (GUILayout.Button("Select Prefab..."))
            {
                loadPrefabFile = EditorUtility.OpenFilePanel("Select Prefab...",
                    Application.dataPath, "prefab");

                PathOS.UI.TruncateStringHead(loadPrefabFile, 
                    ref shortPrefabFile, pathDisplayLength);

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

        EditorGUILayout.LabelField("Agent Motives", headerStyle);

        heuristicMode = (HeuristicMode)GUILayout.SelectionGrid(
            (int)heuristicMode, heuristicModeLabels, heuristicModeLabels.Length);

        //Motive configration panel.
        switch(heuristicMode)
        {
            //Set fixed values for experience/motives for every agent.
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
            
            //Define an acceptable range of values for each motive.
            case HeuristicMode.RANGE:

                if (null == PathOSProfileWindow.profiles)
                    PathOSProfileWindow.ReadPrefsData();

                SyncProfileNames();

                EditorGUI.BeginChangeCheck();
                profileIndex = EditorGUILayout.Popup("Profile: ", profileIndex, profileNames.ToArray());
                selectedProfile = profileNames[profileIndex];

                if (EditorGUI.EndChangeCheck()
                    && profileIndex < PathOSProfileWindow.profiles.Count)
                    LoadProfile(PathOSProfileWindow.profiles[profileIndex]);

                EditorGUI.BeginChangeCheck();

                PathOS.EditorUI.FullMinMaxSlider("Experience Scale",
                    ref rangeExp.min, ref rangeExp.max);

                for (int i = 0; i < rangeHeuristics.Count; ++i)
                {
                    PathOS.EditorUI.FullMinMaxSlider(
                        heuristicLabels[rangeHeuristics[i].heuristic],
                        ref rangeHeuristics[i].range.min,
                        ref rangeHeuristics[i].range.max);
                }

                if(EditorGUI.EndChangeCheck())
                {
                    selectedProfile = customProfile;
                    profileIndex = profileNames.Count - 1;
                }

                break;
            
            //Load a series of values for each agent from a file.
            case HeuristicMode.LOAD:

                EditorGUILayout.LabelField("File to load: ", shortHeuristicsFile);

                if (GUILayout.Button("Select CSV..."))
                {
                    loadHeuristicsFile = EditorUtility.OpenFilePanel("Select CSV...",
                        Application.dataPath, "csv");

                    PathOS.UI.TruncateStringHead(loadHeuristicsFile, 
                        ref shortHeuristicsFile, pathDisplayLength);

                    CheckHeuristicsFile();
                }

                if (!validHeuristicsFile)
                {
                    EditorGUILayout.LabelField("Error! You must select a " +
                        ".csv file on this computer.", errorStyle);
                }

                break;
        }

        //Apply new heuristic values to the agent.
        if(heuristicMode != HeuristicMode.LOAD)
        {
            if (GUILayout.Button("Apply to agent"))
                ApplyHeuristics();
        }

        GUILayout.Label("Simulation Controls", headerStyle);

        //Trigger the start of the simulation.
        if(GUILayout.Button("Start"))
        {
            if (PathOSManager.instance != null)
            {
                if(heuristicMode == HeuristicMode.LOAD
                    && !LoadHeuristics())
                {
                    NPDebug.LogError("Can't start simulation in this mode without " +
                        "a valid motives file containing at least one agent profile!");
                }
                else
                {
                    simultaneous = simultaneousProperty;
                    simulationActive = true;
                    agentsLeft = numAgents;
                    loadAgentIndex = 0;

                    //Initialize settings for logging to a single directory.
                    PlayerPrefs.SetInt(OGLogManager.fileIndexId, 0);
                    PlayerPrefs.SetString(OGLogManager.directoryOverrideId,
                        "Batch-" + PathOS.UI.GetFormattedTimestamp());
                    PlayerPrefs.Save();

                    //If simultaneous simulation is enabled, set any existing agents
                    //to disabled during the batched run.
                    if (simultaneous)
                    {
                        FindSceneAgents();
                        SetSceneAgentsActive(false);
                    }
                }
            }
            else
                NPDebug.LogError("Can't start simulation without a " +
                    "PathOS manager in the scene!");               
        }

        if(GUILayout.Button("Stop"))
        {
            simulationActive = false;
            EditorApplication.isPlaying = false;
            cleanupWait = true;
        }        
    }

    private void Update()
    {
        if (simulationActive)
        {
            //The frame the application should start.
            //(We need to wait a frame for Editor changes to take effect on agents).
            if (triggerFrame)
            {
                //Set a flag to ensure logs are recorded to a single directory
                //between successive batches.
                PlayerPrefs.SetInt(OGLogManager.overrideFlagId, 1);
                PlayerPrefs.Save();

                EditorApplication.isPlaying = true;
                Time.timeScale = timeScale;
                triggerFrame = false;
            }
            else if (!EditorApplication.isPlaying)
            {
                //Completely stop the simulation if there are no agents left
                //or it was ended prematurely (i.e., the user pressed stop from the 
                //editor).
                if (agentsLeft == 0 || (wasPlaying 
                    && !EditorPrefs.GetBool(
                        PathOSManager.simulationEndedEditorPrefsID)))
                {
                    agentsLeft = 0;
                    simulationActive = false;
                    cleanupFrame = true;
                }
                else
                {
                    if (simultaneous)
                    {
                        if (agentsLeft > instantiatedAgents.Count)
                        {
                            InstantiateAgents(Mathf.Min(
                                MAX_AGENTS_SIMULTANEOUS - instantiatedAgents.Count,
                                agentsLeft - instantiatedAgents.Count));
                        }
                        else if (agentsLeft < instantiatedAgents.Count)
                        {
                            DeleteInstantiatedAgents(instantiatedAgents.Count - agentsLeft);
                        }

                        ApplyHeuristicsInstantiated();
                        agentsLeft -= instantiatedAgents.Count;
                    }
                    else
                    {
                        ApplyHeuristics();
                        --agentsLeft;
                    }

                    //We need to wait one frame to ensure Unity
                    //saves the changes to agent heuristic values
                    //in the undo stack.
                    triggerFrame = true;
                }
            }
        }
        //Again, we need to wait a frame to ensure the changes
        //we make editor-side (e.g., deactiviating scene agents)
        //will persist.
        else if(cleanupWait)
        {
            cleanupWait = false;
            cleanupFrame = true;
        }
        else if(cleanupFrame)
        {
            cleanupFrame = false;
            cleanupWait = false;
            triggerFrame = false;

            if (simultaneous)
            {
                SetSceneAgentsActive(true);
                DeleteInstantiatedAgents(instantiatedAgents.Count);
            }

            PlayerPrefs.SetInt(OGLogManager.overrideFlagId, 0);
        }

        wasPlaying = EditorApplication.isPlaying;
    }

    //Load custom agent profile for defining motive ranges.
    private void LoadProfile(AgentProfile profile)
    {
        Dictionary<Heuristic, FloatRange> profileLookup =
            new Dictionary<Heuristic, FloatRange>();

        foreach (HeuristicRange hr in profile.heuristicRanges)
        {
            profileLookup.Add(hr.heuristic, hr.range);
        }

        for (int i = 0; i < rangeHeuristics.Count; ++i)
        {
            if (profileLookup.ContainsKey(rangeHeuristics[i].heuristic))
            {
                FloatRange range = profileLookup[rangeHeuristics[i].heuristic];
                rangeHeuristics[i].range = range;
            }
        }

        rangeExp = profile.expRange;
    }

    //Reconcile UI selection of custom profile with collection of profiles
    //from the profile window.
    private void SyncProfileNames()
    {
        profileNames.Clear();

        for (int i = 0; i < PathOSProfileWindow.profiles.Count; ++i)
        {
            profileNames.Add(PathOSProfileWindow.profiles[i].name);
        }

        profileNames.Add(customProfile);

        int nameIndex = profileNames.FindIndex(name => name == selectedProfile);
        profileIndex = (nameIndex >= 0) ? nameIndex : profileNames.Count - 1;
    }

    private bool LoadHeuristics()
    {
        loadedHeuristics.Clear();

        StreamReader s = new StreamReader(loadHeuristicsFile);
        string line = "";
        string[] lineContents;
        int lineNumber = 0;

        try
        {
            //Consume the header.
            if (!s.EndOfStream)
                line = s.ReadLine();

            List<PathOS.Heuristic> heuristics = new List<PathOS.Heuristic>();

            foreach(PathOS.Heuristic heuristic in 
                System.Enum.GetValues(typeof(PathOS.Heuristic)))
            {
                heuristics.Add(heuristic);
            }

            //Each line should have a value for experience followed by one for 
            //each heuristic, in the same order as they are defined.
            int lineLength = 1 + heuristics.Count;

            while(!s.EndOfStream)
            {
                ++lineNumber;
                line = s.ReadLine();

                lineContents = line.Split(commaSep, System.StringSplitOptions.RemoveEmptyEntries);

                if (lineContents.Length != lineLength)
                {
                    NPDebug.LogWarning(string.Format("Incorrect number of entries on line {0} while " +
                        "loading heuristics from {1}.", lineNumber, loadHeuristicsFile));

                    continue;
                }

                HeuristicSet newSet = new HeuristicSet();

                newSet.exp = float.Parse(lineContents[0]);

                for(int i = 0; i < heuristics.Count; ++i)
                {
                    newSet.scales.Add(new PathOS.HeuristicScale(
                        heuristics[i], float.Parse(lineContents[i + 1])));
                }

                loadedHeuristics.Add(newSet);             
            }
        }
        catch(System.Exception e)
        {
            NPDebug.LogError(string.Format("Exception raised loading heuristics from " +
                "{0} on line {1}: {2}", loadHeuristicsFile, lineNumber, e.Message));
        }

        return loadedHeuristics.Count >= 1;
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

    //Apply motive values to the agent in-scene.
    private void ApplyHeuristics()
    {
        GrabAgentReference();

        if (null == agentReference)
            return;

        Undo.RecordObject(agentReference, "Set Agent Heuristics");

        SetHeuristics(agentReference);
    }

    //Apply heuristics to the given agent.
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

                int ind = loadAgentIndex % loadedHeuristics.Count;
                loadedHeuristics[ind].heuristics.Clear();

                foreach(PathOS.HeuristicScale scale in loadedHeuristics[ind].scales)
                {
                    loadedHeuristics[ind].heuristics.Add(scale.heuristic, scale.scale);
                }

                agent.experienceScale = loadedHeuristics[ind].exp;

                foreach(PathOS.HeuristicScale scale in agent.heuristicScales)
                {
                    scale.scale = loadedHeuristics[ind].heuristics[scale.heuristic];
                }

                ++loadAgentIndex;
                break;
        }
    }

    private void ApplyHeuristicsInstantiated()
    {
        for (int i = 0; i < instantiatedAgents.Count; ++i)
        {
            instantiatedAgents[i].UpdateReference();

            EditorUtility.SetDirty(instantiatedAgents[i].agent);
            SetHeuristics(instantiatedAgents[i].agent);
        }
    }

    private void CheckPrefabFile()
    {
        string loadPrefabFileLocal = GetLocalPrefabFile();
        validPrefabFile = AssetDatabase.LoadAssetAtPath<PathOSAgent>(loadPrefabFileLocal);
    }

    private void CheckHeuristicsFile()
    {
        validHeuristicsFile = File.Exists(loadHeuristicsFile)
            && loadHeuristicsFile.Substring(Mathf.Max(0, loadHeuristicsFile.Length - 3))
            == "csv";
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

        foreach(PathOSAgent agent in FindObjectsOfType<PathOSAgent>())
        {
            existingSceneAgents.Add(new RuntimeAgentReference(agent));
        }
    }

    private void SetSceneAgentsActive(bool active)
    {
        for(int i = 0; i < existingSceneAgents.Count; ++i)
        {
            existingSceneAgents[i].UpdateReference();
            existingSceneAgents[i].agent.gameObject.SetActive(active);
            EditorUtility.SetDirty(existingSceneAgents[i].agent.gameObject);
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
            newAgent.name = "Temporary Batch Agent " + 
                (instantiatedAgents.Count).ToString();

            instantiatedAgents.Add(new RuntimeAgentReference(
                newAgent.GetComponent<PathOSAgent>()));
        }
    }

    private void DeleteInstantiatedAgents(int count)
    {
        if (count > instantiatedAgents.Count)
            count = instantiatedAgents.Count;

        for (int i = 0; i < count; ++i)
        {      
            instantiatedAgents[instantiatedAgents.Count - 1].UpdateReference();

            if (instantiatedAgents[instantiatedAgents.Count - 1].agent)
                Object.DestroyImmediate(
                    instantiatedAgents[instantiatedAgents.Count - 1].agent.gameObject);

            instantiatedAgents.RemoveAt(instantiatedAgents.Count - 1);
        }
    }
}
