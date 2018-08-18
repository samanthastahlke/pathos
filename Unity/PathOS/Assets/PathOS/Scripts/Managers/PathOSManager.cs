using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PathOS;

/*
PathOSManager.cs 
PathOSManager (c) Nine Penguins (Samantha Stahlke) 2018
*/

//Simple class for defining entities in the level.
public class PathOSManager : NPSingleton<PathOSManager>
{
    public List<LevelEntity> levelEntities;

	void Awake()
	{
        //Grab renderers for object visibility checks by the agent.
		for(int i = 0; i < levelEntities.Count; ++i)
        {
            levelEntities[i].rend = levelEntities[i].entityRef.GetComponent<Renderer>();
        }
	}

    //Dynamic entity adding/removal.
    //Placeholder right now, will be expanded to allow more runtime manipulation
    //of the framework.
    public void AddEntity(int index)
    {
        if(index >= 0 && index <= levelEntities.Count)
            levelEntities.Insert(index, new LevelEntity());
    }

    public void RemoveEntity(int index)
    {
        if (index >= 0 && index < levelEntities.Count)
            levelEntities.RemoveAt(index);
    }

    public void ClearEntities()
    {
        levelEntities.Clear();
    }
}
