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

    public PathOSAgentMemory memory;

    [Range(-1.0f, 1.0f)]
    public float exploreScaling;
    [Range(-1.0f, 1.0f)]
    public float achievementScaling;
    [Range(-1.0f, 1.0f)]
    public float aggressiveScaling;
    [Range(0.0f, 1.0f)]
    public float experienceScaling;

    public float routeComputeTime = 1.0f;
    public float perceptionComputeTime = 0.25f;
    public float visitThreshold = 1.0f;
    public Camera playerEyes;
    private float perceptionTimer = 0.0f;
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

    private void Start()
    {
        ProcessPerception();
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
                perceivedInfo.entities.Add(new PerceivedEntity(entity.entityRef, entity.entityType, entityPos));
        }

        for(int i = 0; i < perceivedInfo.entities.Count; ++i)
        {
            memory.Memorize(perceivedInfo.entities[i]);
        }

        for(int i = 0; i < memory.memory.Count; ++i)
        {
            if ((agent.transform.position - memory.memory[i].pos).magnitude < visitThreshold)
                memory.memory[i].visited = true;      
        }
    }

    void ComputeNewDestination()
    {
        Vector3 dest = agent.transform.position;
        float maxScore = -10000.0f;

        for(int i = 0; i < memory.memory.Count; ++i)
        {
            ScoreEntity(memory.memory[i], ref dest, ref maxScore);
        }

        currentDestination = dest;
        agent.SetDestination(dest);
    }

    void ScoreEntity(PerceivedEntity entity, ref Vector3 dest, ref float maxScore)
    {
        if (memory.Visited(entity)) 
            return;

        float bias = 0.0f;

        switch (entity.entityType)
        {
            case EntityType.ET_GOAL:
                bias = achievementScaling;
                break;

            case EntityType.ET_POI:
                bias = exploreScaling;
                break;

            case EntityType.ET_ENEMY:
                bias = aggressiveScaling;
                break;
        }

        float score = ScoreDirection(entity.pos - agent.transform.position, bias);

        if (score > maxScore)
        {
            maxScore = score;
            dest = entity.pos;
        }
    }

    float ScoreDirection(Vector3 dir, float bias)
    {
        dir.Normalize();
        float score = bias;

        for(int i = 0; i < memory.memory.Count; ++i)
        {
            if (memory.memory[i].visited)
                continue;

            Vector3 entityVec = memory.memory[i].pos - agent.transform.position;
            float dist2entity = entityVec.magnitude;
            float distFactor = 1.0f / dist2entity * dist2entity;
            Vector3 dir2entity = entityVec.normalized;

            float dot = Vector3.Dot(dir, dir2entity);

            switch(memory.memory[i].entityType)
            {
                case EntityType.ET_ENEMY:
                    score += aggressiveScaling * dot * distFactor;
                    break;

                case EntityType.ET_GOAL:
                    score += achievementScaling * dot * distFactor;
                    break;

                case EntityType.ET_POI:
                    score += exploreScaling * dot * distFactor;
                    break;
            }
        }

        return score;
    }

	void Update() 
	{
        routeTimer += Time.deltaTime;
        perceptionTimer += Time.deltaTime;

        if(perceptionTimer >= perceptionComputeTime)
        {
            perceptionTimer = 0.0f;
            ProcessPerception();
        }

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
