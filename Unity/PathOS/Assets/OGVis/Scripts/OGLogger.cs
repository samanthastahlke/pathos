using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

/*
OGLogger.cs
OGLogger (c) Ominous Games 2018-19
*/

public class OGLogger : MonoBehaviour 
{
    //Expected log row lengths.
    public const int POSLOG_L = 8;
    public const int INTLOG_L = 6;

    //Output and timers.
    public StreamWriter logOutput { get; set; }
    private float sampleTimer = 0.0f;

    private static OGLogManager mgr;

    private void Awake()
    {
        if (null == mgr)
            mgr = OGLogManager.instance;
    }

    private void Start()
    {
        sampleTimer = mgr.sampleTime;
    }

    private void Update()
	{
        //Sample position/orientation.
        if (sampleTimer >= mgr.sampleTime)
        {
            sampleTimer -= mgr.sampleTime;
            LogPosition();
        }

        sampleTimer += Time.deltaTime;
    }
    
    //Called from manager to write custom data into log file.
    public void WriteHeader(string header)
    {
        string line = OGLogManager.LogItemType.HEADER + "," +
            header;

        logOutput.WriteLine(line);
    }

    //Called from manager for custom GameObject interactions.
    public void LogInteraction(string objectName, Transform location)
    {
        objectName = Regex.Replace(objectName, ",", string.Empty);

        string line = OGLogManager.LogItemType.INTERACTION + "," +
            mgr.gameTimer + "," +
            objectName + "," +
            location.position.x + "," +
            location.position.y + "," +
            location.position.z;

        logOutput.WriteLine(line);
    }

    //Transform logging.
    private void LogPosition()
    {
        string line = OGLogManager.LogItemType.POSITION + "," +
            mgr.gameTimer + "," +
            transform.position.x + "," +
            transform.position.y + "," +
            transform.position.z + "," +
            transform.rotation.x + "," +
            transform.rotation.y + "," +
            transform.rotation.z;

        logOutput.WriteLine(line);
    }
}
