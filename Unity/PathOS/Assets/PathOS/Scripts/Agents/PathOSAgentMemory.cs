using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PathOS;

/*
PathOSAgentMemory.cs 
PathOSAgentMemory (c) Nine Penguins (Samantha Stahlke) and Atiya Nova 2018
*/

[RequireComponent(typeof(PathOSAgent))]
public class PathOSAgentMemory : MonoBehaviour 
{
    private PathOSAgent agent;
    private static PathOSManager manager;

    //Remembered entities.
    public List<EntityMemory> entities { get; set; }
    private List<EntityMemory> stm;

    //Keep track of mandatory goals.
    private List<EntityMemory> finalGoalTracker;
    private EntityMemory finalGoal;
    private bool finalGoalCompleted;

    //Remembered paths/directions.
    public List<ExploreMemory> paths { get; set; }

    //The agent's memory model of the complete navmesh.
    [Header("Navmesh Memory Model")]
    [Tooltip("The edge length of a tile in the memory map (in units)")]
    public float gridSampleSize = 2.0f;
    public PathOSNavUtility.NavmeshBoundsXZ navmeshBounds;

    //For automatic memory map generation.
    //Limiting to user-specified for this version.
    private bool autogenerateMapExtents = false;
    private float gridSampleElevation = 8.0f;
    private float worldExtentRadius = 200.0f;

    public PathOSNavUtility.NavmeshMemoryMapper memoryMap { get; set; }

    //Check to see if there are any goals left
    protected bool goalsLeft = true;

    private void Awake()
    {
        agent = GetComponent<PathOSAgent>();

        entities = new List<EntityMemory>();
        stm = new List<EntityMemory>();

        finalGoalTracker = new List<EntityMemory>();
        paths = new List<ExploreMemory>();

        if (null == manager)
            manager = PathOSManager.instance;

        //Initialize the (blank) model of the agent's internal "map".
        if (autogenerateMapExtents)
            memoryMap = new PathOSNavUtility.NavmeshMemoryMapper(gridSampleSize, worldExtentRadius, gridSampleElevation);
        else
            memoryMap = new PathOSNavUtility.NavmeshMemoryMapper(gridSampleSize, navmeshBounds);

        memoryMap.memory = this;

        //Commit any "always-known" entities to memory.
        foreach (PerceivedEntity entity in agent.eyes.perceptionInfo)
        {
            if(entity.entityRef.alwaysKnown)
            {
                EntityMemory newMemory = new EntityMemory(entity);
                newMemory.MakeUnforgettable();

                entities.Add(newMemory);
            }

            if(entity.entityType == EntityType.ET_GOAL_MANDATORY)
            {
                EntityMemory newMemory = new EntityMemory(entity);
                finalGoalTracker.Add(newMemory);
            }
            else if(entity.entityType == EntityType.ET_GOAL_COMPLETION)
                finalGoal = new EntityMemory(entity);
        }
    }

    private void Update()
    {
        memoryMap.BakeVisualGrid();

        Vector3 agentPos = agent.GetPosition();

        stm.Clear();

        for(int i = entities.Count - 1; i >= 0; --i)
        {
            EntityMemory entity = entities[i];

            entity.impressionTime += Time.deltaTime;

            //Flag an entity as visited if we pass by in close range.
            //Inelegant brute-force to prevent "accidental" completion.
            if (Vector3.SqrMagnitude(entity.entity.ActualPosition() - agentPos) 
                < PathOS.Constants.Navigation.VISIT_THRESHOLD_SQR
                && entity.entity.entityType != EntityType.ET_GOAL_COMPLETION)
                entity.Visit(this.gameObject, PathOSAgent.logger);

            //Only something which is no longer visible and forgettable
            //can be discarded from memory.
            if (!entity.entity.visible 
                && entity.forgettable
                && entity.impressionTime >= agent.forgetTime)
            {
                entities.RemoveAt(i);
                continue;
            }
                

            if (!entities[i].ltm
                && !entities[i].entity.visible)
                stm.Add(entities[i]);
        }

        //Forget any non-visible entities that aren't in long-term memory 
        //over the STM size cap.
        if(stm.Count > agent.stmSize)
        {
            stm.Sort((m1, m2) => m1.impressionTime.CompareTo(m2.impressionTime));

            while (stm.Count > agent.stmSize)
            {
                entities.Remove(stm[stm.Count - 1]);
                stm.RemoveAt(stm.Count - 1);          
            }               
        }

        for(int i = 0; i < finalGoalTracker.Count; ++i)
        {
            if (Vector3.SqrMagnitude(finalGoalTracker[i].entity.ActualPosition() - agentPos) 
                < PathOS.Constants.Navigation.VISIT_THRESHOLD_SQR)
                finalGoalTracker[i].Visit();             
        }

        //Only mark completion if the agent actively targets the final goal.
        if(finalGoal != null 
            && agent.IsTargeted(finalGoal.entity)
            && Vector3.SqrMagnitude(finalGoal.entity.ActualPosition() - agentPos)
            < PathOS.Constants.Navigation.VISIT_THRESHOLD_SQR)
        {
            finalGoal.Visit(this.gameObject, PathOSAgent.logger);
            finalGoalCompleted = true;
        }

        for(int i = paths.Count - 1; i >= 0; --i)
        {
            paths[i].impressionTime += Time.deltaTime;

            if (paths[i].impressionTime >= agent.forgetTime)
                paths.RemoveAt(i);
        }       
    }

    //Push an entity into the agent's memory.
    public void Memorize(PerceivedEntity entity)
    {
        for(int i = 0; i < entities.Count; ++i)
        {
            if (PerceivedEntity.SameEntity(entity, entities[i]))
            {
                entities[i].impressionTime = 0.0f;
                entities[i].entity.perceivedPos = entity.perceivedPos;
                return;
            }
        }

        entities.Add(new EntityMemory(entity));
    }

    public void CommitLTM(PerceivedEntity entity)
    {
        for(int i = 0; i < entities.Count; ++i)
        {
            if(PerceivedEntity.SameEntity(entity, entities[i]))
            {
                entities[i].ltm = true;
                return;
            }
        }

        entities.Add(new EntityMemory(entity));
        entities[entities.Count - 1].ltm = true;
    }

    public void TryCommitLTM(PerceivedEntity entity)
    {
        EntityMemory memory = null;

        for(int i = 0; i < entities.Count; ++i)
        {
            if(PerceivedEntity.SameEntity(entity, entities[i]))
            {
                memory = entities[i];
                break;
            }
        }

        if(null == memory)
        {
            memory = new EntityMemory(entity);
            entities.Add(memory);
        }

        if(!memory.ltm)
        {
            if (entity.visibilityTimer >= PathOS.Constants.Memory.IMPRESSION_TIME_CONVERT_LTM
                || entity.impressionCount >= PathOS.Constants.Memory.IMPRESSIONS_CONVERT_LTM)
                memory.ltm = true;  
        }
    }

    public void CommitUnforgettable(PerceivedEntity entity)
    {
        for(int i = 0; i < entities.Count; ++i)
        {
            if(PerceivedEntity.SameEntity(entity, entities[i]))
            {
                entities[i].MakeUnforgettable();
                return;
            }
        }
    }

    //Has a visible entity been visited?
    public bool Visited(PerceivedEntity entity)
    {
        for(int i = 0; i < entities.Count; ++i)
        {
            if (PerceivedEntity.SameEntity(entity, entities[i]))
                return entities[i].visited;
        }

        return false;
    }

    //Adds a new path to memory.
    public void AddPath(ExploreMemory path)
    {
        int minScoringIndex = 0;
        float minScore = PathOS.Constants.Behaviour.SCORE_MAX;

        for(int i = 0; i < paths.Count; ++i)
        {
            if (path == paths[i])
            { 
                paths[i].UpdateScore(path.score);
                return;
            }

            if (paths[i].score < minScore)
            {
                minScore = paths[i].score;
                minScoringIndex = i;
            }
        }

        if(paths.Count >= agent.stmSize)
        {
            if (path.score < minScore)
                return;
            else
                paths.RemoveAt(minScoringIndex);
        }

        paths.Add(path);
    }

    //How many entities is the agent aware of that haven't been visited?
    public int UnvisitedRemaining()
    {
        int unvisitedCount = 0;

        for (int i = 0; i < entities.Count; ++i)
        {
            if (!entities[i].visited)
                ++unvisitedCount;
        }

        return unvisitedCount;
    }

    //Are there optional goals or achievements remaining?
    //(To the agent's knowledge)
    public int AchievementGoalsLeft()
    {
        int goalCount = 0;

        for (int i = 0; i < entities.Count; ++i)
        {
            if ((entities[i].entity.entityType == EntityType.ET_GOAL_OPTIONAL
                || entities[i].entity.entityType == EntityType.ET_RESOURCE_ACHIEVEMENT)
                && !entities[i].visited)
                ++goalCount;
        }

        return goalCount;
    }

    //Are there still mandatory goals remaining?
    public bool MandatoryGoalsLeft()
    {
        for (int i = 0; i < finalGoalTracker.Count; ++i)
        {
            if (!finalGoalTracker[i].visited)
                return true;
        }

        return false;
    }

    public bool FinalGoalCompleted()
    {
        return finalGoalCompleted;
    }

    //Score the area as hazardous on a normalized 0-1 scale. This is 
    //used to modulate the look-around behaviour of cautious agents.
    public float ScoreHazards(Vector3 pos)
    {
        int hazardCount = 0;

        for (int i = 0; i < entities.Count; ++i)
        {
            if(!entities[i].visited
                && (entities[i].entity.entityType == EntityType.ET_HAZARD_ENEMY
                || entities[i].entity.entityType == EntityType.ET_HAZARD_ENVIRONMENT))
            {
                if (Vector3.SqrMagnitude(entities[i].entity.perceivedPos - pos) 
                    < PathOS.Constants.Behaviour.ENEMY_RADIUS_SQR)
                    ++hazardCount;

                if (hazardCount >= PathOS.Constants.Behaviour.ENEMY_COUNT_THRESHOLD)
                    return 1.0f;
            }
        }

        float hazardScore = (float)hazardCount / 
            PathOS.Constants.Behaviour.ENEMY_COUNT_THRESHOLD;

        return hazardScore;
    }

    public float MovementHazardPenalty(Vector3 pos)
    {
        float penalty = 0.0f;

        for (int i = 0; i < entities.Count; ++i)
        {
            if(!entities[i].visited 
                && (entities[i].entity.entityType == EntityType.ET_HAZARD_ENEMY
                || entities[i].entity.entityType == EntityType.ET_HAZARD_ENVIRONMENT))
            {
                penalty += agent.hazardPenalty / Vector3.SqrMagnitude(
                    entities[i].entity.perceivedPos - pos);

                if (penalty >= PathOS.Constants.Navigation.HAZARD_PENALTY_MAX)
                    return PathOS.Constants.Navigation.HAZARD_PENALTY_MAX;
            }
        }

        return penalty;
    }
}
