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

    private GUIStyle foldoutStyle = GUIStyle.none;

    private SerializedProperty limitSimulationTime;
    private SerializedProperty maxSimulationTime;
    private SerializedProperty endOnCompletionGoal;
    private GUIContent completionLabel;

    private bool showMarkup = false;
    private bool iconDrag = false;
    private bool updateSelection = false;
    private GameObject selection = null;

    private SerializedProperty entityList;
    private ReorderableList entityListReorderable;
    private SerializedProperty heuristicWeights;

    private PathOS.Heuristic weightMatrixRowID;
    private PathOS.EntityType weightMatrixColumnID;

    private Dictionary<Heuristic, int> heuristicIndices;
    private Dictionary<EntityType, int> entypeIndices;

    private Dictionary<(Heuristic, EntityType), float> weightLookup;

    private bool transposeWeightMatrix;

    private Texture2D testIcon;
    private Texture2D testCursor;

    private void OnEnable()
    {
        manager = (PathOSManager)target;
        serial = new SerializedObject(manager);

        testIcon = Resources.Load<Texture2D>("hazard_enemy");
        testCursor = Resources.Load<Texture2D>("cursor_hazard_enemy");

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

        //Placed here since Unity seems to have issues with having these 
        //styles initialized on enable sometimes.
        foldoutStyle = EditorStyles.foldout;
        foldoutStyle.fontStyle = FontStyle.Bold;

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

        showMarkup = EditorGUILayout.Foldout(
            showMarkup, "Level Markup", foldoutStyle);

        if (showMarkup)
        {
            GUIStyle testStyle = new GUIStyle();
            testStyle.stretchHeight = true;
            testStyle.stretchWidth = true;
            testStyle.fixedHeight = 32.0f;
            testStyle.fixedWidth = 32.0f;
            
            GUILayout.Box(testIcon, testStyle);
            
            if(GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition))
            {
                if (Event.current.type == EventType.MouseDown)
                {
                    Event.current.Use();
                    iconDrag = true;
                    selection = null;
                }
            }
            
            if(Event.current.type == EventType.MouseUp && iconDrag)
            {
                Event.current.Use();
                iconDrag = false;

                if (selection != null)
                    manager.levelEntities.Add(new LevelEntity(selection, EntityType.ET_HAZARD_ENEMY));

                selection = null;
            }

            if (Event.current.type == EventType.MouseDrag)
                SceneView.RepaintAll();
        }

        //Entity list.
        entityListReorderable.DoLayoutList();

        serial.ApplyModifiedProperties();

        if (GUI.changed && !EditorApplication.isPlaying)
        {
            EditorUtility.SetDirty(manager);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        }   
    }

    private void OnSceneGUI()
    {
        selection = null;

        if (iconDrag)
        {
            GUI.DrawTexture(new Rect(Event.current.mousePosition.x,
                Event.current.mousePosition.y, 32.0f, 32.0f), testIcon,
                ScaleMode.ScaleToFit);

            Cursor.SetCursor(testCursor, Vector2.zero, CursorMode.Auto);
            EditorGUIUtility.AddCursorRect(new Rect(0.0f, 0.0f, 10000.0f, 10000.0f), MouseCursor.CustomCursor);

            if(EditorWindow.mouseOverWindow.ToString() == " (UnityEditor.SceneView)")
            {
                Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
                RaycastHit hit;

                if (Physics.Raycast(ray, out hit, 100.0f))
                {
                    GameObject obj = hit.transform.gameObject;
                    manager.curMouseover = obj;
                    selection = obj;
                }

                //int blocker = GUIUtility.GetControlID(FocusType.Passive);
                //HandleUtility.AddDefaultControl(blocker);
                //GUIUtility.hotControl = blocker;

                //manager.curMouseover = HandleUtility.PickGameObject(Event.current.mousePosition, true);
            }         
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
