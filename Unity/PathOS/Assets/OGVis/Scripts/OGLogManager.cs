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
    //Maximum number of file duplicates to check.
    private const int DUPLICATE_MAX = 32;

    //Whether logging should be enabled.
    public bool enableLogging = true;

    //Reference to the GameObject serving as the player.
    public GameObject playerObject;
    private OGLogger pLog;

    //Specify directory/filename.
    public string logFolder = "OGLogs";
    public string playerID;

    public enum LogItemType
    {
        POSITION = 0,
        INPUT,
        GAME_EVENT
    };

    //Sample rate (for position/orientation data).
    [Range(0.1f, 60.0f)]
    public float sampleRate = 2.0f;
    public float sampleTime { get; set; }

    //Desired input events.
    public List<KeyCode> buttonEvents;

    private string logFilename;
    private string logDirectory;
    private string logFilenameFull;
    private StreamWriter logOutput;

    private void Awake()
	{
        if (!enableLogging)
            return;

        //Calculate the sampling time for our logger.
        sampleTime = 1.0f / sampleRate;

        //Create a logger and attach it to the player.
        pLog = playerObject.AddComponent<OGLogger>();

        //Create a directory if we don't have one.
        logDirectory = Application.dataPath + "/" + logFolder + "/";

        if (!Directory.Exists(logDirectory))
            Directory.CreateDirectory(logDirectory);

        //Grab a filename that doesn't yet exist.
        string filename = playerID + ".csv";

        for(int i = 0; i < DUPLICATE_MAX; ++i)
        {
            if (!File.Exists(logDirectory + filename))
                break;

            filename = playerID + "-" + (i + 1) + ".csv";
        }

        //Initialize and register stream output.
        logFilenameFull = logDirectory + filename;
        logOutput = File.CreateText(logFilenameFull);
        pLog.RegisterOutput(logOutput);
	}

    private void OnApplicationQuit()
    {
        //Clean up after ourselves.
        if (logOutput != null)
        {
            print("Wrote playtesting log to " + logFilenameFull);
            logOutput.Close();
        }
    }

    //Hook for custom game events.
    public void FireEvent(string eventKey)
    {
        if(pLog != null)
            pLog.LogGameEvent(eventKey);
    }
}
