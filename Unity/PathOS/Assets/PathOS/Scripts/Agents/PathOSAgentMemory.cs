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
    //This will be extended to include remembered "areas" for exploration.
    public List<EntityMemory> entities { get; set; }

    private void Awake()
    {
        entities = new List<EntityMemory>();
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
