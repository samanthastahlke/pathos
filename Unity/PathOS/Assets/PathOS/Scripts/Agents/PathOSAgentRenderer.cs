using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

/*
PathOSAgentRenderer.cs 
PathOSAgentRenderer (c) Nine Penguins (Samantha Stahlke) 2018
*/

public class PathOSAgentRenderer : MonoBehaviour 
{
    public PathOSAgent agent;
    public float iconSize = 16.0f;
    public Texture eyecon;
    public Texture target;

    private Camera transformCam;
    private bool sceneInit = false;

    private void Start()
    {
        transformCam = Camera.main;
        sceneInit = true;
    }

    private void OnGUI()
    {
        if (!sceneInit)
            return;

        List<Vector3> visiblePos = agent.GetPerceivedEntityPositions();
        Vector3 targetPos = agent.GetTargetPosition();

        for(int i = 0; i < visiblePos.Count; ++i)
        {
            GUI.DrawTexture(GetIconRect(visiblePos[i]), eyecon);
        }

        Rect targetRect = GetIconRect(targetPos);
        GUI.DrawTexture(targetRect, target);
    }

    private Rect GetIconRect(Vector3 pos)
    {
        Vector3 screenPoint = transformCam.WorldToScreenPoint(pos);

        return new Rect(screenPoint.x - 0.5f * iconSize, 
            Screen.height - (screenPoint.y + 0.5f * iconSize), iconSize, iconSize);
    }
}
