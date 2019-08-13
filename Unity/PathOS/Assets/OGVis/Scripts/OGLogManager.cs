using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/*
OGLogManager.cs
OGLogManager (c) Ominous Games 2018
*/

public class OGLogManager : OGSingleton<OGLogManager> 
{
    //Editor flags for manipulation during batching.
    public const string directoryOverrideId = "OGLogDirectoryOverride";
    public const string overrideFlagId = "OGLogOverrideFlag";
    public const string fileIndexId = "OGLogFileIndex";

    //Whether logging should be enabled.
    public bool enableLogging = true;
    
    //Specify directory/filename.
    public string logDirectory = "--";
    public string logFilePrefix = "agent";

    //Sample rate (for position/orientation data).
    [Range(0.1f, 60.0f)]
    [Tooltip("How often position/orientation should be recorded (per second)")]
    public float sampleRate = 2.0f;
    public float sampleTime { get; set; }

    private List<GameObject> logObjects = new List<GameObject>();
    private Dictionary<int, OGLogger> loggers = new Dictionary<int, OGLogger>();

    public enum LogItemType
    {
        POSITION = 0,
        INTERACTION,
        HEADER
    };

    //Queried by loggers for timestamps.
    public float gameTimer { get; set; }

    private void Awake()
	{
        if (!enableLogging)
            return;

        if(!LogDirectoryValid())
        {
            Debug.LogError("Log manager has no valid directory set! Logs will not " +
                "be recorded.\n Log manager needs a directory on this computer " +
                "outside of the Assets folder.");

            return;
        }

        bool forceDirectoryOverride = PlayerPrefs.GetInt(overrideFlagId) != 0;

        if(forceDirectoryOverride)
        {
            logDirectory += "/" + PlayerPrefs.GetString(directoryOverrideId) + "/";

            if (!Directory.Exists(logDirectory))
                Directory.CreateDirectory(logDirectory);
        }
        else
        {
            //Create a unique folder inside the logging directory
            //with the current timestamp.
            logDirectory += "/" + PathOS.UI.GetFormattedTimestamp() + "/";

            if (!Directory.Exists(logDirectory))
                Directory.CreateDirectory(logDirectory);
            else
            {
                Debug.LogWarning("A log folder with this timestamp " +
                    "already exists in the specified directory! Logs will " +
                    "not be written.");

                return;
            }
        }
        
        foreach(PathOSAgent agent in FindObjectsOfType<PathOSAgent>())
        {
            logObjects.Add(agent.gameObject);
        }

        //Calculate the sampling time for our logger.
        sampleTime = 1.0f / sampleRate;

        //File index for numbering logs.
        int fileIndex = (forceDirectoryOverride) ? 
            PlayerPrefs.GetInt(fileIndexId, 0) : 0;

        //Create loggers for each of the needed objects.
        for (int i = logObjects.Count - 1; i >= 0; --i)
        {
            if(null == logObjects[i])
            {
                logObjects.RemoveAt(i);
                continue;
            }

            OGLogger logger = logObjects[i].AddComponent<OGLogger>();

            string filename = logFilePrefix + "-" + fileIndex.ToString() + ".csv";
            logger.InitStream(logDirectory + filename);

            logger.WriteHeader("SAMPLE," + sampleRate);

            loggers.Add(logObjects[i].GetInstanceID(), logger);

            ++fileIndex;
        }

        PlayerPrefs.SetInt(fileIndexId, fileIndex);
        PlayerPrefs.Save();

        gameTimer = 0.0f;
	}

    private void Update()
    {
        gameTimer += Time.deltaTime;
    }

    public bool LogDirectoryValid()
    {
        return Directory.Exists(logDirectory)
            && !logDirectory.StartsWith(Application.dataPath);
    }

    private void OnApplicationQuit()
    {
        PlayerPrefs.SetInt(overrideFlagId, 0);

        if(loggers.Count > 0)
            print("Wrote agent logs to " + logDirectory);

        //Clean up after ourselves.
        foreach (KeyValuePair<int, OGLogger> logPair in loggers)
        {
            logPair.Value.DisposeStream();
        }

        loggers.Clear();
    }

    //Hook for writing headers/metadata.
    public void WriteHeader(GameObject caller, string header)
    {
        if(enableLogging)
        {
            int instanceID = caller.GetInstanceID();

            if (loggers.ContainsKey(instanceID))
                loggers[instanceID].WriteHeader(header);
        }
    }

    //Hook for interaction/visiting objects.
    public void FireInteractionEvent(GameObject caller, GameObject interacted)
    {
        if(enableLogging)
        {
            int instanceID = caller.GetInstanceID();

            if (loggers.ContainsKey(instanceID))
                loggers[instanceID].LogInteraction(interacted.name, interacted.transform);
        }
    }
}
