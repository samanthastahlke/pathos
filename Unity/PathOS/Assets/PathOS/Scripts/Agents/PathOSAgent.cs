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

    //The agent's memory/internal world model.
    public PathOSAgentMemory memory;

    /* MOTIVATORS */
    //This list is in flux alongside the typology review.
    //Most of these are not used yet, and none are implemented FULLY.
    //Right now we're based on aggression, caution, curiosity, 
    //completion, and achievement.
    //These are used to score entities as prospective goals.
    [Header("Agent Motivations")]
    [Range(0.0f, 1.0f)]
    public float curiosityScaling;
    [Range(0.0f, 1.0f)]
    public float achievementScaling;
    [Range(0.0f, 1.0f)]
    public float aggressiveScaling;
    [Range(0.0f, 1.0f)]
    public float cautionScaling;
    [Range(0.0f, 1.0f)]
    public float adrenalineScaling;
    [Range(0.0f, 1.0f)]
    public float completionScaling;
    [Range(0.0f, 1.0f)]
    public float efficiencyScaling;
    [Range(0.0f, 1.0f)]
    public float experienceScaling;

    //How often will the agent re-assess available goals?
    public float routeComputeTime = 1.0f;
    //How often will the agent's "visual system" process information?
    public float perceptionComputeTime = 0.25f;
    //How long does it take to look around?
    public float lookTime = 2.0f;
    //How close does the agent have to get to a goal to mark it as visited?
    public float visitThreshold = 1.0f;

    //How quickly does the agent forget something in its memory?
    //This is for testing right now, basically just a flat value.
    public float forgetTime = 1.0f;

    //The agent's "eyes" - i.e., the camera the player would use.
    public Camera playerEyes;

    //Timers for handling visual processing, rerouting, and looking around.
    private float perceptionTimer = 0.0f;
    private float routeTimer = 0.0f;
    private float lookTimer = 0.0f;
    private bool lookingAround = false;

    //What can the agent "see" currently?
    private PerceivedInfo perceivedInfo;
    //Where is the agent targeting?
    private Vector3 currentDestination;

	void Awake()
	{
        agent = GetComponent<NavMeshAgent>();

        if (null == manager)
            manager = PathOSManager.instance;

        perceivedInfo = new PerceivedInfo();

        currentDestination = agent.transform.position;
	}

    private void Start()
    {
        ProcessPerception();
    }

    //Update the agent's "visual system".
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

            //Visisbility check.
            //If object's renderer is in bounds of camera...
            //And we can draw a ray to the camera from that object without
            //hitting anything. (The raycast right now is naive and uses
            //only the object's centre, will need to be improved.)
            if (entity.rend != null
                && GeometryUtility.TestPlanesAABB(frustum, entity.rend.bounds)
                && !Physics.Raycast(entityPos, ray.normalized, ray.magnitude))
                perceivedInfo.entities.Add(new PerceivedEntity(entity.entityRef, entity.entityType, entityPos));
        }

        for(int i = 0; i < perceivedInfo.entities.Count; ++i)
        {
            memory.Memorize(perceivedInfo.entities[i]);
        }
    }

    //Update the agent's target position.
    void ComputeNewDestination()
    {
        //Base target = where we're standing.
        Vector3 dest = agent.transform.position;
        float maxScore = -10000.0f;

        //Right now, we're only going to score entities as potential goals.
        //In the future, this should include blank "rays" cast out from the agent
        //which consider environmental geometry in addition to entities in judging
        //their exploration/reward potential.
        for(int i = 0; i < memory.memory.Count; ++i)
        {
            ScoreEntity(memory.memory[i], ref dest, ref maxScore);
        }

        currentDestination = dest;
        agent.SetDestination(dest);
    }

    //maxScore is updated if the entity achieves a higher score.
    void ScoreEntity(PerceivedEntity entity, ref Vector3 dest, ref float maxScore)
    {
        if (memory.Visited(entity)) 
            return;

        //Initial bias added to account for object's type.
        float bias = 0.0f;

        //Initial placeholder bias for preferring the goal we have already set.
        if ((entity.pos - currentDestination).magnitude < 0.1f)
            bias += 1.0f;

        switch (entity.entityType)
        {
            case EntityType.ET_GOAL_OPTIONAL:
                bias += achievementScaling + completionScaling;
                break;

            case EntityType.ET_POI:
                bias += curiosityScaling;
                break;

            case EntityType.ET_HAZARD_ENEMY:
                bias += aggressiveScaling - cautionScaling;
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

        //Score base = bias.
        float score = bias;

        //Enumerate over all entities the agent knows about, and use them
        //to affect our assessment of the potential target.
        for(int i = 0; i < memory.memory.Count; ++i)
        {
            if (memory.memory[i].visited)
                continue;

            //Vector to the entity.
            Vector3 entityVec = memory.memory[i].pos - agent.transform.position;
            float dist2entity = entityVec.magnitude;
            //Scale our factor by inverse square of distance.
            float distFactor = 1.0f / (dist2entity * dist2entity);
            Vector3 dir2entity = entityVec.normalized;

            float dot = Vector3.Dot(dir, dir2entity);
            
            switch(memory.memory[i].entityType)
            {
                case EntityType.ET_HAZARD_ENEMY:
                    score += aggressiveScaling * dot * distFactor - cautionScaling * dot * distFactor;
                    break;

                case EntityType.ET_GOAL_OPTIONAL:
                    dot = Mathf.Clamp(dot, 0.0f, 1.0f);
                    score += achievementScaling * dot * distFactor + completionScaling * dot * distFactor;
                    break;

                case EntityType.ET_POI:
                    dot = Mathf.Clamp(dot, 0.0f, 1.0f);
                    score += curiosityScaling * dot * distFactor;
                    break;
            }
        }

        return score;
    }

	void Update() 
	{
        routeTimer += Time.deltaTime;
        perceptionTimer += Time.deltaTime;

        if(!lookingAround)
            lookTimer += Time.deltaTime;

        //Visual processing update.
        if(perceptionTimer >= perceptionComputeTime)
        {
            perceptionTimer = 0.0f;
            ProcessPerception();
        }

        //Rerouting update.
        if(routeTimer >= routeComputeTime)
        {
            routeTimer = 0.0f;
            ProcessPerception();
            ComputeNewDestination();
        }

        //Look-around update.
        if(lookTimer >= lookTime)
        {
            lookTimer = 0.0f;
            lookingAround = true;
            StartCoroutine(LookAround());
        }

        //Check to see if we've visited something.
        //This should be shifted to a more elegant trigger mechanism in the future.
        for (int i = 0; i < memory.memory.Count; ++i)
        {
            if ((agent.transform.position - memory.memory[i].pos).magnitude < visitThreshold)
                memory.memory[i].visited = true;
        }
    }

    //Inelegant and brute-force "animation" of the agent to look around.
    //In the future this should add non-determinism and preferably be abstracted somewhere else.
    //And cleaned up. Probably.
    IEnumerator LookAround()
    {
        agent.isStopped = true;
        agent.updateRotation = false;

        //Simple 90-degree sweep centred on current heading.
        Quaternion home = transform.rotation;
        Quaternion right = Quaternion.AngleAxis(45.0f, Vector3.up) * home;
        Quaternion left = Quaternion.AngleAxis(-45.0f, Vector3.up) * home;

        float lookingTime = 0.5f;
        float lookingTimer = 0.0f;

        while (lookingTimer < lookingTime)
        {
            transform.rotation = Quaternion.Slerp(home, right, lookingTimer / lookingTime);
            lookingTimer += Time.deltaTime;
            yield return null;
        }

        lookingTimer = 0.0f;

        while (lookingTimer < lookingTime)
        {
            lookingTimer += Time.deltaTime;
            yield return null;
        }

        lookingTimer = 0.0f;

        while (lookingTimer < lookingTime)
        {
            transform.rotation = Quaternion.Slerp(right, left, lookingTimer / lookingTime);
            lookingTimer += Time.deltaTime;
            yield return null;
        }

        lookingTimer = 0.0f;

        while (lookingTimer < lookingTime)
        {
            lookingTimer += Time.deltaTime;
            yield return null;
        }

        lookingTimer = 0.0f;

        while (lookingTimer < lookingTime)
        {
            transform.rotation = Quaternion.Slerp(left, home, lookingTimer / lookingTime);
            lookingTimer += Time.deltaTime;
            yield return null;
        }

        lookingTimer = 0.0f;
        lookingAround = false;
        agent.updateRotation = true;
        agent.isStopped = false;
    }

    public List<PerceivedEntity> GetPerceivedEntities()
    {
        return perceivedInfo.entities;
    }

    public Vector3 GetTargetPosition()
    {
        return currentDestination;
    }
    
}
