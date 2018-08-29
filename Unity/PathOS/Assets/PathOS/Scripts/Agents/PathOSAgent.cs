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

    //The agent's memory/internal world model.
    public PathOSAgentMemory memory;
    //The agent's eyes/perception model.
    public PathOSAgentEyes eyes;

    //Used for testing.
    public bool freezeAgent;

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
    //How narrow are the bands checked for "explorability"?
    public float exploreDegrees = 5.0f;
    //How many degrees do we 
    public float lookDegrees = 60.0f;
    //How long does it take to look around?
    public float lookTime = 2.0f;
    //How close does the agent have to get to a goal to mark it as visited?
    public float visitThreshold = 1.0f;

    //How quickly does the agent forget something in its memory?
    //This is for testing right now, basically just a flat value.
    public float forgetTime = 1.0f;

    //Timers for handling rerouting and looking around.
    private float routeTimer = 0.0f;
    private float perceptionTimer = 0.0f;
    private float lookTimer = 0.0f;
    private bool lookingAround = false;

    //Where is the agent targeting?
    private Vector3 currentDestination;

	void Awake()
	{
        agent = GetComponent<NavMeshAgent>();
        currentDestination = agent.transform.position;
	}

    private void Start()
    {
        eyes.ProcessPerception();
    }

    //Update the agent's target position.
    void ComputeNewDestination()
    {
        //Base target = where we're standing.
        Vector3 dest = agent.transform.position;
        float maxScore = -10000.0f;

        //Potential entity goals.
        for(int i = 0; i < memory.entities.Count; ++i)
        {
            ScoreEntity(memory.entities[i], ref dest, ref maxScore);
        }

        //Potential directional goals.
        float halfX = eyes.XFOV() * 0.5f;
        int steps = (int)(halfX / exploreDegrees);

        Vector3 XZForward = transform.forward;
        XZForward.y = 0.0f;
        XZForward.Normalize();

        ScoreExploreDirection(XZForward, ref dest, ref maxScore);

        for(int i = 1; i <= steps; ++i)
        {
            ScoreExploreDirection(Quaternion.AngleAxis(i * exploreDegrees, Vector3.up) * XZForward,
                ref dest, ref maxScore);
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

            case EntityType.ET_GOAL_MANDATORY:
                bias += completionScaling + efficiencyScaling;
                break;

            case EntityType.ET_GOAL_COMPLETION:
                bias += efficiencyScaling;
                break;

            case EntityType.ET_RESOURCE_ACHIEVEMENT:
                bias += completionScaling;
                break;

            case EntityType.ET_RESOURCE_PRESERVATION:
                bias += cautionScaling + completionScaling;
                break;

            case EntityType.ET_HAZARD_ENVIRONMENT:
                bias += aggressiveScaling + adrenalineScaling - cautionScaling;
                break;
        }

        float score = ScoreDirection(entity.pos - agent.transform.position, bias);

        if (score > maxScore)
        {
            maxScore = score;
            dest = entity.pos;
        }
    }

    //maxScore is updated if the direction achieves a higher score.
    void ScoreExploreDirection(Vector3 dir, ref Vector3 dest, ref float maxScore)
    {
        //Grab the "extent" of the direction on the navmesh from the perceptual system.
        NavMeshHit hit = eyes.ExploreVisibilityCheck(dir);

        float bias = (hit.distance
            / eyes.navmeshCastDistance)
            * (curiosityScaling + 0.1f);

        float score = ScoreDirection(dir, bias);

        if(score > maxScore)
        {
            maxScore = score;
            dest = hit.position;
        }
    }

    float ScoreDirection(Vector3 dir, float bias)
    {
        dir.Normalize();

        //Score base = bias.
        float score = bias;

        //Enumerate over all entities the agent knows about, and use them
        //to affect our assessment of the potential target.
        for(int i = 0; i < memory.entities.Count; ++i)
        {
            if (memory.entities[i].visited)
                continue;

            //Vector to the entity.
            Vector3 entityVec = memory.entities[i].pos - agent.transform.position;
            float dist2entity = entityVec.magnitude;
            //Scale our factor by inverse square of distance.
            float distFactor = 1.0f / (dist2entity * dist2entity);
            Vector3 dir2entity = entityVec.normalized;

            float dot = Vector3.Dot(dir, dir2entity);
            
            switch(memory.entities[i].entityType)
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

                case EntityType.ET_GOAL_MANDATORY:
                    dot = Mathf.Clamp(dot, 0.0f, 1.0f);
                    score += completionScaling * dot * distFactor + efficiencyScaling * dot * distFactor;
                    break;

                case EntityType.ET_GOAL_COMPLETION:
                    dot = Mathf.Clamp(dot, 0.0f, 1.0f);
                    score += efficiencyScaling * dot * distFactor;
                    break;

                case EntityType.ET_RESOURCE_ACHIEVEMENT:
                    dot = Mathf.Clamp(dot, 0.0f, 1.0f);
                    score += completionScaling * dot * distFactor;
                    break;

                case EntityType.ET_RESOURCE_PRESERVATION:
                    dot = Mathf.Clamp(dot, 0.0f, 1.0f);
                    score += cautionScaling * dot * distFactor + completionScaling * dot * distFactor;
                    break;

                case EntityType.ET_HAZARD_ENVIRONMENT:
                    dot = Mathf.Clamp(dot, 0.0f, 1.0f);
                    score += aggressiveScaling * dot * distFactor + adrenalineScaling * dot * distFactor - cautionScaling * dot * distFactor;
                    break;
            }
        }

        return score;
    }

	void Update() 
	{
        if(Input.GetKeyDown(KeyCode.Space))
            print(eyes.ExploreVisibilityCheck(transform.forward));

        if (freezeAgent)
            return;

        //Update spatial memory.
        memory.memoryMap.Traverse(agent.transform.position);

        //Update of periodic actions.
        routeTimer += Time.deltaTime;
        perceptionTimer += Time.deltaTime;

        if(!lookingAround)
            lookTimer += Time.deltaTime;

        //Rerouting update.
        if(routeTimer >= routeComputeTime)
        {
            routeTimer = 0.0f;
            perceptionTimer = 0.0f;
            eyes.ProcessPerception();
            ComputeNewDestination();
        }

        //Perception update.
        if(perceptionTimer >= perceptionComputeTime)
        {
            perceptionTimer = 0.0f;
            eyes.ProcessPerception();
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
        for (int i = 0; i < memory.entities.Count; ++i)
        {
            if ((agent.transform.position - memory.entities[i].pos).magnitude < visitThreshold)
                memory.entities[i].visited = true;
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
        Quaternion right = Quaternion.AngleAxis(lookDegrees, Vector3.up) * home;
        Quaternion left = Quaternion.AngleAxis(-lookDegrees, Vector3.up) * home;

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
        return eyes.visible;
    }

    public Vector3 GetTargetPosition()
    {
        return currentDestination;
    }   
}
