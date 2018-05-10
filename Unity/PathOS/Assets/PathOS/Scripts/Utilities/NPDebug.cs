using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
NPDebug.cs 
NPDebug (c) Nine Penguins (Samantha Stahlke) 2018
*/

public class NPDebug
{
    public static void LogWarning(string msg, System.Type caller = null)
    {
        if (caller != null)
            Debug.LogWarning(string.Format("Warning <{0}>: {1}", caller, msg));
        else
            Debug.LogWarning(string.Format("Warning: {0}", msg));

    }

    public static void LogError(string msg, System.Type caller = null)
    {
        if (caller != null)
            Debug.LogError(string.Format("Error <{0}>: {1}", caller, msg));
        else
            Debug.LogError(string.Format("Error: {0}", msg));
    }

    public static void LogMessage(string msg, System.Type caller = null)
    {
        if (caller != null)
            Debug.Log(string.Format("Message <{0}>: {1}", caller, msg));
        else
            Debug.Log(msg);
    }
}
