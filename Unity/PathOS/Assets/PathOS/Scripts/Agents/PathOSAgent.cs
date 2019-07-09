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
[RequireComponent(typeof(PathOSAgentMemory))]
[RequireComponent(typeof(PathOSAgentEyes))]
public class PathOSAgent : MonoBehaviour 
{
    /* OBJECT REFERENCES AND DEBUGGING */
    private NavMeshAgent navAgent;

    //The agent's memory/internal world model.
    public PathOSAgentMemory memory { get; set; }
    //The agent's eyes/perception model.
    public PathOSAgentEyes eyes { get; set; }

    private static PathOSManager manager;
    public static OGLogManager logger { get; set; }

    //Used for testing.
    public bool freezeAgent;
    private bool verboseDebugging = false;

    /* PLAYER CHARACTERISTICS */

    [Range(0.0f, 1.0f)]
    public float experienceScale;

    public List<HeuristicScale> heuristicScales;
    private Dictionary<Heuristic, float> heuristicScaleLookup;
    private Dictionary<(Heuristic, EntityType), float> entityScoringLookup;

    /* NAVIGATION PROPERTIES */

    [Tooltip("How close (in units) does the agent have to get " +
        "to a goal to mark it as visited?")]
    public float visitThreshold = 1.0f;
    public float visitThresholdSqr { get; private set; }

    [Tooltip("How many degrees should separate lines of " +
        "sight checked for \"explorability\" by the agent?")]
    public float exploreDegrees = 5.0f;
    
    [DisplayName("Explore Degrees (Back)")]
    [Tooltip("How many degrees should separate paths checked for " +
        "\"explorability\" behind (out of sight of) the agent?")]
    public float invisibleExploreDegrees = 30.0f;

    [Tooltip("How many degrees should the agent sway to either " +
        "side when looking around?")]
    public float lookDegrees = 60.0f;
    
    [Tooltip("How close do two \"exploration\" goals have to " +
        "be to be considered the same?")]
    public float exploreThreshold = 2.0f;

    [Tooltip("What's the search radius for finding a point on the navmesh when " +
        "setting an exploration target?")]
    public float exploreTargetMargin = 25.0f;

    /* MEMORY STATS */
    //How quickly does the agent forget something in its memory?
    //This is for testing right now, basically just a flat value.
    public float forgetTime { get; set; }
    public int stmSize { get; set; }

    //Timers for handling rerouting and looking around.
    private float routeTimer = 0.0f;
    private float perceptionTimer = 0.0f;
    private float baseLookTime = PathOS.Constants.Behaviour.LOOK_TIME_MAX;
    private float lookTime = PathOS.Constants.Behaviour.LOOK_TIME_MAX;
    private float lookTimer = 0.0f;
    private bool lookingAround = false;

    //Where is the agent targeting?
    private TargetDest currentDest;
    private bool pathResolved = true;

    //What is the total positive impact of all unvisited entities?
    //(Used to penalize level completion). 
    private float cumulativeEntityScore = 0.0f;
    private float pastCumulativeEntityScore = 0.0f;
    //Prevent initial selection of final goal as the target (edge case).
    private bool assessedGoalsInit = false;

    //Is the agent "finished" the level?
    public bool completed { get; set; }

    //For backtracking traversal.
    public float hazardPenalty { get; set; }
    private float memPathChance = PathOS.Constants.Behaviour.BASE_MEMORY_NAV_CHANCE;
    private bool onMemPath = false;
    private List<Vector3> memPathWaypoints;
    private Vector3 memWaypoint = Vector3.zero;

    private List<Vector3> unreachableReference;

    private void Awake()
	{
        eyes = GetComponent<PathOSAgentEyes>();
        memory = GetComponent<PathOSAgentMemory>();

        navAgent = GetComponent<NavMeshAgent>();
        completed = false;

        currentDest = new TargetDest();
        currentDest.pos = GetPosition();

        memPathWaypoints = new List<Vector3>();
        unreachableReference = new List<Vector3>();

        heuristicScaleLookup = new Dictionary<Heuristic, float>();
        entityScoringLookup = new Dictionary<(Heuristic, EntityType), float>();

        if(null == manager)
            manager = PathOSManager.instance;

        if (null == logger)
            logger = OGLogManager.instance;

        foreach (HeuristicScale curScale in heuristicScales)
        {
            heuristicScaleLookup.Add(curScale.heuristic, curScale.scale);
        }

        foreach(HeuristicWeightSet curSet in manager.heuristicWeights)
        {
            for(int j = 0; j < curSet.weights.Count; ++j)
            {
                entityScoringLookup.Add((curSet.heuristic, curSet.weights[j].entype), curSet.weights[j].weight);
            }
        }

        float avgAggressionScore = 0.5f
            * (entityScoringLookup[(Heuristic.AGGRESSION, EntityType.ET_HAZARD_ENEMY)]
            + entityScoringLookup[(Heuristic.AGGRESSION, EntityType.ET_HAZARD_ENVIRONMENT)]);

        float avgAdrenalineScore = 0.5f
            * (entityScoringLookup[(Heuristic.ADRENALINE, EntityType.ET_HAZARD_ENEMY)]
            + entityScoringLookup[(Heuristic.ADRENALINE, EntityType.ET_HAZARD_ENVIRONMENT)]);

        float avgCautionScore = 0.5f
            * (entityScoringLookup[(Heuristic.CAUTION, EntityType.ET_HAZARD_ENEMY)]
            + entityScoringLookup[(Heuristic.CAUTION, EntityType.ET_HAZARD_ENVIRONMENT)]);

        float hazardScore = heuristicScaleLookup[Heuristic.AGGRESSION] * avgAggressionScore
            + heuristicScaleLookup[Heuristic.ADRENALINE] * avgAdrenalineScore
            + heuristicScaleLookup[Heuristic.CAUTION] * avgCautionScore;

        hazardPenalty = -hazardScore;

        visitThresholdSqr = visitThreshold * visitThreshold;

        //Duration of working memory for game entities is scaled by experience level.
        forgetTime = Mathf.Lerp(PathOS.Constants.Memory.FORGET_TIME_MIN,
            PathOS.Constants.Memory.FORGET_TIME_MAX,
            experienceScale);

        //Capacitiy of working memory is also scaled by experience level.
        stmSize = Mathf.RoundToInt(Mathf.Lerp(PathOS.Constants.Memory.MEM_CAPACITY_MIN,
            PathOS.Constants.Memory.MEM_CAPACITY_MAX,
            experienceScale));

        //Base look time is scaled by curiosity.
        baseLookTime = Mathf.Lerp(PathOS.Constants.Behaviour.LOOK_TIME_MIN_EXPLORE,
            PathOS.Constants.Behaviour.LOOK_TIME_MAX,
            heuristicScaleLookup[Heuristic.CURIOSITY]);

        float memPathScale = (heuristicScaleLookup[Heuristic.CAUTION] 
            + 1.0f - heuristicScaleLookup[Heuristic.CURIOSITY]) 
            * 0.5f;

        memPathChance = Mathf.Lerp(PathOS.Constants.Behaviour.MEMORY_NAV_CHANCE_MIN,
            PathOS.Constants.Behaviour.MEMORY_NAV_CHANCE_MAX,
            memPathScale);

        lookTime = baseLookTime;
    }

    private void Start()
    {
        LogAgentData();
        PerceptionUpdate();

        //Stochastic initialization of look time.
        lookTimer = Random.Range(0.0f, lookTime);
    }

    private void LogAgentData()
    {
        if(logger != null)
        {
            string header = "";

            header += "HEURISTICS,";
            header += "EXPERIENCE," + experienceScale + ",";

            foreach(HeuristicScale scale in heuristicScales)
            {
                header += scale.heuristic + "," + scale.scale + ",";
            }

            logger.WriteHeader(this.gameObject, header);
        }
    }

    public Vector3 GetPosition()
    {
        return navAgent.transform.position;
    }

    private void UpdateLookTime()
    {
        lookTime = baseLookTime;

        //Actual look time can fluctuate based on the agent's caution and the 
        //danger in the current area.
        float lookTimeScale = memory.ScoreHazards(GetPosition()) * 
            heuristicScaleLookup[Heuristic.CAUTION];

        lookTime = Mathf.Min(baseLookTime,
            Mathf.Lerp(PathOS.Constants.Behaviour.LOOK_TIME_MAX,
            PathOS.Constants.Behaviour.LOOK_TIME_MIN_CAUTION,
            lookTimeScale));
    }
    
    private float RouteComputeTimeCalculated()
    {
        return PathOS.Constants.Navigation.ROUTE_COMPUTE_BASE
            + PathOS.Constants.Memory.RETRIEVAL_TIME * memory.entities.Count;
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
        TargetDest dest = new TargetDest(currentDest);

        float maxScore = -10000.0f;

        pastCumulativeEntityScore = cumulativeEntityScore;
        cumulativeEntityScore = 0.0f;

        //Potential entity goals.
        for(int i = 0; i < memory.entities.Count; ++i)
        {
            ScoreEntity(memory.entities[i], ref dest, ref maxScore);
        }

        //Potential directional goals.

        //Memorized paths.
        //Treated as not visible since they are based on the player's "idea" of the space.
        for (int i = 0; i < memory.paths.Count; ++i)
        {
            ScoreExploreDirection(memory.paths[i].originPoint,
                memory.paths[i].direction,
                false, ref dest, ref maxScore);
        }

        //Only considering the XZ plane.
        float halfX = eyes.XFOV() * 0.5f;
        int steps = (int)(halfX / exploreDegrees);

        //In front of the agent.
        Vector3 XZForward = transform.forward;
        XZForward.y = 0.0f;
        XZForward.Normalize();

        ScoreExploreDirection(GetPosition(), XZForward, true, ref dest, ref maxScore);

        for(int i = 1; i <= steps; ++i)
        {
            ScoreExploreDirection(GetPosition(), Quaternion.AngleAxis(i * exploreDegrees, Vector3.up) * XZForward,
                true, ref dest, ref maxScore);
            ScoreExploreDirection(GetPosition(), Quaternion.AngleAxis(i * -exploreDegrees, Vector3.up) * XZForward,
                true, ref dest, ref maxScore);
        }

        //Behind the agent (from memory).
        Vector3 XZBack = -XZForward;

        ScoreExploreDirection(GetPosition(), XZBack, false, ref dest, ref maxScore);
        halfX = (360.0f - eyes.XFOV()) * 0.5f;
        steps = (int)(halfX / invisibleExploreDegrees);

        for(int i = 1; i <= steps; ++i)
        {
            ScoreExploreDirection(GetPosition(), Quaternion.AngleAxis(i * invisibleExploreDegrees, Vector3.up) * XZBack,
                false, ref dest, ref maxScore);
            ScoreExploreDirection(GetPosition(), Quaternion.AngleAxis(i * -invisibleExploreDegrees, Vector3.up) * XZBack,
                false, ref dest, ref maxScore);
        }

        //The existing goal.
        if(currentDest.entity == null)
        {
            Vector3 goalForward = currentDest.pos - GetPosition();
            goalForward.y = 0.0f;

            if (goalForward.sqrMagnitude > 0.1f)
            {
                goalForward.Normalize();
                bool goalVisible = Mathf.Abs(Vector3.Angle(XZForward, goalForward)) < (eyes.XFOV() * 0.5f);
                ScoreExploreDirection(GetPosition(), goalForward, goalVisible, ref dest, ref maxScore,
                    true, currentDest.pos);
            }
        }

        //Only recompute goal routing if our new goal is different
        //from the previous goal.
        if(Vector3.SqrMagnitude(currentDest.pos - dest.pos) 
            > PathOS.Constants.Navigation.GOAL_EPSILON_SQR)
        {
            currentDest = dest;
            float memChanceRoll = Random.Range(0.0f, 1.0f);
            onMemPath = false;

            if (memChanceRoll <= memPathChance)
                onMemPath = memory.memoryMap.NavigateAStar(GetPosition(), currentDest.pos, ref memPathWaypoints);

            if (onMemPath)
            {
                navAgent.SetDestination(memPathWaypoints[0]);
                pathResolved = false;
                memWaypoint.x = memPathWaypoints[0].x;
                memWaypoint.z = memPathWaypoints[0].z;
            }
            else
                RouteDestination();

            //Once something has been selected as a destination,
            //commit it to long-term memory.
            if (null != dest.entity)
                memory.CommitLTM(dest.entity);

            currentDest = dest;
        }

        assessedGoalsInit = true;

        if(verboseDebugging)
            NPDebug.LogMessage("Position: " + navAgent.transform.position + 
                ", Destination: " + currentDest);
    }

    public PerceivedEntity GetDestinationEntity()
    {
        return currentDest.entity;
    }

    //maxScore is updated if the entity achieves a higher score.
    private void ScoreEntity(EntityMemory memory, ref TargetDest dest, ref float maxScore)
    {
        //A previously visited entity shouldn't be targeted.
        //Likewise, an entity found to be unreachable shouldn't be targeted.
        if (memory.visited || memory.unreachable) 
            return;

        bool isFinalGoal = memory.entity.entityType == EntityType.ET_GOAL_COMPLETION;
        
        float bias = 0.0f;

        //Special circumstances for the final goal - since it marks the end of play
        //for a player.
        if (isFinalGoal)
        {
            //If mandatory goals remain, the final goal can't be targeted.
            if (this.memory.MandatoryGoalsLeft() || !assessedGoalsInit)
                return;

            bias += Mathf.Lerp(PathOS.Constants.Behaviour.FINAL_GOAL_BONUS_MIN, 
                PathOS.Constants.Behaviour.FINAL_GOAL_BONUS_MAX,
                heuristicScaleLookup[Heuristic.EFFICIENCY]);

            //Penalize for the agent's assessment of benefit for all unvisited
            //positive entities.
            bias -= pastCumulativeEntityScore;
        }

        //Bias for preferring the goal we have already set.
        //(If we haven't already reached it).
        if (memory.entity == currentDest.entity
            && Vector3.SqrMagnitude(GetPosition() - currentDest.pos)
            > PathOS.Constants.Navigation.GOAL_EPSILON_SQR)
            bias += PathOS.Constants.Behaviour.EXISTING_GOAL_BIAS;

        Vector3 toEntity = memory.RecallPos() - GetPosition();

        //Weighted scoring function.
        //Bias added to account for entity's type.
        foreach (HeuristicScale heuristicScale in heuristicScales)
        {
            (Heuristic, EntityType) key = (heuristicScale.heuristic, memory.entity.entityType);

            if(!entityScoringLookup.ContainsKey(key))
            {
                NPDebug.LogError("Couldn't find key " + key.ToString() + " in heuristic scoring lookup!", typeof(PathOSAgent));
                continue;
            }

            bias += heuristicScale.scale * entityScoringLookup[key]
                * PathOS.Constants.Behaviour.DIST_SCORE_FACTOR_SQR / toEntity.sqrMagnitude;
        }

        float score = ScoreDirection(GetPosition(), toEntity, bias, toEntity.magnitude);

        if (!isFinalGoal && score > 0.0f)
            cumulativeEntityScore += score;

        if (bias > 0.0f && score > 0.0f)
            score += PathOS.Constants.Behaviour.INTERACTIVITY_BIAS;

        //Stochasticity introduced to goal update.
        if (PathOS.ScoringUtility.UpdateScore(score, maxScore))
        {
            //Only update maxScore if the new score is actually higher.
            //(Prevent over-accumulation of error.)
            if (score > maxScore)
                maxScore = score;

            dest.entity = memory.entity;

            //If the entity is visible/always known to the player, ensure 
            //its position is set to the actual position of the entity.
            if (memory.entity.visible || memory.entity.entityRef.alwaysKnown)
            {
                dest.pos = memory.entity.ActualPosition();
                dest.accurate = true;
            }
            //If this entity is a "new" target, fetch its position from memory.
            //(Imperfect recall, done when the decision is made).
            else if(dest.entity != currentDest.entity)
            {
                dest.pos = memory.RecallPos();
                dest.accurate = false;
            }
        }    
    }

    //maxScore is updated if the direction achieves a higher score.
    void ScoreExploreDirection(Vector3 origin, Vector3 dir, bool visible, ref TargetDest dest, ref float maxScore,
        bool overridePos = false, Vector3 overrideDest = default)
    {
        float distance = 0.0f;
        Vector3 newDest = origin;

        if (overridePos && overrideDest != null)
            newDest = overrideDest;
        else
        {
            if (visible)
            {
                //Grab the "extent" of the direction on the navmesh from the perceptual system.
                NavMeshHit hit = eyes.ExploreVisibilityCheck(GetPosition(), dir);
                distance = hit.distance;
                newDest = hit.position;
            }
            else
            {
                //Grab the "extent" of the direction on our memory model of the navmesh.
                PathOSNavUtility.NavmeshMemoryMapper.NavmeshMemoryMapperCastHit hit;
                memory.memoryMap.RaycastMemoryMap(origin, dir, eyes.navmeshCastDistance, out hit);
                distance = hit.distance;

                newDest = PathOSNavUtility.GetClosestPointWalkable(
                    origin + distance * dir, exploreTargetMargin);

                //Disqualify a target if the agent has determined it to be unreachable.
                if (IsUnreachable(newDest))
                    return;
            }
        }

        float bias = 0.0f;

        //Bias for preferring the goal we have already set.
        //(If we haven't reached it already.)
        if (Vector3.SqrMagnitude(newDest - currentDest.pos) < PathOS.Constants.Navigation.GOAL_EPSILON_SQR
            && (GetPosition() - currentDest.pos).magnitude > exploreThreshold)
        {
            bias += PathOS.Constants.Behaviour.EXISTING_GOAL_BIAS;
        }

        float score = ScoreDirection(origin, dir, bias, distance);

        //Same stochasticity logic as for entity goals.
        if(PathOS.ScoringUtility.UpdateScore(score, maxScore))
        {
            if(score > maxScore)
                maxScore = score;

            //If we're originating from where we stand, target the "end" point.
            //Else, target the "start" point, and the agent will re-assess its 
            //options when it gets there.
            if (Vector3.SqrMagnitude(origin - GetPosition())
                < PathOS.Constants.Navigation.EXPLORE_PATH_POS_THRESHOLD_FAC 
                * exploreThreshold)
                dest.pos = newDest;
            else
                dest.pos = origin;

            dest.accurate = true;
            dest.entity = null;
        }

        memory.AddPath(new ExploreMemory(origin, dir, newDest, score));
    }

    float ScoreDirection(Vector3 origin, Vector3 dir, float bias, float maxDistance)
    {
        dir.Normalize();

        //Score base = bias.
        float score = bias;

        //Add to the score based on our curiosity and the potential to 
        //"fill in our map" as we move in this direction.
        //This is similar to the scaling created by assessing an exploration direction.
        PathOSNavUtility.NavmeshMemoryMapper.NavmeshMemoryMapperCastHit hit;
        memory.memoryMap.RaycastMemoryMap(origin, dir, maxDistance, out hit);

        score += (heuristicScaleLookup[Heuristic.CURIOSITY]) 
            * hit.numUnexplored / PathOSNavUtility.NavmeshMemoryMapper.maxCastSamples
            * hit.distance / eyes.navmeshCastDistance;

        //Enumerate over all entities the agent knows about, and use them
        //to affect our assessment of the potential target.
        for (int i = 0; i < memory.entities.Count; ++i)
        {
            if (memory.entities[i].visited || memory.entities[i].unreachable)
                continue;

            //Vector to the entity.
            Vector3 entityVec = memory.entities[i].RecallPos() - origin;
            //Scale our factor by inverse square of distance.
            float distFactor = PathOS.Constants.Behaviour.DIST_SCORE_FACTOR_SQR / entityVec.sqrMagnitude;
            Vector3 dir2entity = entityVec.normalized;

            float dot = Vector3.Dot(dir, dir2entity);
            dot = Mathf.Clamp(dot, 0.0f, 1.0f);

            //Weighted scoring function.
            foreach(HeuristicScale heuristicScale in heuristicScales)
            {
                (Heuristic, EntityType) key = (heuristicScale.heuristic, 
                    memory.entities[i].entity.entityType);

                if(!entityScoringLookup.ContainsKey(key))
                {
                    NPDebug.LogError("Couldn't find key " + key.ToString() + " in heuristic scoring lookup!", typeof(PathOSAgent));
                    continue;
                }

                bias += heuristicScale.scale * entityScoringLookup[key] * dot * distFactor;
            }
        }

        return score;
    }

	private void Update() 
	{
        //Inactive state toggle for debugging purposes (or if the agent is finished).
        if (freezeAgent || completed)
            return;

        //Update spatial memory.
        memory.memoryMap.Fill(navAgent.transform.position);

        //Update of periodic actions.
        routeTimer += Time.deltaTime;
        perceptionTimer += Time.deltaTime;

        if(!lookingAround)
            lookTimer += Time.deltaTime;

        //Rerouting update.
        if (routeTimer >= RouteComputeTimeCalculated())
        {
            routeTimer = 0.0f;
            ComputeNewDestination();
        }

        //Memory path update.
        if (onMemPath)
        {
            Vector3 curXZ = GetPosition();
            curXZ.y = 0.0f;

            if (Vector3.SqrMagnitude(curXZ - memWaypoint)
                < PathOS.Constants.Navigation.WAYPOINT_EPSILON_SQR)
            {
                memPathWaypoints.RemoveAt(0);

                if (memPathWaypoints.Count == 0)
                {
                    onMemPath = false;
                    RouteDestination();
                }
                else
                {
                    navAgent.SetDestination(memPathWaypoints[0]);
                    pathResolved = false;
                    memWaypoint.x = memPathWaypoints[0].x;
                    memWaypoint.z = memPathWaypoints[0].z;
                }
            }
        }
        else if(currentDest.entity != null && !currentDest.accurate
            && currentDest.entity.visible)
        {
            currentDest.pos = currentDest.entity.ActualPosition();
            currentDest.accurate = true;
            RouteDestination();
        }

        //Targeting update. This prevents the agent from getting stuck.
        if(!pathResolved && NavmeshPathEnded())
        {
            //If we're following a memory path,
            //abort and route to the final target on the Navmesh.
            if(onMemPath)
            {
                onMemPath = false;
                RouteDestination();
            }
            //If we're dealing with an entity...
            else if(currentDest.entity != null)
            {
                PerceivedEntity entity = currentDest.entity;

                if (!currentDest.accurate)
                {
                    currentDest.pos = entity.ActualPosition();
                    currentDest.accurate = true;
                    RouteDestination();
                }
                else
                {
                    float adjVisitSqr = (entity.entityRef.overrideVisitRadius) ?
                        entity.entityRef.visitRadiusSqr : visitThresholdSqr;

                    //Compress unreachability check to XZ plane.
                    Vector3 agentPos = GetPosition();
                    agentPos.y = 0.0f;

                    Vector3 targetPos = currentDest.pos;
                    targetPos.y = 0.0f;

                    if (Vector3.SqrMagnitude(agentPos - targetPos)
                        >= adjVisitSqr)
                        memory.MakeUnreachable(entity);
                }
            }
            //If we're dealing with an exploration target...
            else
            {
                //This will prevent the agent from retargeting the current destination.
                AddUnreachable(currentDest.pos);
            }

            pathResolved = true;
        }

        //Perception update.
        //This will allow the agent's eyes to "process" nearby entities
        //and also update the time threshold for looking around based 
        //on nearby hazards.
        if (perceptionTimer >= PathOS.Constants.Perception.PERCEPTION_COMPUTE_TIME)
        {
            perceptionTimer = 0.0f;
            PerceptionUpdate();
        }

        //Look-around update.
        if(lookTimer >= lookTime)
        {
            lookTimer = 0.0f;
            lookingAround = true;
            StartCoroutine(LookAround());
        }

        //Set the agent's completion flag.
        if (manager.endOnCompletionGoal
            && memory.FinalGoalCompleted())
        {
            completed = true;
            gameObject.SetActive(false);
        }                  
    }

    private void RouteDestination()
    {
        navAgent.SetDestination(currentDest.pos);
        pathResolved = false;
    }

    private void AddUnreachable(Vector3 target)
    {
        for(int i = 0; i < unreachableReference.Count; ++i)
        {
            if (Vector3.SqrMagnitude(target - unreachableReference[i])
                < PathOS.Constants.Navigation.UNREACHABLE_POS_SIMILARITY_SQR)
                return;
        }

        unreachableReference.Add(target);
    }

    public bool IsUnreachable(Vector3 target)
    {
        for(int i = 0; i < unreachableReference.Count; ++i)
        {
            if (Vector3.SqrMagnitude(target - unreachableReference[i])
                < PathOS.Constants.Navigation.UNREACHABLE_POS_CHECK_SQR)
                return true;
        }

        return false;
    }

    private void PerceptionUpdate()
    {
        UpdateLookTime();
    }

    private bool NavmeshPathEnded()
    {
        return !navAgent.pathPending && !navAgent.hasPath;
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
    
    public Vector3 GetTargetPosition()
    {
        return currentDest.pos;
    }

    public bool IsTargeted(PerceivedEntity entity)
    {
        return currentDest.entity == entity;
    }
}
