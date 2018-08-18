using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

/*
PathOSAgentRenderer.cs 
PathOSAgentRenderer (c) Nine Penguins (Samantha Stahlke) 2018
*/

//Used to draw the in-game overlay for debugging/visualization purposes.
public class PathOSAgentRenderer : MonoBehaviour 
{
    public PathOSAgent agent;
    public float iconSize = 16.0f;

    //Haha, eye-con, get it? Thank you, thank you.
    //I'll be here all week. Well, for another four years. Well, hopefully longer.
    //I'm going to die at this damn school, aren't I? D:
    public Texture eyecon;

    public Texture targetIcon;
    public Texture visitedIcon;
    public Texture memoryIcon;

    //Which camera should be used for screen-space transformation?
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

        List<PathOS.EntityMemory> memory = agent.memory.memory;

        //Memory will contain visible objects by default.
        for(int i = 0; i < memory.Count; ++i)
        {
            //Skip if this entity is the target.
            if (Vector3.SqrMagnitude(memory[i].pos - targetPos) < 0.2f)
                continue;

            //If not visited and visible, draw the visible icon.
            if (!memory[i].visited && visible.Contains(memory[i]))
                GUI.DrawTexture(GetIconRect(memory[i].pos), eyecon);
            //Draw the visited icon or memorized icon as appropriate.
            else
                GUI.DrawTexture(GetIconRect(memory[i].pos), 
                    (memory[i].visited) ? visitedIcon : memoryIcon);
        }

        //Draw the agent's target.
        Rect targetRect = GetIconRect(targetPos);
        GUI.DrawTexture(targetRect, targetIcon);
    }

    //Screen-space transformation for drawing overlay icons.
    private Rect GetIconRect(Vector3 pos)
    {
        Vector3 screenPoint = transformCam.WorldToScreenPoint(pos);

        return new Rect(screenPoint.x - 0.5f * iconSize, 
            Screen.height - (screenPoint.y + 0.5f * iconSize), iconSize, iconSize);
    }
}
