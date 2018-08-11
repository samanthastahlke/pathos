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
    public Texture targetIcon;
    public Texture visitedIcon;
    public Texture memoryIcon;

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

        List<PathOS.PerceivedEntity> visible = agent.GetPerceivedEntities();
        Vector3 targetPos = agent.GetTargetPosition();

        List<PathOS.PerceivedEntity> memory = agent.memory.memory;

        for(int i = 0; i < memory.Count; ++i)
        {
            if (Vector3.SqrMagnitude(memory[i].pos - targetPos) < 0.2f)
                continue;

            if (!memory[i].visited && visible.Contains(memory[i]))
                GUI.DrawTexture(GetIconRect(memory[i].pos), eyecon);
            else
                GUI.DrawTexture(GetIconRect(memory[i].pos), 
                    (memory[i].visited) ? visitedIcon : memoryIcon);
        }

        Rect targetRect = GetIconRect(targetPos);
        GUI.DrawTexture(targetRect, targetIcon);
    }

    private Rect GetIconRect(Vector3 pos)
    {
        Vector3 screenPoint = transformCam.WorldToScreenPoint(pos);

        return new Rect(screenPoint.x - 0.5f * iconSize, 
            Screen.height - (screenPoint.y + 0.5f * iconSize), iconSize, iconSize);
    }
}
