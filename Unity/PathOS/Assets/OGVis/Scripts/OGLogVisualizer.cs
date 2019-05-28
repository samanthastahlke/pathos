using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

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

    private static char[] commaSep = { ',' };

    //Utility class for defining custom scales.
    [System.Serializable]
    public struct DataRange
    {
        public int min;
        public int max;
    }

    //Path settings.
    public bool showIndividualPaths;
    public bool showHeatmap;
    public bool aggregateActiveOnly;

    public float displayHeight = 1.0f;

    //Used in creating the heatmap.
    private Vector3 maxExtents = Vector3.zero;
    public Vector3 gridSize = Vector3.zero;

    //Default categorical palettes for paths/events.
    private Color[] defaultPathColors;
    private int cIndex = 0;

    private bool colsInit = false;

    //Display (on-screen) scales.
    public const float MIN_PATH_WIDTH = 2.0f;
    public const float MAX_PATH_WIDTH = 10.0f;

    //Utility class for loading/storing player logfiles.
    public class PlayerLog
    {
        //Shell class for timestamped events.
        public abstract class EventRecord
        {
            public float timestamp;

            //"Real position" of the event data.
            public Vector3 realPos;

            //"Display position" updated by path resampling to account for 
            //axis flattening.
            public Vector3 pos;
            
            public EventRecord(float timestamp, Vector3 pos)
            {
                this.timestamp = timestamp;
                this.realPos = new Vector3(pos.x, pos.y, pos.z);
                this.pos = new Vector3(pos.x, pos.y, pos.z);
            }
        }

        //Interaction events.
        public class InteractionEvent : EventRecord
        {
            public string objectName;

            public InteractionEvent(float timestamp, Vector3 pos, string objectName)
                : base(timestamp, pos)
            {
                this.objectName = objectName;
            }
        }

        //Time-series position/orientation data.
        public List<Vector3> positions;
        public List<Quaternion> orientations;

        public List<InteractionEvent> interactionEvents;

        public List<Vector3> pathPoints;

        public float sampleRate;

        public int displayStartIndex;
        public int displayEndIndex;

        public bool visInclude = true;
        public Color pathColor = Color.white;

        public PlayerLog()
        {
            positions = new List<Vector3>();
            orientations = new List<Quaternion>();
            interactionEvents = new List<InteractionEvent>();

            pathPoints = new List<Vector3>();
        }

        public void UpdateDisplayPath(float displayHeight)
        { 
            pathPoints.Clear();

            if (positions.Count == 0)
                return;

            foreach(Vector3 pos in positions)
            {
                pathPoints.Add(new Vector3(
                        pos.x, displayHeight, pos.z));
            }

            //Resample event positions to account for axis flattening.
            foreach(InteractionEvent e in interactionEvents)
            {
                e.pos.x = e.realPos.x;
                e.pos.y = displayHeight;
                e.pos.z = e.realPos.z;
            }
        }
    }

    public Dictionary<string, PlayerLog> pLogs = new Dictionary<string, PlayerLog>();

    //For displaying interactions with game objects.
    public abstract class AggregateInteraction
    {
        //Used to set radius of the mark displayed on the visualization.
        public float displaySize = 0.0f;
        //Number of events represented by the aggregate.
        public int tCount = 0;
        //Cluster centre.
        public Vector3 pos;

        public AggregateInteraction(Vector3 pos)
        {
            this.pos = pos;
        }
    }

    public Dictionary<string, AggregateInteraction> aggregateInteractions =
        new Dictionary<string, AggregateInteraction>();

    //Called when the script is re-enabled, the editor is launched, code recompiled...
    //Simply initialize non-serializable properties, etc.
    private void OnEnable()
    {
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

    //Called when the editor detects a change in the object's properties.
    //Can in theory be called every frame in real-time.
    private void Update()
    {

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

            //Disallow duplicate simultaneous player IDs.
            if(pLogs.ContainsKey(pKey))
            {
                Debug.LogError(string.Format("Duplicate player ID \"{0}\" found. Skipping logfile.", pKey));
                continue;
            }

            //Attempt to load the player log.
            if(LoadLog(logFiles[i], pKey))
                ++logsAdded;          
        }

        print("Loaded " + logsAdded + " logfiles.");
        directoriesLoaded.Add(directoryPath);

        ReclusterEvents();
    }

    private bool LoadLog(string filepath, string pKey)
    {
        PlayerLog pLog = new PlayerLog();

        StreamReader logReader = new StreamReader(filepath);
        string line = "";
        string[] lineContents;
        int lineNumber = 0;
        OGLogManager.LogItemType itemType;

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
                            throw new System.Exception(string.Format("Log parsing error on line {0}.", lineNumber));

                        Vector3 p = new Vector3(
                            float.Parse(lineContents[2]), 
                            float.Parse(lineContents[3]), 
                            float.Parse(lineContents[4]));

                        //Store the "extents" of our data - used in heatmap generation.
                        if (Mathf.Abs(p.x) > maxExtents.x)
                            maxExtents.x = Mathf.Abs(p.x);
                        if (Mathf.Abs(p.y) > maxExtents.y)
                            maxExtents.y = Mathf.Abs(p.y);
                        if (Mathf.Abs(p.z) > maxExtents.z)
                            maxExtents.z = Mathf.Abs(p.z);

                        pLog.positions.Add(p);

                        pLog.orientations.Add(Quaternion.Euler(
                            float.Parse(lineContents[5]), 
                            float.Parse(lineContents[6]), 
                            float.Parse(lineContents[7])));

                        break;

                    //TODO: Interaction with a game object.
                    case OGLogManager.LogItemType.INTERACTION:
                        {
                            break;
                        }

                    //TODO: Header/metadata.
                    case OGLogManager.LogItemType.HEADER:
                        {
                            break;
                        }

                    default:
                        break;
                }
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

        pLogs.Add(pKey, pLog);
        return true;
    }

    //Reset the vis information.
    public void ClearData()
    {
        //Clear logs and directory information.
        pLogs.Clear();
        directoriesLoaded.Clear();

        //Clear event data.
        aggregateInteractions.Clear();

        //Reset path/event colour defaults.
        cIndex = 0;
    }
    
    //Resample individual paths.
    //Updates according to desired time window, display height, etc.
    public void UpdateDisplayPaths()
    {
        foreach (KeyValuePair<string, PlayerLog> pLog in pLogs)
        {
            pLog.Value.UpdateDisplayPath(displayHeight);
        }
    }

    //Re-do event aggregation.
    public void ReclusterEvents()
    {
        foreach(KeyValuePair<string, AggregateInteraction> interaction in aggregateInteractions)
        {
            interaction.Value.tCount = 0;
        }

        //Grab active player data for reclustering.
        List<string> pKeys = new List<string>();

        foreach(KeyValuePair<string, PlayerLog> pLog in pLogs)
        {
            if (!pLog.Value.visInclude && aggregateActiveOnly)
                continue;

            pKeys.Add(pLog.Key);
        }
        
        if(pKeys.Count > 0)
            AddEventsToAggregate(new List<string>(pKeys));
    }

    //Aggregates all interactions from the given list of player IDs.
    private void AddEventsToAggregate(List<string> pids)
    {
        //Aggregate interaction events.
        //For every new player log...
        foreach (string pid in pids)
        {
            //For every input event in that player log...
            foreach (PlayerLog.InteractionEvent inputEvent in pLogs[pid].interactionEvents)
            {
                //TODO: Place into the corresponding global event.
            }
        }

        ReweightAggregateEvents();
    }

    //Recalculates display size/colour for aggregate events.
    private void ReweightAggregateEvents()
    {
        //Based on the number of events for active player records,
        //adjust the size of the displayed icons.
        foreach(KeyValuePair<string, AggregateInteraction> interaction in 
            aggregateInteractions)
        {
            //TODO
        }
    }

    //Maps v defined on scale [o1, o2] to return value on scale [s1, s2].
    public static float RangeRemap(float v, float o1, float o2, float s1, float s2)
    {
        return Mathf.Clamp(s1 + ((v - o1) / (o2 - o1)) * (s2 - s1), s1, s2);
    }
}
