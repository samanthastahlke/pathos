using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PathOS;

/*
PathOSAgentMemory.cs 
PathOSAgentMemory (c) Nine Penguins (Samantha Stahlke) 2018
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
    public float worldExtentRadius = 200.0f;
    public float gridSampleSize = 1.0f;

    public PathOSNavUtility.NavmeshMemoryMapper memoryMap { get; set; }

    private void Awake()
    {
        entities = new List<EntityMemory>();
        paths = new List<ExploreMemory>();

        //Initialize the (blank) model of the agent's internal "map".
        memoryMap = new PathOSNavUtility.NavmeshMemoryMapper(gridSampleSize, worldExtentRadius);
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
}
