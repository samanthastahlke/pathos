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
    //Whether logging should be enabled.
    public bool enableLogging = true;

    //Specify directory/filename.
    public string logDirectory = "--";
    public string logFilePrefix = "agent";

    //Sample rate (for position/orientation data).
    [Range(0.1f, 60.0f)]
    [Tooltip("How often position/orientation should be recorded (every X seconds)")]
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

    public float gameTimer { get; set; }

    private void Awake()
	{
        if (!enableLogging)
            return;

        if(!LogDirectoryValid())
        {
            Debug.LogError("Log manager has no valid directory set! Logs will not " +
                "be recorded.");

            return;
        }

        //Create a unique folder inside the logging directory
        //with the current timestamp.
        logDirectory += "/" + System.DateTime.Now.ToString(
            "yyyy'-'MM'-'dd' 'HH'-'mm'-'ss") + "/";

        if (!Directory.Exists(logDirectory))
            Directory.CreateDirectory(logDirectory);
        else
        {
            Debug.LogWarning("A log folder with this timestamp " +
                "already exists in the specified directory! Logs will " +
                "not be written.");

            return;
        }

        //Calculate the sampling time for our logger.
        sampleTime = 1.0f / sampleRate;

        foreach(PathOSAgent agent in FindObjectsOfType<PathOSAgent>())
        {
            logObjects.Add(agent.gameObject);
        }

        int fileIndex = 0;

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
            logger.logOutput = File.CreateText(logDirectory + filename);

            logger.WriteHeader("SAMPLE " + sampleRate);

            loggers.Add(logObjects[i].GetInstanceID(), logger);

            ++fileIndex;
        }

        gameTimer = 0.0f;
	}

    private void Update()
    {
        gameTimer += Time.deltaTime;
    }

    public bool LogDirectoryValid()
    {
        return Directory.Exists(logDirectory);
    }

    private void OnApplicationQuit()
    {
        if(loggers.Count > 0)
            print("Wrote agent logs to " + logDirectory);

        //Clean up after ourselves.
        foreach (KeyValuePair<int, OGLogger> logPair in loggers)
        {
            logPair.Value.logOutput.Close();
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
