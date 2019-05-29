using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using OGVis;

/*
OGLogVisualizer.cs
OGLogVisualizer (c) Ominous Games 2018

Master class for handling the vis.
*/

[ExecuteInEditMode]
public class OGLogVisualizer : MonoBehaviour 
{
    //File operations.
    public string logDirectory = "--";
    public List<string> directoriesLoaded;

    public OGLogHeatmap heatmapVisualizer;
    public Gradient heatmapGradient;
    public Gradient interactionGradient;

    private static char[] commaSep = { ',' };

    //Path settings.
    public bool showIndividualPaths;
    public bool showHeatmap;
    public bool showEntities;
    public bool aggregateActiveOnly;
    public bool aggregateWindowOnly;

    public bool showIndividualInteractions = true;
    public float displayHeight = 1.0f;

    public TimeRange displayTimeRange = new TimeRange();
    public TimeRange fullTimeRange = new TimeRange();

    //Used in creating the heatmap.
    private Extents dataExtents = new Extents();

    public Vector3 gridSize = Vector3.zero;

    //Default categorical palettes for paths/events.
    private Color[] defaultPathColors;
    private int cIndex = 0;

    private bool colsInit = false;

    //Display (on-screen) scales.
    public const float MIN_PATH_WIDTH = 2.0f;
    public const float MAX_PATH_WIDTH = 10.0f;

    public List<PlayerLog> pLogs = new List<PlayerLog>();

    //For displaying interactions with game objects.
    public class AggregateInteraction
    {
        //Used to set radius of the mark displayed on the visualization.
        public float displaySize = 0.0f;
        public Color displayColor = Color.white;
        //Number of events represented by the aggregate.
        public int tCount = 0;
        //Cluster centre.
        public Vector3 pos;

        public string displayName = "";

        public AggregateInteraction(string displayName, Vector3 pos)
        {
            this.displayName = displayName;
            this.pos = pos;
        }
    }

    public Dictionary<string, AggregateInteraction> aggregateInteractions =
        new Dictionary<string, AggregateInteraction>();

    //Called when the script is re-enabled, the editor is launched, code recompiled...
    //Simply initialize non-serializable properties, etc.
    private void OnEnable()
    {
        if(null == heatmapVisualizer)
            heatmapVisualizer = GetComponentInChildren<OGLogHeatmap>();

        if(!colsInit)
        {
            //Palette source:
            //https://vega.github.io/vega/docs/schemes/

            defaultPathColors = new Color[11];

            //Using the D3/Vega Category10 scale:
            defaultPathColors[0] = new Color32(0x1F, 0x77, 0xB4, 0xFF);
            defaultPathColors[1] = new Color32(0xFF, 0x7F, 0x0E, 0xFF);
            defaultPathColors[2] = new Color32(0x2C, 0xA0, 0x2C, 0xFF);
            defaultPathColors[3] = new Color32(0xD6, 0x27, 0x28, 0xFF);
            defaultPathColors[4] = new Color32(0x94, 0x67, 0xBD, 0xFF);
            defaultPathColors[5] = new Color32(0x8C, 0x56, 0x4B, 0xFF);
            defaultPathColors[6] = new Color32(0xE3, 0x77, 0xC2, 0xFF);
            defaultPathColors[7] = new Color32(0x7F, 0x7F, 0x7F, 0xFF);
            defaultPathColors[8] = new Color32(0xBC, 0xBD, 0x22, 0xFF);
            defaultPathColors[9] = new Color32(0x17, 0xBE, 0xCF, 0xFF);
            //End the array with white - this will serve to "flag" the user that
            //they need to start picking their own colors (avoid confusion).
            defaultPathColors[10] = new Color32(0xFF, 0xFF, 0xFF, 0xFF);

            colsInit = true;
        }

        ClearData();
    }

    private void PopulateManagerEntities()
    {
        PathOSManager levelManager = PathOSManager.instance;

        for(int i = 0; i < levelManager.levelEntities.Count; ++i)
        { 
            PathOS.LevelEntity entity = levelManager.levelEntities[i];
            string name = entity.objectRef.name;
            Vector3 pos = entity.objectRef.transform.position;

            aggregateInteractions.Add(
                PlayerLog.InteractionEvent.GetStringHash(pos, name), 
                new AggregateInteraction(name, pos));
        }
    }

    public void ApplyDisplayRange()
    {
        for(int i = 0; i < pLogs.Count; ++i)
        {
            pLogs[i].SliceDisplayPath(displayTimeRange);
        }

        heatmapVisualizer.UpdateData(pLogs, aggregateActiveOnly, aggregateWindowOnly);
    }
    
    //Called when the user requests to load log files from a given directory.
    public void LoadLogs()
    {
        //Create directory path from specified folder.
        string directoryPath = logDirectory + "/";

        //Check directory validity.
        if(!Directory.Exists(logDirectory))
        {
            Debug.LogWarning(string.Format("Directory {0} doesn't exist!", directoryPath));
            return;
        }
        else if(directoriesLoaded.Contains(directoryPath))
        {
            //Don't load from the same directory twice.
            Debug.LogWarning(string.Format("Logs already loaded from directory {0}.", directoryPath));
            return;
        }

        print(string.Format("Loading logs from {0}...", directoryPath));

        //Grab all files in the directory.
        //Only attempt to load .csv files as logs.
        string[] logFiles = Directory.GetFiles(directoryPath);
        int filenameStart = 0;
        string filename = "";
        string pKey = "";
        int logsAdded = 0;

        for(int i = 0; i < logFiles.Length; ++i)
        {
            filenameStart = logFiles[i].LastIndexOf('/');

            if (!(filenameStart < (logFiles[i].Length - 6)))
                continue;

            filename = logFiles[i].Substring(filenameStart + 1);

            //Only .csv files are considered  as readable logs.
            if (!filename.Substring(filename.Length - 4).Equals(".csv"))
                continue;

            pKey = filename.Substring(0, filename.IndexOf('.'));

            //Attempt to load the player log.
            if(LoadLog(logFiles[i], pKey))
                ++logsAdded;          
        }

        print("Loaded " + logsAdded + " logfiles.");
        directoriesLoaded.Add(directoryPath);

        fullTimeRange.max = Mathf.Ceil(fullTimeRange.max);
        displayTimeRange = fullTimeRange;

        if(heatmapVisualizer != null)
            heatmapVisualizer.Initialize(dataExtents, heatmapGradient, displayHeight, 4.0f);

        ApplyDisplayHeight();
        ApplyDisplayRange();
        ReclusterEvents();

        if(heatmapVisualizer != null)
        {
            heatmapVisualizer.UpdateData(pLogs, aggregateActiveOnly, aggregateWindowOnly);
            heatmapVisualizer.SetVisible(showHeatmap);
        }
    }

    public void UpdateHeatmapVisibility()
    {
        if (heatmapVisualizer != null)
            heatmapVisualizer.SetVisible(showHeatmap);
    }

    private bool LoadLog(string filepath, string pKey)
    {
        PlayerLog pLog = new PlayerLog(pKey);

        StreamReader logReader = new StreamReader(filepath);
        string line = "";
        string[] lineContents;
        int lineNumber = 0;
        OGLogManager.LogItemType itemType;

        float timestamp = 0.0f;

        Vector3 p = Vector3.zero;
        Quaternion q = Quaternion.identity;

        try
        {
            while (!logReader.EndOfStream)
            {
                //Split the line into attributes.
                ++lineNumber;
                line = logReader.ReadLine();
                lineContents = line.Split(commaSep, System.StringSplitOptions.RemoveEmptyEntries);

                if (lineContents.Length < 1)
                    throw new System.Exception(string.Format("Log parsing error on line {0}.", lineNumber));

                //Check the type of this entry.
                itemType = (OGLogManager.LogItemType)System.Enum.Parse(typeof(OGLogManager.LogItemType), lineContents[0]);

                //Parse entries based on their type.
                switch(itemType)
                {
                    //Sampled position/orientation data.
                    case OGLogManager.LogItemType.POSITION:

                        if(lineContents.Length < OGLogger.POSLOG_L)
                            throw new System.Exception(string.Format(
                                "Log parsing error on line {0}.", lineNumber));

                        timestamp = float.Parse(lineContents[1]);

                        p = new Vector3(
                            float.Parse(lineContents[2]), 
                            float.Parse(lineContents[3]), 
                            float.Parse(lineContents[4]));

                        //Store the "extents" of our data - used in heatmap generation.
                        if (p.x > dataExtents.max.x)
                            dataExtents.max.x = p.x;
                        else if (p.x < dataExtents.min.x)
                            dataExtents.min.x = p.x;
                        if (p.z > dataExtents.max.z)
                            dataExtents.max.z = p.z;
                        else if (p.z < dataExtents.min.z)
                            dataExtents.min.z = p.z;

                        pLog.AddPosition(timestamp, p);

                        q = Quaternion.Euler(
                            float.Parse(lineContents[5]), 
                            float.Parse(lineContents[6]), 
                            float.Parse(lineContents[7]));

                        pLog.AddOrientation(timestamp, q);

                        break;

                    case OGLogManager.LogItemType.INTERACTION:

                        if(lineContents.Length < OGLogger.INTLOG_L)
                            throw new System.Exception(string.Format(
                                "Log parsing error on line {0}.", lineNumber));

                        timestamp = float.Parse(lineContents[1]);

                        p = new Vector3(
                            float.Parse(lineContents[3]),
                            float.Parse(lineContents[4]),
                            float.Parse(lineContents[5]));

                        pLog.AddInteractionEvent(timestamp, p, lineContents[2]);

                        break;

                    case OGLogManager.LogItemType.HEADER:

                        ParseHeader(pLog, lineContents);
                        break;

                    default:
                        break;
                }

                if (timestamp > fullTimeRange.max)
                    fullTimeRange.max = timestamp;
            }
        }
        catch(System.Exception e)
        {
            Debug.LogError(string.Format("Exception raised reading file {0}: ", filepath) + e.Message);
            return false;
        }

        //Resample path data according to path sample rate.
        pLog.UpdateDisplayPath(displayHeight);

        //Set color of path.
        pLog.pathColor = defaultPathColors[cIndex];
        cIndex = Mathf.Clamp(cIndex + 1, 0, defaultPathColors.Length - 1);

        pLogs.Add(pLog);
        return true;
    }

    public void ParseHeader(PlayerLog pLog, string[] lineContents)
    {
        switch(lineContents[1])
        {
            case "SAMPLE":

                pLog.sampleRate = float.Parse(lineContents[2]);
                break;

            case "HEURISTICS":

                pLog.experience = float.Parse(lineContents[3]);

                for(int i = 4; i < lineContents.Length; i += 2)
                {
                    pLog.heuristics.Add(
                        (PathOS.Heuristic)System.Enum.Parse(typeof(PathOS.Heuristic), lineContents[i]),
                        float.Parse(lineContents[i + 1]));
                }

                break;

            default:
                break;
        }
    }

    //Reset the vis information.
    public void ClearData()
    {
        //Clear logs and directory information.
        pLogs.Clear();
        directoriesLoaded.Clear();

        //Clear event data.
        aggregateInteractions.Clear();

        //Clear time range data.
        displayTimeRange.min = displayTimeRange.max = 0.0f;
        fullTimeRange.min = fullTimeRange.max = 0.0f;

        dataExtents.min = new Vector3(float.MaxValue, 0.0f, float.MaxValue);
        dataExtents.max = new Vector3(float.MinValue, 0.0f, float.MinValue);

        //Reset path/event colour defaults.
        cIndex = 0;

        if (heatmapVisualizer != null)
            heatmapVisualizer.Clear();
    }
    
    //Apply changes to display height of the vis.
    public void ApplyDisplayHeight()
    {
        for(int i = 0; i < pLogs.Count; ++i)
        {
            pLogs[i].UpdateDisplayPath(displayHeight);
        }

        if (heatmapVisualizer != null)
            heatmapVisualizer.SetDisplayHeight(displayHeight);
    }

    public void ApplyFilters()
    {

    }

    //Re-do event aggregation.
    public void ReclusterEvents()
    {
        aggregateInteractions.Clear();
        PopulateManagerEntities();

        //Grab active player data for reclustering.
        for (int i = 0; i < pLogs.Count; ++i)
        {
            PlayerLog pLog = pLogs[i];

            if(pLog.visInclude)
            {
                for(int j = 0; j < pLog.interactionEvents.Count; ++j)
                {
                    PlayerLog.InteractionEvent curEvent = pLog.interactionEvents[j];
                    string key = curEvent.GetStringHash();

                    if(!aggregateInteractions.ContainsKey(key))
                    { 
                        Debug.Log("Found new interaction event key!");

                        aggregateInteractions.Add(key, new AggregateInteraction(
                            curEvent.objectName, curEvent.pos));
                    }

                    ++aggregateInteractions[key].tCount;
                }
            }
        }

        ReweightAggregateEvents();
    }

    //Recalculates display size/colour for aggregate events.
    private void ReweightAggregateEvents()
    {
        //Based on the number of events for active player records,
        //adjust the size of the displayed icons.
        int maxInteractionCount = 0;

        for(int i = 0; i < pLogs.Count; ++i)
        {
            if (pLogs[i].visInclude)
                ++maxInteractionCount;
        }

        foreach(KeyValuePair<string, AggregateInteraction> interaction in 
            aggregateInteractions)
        {
            float fac = (maxInteractionCount <= 0) ? 0.0f :
                Mathf.Min(1.0f, (float)interaction.Value.tCount / maxInteractionCount);

            interaction.Value.displaySize = Mathf.Lerp(0.1f, 2.0f, fac);
            interaction.Value.displayColor = Color.Lerp(Color.white, Color.red, fac);
        }
    }

    //Maps v defined on scale [o1, o2] to return value on scale [s1, s2].
    public static float RangeRemap(float v, float o1, float o2, float s1, float s2)
    {
        return Mathf.Clamp(s1 + ((v - o1) / (o2 - o1)) * (s2 - s1), s1, s2);
    }
}
