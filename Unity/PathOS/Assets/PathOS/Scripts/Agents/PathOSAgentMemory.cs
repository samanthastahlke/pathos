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
            if ((entities[i].pos - currentDestination).magnitude < 12f && (entities[i].entityType == EntityType.ET_HAZARD_ENEMY || entities[i].entityType == EntityType.ET_HAZARD_ENVIRONMENT))
            {
                //we increment the counter to see how many hazards are close by
                hazardCounter++;

                //the 2 is just a placeholder to test that it works
                if (hazardCounter >= 2)
                {
                    //if it's hazardous it returns true
                    return true;
                }
            }
        }

        return false;
    }
}
