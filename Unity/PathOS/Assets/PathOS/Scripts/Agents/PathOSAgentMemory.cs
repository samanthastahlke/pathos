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
    public List<EntityMemory> memory { get; set; }

    private void Awake()
    {
        memory = new List<EntityMemory>();
    }

    private void Update()
    {
        for(int i = memory.Count - 1; i > 0; --i)
        {
            memory[i].impressionTime += Time.deltaTime;

            //Placeholder for "forgetting", to test things out.
            if (!memory[i].visited && memory[i].impressionTime >= agent.forgetTime)
                memory.RemoveAt(i);
        }
    }

    //Push an entity into the agent's memory.
    public void Memorize(PerceivedEntity entity)
    {
        for(int i = 0; i < memory.Count; ++i)
        {
            if (entity == memory[i])
            {
                memory[i].impressionTime = 0.0f;
                memory[i].pos = entity.pos;
                return;
            }             
        }

        memory.Add(new EntityMemory(entity));
    }

    //Has a visible entity been visited?
    public bool Visited(PerceivedEntity entity)
    {
        for(int i = 0; i < memory.Count; ++i)
        {
            if (entity == memory[i])
                return memory[i].visited;
        }

        return false;
    }
}
