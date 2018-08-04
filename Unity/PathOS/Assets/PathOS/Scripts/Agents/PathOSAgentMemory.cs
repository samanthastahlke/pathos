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
    public List<PerceivedEntity> memory { get; set; }

    private void Awake()
    {
        memory = new List<PerceivedEntity>();
    }

    public void Memorize(PerceivedEntity entity)
    {
        for(int i = 0; i < memory.Count; ++i)
        {
            if (entity == memory[i])
                return;
        }

        memory.Add(entity);
    }

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
