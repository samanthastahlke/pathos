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
    public List<LevelEntity> levelEntities;

    public List<HeuristicWeightSet> heuristicWeights;
    
	void Awake()
	{
        //Grab renderers for object visibility checks by the agent.
		for(int i = 0; i < levelEntities.Count; ++i)
        {
            levelEntities[i].rend = levelEntities[i].entityRef.GetComponentInChildren<Renderer>();
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
