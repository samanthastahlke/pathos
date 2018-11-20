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

    //Remembered entities.
    public List<EntityMemory> entities { get; set; }

    //This array is merely a placeholder for now, it is not properly used.
    //(TODO)
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

    //for hazardous area
    private Vector3[] nearbyEnemies = new Vector3[2];
    private float hazardRadius = 0;
    private int hazardLimit = 2;

    private void Awake()
    {
        entities = new List<EntityMemory>();
        paths = new List<ExploreMemory>();

        //Initialize the (blank) model of the agent's internal "map".
        if (autogenerateMapExtents)
            memoryMap = new PathOSNavUtility.NavmeshMemoryMapper(gridSampleSize, worldExtentRadius, gridSampleElevation);
        else
            memoryMap = new PathOSNavUtility.NavmeshMemoryMapper(gridSampleSize, navmeshBounds);
    }

    private void Update()
    {
        for(int i = entities.Count - 1; i > 0; --i)
        {
            entities[i].impressionTime += Time.deltaTime;

            //Placeholder for "forgetting", to test things out.
            if (!entities[i].visited && entities[i].impressionTime >= agent.forgetTime)
                entities.RemoveAt(i);
        }

        for(int i = paths.Count - 1; i > 0; --i)
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
                entities[i].pos = entity.pos;
                return;
            }             
        }

        entities.Add(new EntityMemory(entity));
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
    bool CheckIfPathExists(ExploreMemory thePath)
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


    //Are there any goals left? 
    bool GoalsRemaining()
    {
        for (int i = 0; i < entities.Count; i++)
        {
            //this really, really, really needs to be cleaned up pleasedonthateme
            if ((entities[i].entityType == EntityType.ET_GOAL_OPTIONAL || entities[i].entityType == EntityType.ET_GOAL_MANDATORY
                || entities[i].entityType == EntityType.ET_RESOURCE_ACHIEVEMENT || entities[i].entityType == EntityType.ET_RESOURCE_PRESERVATION)
                && entities[i].visited == false)
            {
                return true;
            }
        }
        return false;
    }

    public void CheckGoals()
    {
        goalsLeft = GoalsRemaining(); //calculates whether or not there are goals it still needs to go to
    }

    public bool GetGoalsLeft()
    {
        return goalsLeft; //returns whether or not there are goals left
    }

    //checks the number of hazards in close proximity
    public bool CheckHazards(Vector3 currentDestination)
    {
        int hazardCounter = 0;

        for (int i = 0; i < entities.Count; i++)
        {
            //if the hazard is within range... (the range is just a placeholder for now as well, I'm worried that it's too short?)
            if ((entities[i].pos - currentDestination).magnitude < 8f && (entities[i].entityType == EntityType.ET_HAZARD_ENEMY || entities[i].entityType == EntityType.ET_HAZARD_ENVIRONMENT))
            {
                //we increment the counter to see how many hazards are close by
                hazardCounter++;

                //the 2 is just a placeholder to test that it works
                if (hazardCounter >= hazardLimit)
                {
                    //if it's hazardous it returns true
                    return true;
                }
            }
        }

        return false;
    }

    //based off of the area and the paths, it gets a new destination for the backtracking
    public Vector3 CalculateBacktrackDestination(int startingIndex)
    {
        Vector3 midpoint = CalculateMidpoint(nearbyEnemies[0], nearbyEnemies[1]);
        Vector3 newPath = ChooseNewPath(midpoint, startingIndex);
        return newPath;
    }

    //Chooses path away from the area where the enemies are
    public Vector3 ChooseNewPath(Vector3 centerPoint, int startingIndex)
    {
        for (int i = startingIndex; i > 0; i--)
        {
            //Uses centerpoint of where the enemies are to pick paths that fall outside of that radius
            if (Vector3.Distance(CalculatePathDestination(i), centerPoint) > (hazardRadius))
            {
                return CalculatePathDestination(i);
            }
        }
        return CalculatePathDestination(0);
    }

    //Calculates the midpoint
    //This will be made more sophisticated
    public Vector3 CalculateMidpoint(Vector3 point1, Vector3 point2)
    {
        hazardRadius = Vector3.Distance(point1, point2);
        return 0.5f * (point1 + point2);
    }

    //Calculates the destination for that path
    public Vector3 CalculatePathDestination(int pathIndex)
    {
        return paths[pathIndex].originPoint + (paths[pathIndex].direction * paths[pathIndex].dEstimate);
    }
}
