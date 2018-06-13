using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using PathOS;

/*
PathOSAgent.cs 
PathOSAgent (c) Ominous Games 2018
*/

[RequireComponent(typeof(NavMeshAgent))]
public class PathOSAgent : MonoBehaviour 
{
    private NavMeshAgent agent;
    private static PathOSManager manager;

    [Range(-1.0f, 1.0f)]
    public float explorerScaling;
    [Range(-1.0f, 1.0f)]
    public float achieverScaling;
    [Range(-1.0f, 1.0f)]
    public float aggressiveScaling;

    public float routeComputeTime = 1.0f;
    public Camera playerEyes;
    private float routeTimer = 0.0f; 

    private PerceivedInfo perceivedInfo;
    private Vector3 currentDestination;

    private float xFOV;

	void Awake()
	{
        agent = GetComponent<NavMeshAgent>();

        if (null == manager)
            manager = PathOSManager.instance;

        perceivedInfo = new PerceivedInfo();

        xFOV = playerEyes.fieldOfView;
        currentDestination = agent.transform.position;
	}

    void ProcessPerception()
    {
        Plane[] frustum = GeometryUtility.CalculateFrustumPlanes(playerEyes);

        perceivedInfo.entities.Clear();
        perceivedInfo.navDirections.Clear();

        for (int i = 0; i < manager.levelEntities.Count; ++i)
        {
            LevelEntity entity = manager.levelEntities[i];
            Vector3 entityPos = entity.entityRef.transform.position;
            Vector3 ray = playerEyes.transform.position - entityPos;

            if (entity.rend != null
                && GeometryUtility.TestPlanesAABB(frustum, entity.rend.bounds)
                && !Physics.Raycast(entityPos, ray.normalized, ray.magnitude))
                perceivedInfo.entities.Add(new PerceivedEntity(entity.entityType, entityPos));

        }
    }

    void ComputeNewDestination()
    {
        Vector3 dest = agent.transform.position;

        for(int i = 0; i < perceivedInfo.entities.Count; ++i)
        {
            PerceivedEntity entity = perceivedInfo.entities[i];

            if (entity.entityType == EntityType.ET_GOAL)
                dest = entity.pos;
        }

        currentDestination = dest;
        agent.SetDestination(dest);
    }

	void Update() 
	{
        routeTimer += Time.unscaledDeltaTime;

        if(routeTimer >= routeComputeTime)
        {
            routeTimer = 0.0f;
            ProcessPerception();
            ComputeNewDestination();
        }
	}

    public List<Vector3> GetPerceivedEntityPositions()
    {
        List<Vector3> results = new List<Vector3>();

        for(int i = 0; i < perceivedInfo.entities.Count; ++i)
        {
            if((currentDestination - perceivedInfo.entities[i].pos).magnitude > 0.1f)
                results.Add(perceivedInfo.entities[i].pos);
        }

        return results;
    }

    public Vector3 GetTargetPosition()
    {
        return currentDestination;
    }
    
}
