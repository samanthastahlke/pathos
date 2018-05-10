using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/*
NPSingleton.cs 
NPSingleton (c) Nine Penguins (Samantha Stahlke) 2018
*/

public class NPSingleton<T> : MonoBehaviour where T : MonoBehaviour 
{
    private static T instanceRef;
    private static object lockRef = new object();

    public static T instance
    {
        get
        {
            lock(lockRef)
            {
                if(FindObjectsOfType(typeof(T)).Length > 1)
                {
                    NPDebug.LogError("Multiple instances found!", typeof(T));
                }

                if(null == instanceRef)
                {
                    instanceRef = (T)FindObjectOfType(typeof(T));

                    if (null == instanceRef)
                        NPDebug.LogError("No reference found, but something is trying to access one!", typeof(T));
                }

                return instanceRef;
            }
        }
    }
}
