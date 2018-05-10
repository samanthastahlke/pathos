using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/*
PathOSAgent.cs 
PathOSAgent (c) Ominous Games 2018
*/

[RequireComponent(typeof(NavMeshAgent))]
public class PathOSAgent : MonoBehaviour 
{
    private NavMeshAgent agent;
    
    private static PathOSManager manager;

    public float routeComputeTime = 1.0f;
    private float routeTimer = 0.0f;

	void Awake()
	{
        agent = GetComponent<NavMeshAgent>();

        if (null == manager)
            manager = PathOSManager.instance;
	}
	
	void Start() 
	{
		
	}

    void ComputeNewDestination()
    {
        Vector3 dest = transform.position;

        for(int i = 0; i < manager.levelEntities.Count; ++i)
        {
            if (manager.levelEntities[i].entityType == PathOSManager.EntityType.ET_GOAL)
                dest = manager.levelEntities[i].entityRef.transform.position;
        }

        agent.SetDestination(dest);
    }

	void Update() 
	{
        routeTimer += Time.unscaledDeltaTime;

        if(routeTimer >= routeComputeTime)
        {
            routeTimer = 0.0f;
            ComputeNewDestination();
        }
	}
}
