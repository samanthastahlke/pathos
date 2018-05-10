using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/*
PathOSManager.cs 
PathOSManager (c) Nine Penguins (Samantha Stahlke) 2018
*/

public class PathOSManager : NPSingleton<PathOSManager>
{
    public enum EntityType
    {
        ET_NONE = 0,
        ET_GOAL = 100,
        ET_ENEMY = 200
    };

    [System.Serializable]
    public class LevelEntity
    {
        public GameObject entityRef;
        public EntityType entityType;
        public string entityID;
    }

    public List<LevelEntity> levelEntities;

    public GameObject playerProxy;

	void Awake()
	{
		
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
