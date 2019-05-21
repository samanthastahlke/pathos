using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using PathOS;
using Malee.Editor;

/*
PathOSMainInspector.cs 
PathOSMainInspector (c) Nine Penguins (Samantha Stahlke) 2018
*/

[CustomEditor(typeof(PathOSManager))]
public class PathOSMainInspector : Editor
{
    private PathOSManager manager;
    private SerializedObject serial;

    private SerializedProperty limitSimulationTime;
    private SerializedProperty maxSimulationTime;
    private SerializedProperty endOnCompletionGoal;
    private GUIContent completionLabel;

    private SerializedProperty entityList;
    private ReorderableList entityListReorderable;
    private SerializedProperty heuristicWeights;

    private PathOS.Heuristic weightMatrixRowID;
    private PathOS.EntityType weightMatrixColumnID;

    private Dictionary<Heuristic, int> heuristicIndices;
    private Dictionary<EntityType, int> entypeIndices;

    private Dictionary<(Heuristic, EntityType), float> weightLookup;

    private bool transposeWeightMatrix;

    private void OnEnable()
    {
        manager = (PathOSManager)target;
        serial = new SerializedObject(manager);

        limitSimulationTime = serial.FindProperty("limitSimulationTime");
        maxSimulationTime = serial.FindProperty("maxSimulationTime");
        endOnCompletionGoal = serial.FindProperty("endOnCompletionGoal");

        completionLabel = new GUIContent("Final Goal Triggers End");

        entityList = serial.FindProperty("levelEntities");
        heuristicWeights = serial.FindProperty("heuristicWeights");

        entityListReorderable = new ReorderableList(serial.FindProperty("levelEntities"));
        entityListReorderable.elementNameProperty = "Level Entities";

        heuristicIndices = new Dictionary<Heuristic, int>();
        entypeIndices = new Dictionary<EntityType, int>();

        int index = 0;

        foreach(Heuristic heuristic in System.Enum.GetValues(typeof(Heuristic)))
        {
            heuristicIndices.Add(heuristic, index);
            ++index;
        }

        index = 0;

        foreach(EntityType entype in System.Enum.GetValues(typeof(EntityType)))
        {
            entypeIndices.Add(entype, index);
            ++index;
        }

        weightLookup = new Dictionary<(Heuristic, EntityType), float>();

        BuildWeightDictionary();
    }

    public override void OnInspectorGUI()
    {
        serial.Update();

        EditorGUILayout.PropertyField(limitSimulationTime);
        EditorGUILayout.PropertyField(maxSimulationTime);
        EditorGUILayout.PropertyField(endOnCompletionGoal, completionLabel);

        //Heuristic weight matrix.
        if(EditorGUILayout.PropertyField(heuristicWeights))
        {
            string transposeButtonText = "View by Entity Type";

            if (transposeWeightMatrix)
                transposeButtonText = "View by Heuristic";

            if (GUILayout.Button(transposeButtonText))
                transposeWeightMatrix = !transposeWeightMatrix;

            if (transposeWeightMatrix)
                weightMatrixColumnID = (PathOS.EntityType)EditorGUILayout.EnumPopup("Selected Entity Type:", weightMatrixColumnID);
            else
                weightMatrixRowID = (PathOS.Heuristic)EditorGUILayout.EnumPopup("Selected Heuristic:", weightMatrixRowID);

            Heuristic curHeuristic;
            EntityType curEntityType;

            System.Type indexType = (transposeWeightMatrix) ? typeof(Heuristic) : typeof(EntityType);

            foreach(var index in System.Enum.GetValues(indexType))
            {
                curHeuristic = (transposeWeightMatrix) ? (Heuristic)(index) : weightMatrixRowID;
                curEntityType = (transposeWeightMatrix) ? weightMatrixColumnID : (EntityType)(index);

                string label = (transposeWeightMatrix) ? curHeuristic.ToString() : curEntityType.ToString();

                if (!weightLookup.ContainsKey((curHeuristic, curEntityType)))
                    continue;

                weightLookup[(curHeuristic, curEntityType)] = 
                    EditorGUILayout.FloatField(label, weightLookup[(curHeuristic, curEntityType)]);

                manager.heuristicWeights[heuristicIndices[curHeuristic]].
                    weights[entypeIndices[curEntityType]].weight =
                    weightLookup[(curHeuristic, curEntityType)];              
            }

            if (GUILayout.Button("Refresh Matrix"))
            {
                manager.ResizeWeightMatrix();
                BuildWeightDictionary();
            }

            if(GUILayout.Button("Export Weights..."))
            {
                string exportPath = EditorUtility.SaveFilePanel("Export Weights...", Application.dataPath, "heuristic-weights", "csv");
                manager.ExportWeights(exportPath);
            }

            if(GUILayout.Button("Import Weights..."))
            {
                string importPath = EditorUtility.OpenFilePanel("Import Weights...", Application.dataPath, "csv");
                manager.ImportWeights(importPath);
                BuildWeightDictionary();
            }
        }

        //Entity list.
        entityListReorderable.DoLayoutList();

        serial.ApplyModifiedProperties();
        
        if(GUI.changed && !EditorApplication.isPlaying)
        {
            EditorUtility.SetDirty(manager);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        }       
    }

    private void BuildWeightDictionary()
    {
        weightLookup.Clear();

        for(int i = 0; i < manager.heuristicWeights.Count; ++i)
        {
            for(int j = 0; j < manager.heuristicWeights[i].weights.Count; ++j)
            {
                weightLookup.Add((manager.heuristicWeights[i].heuristic,
                    manager.heuristicWeights[i].weights[j].entype),
                    manager.heuristicWeights[i].weights[j].weight);
            }
        }
    }
}
