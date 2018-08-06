using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/*
OGLogger.cs
OGLogger (c) Ominous Games 2018
*/

public class OGLogger : MonoBehaviour 
{
    //Expected log row lengths.
    public const int POSLOG_L = 8;
    public const int INLOG_L = 6;
    public const int GLOG_L = 6;

    //Output and timers.
    private StreamWriter logOutput;
    private float gameTimer = 0.0f;
    private float sampleTimer = 1000.0f;

    private OGLogManager mgr;

    private void Awake()
    {
        if (mgr == null)
            mgr = OGLogManager.instance;
    }

    public void RegisterOutput(StreamWriter stream)
    {
        logOutput = stream;
    }

	private void Update()
	{
        //Sample position/orientation.
        if (sampleTimer > mgr.sampleTime)
        {
            sampleTimer = 0.0f;
            LogPosition();
        }

        //Check for input events.
        for(int i = 0; i < mgr.buttonEvents.Count; ++i)
        {
            if (Input.GetKeyDown(mgr.buttonEvents[i]))
                LogInputEvent(mgr.buttonEvents[i]);
        }

        gameTimer += Time.deltaTime;
        sampleTimer += Time.deltaTime;
    }

    //Called from manager for custom event hooks.
    public void LogGameEvent(string eventKey)
    {
        string line = OGLogManager.LogItemType.GAME_EVENT + "," +
            gameTimer + "," +
            eventKey + "," +
            transform.position.x + "," +
            transform.position.y + "," +
            transform.position.z;

        logOutput.WriteLine(line);
    }

    //Transform logging.
    private void LogPosition()
    {
        string line = OGLogManager.LogItemType.POSITION + "," +
            gameTimer + "," +
            transform.position.x + "," +
            transform.position.y + "," +
            transform.position.z + "," +
            transform.rotation.x + "," +
            transform.rotation.y + "," +
            transform.rotation.z;

        logOutput.WriteLine(line);
    }

    //Input logging.
    private void LogInputEvent(KeyCode key)
    {
        string line = OGLogManager.LogItemType.INPUT + "," +
            gameTimer + "," +
            key + "," +
            transform.position.x + "," +
            transform.position.y + "," +
            transform.position.z;

        logOutput.WriteLine(line);
    }
}
