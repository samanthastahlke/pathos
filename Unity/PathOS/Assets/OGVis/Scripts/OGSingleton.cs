using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
OGSingleton.cs
OGSingleton (c) Ominous Games 2018
*/

public class OGSingleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T instanceRef;
    private static object lockRef = new object();

    public static T instance
    {
        get
        {
            lock(lockRef)
            {
                if (FindObjectsOfType(typeof(T)).Length > 1)
                    Debug.LogError(string.Format("Multiple instances of {0} found!", typeof(T)));

                if (instanceRef == null)
                    instanceRef = (T)FindObjectOfType(typeof(T));

                return instanceRef;
            }
        }
    }
}
