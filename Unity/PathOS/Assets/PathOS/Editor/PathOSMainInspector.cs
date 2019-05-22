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
    private GameObject selection = null;
    private int hotControlRestore = 0;

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

    private class MarkupToggle
    {
        public static GUIStyle style;

        public EntityType entityType;
        public bool isClear;

        public Texture2D icon;
        public Texture2D cursor;
        private GUIContent content;

        public string label;

        public bool active;

        public MarkupToggle(EntityType entityType, string label,
            Texture2D icon, Texture2D cursor, bool isClear = false)
        {
            this.entityType = entityType;
            this.label = label;
            this.icon = icon;
            this.cursor = cursor;
            this.isClear = isClear;

            content = new GUIContent(icon);
        }

        public bool Layout()
        {
            //Toggle style - should be the same as small Editor buttons.
            //Check is placed here to avoid Unity not having initialized/
            //properly referenced the EditorStyles on load.
            if (null == style)
            {
                style = EditorStyles.miniButton;
                style.fixedHeight = 32.0f;
                style.fixedWidth = 32.0f;
            }

            GUILayout.BeginHorizontal();

            active = GUILayout.Toggle(active, content, style);
            GUILayout.Label(label);

            GUILayout.EndHorizontal();

            return active;
        }
    }

    private List<MarkupToggle> markupToggles = new List<MarkupToggle>();
    private MarkupToggle activeToggle = null;

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

            //Set up toggles.
            markupToggles.Add(new MarkupToggle(entype,
                manager.entityLabelLookup[entype],
                Resources.Load<Texture2D>(manager.entityGizmoLookup[entype]),
                Resources.Load<Texture2D>("cursor_" + manager.entityGizmoLookup[entype])));
        }

        markupToggles.Add(new MarkupToggle(EntityType.ET_NONE,
            "Clear Markup (remove from entity list)",
            Resources.Load<Texture2D>("delete"),
            Resources.Load<Texture2D>("cursor_delete"),
            true));

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
            for(int i = 0; i < markupToggles.Count; ++i)
            {
                if(markupToggles[i].Layout())
                    ActivateToggle(markupToggles[i]);
            }
        }

        //Entity list.
        entityListReorderable.DoLayoutList();

        serial.ApplyModifiedProperties();
        SceneView.RepaintAll();

        if (GUI.changed && !EditorApplication.isPlaying)
        {
            EditorUtility.SetDirty(manager);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        }   
    }

    private void OnSceneGUI()
    {
        if (activeToggle != null)
        {
            Cursor.SetCursor(activeToggle.cursor, Vector2.zero, CursorMode.Auto);
            EditorGUIUtility.AddCursorRect(new Rect(0.0f, 0.0f, 10000.0f, 10000.0f), MouseCursor.CustomCursor);

            if (EditorWindow.mouseOverWindow != null &&
                EditorWindow.mouseOverWindow.ToString() == " (UnityEditor.SceneView)")
            {
                if (Event.current.type == EventType.MouseMove)
                    selection = HandleUtility.PickGameObject(Event.current.mousePosition, true);
            }
            else
                selection = null;

            if (Event.current.type == EventType.MouseDown)
            {
                Event.current.Use();

                hotControlRestore = GUIUtility.hotControl;
                int passiveControlId = GUIUtility.GetControlID(FocusType.Passive);
                GUIUtility.hotControl = passiveControlId;

                if (selection != null)
                {
                    Undo.RecordObject(manager, "Edit Level Markup");

                    int selectedID = selection.GetInstanceID();

                    bool addNewEntry = !activeToggle.isClear;

                    for(int i = 0; i < manager.levelEntities.Count; ++i)
                    {
                        if (manager.levelEntities[i].objectRef.GetInstanceID() == selectedID)
                        {
                            if (activeToggle.isClear)
                                manager.levelEntities.RemoveAt(i);
                            else
                                manager.levelEntities[i].entityType = activeToggle.entityType;

                            addNewEntry = false;
                            break;
                        }  
                    }

                    if(addNewEntry)
                        manager.levelEntities.Add(new LevelEntity(selection, activeToggle.entityType));
                }
            }
            else if (Event.current.type == EventType.KeyDown
            && Event.current.keyCode == KeyCode.Escape)
            {
                ActivateToggle(null);
                Repaint();
            }
        }

        manager.curMouseover = selection;
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

    private void ActivateToggle(MarkupToggle toggle)
    {
        if(activeToggle != null)
            activeToggle.active = false;

        activeToggle = toggle;

        if(null == activeToggle)
        {
            selection = null;
            GUIUtility.hotControl = 0;
        }
        else
        {
            activeToggle.active = true;
        }
    }
}
