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

    //Legend.
    public bool showLegend = false;

    private float padding = 8.0f;
    private float legendSize = 20.0f;

    private List<Rect> mapLegendIcons;
    private List<Rect> mapLegendLabels;

    private List<Rect> gizmoLegendIcons;
    private List<Rect> gizmoLegendLabels;

    public static Color[] mapLegendColors =
    {
        PathOS.UI.mapUnknown,
        PathOS.UI.mapSeen,
        PathOS.UI.mapVisited,
        PathOS.UI.mapObstacle
    };

    public static Texture2D[] mapLegendTextures;

    public static string[] mapLegendText =
    {
        "Unknown",
        "Seen",
        "Visited",
        "Obstacle"
    };

    private Texture2D blankLegendTex;

    //Gizmo/legend textures.
    private string eyeTex = "eyecon";
    private string targetTex = "target";
    private string visitTex = "visited";
    private string memoryTex = "brain";
    private string iconExtension = ".png";

    private Texture[] gizmoLegendTextures;

    private string[] gizmoLegendText =
    {
        "Target",
        "Visited",
        "Visible",
        "In Memory"
    };

    [Header("Map Drawing")]
    //Should we show the navmesh map contained in the agent's memory?
    //The purpose of this is twofold:
    //First, it is great for debugging. Which is invaluable given both the frequency
    //and severity of mistakes I make whilst programming.
    //Second, in the release version of the framework, it helps in understanding the 
    //agent's behaviour and contributes to improved transparency.
    public bool showMemoryMap = true;

    [Tooltip("The maximum size of the memory map on screen (in pixels)")]
    [Range(32.0f, 512.0f)]
    public float mapScreenSize = 128;
    private float oldMapSize;

    private Texture navmeshMemoryMap;
    private Rect navmeshMapScreenCoords;

    [Header("Player View")]
    public bool showPlayerView = true;

    [Tooltip("The maximum size of the player view on screen (in pixels)")]
    [Range(64.0f, 512.0f)]
    public float viewScreenSize = 128;
    private float oldViewSize;
    private const int MAX_PLAYER_RENDER_SIZE = 512;

    private RenderTexture playerViewTexture;
    private Rect playerViewTextureCoords;

    //Which camera should be used for screen-space transformation?
    private Camera transformCam;
    private bool sceneInit = false;

    private void Start()
    {
        transformCam = Camera.main;
        sceneInit = true;

        gizmoLegendTextures = new Texture[4];
        gizmoLegendTextures[0] = Resources.Load<Texture2D>(targetTex);
        gizmoLegendTextures[1] = Resources.Load<Texture2D>(visitTex);
        gizmoLegendTextures[2] = Resources.Load<Texture2D>(eyeTex);
        gizmoLegendTextures[3] = Resources.Load<Texture2D>(memoryTex);

        //We want to draw the memory "map" in the lower-left corner of the screen.
        //Grab a persistent reference to the texture.
        navmeshMemoryMap = agent.memory.memoryMap.GetVisualGrid();

        //Map legend.
        mapLegendIcons = new List<Rect>();
        mapLegendLabels = new List<Rect>();
        mapLegendTextures = new Texture2D[mapLegendColors.Length];

        for (int i = 0; i < mapLegendColors.Length; ++i)
        {
            mapLegendIcons.Add(new Rect(0.0f, 0.0f, legendSize, legendSize));
            mapLegendLabels.Add(new Rect());

            Texture2D colorTex = new Texture2D(1, 1);
            colorTex.SetPixel(0, 0, mapLegendColors[i]);
            colorTex.Apply();

            mapLegendTextures[i] = colorTex;
        }

        UpdateNavmeshMapCoords();

        //Gizmo legend.
        Vector2 pos = Vector2.zero;

        pos.x = padding;
        pos.y = padding;

        gizmoLegendIcons = new List<Rect>();
        gizmoLegendLabels = new List<Rect>();

        for(int i = 0; i < gizmoLegendText.Length; ++i)
        {
            gizmoLegendIcons.Add(new Rect(pos.x, pos.y, legendSize, legendSize));
            gizmoLegendLabels.Add(new Rect(pos.x + legendSize + padding, pos.y, 100.0f, legendSize));

            pos.y += legendSize + padding;
        }

        //Player view texture.
        Camera eyesCamera = agent.eyes.cam;
        float eyesAspect = eyesCamera.aspect;

        float playerViewX = 0.0f, playerViewY = 0.0f;

        if(eyesAspect > 1.0f)
        {
            playerViewX = MAX_PLAYER_RENDER_SIZE;
            playerViewY = playerViewX / eyesAspect;
        }
        else
        {
            playerViewY = MAX_PLAYER_RENDER_SIZE;
            playerViewX = playerViewY * eyesAspect;
        }

        playerViewTexture = new RenderTexture((int)playerViewX, 
            (int)playerViewY, 16);

        eyesCamera.targetTexture = playerViewTexture;
        eyesCamera.enabled = true;

        UpdateRenderViewCoords();
    }

    private void UpdateNavmeshMapCoords()
    {
        //Little bit of simple math to constrain the map's size and ensure
        //it is drawn in the correct location.
        float navmeshMapAsp = agent.memory.memoryMap.GetAspect();
        float navmeshMapX = 0.0f, navmeshMapY = 0.0f;

        if (navmeshMapAsp > 1.0f)
        {
            navmeshMapX = mapScreenSize;
            navmeshMapY = navmeshMapX / navmeshMapAsp;
        }
        else
        {
            navmeshMapY = mapScreenSize;
            navmeshMapX = navmeshMapY * navmeshMapAsp;
        }

        navmeshMapScreenCoords = new Rect(0.0f, Screen.height - navmeshMapY,
            navmeshMapX, navmeshMapY);

        //Map legend.
        Vector2 pos = new Vector2(navmeshMapScreenCoords.x + navmeshMapX + padding,
            navmeshMapScreenCoords.y);

        for (int i = 0; i < mapLegendColors.Length; ++i)
        {
            mapLegendIcons[i] = new Rect(pos.x, pos.y, legendSize, legendSize);
            mapLegendLabels[i] = new Rect(pos.x + legendSize + padding, pos.y, 100.0f, legendSize);

            pos.y += legendSize + padding;
        }
    }

    private void UpdateRenderViewCoords()
    {
        Camera eyesCamera = agent.eyes.cam;
        float eyesAspect = eyesCamera.aspect;

        float playerViewX = 0.0f, playerViewY = 0.0f;

        if (eyesAspect > 1.0f)
        {
            playerViewX = viewScreenSize;
            playerViewY = playerViewX / eyesAspect;
        }
        else
        {
            playerViewY = viewScreenSize;
            playerViewX = playerViewY * eyesAspect;
        }

        playerViewTextureCoords = new Rect(Screen.width - playerViewX,
            Screen.height - playerViewY, playerViewX, playerViewY);
    }

    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.Space))
        {
            showLegend = !showLegend;
        }
    }

    private void OnApplicationQuit()
    {
        Destroy(playerViewTexture);

        for(int i = 0; i < mapLegendTextures.Length; ++i)
        {
            Destroy(mapLegendTextures[i]);
        }
    }

    private void OnGUI()
    {
        if (!sceneInit)
            return;

        if (oldMapSize != mapScreenSize)
            UpdateNavmeshMapCoords();

        if (oldViewSize != viewScreenSize)
            UpdateRenderViewCoords();

        if (showMemoryMap)
            GUI.DrawTexture(navmeshMapScreenCoords,
                navmeshMemoryMap, ScaleMode.ScaleToFit, false);

        if (showPlayerView)
            GUI.DrawTexture(playerViewTextureCoords,
                playerViewTexture, ScaleMode.ScaleToFit, false);

        if(showLegend)
        {
            for (int i = 0; i < mapLegendText.Length; ++i)
            {
                GUI.DrawTexture(mapLegendIcons[i], mapLegendTextures[i]);
                GUI.Label(mapLegendLabels[i], mapLegendText[i]);
            }

            for (int i = 0; i < gizmoLegendText.Length; ++i)
            {
                GUI.DrawTexture(gizmoLegendIcons[i], gizmoLegendTextures[i], ScaleMode.ScaleToFit);
                GUI.Label(gizmoLegendLabels[i], gizmoLegendText[i]);
            }
        }

        oldMapSize = mapScreenSize;
        oldViewSize = viewScreenSize;
    }

    private void OnDrawGizmosSelected()
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

        Vector3 targetPos = agent.GetTargetPosition();

        List<PathOS.EntityMemory> memory = agent.memory.entities;

        //Memorized objects.
        for (int i = 0; i < memory.Count; ++i)
        {
            Vector3 pos = memory[i].entity.perceivedPos;

            //Skip if this entity is the target.
            if (Vector3.SqrMagnitude(pos - targetPos) 
                < PathOS.Constants.Navigation.GOAL_EPSILON_SQR)
                continue;

            //Draw the visited, memorized, or visible icon as appropriate.
            if (memory[i].visited)
                Gizmos.DrawIcon(GetGizmoIconPos(pos), visitTex + iconExtension);
            else if (memory[i].entity.visible)
                Gizmos.DrawIcon(GetGizmoIconPos(pos), eyeTex + iconExtension);
            else
                Gizmos.DrawIcon(GetGizmoIconPos(pos), memoryTex + iconExtension);
        }

        Gizmos.DrawIcon(GetGizmoIconPos(agent.GetTargetPosition()), targetTex + iconExtension);

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
