using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PathOS;

/*
PathOSAgentMemory.cs 
PathOSAgentMemory (c) Nine Penguins (Samantha Stahlke) and Atiya Nova 2018
*/

public class PathOSAgentMemory : MonoBehaviour 
{
    public PathOSAgent agent;
    private static PathOSManager manager;

    //Remembered entities.
    public List<EntityMemory> entities { get; set; }

    //Keep track of mandatory goals.
    private List<EntityMemory> finalGoalTracker;
    private EntityMemory finalGoal;
    private bool finalGoalCompleted;

    //Remembered paths/directions.
    public List<ExploreMemory> paths { get; set; }

    //The agent's memory model of the complete navmesh.
    [Header("Navmesh Memory Model")]
    public bool autogenerateMapExtents = true;
    public PathOSNavUtility.NavmeshBoundsXZ navmeshBounds;
    public float worldExtentRadius = 200.0f;
    public float worldBorderMargin = 25.0f;
    public float gridSampleSize = 1.0f;
    public float gridSampleElevation = 10.0f;

    public PathOSNavUtility.NavmeshMemoryMapper memoryMap { get; set; }

    //Check to see if there are any goals left
    protected bool goalsLeft = true;

    private void Awake()
    {
        entities = new List<EntityMemory>();
        finalGoalTracker = new List<EntityMemory>();
        paths = new List<ExploreMemory>();

        if (null == manager)
            manager = PathOSManager.instance;

        //Initialize the (blank) model of the agent's internal "map".
        if (autogenerateMapExtents)
            memoryMap = new PathOSNavUtility.NavmeshMemoryMapper(gridSampleSize, worldExtentRadius, gridSampleElevation);
        else
            memoryMap = new PathOSNavUtility.NavmeshMemoryMapper(gridSampleSize, navmeshBounds);

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
        Vector3 agentPos = agent.GetPosition();

        for(int i = entities.Count - 1; i >= 0; --i)
        {
            EntityMemory entity = entities[i];

            entity.impressionTime += Time.deltaTime;

            //Flag an entity as visited if we pass by in close range.
            //Inelegant brute-force to prevent "accidental" completion.
            if ((entity.entity.ActualPosition() - agentPos).sqrMagnitude <
                PathOS.Constants.Navigation.VISIT_THRESHOLD_SQR
                && entity.entity.entityType != EntityType.ET_GOAL_COMPLETION)
                entity.visited = true;

            //Only something which is no longer visible and forgettable
            //can be discarded from memory.
            if (!entity.entity.visible 
                && entity.forgettable 
                && !entity.visited 
                && entity.impressionTime >= agent.forgetTime)
                entities.RemoveAt(i);
        }

        for(int i = 0; i < finalGoalTracker.Count; ++i)
        {
            if ((finalGoalTracker[i].entity.ActualPosition() - agentPos).sqrMagnitude <
                PathOS.Constants.Navigation.VISIT_THRESHOLD_SQR)
                finalGoalTracker[i].visited = true;
        }

        //Only mark completion if the agent actively targets the final goal.
        if(agent.IsTargeted(finalGoal.entity)
            && (finalGoal.entity.ActualPosition() - agentPos).sqrMagnitude
            < PathOS.Constants.Navigation.VISIT_THRESHOLD_SQR)
        {
            finalGoal.visited = true;
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
            if (entity == entities[i])
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
            if(entity == entities[i])
            {
                entities[i].ltm = true;
                return;
            }
        }

        entities.Add(new EntityMemory(entity));
        entities[entities.Count - 1].ltm = true;
    }

    public void CommitUnforgettable(PerceivedEntity entity)
    {
        for(int i = 0; i < entities.Count; ++i)
        {
            if(entity == entities[i])
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
            if (entity == entities[i])
                return entities[i].visited;
        }

        return false;
    }

    //Adds a new path to memory
    public void AddPath(ExploreMemory thePath)
    {
        if (!CheckIfPathExists(thePath))
        {
            paths.Add(new ExploreMemory(thePath.originPoint, thePath.direction, thePath.dEstimate));
        }
    }

    //Checks to see if the path already exists in memory
    private bool CheckIfPathExists(ExploreMemory thePath)
    {
        for (int i = paths.Count - 1; i > 0; i--)
        {
            if (thePath == paths[i]) return true;
        }
        return false;
    }

    //gets the last traversed path
    public int GetLastPath()
    {
        Vector3 originPoint = paths[paths.Count - 1].originPoint;

        for (int i = paths.Count - 1; i > 0; i--)
        {
            if (paths[i].originPoint != originPoint)
            {
                return i;
            }
        }
        return 0;
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
                if ((entities[i].entity.perceivedPos - pos).sqrMagnitude >
                    PathOS.Constants.Behaviour.ENEMY_RADIUS_SQR)
                    ++hazardCount;

                if (hazardCount >= PathOS.Constants.Behaviour.ENEMY_COUNT_THRESHOLD)
                    return 1.0f;
            }
        }

        float hazardScore = (float)hazardCount / 
            PathOS.Constants.Behaviour.ENEMY_COUNT_THRESHOLD;

        return hazardScore;
    }

    //Chooses path away from the area where the enemies are
    public Vector3 CalculateNewDirection(Vector3 centerPoint, int startingIndex)
    {
        for (int i = startingIndex; i > 0; i--)
        {
            //Uses centerpoint of where the enemies are to pick paths that fall outside of that radius
            if (Vector3.Distance(CalculatePathDestination(i), centerPoint) > (PathOS.Constants.Behaviour.ENEMY_RADIUS))
            {
                return CalculatePathDestination(i);
            }
        }

        return CalculatePathDestination(0);
    }

    //Calculates the destination for that path
    public Vector3 CalculatePathDestination(int pathIndex)
    {
        //returns a point on the navmesh that the agent can reach with the chosen path
       return PathOSNavUtility.GetClosestPointWalkable(
                agent.transform.position + paths[pathIndex].dEstimate * paths[pathIndex].direction, worldBorderMargin);
    }
}
