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
    public string logDirectory;
    public List<string> directoriesLoaded;

    private static char[] commaSep = { ',' };

    //Utility class for defining custom scales.
    [System.Serializable]
    public struct DataRange
    {
        public int min;
        public int max;
    }

    //Display settings.
    //Whether to form aggregates from all loaded data or only the selected players.
    public bool aggregateActiveOnly;

    //"Axis flattening" - compress data to a particular plane if desired.
    //e.g., this can be used to "flatten" all y-coordinates to a set value,
    //allowing the viewer to consider data only in the XZ plane.
    public Vector3 flattenCoordinates;
    public bool flattenX, flattenY, flattenZ;

    //Path settings.
    public bool showIndividualPaths;
    public bool showAggregatePath;

    [Range(0.1f, 10.0f)]
    public float displaySampleRate = 1.0f;
    [Range(0.1f, 10.0f)]
    public float aggregateGridEdge = 0.5f;
    public DataRange aggregateEdgeExtents;

    //Tracking changes in sample rate/aggregate edge to refresh display.
    private float oldSampleRate = 1.0f;
    private float oldAggregateEdge = 0.5f;

    //Used in calculating aggregate path.
    private Vector3 maxExtents = Vector3.zero;
    private Vector3 gridSize = Vector3.zero;

    //Default categorical palettes for paths/events.
    private Color[] defaultPathColors;
    private int cIndex = 0;

    private Color[] defaultEventColors;
    private int eIndex = 0;

    private bool colsInit = false;

    //Input events.
    public bool showIndividualInputEvents;
    public bool showAggregateInputEvents;
    public Dictionary<KeyCode, bool> enabledInputEvents;
    public List<KeyCode> allInputEvents;
    public DataRange inputExtents;
    
    //Game events.
    public bool showIndividualGameEvents;
    public bool showAggregateGameEvents;
    public Dictionary<string, bool> enabledGameEvents;
    public List<string> allGameEvents;
    public DataRange gameExtents;

    //Display (on-screen) scales.
    public const float MIN_EVENT_RADIUS = 0.1f;
    public const float MAX_EVENT_RADIUS = 2.5f;

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
        //Input (keypress) events.
        public class InputEvent : EventRecord
        {
            public KeyCode key;

            public InputEvent(float timestamp, Vector3 pos, KeyCode key)
                : base(timestamp, pos)
            {
                this.key = key;
            }
        }
        //Game events obtained through code hooks.
        public class GameEvent : EventRecord
        {
            public string eventID;

            public GameEvent(float timestamp, Vector3 pos, string eventID)
                : base(timestamp, pos)
            {
                this.eventID = eventID;
            }
        }

        //Time-series position/orientation data.
        public SortedList<float, Vector3> positions;
        public SortedList<float, Quaternion> orientations;
        public List<InputEvent> inputEvents;
        public List<GameEvent> gameEvents;

        public List<Vector3> pathPoints;

        public bool visInclude = true;
        public Color pathColor = Color.white;

        public PlayerLog()
        {
            positions = new SortedList<float, Vector3>();
            orientations = new SortedList<float, Quaternion>();
            inputEvents = new List<InputEvent>();
            gameEvents = new List<GameEvent>();

            pathPoints = new List<Vector3>();
        }

        public void ResamplePath(float sampleRate, bool flattenX, bool flattenY, bool flattenZ,
            Vector3 flattenCoords)
        { 
            //Calculate the time between samples to display.
            float sampleTime = 1.0f / sampleRate;
            pathPoints.Clear();

            if (positions.Count == 0)
                return;

            float nextSample = positions.Keys[0];

            foreach(KeyValuePair<float, Vector3> posLog in positions)
            {
                //Check to see if we should add this point to the path.
                //This will skip over samples if the resolution of the logged data
                //is greater than the display resolution (as defined by sample rate).
                if(posLog.Key >= nextSample)
                {
                    pathPoints.Add(new Vector3(
                        (flattenX) ? flattenCoords.x : posLog.Value.x, 
                        (flattenY) ? flattenCoords.y : posLog.Value.y, 
                        (flattenZ) ? flattenCoords.z : posLog.Value.z));
                    nextSample = posLog.Key + sampleTime;
                }
            }

            //Resample event positions to account for axis flattening.
            foreach(GameEvent e in gameEvents)
            {
                e.pos.x = (flattenX) ? flattenCoords.x : e.realPos.x;
                e.pos.y = (flattenY) ? flattenCoords.y : e.realPos.y;
                e.pos.z = (flattenZ) ? flattenCoords.z : e.realPos.z;
            }

            foreach(InputEvent e in inputEvents)
            {
                e.pos.x = (flattenX) ? flattenCoords.x : e.realPos.x;
                e.pos.y = (flattenY) ? flattenCoords.y : e.realPos.y;
                e.pos.z = (flattenZ) ? flattenCoords.z : e.realPos.z;
            }
        }
    }

    public Dictionary<string, PlayerLog> pLogs;

    //Data aggregation.
    //Base class for aggregate events.
    public abstract class AggregateEvent
    {
        //Used to set radius of the mark displayed on the visualization.
        public float displaySize = 0.0f;
        //Store per-player counts (future work).
        public Dictionary<string, int> pCounts;
        //Number of events represented by the aggregate.
        public int tCount = 0;
        //Cluster centre.
        public Vector3 pos;

        public AggregateEvent(Vector3 pos)
        {
            this.pos = pos;
            pCounts = new Dictionary<string, int>();
        }
    }

    //Input event aggregation...
    public class InputAggregateEvent : AggregateEvent
    {
        public KeyCode key;

        public InputAggregateEvent(KeyCode key, Vector3 pos)
            : base(pos)
        {
            this.key = key;
        }
    }

    //Game event aggregation...
    public class GameAggregateEvent : AggregateEvent
    {
        public string eventID;

        public GameAggregateEvent(string eventID, Vector3 pos)
            : base(pos)
        {
            this.eventID = eventID;
        }
    }

    //Aggregate path edges.
    public class PathAggregateEdge
    {
        //Origin/destination of the edge.
        public Vector3[] points;
        //Weight of the edge (number of traversals).
        public int weight;
        //Thickness to display the edge, based on weight.
        public float displayWidth;

        public PathAggregateEdge(Vector3 origin, Vector3 dest, int weight)
        {
            points = new Vector3[2];
            points[0] = origin;
            points[1] = dest;
            this.weight = weight;
        }
    }

    //Configurable radii for aggregating events.
    [Range(0.1f, 20.0f)]
    public float inputClusterRadius = 1.0f;
    private float oldInputClusterRadius = 1.0f;
    [Range(0.1f, 20.0f)]
    public float gameClusterRadius = 1.0f;
    private float oldGameClusterRadius = 1.0f;

    public Dictionary<KeyCode, List<InputAggregateEvent>> aggregateInputEvents;
    public Dictionary<KeyCode, Color> aggregateInputColors;

    public Dictionary<string, List<GameAggregateEvent>> aggregateGameEvents;
    public Dictionary<string, Color> aggregateEventColors;

    public Dictionary<int, Dictionary<int, int>> aggregatePathEdges;
    public List<PathAggregateEdge> aggregatePath;

    //Called when the script is re-enabled, the editor is launched, code recompiled...
    //Simply initialize non-serializable properties, etc.
    private void OnEnable()
    {
        //print("Waking OGLog Visualizer...");

        if(!colsInit)
        {
            //Palette sources:
            //https://vega.github.io/vega/docs/schemes/
            //https://github.com/bokeh/bokeh/blob/0.12.14/bokeh/palettes.py

            defaultPathColors = new Color[11];
            defaultEventColors = new Color[9];

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
            //End the array with black - this will serve to "flag" the user that
            //they need to start picking their own colors (avoid confusion).
            defaultPathColors[10] = new Color32(0x00, 0x00, 0x00, 0xFF);

            //Using the D3/Vega Set2 scale:
            defaultEventColors[0] = new Color32(0x66, 0xC2, 0xA5, 0x88);
            defaultEventColors[1] = new Color32(0xFC, 0x8D, 0x62, 0x88);
            defaultEventColors[2] = new Color32(0x8D, 0xA0, 0xCB, 0x88);
            defaultEventColors[3] = new Color32(0xE7, 0x8A, 0xC3, 0x88);
            defaultEventColors[4] = new Color32(0xA6, 0xD8, 0x54, 0x88);
            defaultEventColors[5] = new Color32(0xFF, 0xD9, 0x2F, 0x88);
            defaultEventColors[6] = new Color32(0xE5, 0xC4, 0x94, 0x88);
            defaultEventColors[7] = new Color32(0xB3, 0xB3, 0xB3, 0x88);
            defaultEventColors[8] = new Color32(0x00, 0x00, 0x00, 0x88);

            colsInit = true;
        }
        
        if (enabledInputEvents == null)
            enabledInputEvents = new Dictionary<KeyCode, bool>();

        if (enabledGameEvents == null)
            enabledGameEvents = new Dictionary<string, bool>();

        if (aggregateInputEvents == null)
            aggregateInputEvents = new Dictionary<KeyCode, List<InputAggregateEvent>>();

        if (aggregateInputColors == null)
            aggregateInputColors = new Dictionary<KeyCode, Color>();

        if (aggregateGameEvents == null)
            aggregateGameEvents = new Dictionary<string, List<GameAggregateEvent>>();

        if (aggregateEventColors == null)
            aggregateEventColors = new Dictionary<string, Color>();

        if (aggregatePathEdges == null)
            aggregatePathEdges = new Dictionary<int, Dictionary<int, int>>();

        if (aggregatePath == null)
            aggregatePath = new List<PathAggregateEdge>();

        if (pLogs == null)
        {
            pLogs = new Dictionary<string, PlayerLog>();
            ClearData();
        }
    }

    //Called when the editor detects a change in the object's properties.
    //Can in theory be called every frame in real-time.
    private void Update()
    {
        //Check to see if path data needs to be resampled.
        if(displaySampleRate != oldSampleRate)
        {
            //Resample data for display.
            ResamplePaths();
            ReaggregatePath();
        }

        if (aggregateGridEdge != oldAggregateEdge)
            ReaggregatePath();

        //Check to see if cluster radii have changed.
        if (gameClusterRadius != oldGameClusterRadius)
            ReclusterEvents();

        if (inputClusterRadius != oldInputClusterRadius)
            ReclusterEvents();

        oldSampleRate = displaySampleRate;
        oldAggregateEdge = aggregateGridEdge;
        oldGameClusterRadius = gameClusterRadius;
        oldInputClusterRadius = inputClusterRadius;
    }
    
    //Called when the user requests to load log files from a given directory.
    public void LoadLogs()
    {
        //Create directory path from specified folder.
        string directoryPath = Application.dataPath + "/" + logDirectory + "/";

        //Check directory validity.
        if(!Directory.Exists(directoryPath))
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

        //Update input/game event keys present in dataset.
        allInputEvents.Clear();
        allGameEvents.Clear();

        allInputEvents = new List<KeyCode>(enabledInputEvents.Keys);
        allGameEvents = new List<string>(enabledGameEvents.Keys);

        ReaggregatePath();
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

                        Vector3 p = new Vector3(float.Parse(lineContents[2]), float.Parse(lineContents[3]), float.Parse(lineContents[4]));

                        //Store the "extents" of our data - used in path aggregation.
                        if (Mathf.Abs(p.x) > maxExtents.x)
                            maxExtents.x = Mathf.Abs(p.x);
                        if (Mathf.Abs(p.y) > maxExtents.y)
                            maxExtents.y = Mathf.Abs(p.y);
                        if (Mathf.Abs(p.z) > maxExtents.z)
                            maxExtents.z = Mathf.Abs(p.z);

                        pLog.positions.Add(float.Parse(lineContents[1]), p);

                        pLog.orientations.Add(float.Parse(lineContents[1]),
                            Quaternion.Euler(float.Parse(lineContents[5]), float.Parse(lineContents[6]), float.Parse(lineContents[7])));

                        break;
                    
                    //Input events.
                    case OGLogManager.LogItemType.INPUT:
                        {
                            if (lineContents.Length < OGLogger.INLOG_L)
                                throw new System.Exception(string.Format("Log parsing error on line {0}.", lineNumber));

                            Vector3 pos = new Vector3(
                                float.Parse(lineContents[3]),
                                float.Parse(lineContents[4]),
                                float.Parse(lineContents[5]));

                            KeyCode key = (KeyCode)System.Enum.Parse(typeof(KeyCode), lineContents[2]);

                            if (!enabledInputEvents.ContainsKey(key))
                            {
                                enabledInputEvents.Add(key, false);
                                aggregateInputColors.Add(key, defaultEventColors[eIndex]);
                                aggregateInputEvents.Add(key, new List<InputAggregateEvent>());

                                eIndex = Mathf.Clamp(eIndex + 1, 0, defaultEventColors.Length - 1);
                            }
                                
                            pLog.inputEvents.Add(new PlayerLog.InputEvent(float.Parse(lineContents[1]),
                                pos, key));

                            break;
                        }

                    //Game events.
                    case OGLogManager.LogItemType.GAME_EVENT:
                        {
                            if (lineContents.Length < OGLogger.GLOG_L)
                                throw new System.Exception(string.Format("Log parsing error on line {0}.", lineNumber));

                            Vector3 pos = new Vector3(
                                float.Parse(lineContents[3]),
                                float.Parse(lineContents[4]),
                                float.Parse(lineContents[5]));

                            if (!enabledGameEvents.ContainsKey(lineContents[2]))
                            {
                                enabledGameEvents.Add(lineContents[2], false);
                                aggregateEventColors.Add(lineContents[2], defaultEventColors[eIndex]);
                                aggregateGameEvents.Add(lineContents[2], new List<GameAggregateEvent>());

                                eIndex = Mathf.Clamp(eIndex + 1, 0, defaultEventColors.Length - 1);
                            }
                                
                            pLog.gameEvents.Add(new PlayerLog.GameEvent(float.Parse(lineContents[1]),
                                pos, lineContents[2]));

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
        pLog.ResamplePath(displaySampleRate, flattenX, flattenY, flattenZ, flattenCoordinates);

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
        allInputEvents.Clear();
        enabledInputEvents.Clear();
        allGameEvents.Clear();
        enabledGameEvents.Clear();

        //Clear aggregation.
        aggregateInputEvents.Clear();
        aggregateInputColors.Clear();
        aggregateGameEvents.Clear();
        aggregateEventColors.Clear();

        //Clear path aggregation.
        maxExtents = new Vector3(-1, -1, -1);
        aggregatePath.Clear();
        aggregatePathEdges.Clear();

        //Reset path/event colour defaults.
        cIndex = 0;
        eIndex = 0;

        //print("OGVis data cleared from current session.");
    }
    
    //Resample individual paths.
    //Updates according to desired sample rate, axis flattening, etc.
    public void ResamplePaths()
    {
        foreach (KeyValuePair<string, PlayerLog> pLog in pLogs)
        {
            pLog.Value.ResamplePath(displaySampleRate, flattenX, flattenY, flattenZ, flattenCoordinates);
        }
    }

    //Re-do event aggregation.
    public void ReclusterEvents()
    {
        //Clear out aggregation lists.
        foreach(KeyValuePair<KeyCode, List<InputAggregateEvent>> aggregate in 
            aggregateInputEvents)
        {
            aggregate.Value.Clear();
        }
        
        foreach(KeyValuePair<string, List<GameAggregateEvent>> aggregate in
            aggregateGameEvents)
        {
            aggregate.Value.Clear();
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

    //Re-do path aggregation.
    public void ReaggregatePath()
    {
        //Clear out any existing data.
        aggregatePath.Clear();
        aggregatePathEdges.Clear();

        //Recompute grid dimensions.
        //Integer number of rows, columns, and "planes" based on grid edge.
        gridSize = new Vector3(
            2 * Mathf.Ceil(maxExtents.x / aggregateGridEdge) + 1, 
            2 * Mathf.Ceil(maxExtents.y / aggregateGridEdge) + 1, 
            2 * Mathf.Ceil(maxExtents.z / aggregateGridEdge) + 1);

        int XZPlaneSize = (int)(gridSize.x * gridSize.z);
        int XRowSize = (int)(gridSize.x);

        //Used per-point to clamp to the grid.
        Vector3 gridCoords = Vector3.zero;
        int gridIndex = 0;

        foreach (PlayerLog pLog in pLogs.Values)
        {
            if (!pLog.visInclude && aggregateActiveOnly)
                continue;

            //This list will store all of our individual path points as grid indices.
            //These indices are sequential, using the unsigned indices of our grid edges
            //to compute a unique point id. (See calculation of "gridIndex" below.)
            List<int> gridIndices = new List<int>();

            foreach(Vector3 pos in pLog.pathPoints)
            {
                //Compute signed grid coordinates (integer indices of Cartesian grid edges).
                gridCoords.x = Mathf.Round(pos.x / aggregateGridEdge);
                gridCoords.y = Mathf.Round(pos.y / aggregateGridEdge);
                gridCoords.z = Mathf.Round(pos.z / aggregateGridEdge);

                //Compute unsigned grid coordinates ("most negative" edge maps to 0).
                gridCoords.x += Mathf.Floor(gridSize.x / 2);
                gridCoords.y += Mathf.Floor(gridSize.y / 2);
                gridCoords.z += Mathf.Floor(gridSize.z / 2);

                //What is our unique point id?
                //Y grid index times size of XZ plane plus...
                //Z grid index times size of X row plus...
                //X grid index.
                gridIndex = (int)((gridCoords.y * XZPlaneSize) +
                    (gridCoords.z * XRowSize) +
                    gridCoords.x);

                gridIndices.Add(gridIndex);
            }

            //Now we're going to go through the list of positions which have been 
            //snapped to the grid, to extract the "edges" as represented by our aggregate path.
            //This discretization isn't perfect as it may skip some edges at low sample rates,
            //but it is fairly scaleable in real time, which is what we care about.

            //This will be the "origin point" of our current edge.
            int lastGridIndex = 0;

            if (gridIndices.Count > 0)
                lastGridIndex = gridIndices[0];

            for(int i = 1; i < gridIndices.Count; ++i)
            {
                //We haven't moved on the grid - skip a "self-edge".
                if (gridIndices[i] == lastGridIndex)
                    continue;

                //If we don't have a collection of edges with the current origin, make a 
                //new one.
                if (!aggregatePathEdges.ContainsKey(lastGridIndex))
                    aggregatePathEdges.Add(lastGridIndex, new Dictionary<int, int>());

                //If our current edge collection doesn't contain a copy of this edge,
                //make one.
                if (!aggregatePathEdges[lastGridIndex].ContainsKey(gridIndices[i]))
                    aggregatePathEdges[lastGridIndex].Add(gridIndices[i], 0);

                //Increase the count of edges between these grid points.
                ++aggregatePathEdges[lastGridIndex][gridIndices[i]];
                //Update our "origin" point.
                lastGridIndex = gridIndices[i];
            }
        }

        //Now we have to decode our edge list back into points that can be drawn
        //on the screen.
        int yComponent = 0, zComponent = 0, xComponent = 0;

        foreach (KeyValuePair<int, Dictionary<int, int>> edgeGroup in aggregatePathEdges)
        {
            //Decode index back into unsigned grid coordinates.
            yComponent = edgeGroup.Key / XZPlaneSize;
            zComponent = (edgeGroup.Key % XZPlaneSize) / XRowSize;
            xComponent = edgeGroup.Key - yComponent * XZPlaneSize - zComponent * XRowSize;

            //Decode into signed grid coordinates and multiply by edge length.
            Vector3 origin = new Vector3(
                (xComponent - Mathf.Floor(gridSize.x / 2)) * aggregateGridEdge,
                (yComponent - Mathf.Floor(gridSize.y / 2)) * aggregateGridEdge,
                (zComponent - Mathf.Floor(gridSize.z / 2)) * aggregateGridEdge);

            //Do the same thing for our destination points.
            foreach (KeyValuePair<int, int> edge in edgeGroup.Value)
            {
                yComponent = edge.Key / XZPlaneSize;
                zComponent = (edge.Key % XZPlaneSize) / XRowSize;
                xComponent = edge.Key - yComponent * XZPlaneSize - zComponent * XRowSize;

                Vector3 dest = new Vector3(
                    (xComponent - Mathf.Floor(gridSize.x / 2)) * aggregateGridEdge,
                    (yComponent - Mathf.Floor(gridSize.y / 2)) * aggregateGridEdge,
                    (zComponent - Mathf.Floor(gridSize.z / 2)) * aggregateGridEdge);

                //Compose a new aggregate edge based on our decoded origin, destination,
                //and the count of edges recorded between those points.
                aggregatePath.Add(new PathAggregateEdge(origin, dest, edge.Value));
            }
        }

        //Compute the appropriate display width of each edge.
        foreach(PathAggregateEdge edge in aggregatePath)
        {
            edge.displayWidth = RangeRemap(edge.weight, 
                aggregateEdgeExtents.min, aggregateEdgeExtents.max,
                MIN_PATH_WIDTH, MAX_PATH_WIDTH);
        }
    }

    //Aggregates all events from the given list of player IDs.
    private void AddEventsToAggregate(List<string> pids)
    {
        float inputEventRadius = inputClusterRadius * inputClusterRadius;

        //Aggregate input.
        //For every new player log...
        foreach (string pid in pids)
        {
            //For every input event in that player log...
            foreach (PlayerLog.InputEvent inputEvent in pLogs[pid].inputEvents)
            {
                bool clustered = false;

                List<InputAggregateEvent> clusterList = aggregateInputEvents[inputEvent.key];

                //Check for a viable cluster.
                foreach (InputAggregateEvent cluster in clusterList)
                {
                    if (Vector3.SqrMagnitude(inputEvent.pos - cluster.pos) <= inputEventRadius)
                    {
                        clustered = true;

                        if (!cluster.pCounts.ContainsKey(pid))
                            cluster.pCounts[pid] = 0;

                        ++cluster.pCounts[pid];
                        ++cluster.tCount;

                        //Recalculate cluster centre.
                        float weight = 1.0f / cluster.tCount;
                        cluster.pos = (1.0f - weight) * cluster.pos + weight * inputEvent.pos;

                        break;
                    }
                }

                //If no suitable cluster is found, create a new one.
                if (!clustered)
                {
                    InputAggregateEvent newAggregate = new InputAggregateEvent(
                        inputEvent.key, inputEvent.pos);

                    newAggregate.pCounts[pid] = 1;
                    newAggregate.tCount = 1;

                    clusterList.Add(newAggregate);
                }
            }
        }

        float gameEventRadius = gameClusterRadius * gameClusterRadius;

        //Aggregate game events.
        //For every new player log...
        foreach (string pid in pids)
        {
            //For every game event in that player log...
            foreach (PlayerLog.GameEvent gameEvent in pLogs[pid].gameEvents)
            {
                bool clustered = false;
                List<GameAggregateEvent> clusterList = aggregateGameEvents[gameEvent.eventID];

                //Check for a viable cluster.
                foreach (GameAggregateEvent cluster in clusterList)
                {
                    if (Vector3.SqrMagnitude(gameEvent.pos - cluster.pos) <= gameEventRadius)
                    {
                        clustered = true;

                        if (!cluster.pCounts.ContainsKey(pid))
                            cluster.pCounts[pid] = 0;

                        ++cluster.pCounts[pid];
                        ++cluster.tCount;

                        //Recalculate cluster centre.
                        float weight = 1.0f / cluster.tCount;
                        cluster.pos = (1.0f - weight) * cluster.pos + weight * gameEvent.pos;

                        break;
                    }
                }

                //If no suitable cluster is found, create a new one.
                if (!clustered)
                {
                    GameAggregateEvent newAggregate = new GameAggregateEvent(
                        gameEvent.eventID, gameEvent.pos);

                    newAggregate.pCounts[pid] = 1;
                    newAggregate.tCount = 1;
                    clusterList.Add(newAggregate);
                }
            }
        }

        ReweightAggregateEvents();
    }

    //Recalculates display size for aggregate events.
    private void ReweightAggregateEvents()
    {
        //Based on the number of events for active player records,
        //adjust the size of the displayed icons.
        foreach(KeyValuePair<KeyCode, List<InputAggregateEvent>> aggregation in 
            aggregateInputEvents)
        {
            foreach(InputAggregateEvent aggregate in aggregation.Value)
            {
                aggregate.displaySize = RangeRemap(aggregate.tCount, 
                    inputExtents.min, inputExtents.max, 
                    MIN_EVENT_RADIUS, MAX_EVENT_RADIUS);
            }
        }

        //Repeat for game events.
        foreach(KeyValuePair<string, List<GameAggregateEvent>> aggregation in
            aggregateGameEvents)
        {
            foreach(GameAggregateEvent aggregate in aggregation.Value)
            {
                aggregate.displaySize = RangeRemap(aggregate.tCount,
                    gameExtents.min, gameExtents.max,
                    MIN_EVENT_RADIUS, MAX_EVENT_RADIUS);
            }
        }
    }

    //Maps v defined on scale [o1, o2] to return value on scale [s1, s2].
    public static float RangeRemap(float v, float o1, float o2, float s1, float s2)
    {
        return Mathf.Clamp(s1 + ((v - o1) / (o2 - o1)) * (s2 - s1), s1, s2);
    }
}
