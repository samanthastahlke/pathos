﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using PathOS;

/*
PathOSAgentEyes.cs 
PathOSAgentEyes (c) Nine Penguins (Samantha Stahlke) 2018
*/

public class PathOSAgentEyes : MonoBehaviour 
{
    public PathOSAgent agent;
    private static PathOSManager manager;

    public float navmeshCastDistance = 50.0f;
    public float navmeshCastHeight = 5.0f;

    //The agent's "eyes" - i.e., the camera the player would use.
    public Camera cam;

    //What can the agent "see" currently?
    public List<PerceivedEntity> visible { get; set; }

    //Timer to handle visual processing checks. Roll for perception.
    private float perceptionTimer = 0.0f;

    //Vertex set for visibility checks on object bounds.
    private Vector3[] boundsCheck;

    //Field of view for immediate-range "explorability" checks.
    private float xFOV;
    
	void Awake()
	{
        visible = new List<PerceivedEntity>();

        if (null == manager)
            manager = PathOSManager.instance;

        boundsCheck = new Vector3[8];

        xFOV = Mathf.Rad2Deg * 2.0f * Mathf.Atan(
            cam.aspect * Mathf.Tan(cam.fieldOfView * Mathf.Deg2Rad * 0.5f));
    }

    public float XFOV()
    {
        return xFOV;
    }

    public void ProcessPerception()
    {
        Plane[] frustum = GeometryUtility.CalculateFrustumPlanes(cam);

        visible.Clear();

        Vector3 camForwardXZ = cam.transform.forward;
        camForwardXZ.y = 0.0f;

        for (int i = 0; i < manager.levelEntities.Count; ++i)
        {
            LevelEntity entity = manager.levelEntities[i];
            Vector3 entityPos = entity.entityRef.transform.position;

            Vector3 entityVecXZ = entityPos - cam.transform.position;
            entityVecXZ.y = 0.0f;
          
            //Visisbility check.
            //If object's renderer is in bounds of camera...
            //And we can draw a ray to the camera from that object without
            //hitting anything.
            if (entity.rend != null
                && Vector3.Dot(camForwardXZ, entityVecXZ) > 0
                && GeometryUtility.TestPlanesAABB(frustum, entity.rend.bounds)
                && RaycastVisibilityCheck(entity.rend.bounds, entityPos))
                visible.Add(new PerceivedEntity(entity.entityRef, entity.entityType, entityPos));
        }

        for (int i = 0; i < visible.Count; ++i)
        {
            agent.memory.Memorize(visible[i]);
        }
    }

    //Uses an AABB and given position as nine points for checking
    //visibility via raycast.
    bool RaycastVisibilityCheck(Bounds bounds, Vector3 pos)
    {
        Vector3 ray = cam.transform.position - pos;

        if (!Physics.Raycast(pos, ray.normalized, ray.magnitude))
            return true;

        boundsCheck[0].Set(bounds.min.x, bounds.min.y, bounds.min.z);
        boundsCheck[1].Set(bounds.min.x, bounds.min.y, bounds.max.z);
        boundsCheck[2].Set(bounds.min.x, bounds.max.y, bounds.min.z);
        boundsCheck[3].Set(bounds.min.x, bounds.max.y, bounds.max.z);
        boundsCheck[4].Set(bounds.max.x, bounds.min.y, bounds.min.z);
        boundsCheck[5].Set(bounds.max.x, bounds.min.y, bounds.max.z);
        boundsCheck[6].Set(bounds.max.x, bounds.max.y, bounds.min.z);
        boundsCheck[7].Set(bounds.max.x, bounds.max.y, bounds.max.z);

        for(int i = 0; i < boundsCheck.Length; ++i)
        {
            ray = cam.transform.position - boundsCheck[i];

            if (!Physics.Raycast(boundsCheck[i], ray.normalized, ray.magnitude))
                return true;
        }

        return false;
    }
	
    //This should be updated eventually to do a more sophisticated check accounting
    //for *apparent* distance - i.e., by adding a couple of physics raycasts from the 
    //camera.
    public NavMeshHit ExploreVisibilityCheck(Vector3 dir)
    {
        NavMeshHit hit = new NavMeshHit();
        bool result = NavMesh.Raycast(agent.transform.position,
            agent.transform.position + dir.normalized * navmeshCastDistance + Vector3.up * navmeshCastHeight,
            out hit, NavMesh.AllAreas);

        agent.memory.memoryMap.Fill(hit.position, 
            PathOSNavUtility.NavmeshMemoryMapper.NavmeshMapCode.NM_OBSTACLE);

        return hit;
    }

	void Update() 
	{
        perceptionTimer += Time.deltaTime;

        //Visual processing update.
        if (perceptionTimer >= agent.perceptionComputeTime)
        {
            perceptionTimer = 0.0f;
            ProcessPerception();
        }
    }
}