using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using PathOS;

/*
PathOSManager.cs 
PathOSManager (c) Nine Penguins (Samantha Stahlke) 2018
*/

//Simple class for defining entities in the level.
public class PathOSManager : NPSingleton<PathOSManager>
{
    public bool limitSimulationTime = false;
    public float maxSimulationTime = 180.0f;
    public bool endOnCompletionGoal = true;

    public List<LevelEntity> levelEntities;

    public List<HeuristicWeightSet> heuristicWeights;

    private Dictionary<EntityType, string> entityGizmoLookup = new Dictionary<EntityType, string>
    {
        {EntityType.ET_NONE, "entity_null.png" },
        {EntityType.ET_GOAL_OPTIONAL, "goal_optional.png" },
        {EntityType.ET_GOAL_MANDATORY, "goal_mandatory.png" },
        {EntityType.ET_GOAL_COMPLETION, "goal_completion.png" },
        {EntityType.ET_RESOURCE_ACHIEVEMENT, "resource_achievement.png" },
        {EntityType.ET_RESOURCE_PRESERVATION, "resource_preservation.png" },
        {EntityType.ET_HAZARD_ENEMY, "hazard_enemy.png" },
        {EntityType.ET_HAZARD_ENVIRONMENT, "hazard_environment.png" },
        {EntityType.ET_POI, "poi_environment.png" },
        {EntityType.ET_POI_NPC, "poi_npc.png" }
    };

    private float simulationTimer = 0.0f;

    private void Awake()
	{
        for (int i = 0; i < levelEntities.Count; ++i)
        {
            //Grab renderers for object visibility checks by the agent.
            levelEntities[i].rend = levelEntities[i].objectRef.GetComponentInChildren<Renderer>();
        }     
	}

    private void Update()
    {
        simulationTimer += Time.deltaTime;

#if UNITY_EDITOR
        if(limitSimulationTime && simulationTimer > maxSimulationTime
            && UnityEditor.EditorApplication.isPlaying)
        {
            UnityEditor.EditorApplication.isPlaying = false;
        }
#endif

    }

    private void OnDrawGizmosSelected()
    {
#if UNITY_EDITOR
        if(!UnityEditor.EditorApplication.isPlaying)
        {
            foreach(LevelEntity entity in levelEntities)
            {
                Gizmos.DrawIcon(entity.objectRef.transform.position,
                   entityGizmoLookup[entity.entityType]);
            }
        }
#endif
    }

    //Entity adding/removal (for Inspector).
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

    public void ResizeWeightMatrix()
    {
        //Save existing values and rebuild the weight matrix.
        Dictionary<(Heuristic, EntityType), float> weights = 
            new Dictionary<(Heuristic, EntityType), float>();

        for(int i = 0; i < heuristicWeights.Count; ++i)
        {
            for(int j = 0; j < heuristicWeights[i].weights.Count; ++j)
            {
                weights.Add((heuristicWeights[i].heuristic, 
                    heuristicWeights[i].weights[j].entype),
                    heuristicWeights[i].weights[j].weight);
            }
        }

        heuristicWeights.Clear();
        
        foreach(Heuristic heuristic in System.Enum.GetValues(typeof(Heuristic)))
        {
            heuristicWeights.Add(new HeuristicWeightSet(heuristic));
            HeuristicWeightSet newWeights = heuristicWeights[heuristicWeights.Count - 1];

            foreach(EntityType entype in System.Enum.GetValues(typeof(EntityType)))
            {
                float weight = 0.0f;

                if (weights.ContainsKey((heuristic, entype)))
                    weight = weights[(heuristic, entype)];

                newWeights.weights.Add(new EntityWeight(entype, weight));
            }
        }
    }

    public void ImportWeights(string filename)
    {
        Dictionary<(Heuristic, EntityType), float> weights =
            new Dictionary<(Heuristic, EntityType), float>();

        List<EntityType> entypes = new List<EntityType>();

        StreamReader sr = new StreamReader(filename);
        char[] sep = { ',' };

        string line = sr.ReadLine();

        string[] headerTypes = line.Split(sep, System.StringSplitOptions.RemoveEmptyEntries);

        while((line = sr.ReadLine()) != null)
        {
            string[] lineContents = line.Split(sep, System.StringSplitOptions.RemoveEmptyEntries);

            if (lineContents.Length < 1)
                continue;

            Heuristic heuristic;

            if(System.Enum.TryParse<Heuristic>(lineContents[0], out heuristic))
            {
                for(int i = 1; i < lineContents.Length; ++i)
                {
                    if (i - 1 > headerTypes.Length)
                        break;

                    EntityType entype;

                    if (System.Enum.TryParse<EntityType>(headerTypes[i - 1], out entype))
                        weights[(heuristic, entype)] = float.Parse(lineContents[i]);
                }
            }
        }

        sr.Close();

        for(int i = 0; i < heuristicWeights.Count; ++i)
        {
            for(int j = 0; j < heuristicWeights[i].weights.Count; ++j)
            {
                (Heuristic, EntityType) key = (heuristicWeights[i].heuristic,
                    heuristicWeights[i].weights[j].entype);

                if (weights.ContainsKey(key))
                    heuristicWeights[i].weights[j].weight = weights[key];
            }
        }
    }

    public void ExportWeights(string filename)
    {
        ResizeWeightMatrix();

        StreamWriter sw = new StreamWriter(filename);

        sw.Write(",");

        foreach (EntityType entype in System.Enum.GetValues(typeof(EntityType)))
        {
            sw.Write(entype.ToString() + ",");
        }

        sw.Write("\n");

        for(int i = 0; i < heuristicWeights.Count; ++i)
        {
            sw.Write(heuristicWeights[i].heuristic.ToString() + ",");
            
            for(int j = 0; j < heuristicWeights[i].weights.Count; ++j)
            {
                sw.Write(heuristicWeights[i].weights[j].weight + ",");
            }

            sw.Write("\n");
        }

        sw.Close();
    }
}
