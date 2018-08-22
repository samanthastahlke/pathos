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
    }

    private void OnDrawGizmos()
    {
        if (!sceneInit)
            return;

        Matrix4x4 tmp = Gizmos.matrix;
        Gizmos.matrix = Matrix4x4.TRS(agent.eyes.cam.transform.position,
            agent.eyes.cam.transform.rotation,
            agent.eyes.cam.transform.localScale);

        Gizmos.DrawFrustum(Vector3.zero, 
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
