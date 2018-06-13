using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PathOS;

/*
PathOSManager.cs 
PathOSManager (c) Nine Penguins (Samantha Stahlke) 2018
*/

public class PathOSManager : NPSingleton<PathOSManager>
{
    public List<LevelEntity> levelEntities;

    public GameObject playerProxy;

	void Awake()
	{
		for(int i = 0; i < levelEntities.Count; ++i)
        {
            levelEntities[i].rend = levelEntities[i].entityRef.GetComponent<Renderer>();
        }
	}
	
	void Start() 
	{
		
	}
	
	void FixedUpdate()
	{
		
	}

	void Update() 
	{
		
	}

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
