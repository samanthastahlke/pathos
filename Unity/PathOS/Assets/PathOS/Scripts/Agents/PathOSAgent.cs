using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using PathOS;

/*
PathOSAgent.cs 
PathOSAgent (c) Samantha Stahlke and Atiya Nova 2018
*/

[RequireComponent(typeof(NavMeshAgent))]
public class PathOSAgent : MonoBehaviour 
{
    /* OBJECT REFERENCES AND DEBUGGING */

    private NavMeshAgent navAgent;

    //The agent's memory/internal world model.
    public PathOSAgentMemory memory;
    //The agent's eyes/perception model.
    public PathOSAgentEyes eyes;

    //Used for testing.
    public bool freezeAgent;
    public bool verboseDebugging = false;

    /* PLAYER CHARACTERISTICS */

    [Range(0.0f, 1.0f)]
    public float experienceScale;

    public List<HeuristicScale> heuristicScales;
    private Dictionary<Heuristic, float> heuristicWeightLookup;
    private Dictionary<(Heuristic, EntityType), float> heuristicScoringLookup;

    /* NAVIGATION PROPERTIES */

    //How often will the agent re-assess available goals?
    public float routeComputeTime = 1.0f;
    //How often will the agent's "visual system" process information?
    public float perceptionComputeTime = 0.25f;
    //How narrow are the bands checked for "explorability"?
    public float exploreDegrees = 5.0f;
    //How narrow are the bands out-of-view checked for "explorability"?
    public float invisibleExploreDegrees = 30.0f;
    //How many degrees do we sway to either side when looking around?
    public float lookDegrees = 60.0f;
    //How long does it take to look around?
    public float lookTime = 2.0f;
    //How close does the agent have to get to a goal to mark it as visited?
    public float visitThreshold = 1.0f;
    //How close do two "exploration" goals have to be to be considered the same?
    public float exploreSimilarityThreshold = 2.0f;

    /* MEMORY STATS */
    //How quickly does the agent forget something in its memory?
    //This is for testing right now, basically just a flat value.
    public float forgetTime { get; set; }
    public int stmSize { get; set; }

    //Timers for handling rerouting and looking around.
    private float routeTimer = 0.0f;
    private float perceptionTimer = 0.0f;
    private float lookTimer = 0.0f;
    private bool lookingAround = false;

    //Where is the agent targeting?
    private Vector3 currentDestination;

    //Hazardous area detection.
    private float previousLookTime; 
    private float hazardousAreaTimer = 0;
    private bool hazardousArea = false;

    //For backtracking traversal.
    private bool detour = false; //can apply to a cautious agent backtracking or a reckless one heading towards danger
    private int currentPath = 0;

    private void Awake()
	{
        navAgent = GetComponent<NavMeshAgent>();
        currentDestination = navAgent.transform.position;

        heuristicWeightLookup = new Dictionary<Heuristic, float>();
        heuristicScoringLookup = new Dictionary<(Heuristic, EntityType), float>();

        PathOSManager manager = PathOSManager.instance;

        foreach(HeuristicScale curScale in heuristicScales)
        {
            heuristicWeightLookup.Add(curScale.heuristic, curScale.scale);
        }

        foreach(HeuristicWeightSet curSet in manager.heuristicWeights)
        {
            for(int j = 0; j < curSet.weights.Count; ++j)
            {
                heuristicScoringLookup.Add((curSet.heuristic, curSet.weights[j].entype), curSet.weights[j].weight);
            }
        }

        //Duration of working memory for game entities is scaled by experience level.
        forgetTime = Mathf.Lerp(PathOS.Constants.Memory.FORGET_TIME_MIN,
            PathOS.Constants.Memory.FORGET_TIME_MAX,
            experienceScale);

        //Capacitiy of working memory is also scaled by experience level.
        stmSize = Mathf.RoundToInt(Mathf.Lerp(PathOS.Constants.Memory.MEM_CAPACITY_MIN,
            PathOS.Constants.Memory.MEM_CAPACITY_MAX,
            experienceScale));

	}

    private void Start()
    {
        eyes.ProcessPerception();

        //The look timer changes depending on the curiosity
        lookTime -= (lookTime * (heuristicWeightLookup[Heuristic.CURIOSITY] * 0.5f));

        //Storing the original lookTime value so that we can switch back to it later
        previousLookTime = lookTime;

        //in case there's only the final goal
        memory.CheckGoals();
    }

    public Vector3 GetPosition()
    {
        return navAgent.transform.position;
    }

    //Used by the Inspector to ensure scale widgets will appear for all defined heuristics.
    //This SHOULD NOT be called by anything else.
    public void RefreshHeuristicList()
    {
        Dictionary<Heuristic, float> weights = new Dictionary<Heuristic, float>();

        for(int i = 0; i < heuristicScales.Count; ++i)
        {
            weights.Add(heuristicScales[i].heuristic, heuristicScales[i].scale);
        }

        heuristicScales.Clear();

        foreach(Heuristic heuristic in System.Enum.GetValues(typeof(Heuristic)))
        {
            float weight = 0.0f;

            if (weights.ContainsKey(heuristic))
                weight = weights[heuristic];

            heuristicScales.Add(new HeuristicScale(heuristic, weight));
        }
    }

    //Update the agent's target position.
    private void ComputeNewDestination()
    {
        //Base target = our existing destination.
        Vector3 dest = currentDestination;

        float maxScore = -10000.0f;

        //Potential entity goals.
        for(int i = 0; i < memory.entities.Count; ++i)
        {
            ScoreEntity(memory.entities[i].entity, ref dest, ref maxScore);
        }

        //Potential directional goals.
        //Only considering the XZ plane.
        float halfX = eyes.XFOV() * 0.5f;
        int steps = (int)(halfX / exploreDegrees);

        //In front of the agent.
        Vector3 XZForward = transform.forward;
        XZForward.y = 0.0f;
        XZForward.Normalize();
        
        ScoreExploreDirection(XZForward, true, ref dest, ref maxScore);

        for(int i = 1; i <= steps; ++i)
        {
            ScoreExploreDirection(Quaternion.AngleAxis(i * exploreDegrees, Vector3.up) * XZForward,
                true, ref dest, ref maxScore);
            ScoreExploreDirection(Quaternion.AngleAxis(i * -exploreDegrees, Vector3.up) * XZForward,
                true, ref dest, ref maxScore);
        }

        //Behind the agent (from memory).
        Vector3 XZBack = -XZForward;

        ScoreExploreDirection(XZBack, false, ref dest, ref maxScore);
        halfX = (360.0f - eyes.XFOV()) * 0.5f;
        steps = (int)(halfX / invisibleExploreDegrees);

        for(int i = 1; i <= steps; ++i)
        {
            ScoreExploreDirection(Quaternion.AngleAxis(i * invisibleExploreDegrees, Vector3.up) * XZBack,
                false, ref dest, ref maxScore);
            ScoreExploreDirection(Quaternion.AngleAxis(i * -invisibleExploreDegrees, Vector3.up) * XZBack,
                false, ref dest, ref maxScore);
        }

        //The existing goal.
        Vector3 goalForward = currentDestination - navAgent.transform.position;
        goalForward.y = 0.0f;
        
        if(goalForward.sqrMagnitude > 0.1f)
        {
            goalForward.Normalize();
            bool goalVisible = Mathf.Abs(Vector3.Angle(XZForward, goalForward)) < (eyes.XFOV() * 0.5f);
            ScoreExploreDirection(goalForward, goalVisible, ref dest, ref maxScore);
        }
        
        currentDestination = dest;
        navAgent.SetDestination(dest);

        if(verboseDebugging)
            NPDebug.LogMessage("Position: " + navAgent.transform.position + 
                ", Destination: " + dest);

        currentPath = memory.paths.Count - 1; //the most recent path

    }

    //maxScore is updated if the entity achieves a higher score.
    private void ScoreEntity(PerceivedEntity entity, ref Vector3 dest, ref float maxScore)
    {
        //A previously visited entity shouldn't be targeted.
        if (memory.Visited(entity)) 
            return;

        //Initial bias added to account for object's type.
        float bias = 0.0f;

        //Initial placeholder bias for preferring the goal we have already set.
        if ((entity.perceivedPos - currentDestination).magnitude < 0.1f
            && (navAgent.transform.position - currentDestination).magnitude > 0.1f)
            bias += 1.0f;

        //Weighted scoring function.
        foreach(HeuristicScale heuristicScale in heuristicScales)
        {
            (Heuristic, EntityType) key = (heuristicScale.heuristic, entity.entityType);

            if(!heuristicScoringLookup.ContainsKey(key))
            {
                NPDebug.LogError("Couldn't find key " + key.ToString() + " in heuristic scoring lookup!", typeof(PathOSAgent));
                continue;
            }

            bias += heuristicScale.scale * heuristicScoringLookup[key];
        }

        Vector3 toEntity = entity.perceivedPos - navAgent.transform.position;
        float score = ScoreDirection(toEntity, bias, toEntity.magnitude);

        if (score > maxScore)
        {
            maxScore = score;
            dest = entity.perceivedPos;
        }
    }

    //maxScore is updated if the direction achieves a higher score.
    void ScoreExploreDirection(Vector3 dir, bool visible, ref Vector3 dest, ref float maxScore)
    {
        float distance = 0.0f;
        Vector3 newDest = navAgent.transform.position;

        if(visible)
        {
            //Grab the "extent" of the direction on the navmesh from the perceptual system.
            NavMeshHit hit = eyes.ExploreVisibilityCheck(dir);
            distance = hit.distance;
            newDest = hit.position;
        }
        else
        {
            //Grab the "extent" of the direction on our memory model of the navmesh.
            PathOSNavUtility.NavmeshMemoryMapper.NavmeshMemoryMapperCastHit hit;
            memory.memoryMap.RaycastMemoryMap(navAgent.transform.position, dir, eyes.navmeshCastDistance, out hit);
            distance = hit.distance;

            newDest = PathOSNavUtility.GetClosestPointWalkable(
                navAgent.transform.position + distance * dir, memory.worldBorderMargin);
        }

        float bias = (distance / eyes.navmeshCastDistance) 
            * (heuristicWeightLookup[Heuristic.CURIOSITY] + 0.1f);

        //Initial placeholder bias for preferring the goal we have already set.
        //(If we haven't reached it already.)
        if ((newDest - currentDestination).magnitude < exploreSimilarityThreshold
            && (navAgent.transform.position - currentDestination).magnitude > exploreSimilarityThreshold)
            bias += 1.0f;

        float score = ScoreDirection(dir, bias, distance);

        if(score > maxScore)
        {
            maxScore = score;
            dest = newDest;
        }

        memory.AddPath(new ExploreMemory(navAgent.transform.position, dir, Vector3.Distance(navAgent.transform.position, dest)));
    }

    float ScoreDirection(Vector3 dir, float bias, float maxDistance)
    {
        dir.Normalize();

        //Score base = bias.
        float score = bias;

        //Add to the score based on our curiosity and the potential to 
        //"fill in our map" as we move in this direction.
        //This is similar to the scaling created by assessing an exploration direction.
        PathOSNavUtility.NavmeshMemoryMapper.NavmeshMemoryMapperCastHit hit;
        memory.memoryMap.RaycastMemoryMap(navAgent.transform.position, dir, maxDistance, out hit);
        score += (heuristicWeightLookup[Heuristic.CURIOSITY] + 0.1f) * hit.numUnexplored / PathOSNavUtility.NavmeshMemoryMapper.maxCastSamples;

        //Enumerate over all entities the agent knows about, and use them
        //to affect our assessment of the potential target.
        for (int i = 0; i < memory.entities.Count; ++i)
        {
            if (memory.entities[i].visited)
                continue;

            //Vector to the entity.
            Vector3 entityVec = memory.entities[i].entity.perceivedPos - navAgent.transform.position;
            float dist2entity = entityVec.magnitude;
            //Scale our factor by inverse square of distance.
            float distFactor = 1.0f / (dist2entity * dist2entity);
            Vector3 dir2entity = entityVec.normalized;

            float dot = Vector3.Dot(dir, dir2entity);
            dot = Mathf.Clamp(dot, 0.0f, 1.0f);

            //Weighted scoring function.
            foreach(HeuristicScale heuristicScale in heuristicScales)
            {
                (Heuristic, EntityType) key = (heuristicScale.heuristic, 
                    memory.entities[i].entity.entityType);

                if(!heuristicScoringLookup.ContainsKey(key))
                {
                    NPDebug.LogError("Couldn't find key " + key.ToString() + " in heuristic scoring lookup!", typeof(PathOSAgent));
                    continue;
                }

                bias += heuristicScale.scale * heuristicScoringLookup[key] * dot * distFactor;
            }
        }

        return score;
    }

	void Update() 
	{
        //Inactive state toggle for debugging purposes.
        if (freezeAgent)
            return;

        //Update spatial memory.
        memory.memoryMap.Fill(navAgent.transform.position);

        //Update of periodic actions.
        routeTimer += Time.deltaTime;
        perceptionTimer += Time.deltaTime;

        if(!lookingAround)
            lookTimer += Time.deltaTime;

        //Whenever this timer reaches a certain point, the agent checks to see if there are a lot of hazards in the area
        hazardousAreaTimer += Time.deltaTime;

        if (hazardousAreaTimer > 5)
        {
            //we update the lookTime based off of how hazardous the area is
            //if it's really hazardous then the agent is looking around constantly
            //these values are just placeholders, and this code should be more sophisticated for the future
            //**this will get cleaned up I swear**
            if (hazardousArea = memory.CheckHazards(currentDestination))
            {
                //checks to see if it's more cautious than hazardous
                //by comparing the caution scale to the aggression+adrenaline scale
               if (heuristicWeightLookup[Heuristic.CAUTION] >= 
                    ((heuristicWeightLookup[Heuristic.ADRENALINE] + heuristicWeightLookup[Heuristic.AGGRESSION])*0.5f) 
                    && heuristicWeightLookup[Heuristic.CAUTION]>0)
               {
                   ActivateDetour(Backtrack()); //if the conditions are met the backtrack detour will be activated
                   lookTime = 0.8f;
               }
               else if (heuristicWeightLookup[Heuristic.AGGRESSION] + heuristicWeightLookup[Heuristic.ADRENALINE] > 0)
               {
                   ActivateDetour(HeadTowardsHazards()); //otherwise it'll head towards danger
                   lookTime = previousLookTime; //restores the lookTime if it's not hazardous
               
               }
            }
            else
            {
                lookTime = previousLookTime; 
            }

            hazardousAreaTimer = 0;

        }

        //Rerouting update.
        if (routeTimer >= routeComputeTime && detour == false)
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
            if ((navAgent.transform.position - memory.entities[i].entity.perceivedPos).magnitude < visitThreshold)
            {
                memory.entities[i].visited = true;

                //whenever the agent reaches an area, there's a check to see if it can go to the final goal, or if there are still goals remaining
                memory.CheckGoals();
            }
        }
    }

    //Inelegant and brute-force "animation" of the agent to look around.
    //In the future this should add non-determinism and preferably be abstracted somewhere else.
    //And cleaned up. Probably.
    IEnumerator LookAround()
    {
        navAgent.isStopped = true;
        navAgent.updateRotation = false;

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
        navAgent.updateRotation = true;
        navAgent.isStopped = false;
    }

    //Takes the agent back to the last point they were at
    //This will be cleaned up
    IEnumerator Backtrack()
    {
        Vector3 originPoint = memory.paths[currentPath].originPoint;
        currentDestination = originPoint;

        //Then it goes down the path
        while (!((navAgent.transform.position - currentDestination).magnitude < 2))
        {
            navAgent.SetDestination(currentDestination);
            yield return null;
        }

        //the new destination after backtracking gets calculated
        Vector3 newDestination = memory.CalculateNewPath(currentPath);
        currentDestination = newDestination;
        navAgent.SetDestination(currentDestination);

        while (!((navAgent.transform.position - currentDestination).magnitude < 2)) 
        {
            navAgent.SetDestination(currentDestination); //sets the new destination
            yield return null;
        }

        //ends the detour
        detour = false;
        routeTimer = 0;
        hazardousAreaTimer = 0;
        StopCoroutine(Backtrack());
    }

    //If the agent is aggressive this will send them to the center of the hazardous area
    IEnumerator HeadTowardsHazards()
    {
        currentDestination = memory.CalculateCentroid();

        //Then it goes down the path
        while (!((navAgent.transform.position - currentDestination).magnitude < 2))
        {
            navAgent.SetDestination(currentDestination);
            yield return null;
        }

        detour = false;
        routeTimer = 0;
        hazardousAreaTimer = 0;
        StopCoroutine(HeadTowardsHazards());
    }
    
    public List<PerceivedEntity> GetPerceivedEntities()
    {
        return eyes.visible;
    }

    public Vector3 GetTargetPosition()
    {
        return currentDestination;
    }

    bool IsDetourValid()
    {
        //checks to see if the conditions are met to do a detour
        //will get cleaned up
        if (detour)
            return false;
        if (currentPath <= 0)
            return false;
        if (Vector3.Distance(currentDestination, navAgent.transform.position) <= 1.5f)
            return false;
        if (((heuristicWeightLookup[Heuristic.AGGRESSION] + heuristicWeightLookup[Heuristic.ADRENALINE]) * 0.5) 
            > heuristicWeightLookup[Heuristic.CAUTION] && Vector3.Distance(navAgent.transform.position, memory.CalculateCentroid()) < 3)
            return false;

        return true;
    }

    void ActivateDetour(IEnumerator theFunction)
    {
        //activates the relevant coroutine
        if (IsDetourValid())
        {
            detour = true;
            currentPath = memory.GetLastPath();
            StartCoroutine(theFunction);
        }
    }
}
