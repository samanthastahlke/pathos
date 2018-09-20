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

    [Header("Icon Gizmos")]
    public float iconSize = 16.0f;
    public string iconExtension = ".png";

    //Haha, eye-con, get it? Thank you, thank you.
    //I'll be here all week. Well, for another four years. Well, hopefully longer.
    //I'm going to die at this damn school, aren't I? D:
    public Texture eyecon;
    private string eyeTex;
    public Texture targetIcon;
    private string targetTex;
    public Texture visitedIcon;
    private string visitTex;
    public Texture memoryIcon;
    private string memoryTex;

    [Header("Map Drawing")]
    //Should we show the navmesh map contained in the agent's memory?
    //The purpose of this is twofold:
    //First, it is great for debugging. Which is invaluable given both the frequency
    //and severity of mistakes I make whilst programming.
    //Second, in the release version of the framework, it helps in understanding the 
    //agent's behaviour and contributes to improved transparency.
    public bool showNavmeshMemoryMap = true;
    public float navmeshMapScreenMaxSize = 128;
    private Texture navmeshMemoryMap;
    private Rect navmeshMapScreenCoords;

    //Which camera should be used for screen-space transformation?
    private Camera transformCam;
    private bool sceneInit = false;

    private void Start()
    {
        transformCam = Camera.main;
        sceneInit = true;

        eyeTex = eyecon.name + iconExtension;
        targetTex = targetIcon.name + iconExtension;
        visitTex = visitedIcon.name + iconExtension;
        memoryTex = memoryIcon.name + iconExtension;

        //We want to draw the memory "map" in the lower-left corner of the screen.
        //Grab a persistent reference to the texture.
        navmeshMemoryMap = agent.memory.memoryMap.GetVisualGrid();

        //Little bit of simple math to constrain the map's size and ensure
        //it is drawn in the correct location.
        float navmeshMapAsp = agent.memory.memoryMap.GetAspect();
        float navmeshMapX = 0.0f, navmeshMapY = 0.0f;

        if(navmeshMapAsp > 1.0f)
        {
            navmeshMapX = navmeshMapScreenMaxSize;
            navmeshMapY = navmeshMapX / navmeshMapAsp;
        }
        else
        {
            navmeshMapY = navmeshMapScreenMaxSize;
            navmeshMapX = navmeshMapY * navmeshMapAsp;
        }

        navmeshMapScreenCoords = new Rect(0.0f, Screen.height - navmeshMapY, navmeshMapX, navmeshMapY);
    }

    private void OnGUI()
    {
        if (!sceneInit)
            return;

        if (showNavmeshMemoryMap)
            GUI.DrawTexture(navmeshMapScreenCoords,
                navmeshMemoryMap, ScaleMode.ScaleToFit, false);
    }

    private void OnDrawGizmos()
    {
        if (!sceneInit)
            return;

        Matrix4x4 tmp = Gizmos.matrix;

        Gizmos.matrix = Matrix4x4.TRS(Vector3.zero,
            agent.eyes.cam.transform.rotation,
            agent.eyes.cam.transform.localScale);

        Gizmos.DrawFrustum(agent.eyes.cam.transform.position, 
            agent.eyes.cam.fieldOfView, 
            agent.eyes.cam.farClipPlane, agent.eyes.cam.nearClipPlane, 
            agent.eyes.cam.aspect);

        Gizmos.matrix = tmp;

        if (Camera.current != null)
            transformCam = Camera.current;

        List<PathOS.PerceivedEntity> visible = agent.GetPerceivedEntities();
        Vector3 targetPos = agent.GetTargetPosition();

        List<PathOS.EntityMemory> memory = agent.memory.entities;

        //Memory will contain visible objects by default.
        for (int i = 0; i < memory.Count; ++i)
        {
            //Skip if this entity is the target.
            if (Vector3.SqrMagnitude(memory[i].pos - targetPos) < 0.2f)
                continue;

            //If not visited and visible, draw the visible icon.
            if (!memory[i].visited && visible.Contains(memory[i]))
                Gizmos.DrawIcon(GetGizmoIconPos(memory[i].pos), eyeTex);
            //Draw the visited icon or memorized icon as appropriate.
            else
                Gizmos.DrawIcon(GetGizmoIconPos(memory[i].pos),
                    (memory[i].visited) ? visitTex : memoryTex);
        }

        Gizmos.DrawIcon(GetGizmoIconPos(agent.GetTargetPosition()), targetTex);

        transformCam = Camera.main;
    }

    //World-space transformation for drawing overlay icons as gizmos.
    private Vector3 GetGizmoIconPos(Vector3 pos)
    {
        Vector3 screenPos = transformCam.WorldToScreenPoint(pos);
        screenPos.z -= 2.0f;

        return transformCam.ScreenToWorldPoint(screenPos);
    }
}
