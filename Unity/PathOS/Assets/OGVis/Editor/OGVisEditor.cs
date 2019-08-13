using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using OGVis;

/*
OGVisEditor.cs
OGVisEditor (c) Ominous Games 2018

This class manages the Unity Inspector pane for vis customization.
*/

[ExecuteInEditMode]
[CustomEditor(typeof(OGLogVisualizer))]
public class OGVisEditor : Editor
{
    //Core object references.
    private OGLogVisualizer vis;
    private SerializedObject serial;

    //Logfile management.
    private static bool fileFoldout = true;
    private string lblFileFoldout = "Manage Log Files";

    private const int pathDisplayLength = 32;
    private GUIStyle errorStyle = new GUIStyle();

    private string logDirectoryDisplay;
    private string defaultDialogDirectory;

    private SerializedProperty propLogDirectory;

    //Filter/display management.
    private static bool filterFoldout = false;
    private string lblFilterFoldout = "Filtering/Display Options";

    private SerializedProperty propDisplayHeight;

    private GUIStyle expansionToggleStyle;
    private GUIContent expansionLabel = new GUIContent("...", "Show agent profile");
    private GUIContent noContent = new GUIContent("");

    //Heatmap.
    private static bool heatmapFoldout = false;
    private string lblHeatmapFoldout = "Heatmap";

    private SerializedProperty propHeatmapAlpha;
    private SerializedProperty propHeatmapTileSize;

    private SerializedProperty propHeatmapAggregate;
    private GUIContent toggleAggregateLabel = new GUIContent("Active Agents Only",
                "Only include data from agents\nchecked in the filtering tab");

    private SerializedProperty propHeatmapTimeSlice;
    private GUIContent toggleTimeSliceLabel = new GUIContent("Use Time Range",
                "Only include data within the range\nspecified in the filtering tab");

    //Path display settings.
    private static bool pathFoldout = false;
    private string lblPathFoldout = "Individual Paths";

    private PathOSAgent agentReference;
    private List<PathOS.Heuristic> heuristics = new List<PathOS.Heuristic>();

    private SerializedProperty propShowIndividual;
    private SerializedProperty propShowIndividualInteractions;
    private GUIContent individualInteractionsLabel = new GUIContent("Individual Interactions",
                "Show visited entities along\nindividual agent paths");

    private Texture2D polylinetex;

    //Interaction display settings.
    private static bool interactionFoldout = false;
    private string lblInteractionFoldout = "Entity Interactions";

    private SerializedProperty propShowEntities;
    private SerializedProperty propEntityAggregate;
    private SerializedProperty propEntityTimeSlice;

    //Called when the inspector pane is initialized.
    private void OnEnable()
    {
        //Grab our sharp line texture - this looks nicer on screen than the default.
        polylinetex = Resources.Load("polylinetex") as Texture2D;

        //Grab a reference to the serialized representation of the visualization.
        vis = (OGLogVisualizer)target;
        serial = new SerializedObject(vis);

        //These serialized properties will let us skip some of the legwork in displaying
        //interactive widgets in the inspector.
        propLogDirectory = serial.FindProperty("logDirectory");

        propDisplayHeight = serial.FindProperty("displayHeight");

        propHeatmapAlpha = serial.FindProperty("heatmapAlpha");
        propHeatmapTileSize = serial.FindProperty("tileSize");
        propHeatmapAggregate = serial.FindProperty("heatmapAggregateActiveOnly");
        propHeatmapTimeSlice = serial.FindProperty("heatmapUseTimeSlice");

        propShowIndividual = serial.FindProperty("showIndividualPaths");
        propShowIndividualInteractions = serial.FindProperty("showIndividualInteractions");

        propShowEntities = serial.FindProperty("showEntities");
        propEntityAggregate = serial.FindProperty("interactionAggregateActiveOnly");
        propEntityTimeSlice = serial.FindProperty("interactionUseTimeSlice");

        PathOS.UI.TruncateStringHead(vis.logDirectory,
            ref logDirectoryDisplay, pathDisplayLength);

        //Need to chop off "/Assets" - 7 characters.
        defaultDialogDirectory = Application.dataPath.Substring(0,
            Application.dataPath.Length - 7);

        errorStyle.normal.textColor = Color.red;

        heuristics.Clear();
    }

    //Draws the inspector pane itself.
    public override void OnInspectorGUI()
    {
        //Make sure our vis representation is up-to-date.
        serial.Update();

        //Collapsible file management pane.
        fileFoldout = EditorGUILayout.Foldout(fileFoldout, lblFileFoldout);

        if(fileFoldout)
        {
            EditorGUILayout.LabelField("Load Directory: ", logDirectoryDisplay);

            if(GUILayout.Button("Browse..."))
            {
                string defaultDirectory = (Directory.Exists(vis.logDirectory)) ?
                    vis.logDirectory : defaultDialogDirectory;

                string selectedPath = EditorUtility.OpenFolderPanel("Select Folder...",
                    defaultDirectory, "");

                if (selectedPath != "")
                {
                    vis.logDirectory = selectedPath;

                    EditorUtility.SetDirty(vis);
                    EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
                }  

                PathOS.UI.TruncateStringHead(vis.logDirectory,
                    ref logDirectoryDisplay, pathDisplayLength);
            }

            if(!Directory.Exists(vis.logDirectory))
            {
                EditorGUILayout.LabelField("Error! You must choose a " +
                    "valid folder on this computer.", errorStyle);
            }

            //Log file loading/management.
            if (GUILayout.Button("Add Files from " + logDirectoryDisplay + "/"))
            {
                //Add log files, if the provided directory is valid.
                vis.LoadLogs();
            }

            if(GUILayout.Button("Clear All Data"))
            {
                //Clear all logs and records from the current session.
                vis.ClearData();
                Debug.Log("Cleared all visualization data.");
            }
        }

        //Collapsible display options pane.
        filterFoldout = EditorGUILayout.Foldout(filterFoldout, lblFilterFoldout);

        if(filterFoldout)
        {
            PathOS.EditorUI.FullMinMaxSlider("Time Range",
                ref vis.displayTimeRange.min,
                ref vis.displayTimeRange.max,
                vis.fullTimeRange.min,
                vis.fullTimeRange.max);

            if (GUILayout.Button("Apply Time Range"))
                vis.ApplyDisplayRange();

            EditorGUILayout.PropertyField(propDisplayHeight);

            if (GUILayout.Button("Apply Display Height"))
            {
                vis.ApplyDisplayHeight();
                vis.ReclusterEvents();
            }

            bool refreshFilter = false;
            bool oldFilter = false;

            expansionToggleStyle = EditorStyles.miniButton;
            expansionToggleStyle.fixedHeight = 16.0f;
            expansionToggleStyle.fixedWidth = 32.0f;

            if (vis.pLogs.Count > 0)
                GUILayout.Label("Filter Data by Player ID:");


            //Filter options.
            //Enable/disable players.
            foreach (PlayerLog pLog in vis.pLogs)
            {
                GUILayout.BeginHorizontal();

                pLog.pathColor = EditorGUILayout.ColorField(noContent, 
                    pLog.pathColor, false, false, false, GUILayout.Width(16.0f));

                oldFilter = pLog.visInclude;
                pLog.visInclude = GUILayout.Toggle(pLog.visInclude, "", GUILayout.Width(16.0f));
                pLog.playerID = EditorGUILayout.TextField(pLog.playerID);

                if (oldFilter != pLog.visInclude)
                    refreshFilter = true;

                pLog.showDetail = GUILayout.Toggle(pLog.showDetail, expansionLabel, expansionToggleStyle);

                GUILayout.EndHorizontal();

                if (pLog.showDetail)
                {
                    agentReference = EditorGUILayout.ObjectField("Agent to update: ",
                        agentReference, typeof(PathOSAgent), true) as PathOSAgent;

                    if (GUILayout.Button("Copy motives to agent")
                        && agentReference != null)
                    {
                        Undo.RecordObject(agentReference, "Copy motives to agent");

                        agentReference.experienceScale = pLog.experience;

                        foreach (PathOS.HeuristicScale scale in agentReference.heuristicScales)
                        {
                            if (pLog.heuristics.ContainsKey(scale.heuristic))
                                scale.scale = pLog.heuristics[scale.heuristic];
                        }
                    }

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Experience:", GUILayout.Width(84.0f));
                    EditorGUILayout.LabelField(pLog.experience.ToString());
                    EditorGUILayout.EndHorizontal();

                    foreach (KeyValuePair<PathOS.Heuristic, float> stat in pLog.heuristics)
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField(PathOS.UI.heuristicLabels[stat.Key] + ":",
                            GUILayout.Width(84.0f));
                        EditorGUILayout.LabelField(stat.Value.ToString());
                        EditorGUILayout.EndHorizontal();
                    }

                }
            }

            //Shortcut to enable all PIDs in the vis.
            if (GUILayout.Button("Select All"))
            {
                foreach (PlayerLog pLog in vis.pLogs)
                {
                    pLog.visInclude = true;
                }

                refreshFilter = true;
            }

            //Shortcut to exclude all PIDs from the vis.
            if (GUILayout.Button("Select None"))
            {
                foreach (PlayerLog pLog in vis.pLogs)
                {
                    pLog.visInclude = false;
                }

                refreshFilter = true;
            }

            //If we've detected a change that requires re-aggregation, do so.
            if (refreshFilter)
            {
                if (vis.interactionAggregateActiveOnly)
                    vis.ReclusterEvents();

                if (vis.heatmapAggregateActiveOnly)
                    vis.UpdateHeatmap();
            }
        }

        //Collapsible pane for path display settings.
        heatmapFoldout = EditorGUILayout.Foldout(heatmapFoldout, lblHeatmapFoldout);

        if(heatmapFoldout)
        {
            EditorGUI.BeginChangeCheck();
            vis.showHeatmap = EditorGUILayout.Toggle("Show Heatmap", vis.showHeatmap);

            if (EditorGUI.EndChangeCheck())
                vis.UpdateHeatmapVisibility();

            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField("Heatmap Colours", GUILayout.Width(PathOS.UI.longLabelWidth));

            EditorGUILayout.LabelField("Low", GUILayout.Width(PathOS.UI.shortLabelWidth));
            vis.heatmapGradient = EditorGUILayout.GradientField(vis.heatmapGradient);
            EditorGUILayout.LabelField("High", GUILayout.Width(PathOS.UI.mediumLabelWidth));

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.PropertyField(propHeatmapAlpha);
            EditorGUILayout.PropertyField(propHeatmapTileSize);
            EditorGUILayout.PropertyField(propHeatmapAggregate, toggleAggregateLabel);
            EditorGUILayout.PropertyField(propHeatmapTimeSlice, toggleTimeSliceLabel);

            if (GUILayout.Button("Apply Heatmap Settings"))
                vis.ApplyHeatmapSettings();
        }

        //Collapsible pane for path display settings.
        pathFoldout = EditorGUILayout.Foldout(pathFoldout, lblPathFoldout);

        if (pathFoldout)
        {
            //Global path display settings.
            EditorGUILayout.PropertyField(propShowIndividual);

            EditorGUILayout.PropertyField(propShowIndividualInteractions, 
                individualInteractionsLabel);

            if (vis.pLogs.Count > 0)
                GUILayout.Label("Agent Colors:");

            //Filter options.
            //Enable/disable players, set path colour by player ID.
            foreach (PlayerLog pLog in vis.pLogs)
            {
                GUILayout.BeginHorizontal();

                EditorGUILayout.LabelField(pLog.playerID);
                pLog.pathColor = EditorGUILayout.ColorField(pLog.pathColor);

                GUILayout.EndHorizontal();
            }
        }

        //Collapsible pane for managing display of entity interactions.
        interactionFoldout = EditorGUILayout.Foldout(interactionFoldout, lblInteractionFoldout);

        if (interactionFoldout)
        {
            EditorGUILayout.PropertyField(propShowEntities);

            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField("Interaction Gradient", GUILayout.Width(PathOS.UI.longLabelWidth));

            EditorGUILayout.LabelField("Low", GUILayout.Width(PathOS.UI.shortLabelWidth));
            vis.interactionGradient = EditorGUILayout.GradientField(vis.interactionGradient);
            EditorGUILayout.LabelField("High", GUILayout.Width(PathOS.UI.mediumLabelWidth));

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.PropertyField(propEntityAggregate, toggleAggregateLabel);
            EditorGUILayout.PropertyField(propEntityTimeSlice, toggleTimeSliceLabel);

            if (GUILayout.Button("Apply Interaction Display Settings"))
                vis.ReclusterEvents();
        }

        serial.ApplyModifiedProperties();
        SceneView.RepaintAll();
    }

    //Draws content in the scene context.
    private void OnSceneGUI()
    {
        //Don't draw anything if the script is disabled.
        if (!vis.enabled)
            return;

        //Draw individual player paths.
        if (vis.showIndividualPaths)
        {
            foreach (PlayerLog pLog in vis.pLogs)
            {
                if(pLog.visInclude)
                {
                    if (pLog.pathPoints.Count > 0)
                    {
                        //Draw path trace.
                        Vector3[] points = pLog.pathPoints.GetRange(
                            pLog.displayStartIndex,
                            pLog.displayEndIndex - pLog.displayStartIndex + 1)
                            .ToArray();

                        Handles.color = pLog.pathColor;
                        Handles.DrawAAPolyLine(polylinetex, OGLogVisualizer.PATH_WIDTH, points);
                    }

                    //Individual interactions are only shown if aggregate interactions
                    //are hidden (to prevent overlap).
                    if (vis.showIndividualInteractions && !vis.showEntities)
                    {
                        for (int i = 0; i < pLog.interactionEvents.Count; ++i)
                        {
                            PlayerLog.InteractionEvent curEvent = pLog.interactionEvents[i];

                            if (curEvent.timestamp < vis.currentTimeRange.min)
                                continue;
                            else if (curEvent.timestamp > vis.currentTimeRange.max)
                                break;

                            Handles.color = Color.white;
                            Handles.DrawSolidDisc(curEvent.pos, Vector3.up, OGLogVisualizer.MIN_ENTITY_RADIUS);
                            Handles.Label(curEvent.pos, curEvent.objectName, GUI.skin.textArea);
                        }
                    }
                }
            }
        } 
        
        //Draw aggregate entity interactions.
        if(vis.showEntities)
        {
            foreach(KeyValuePair<string, OGLogVisualizer.AggregateInteraction> interaction 
                in vis.aggregateInteractions)
            {
                Handles.color = interaction.Value.displayColor;
                Handles.DrawSolidDisc(interaction.Value.pos, Vector3.up, 
                    interaction.Value.displaySize);
            }

            foreach (KeyValuePair<string, OGLogVisualizer.AggregateInteraction> interaction
                in vis.aggregateInteractions)
            {
                Handles.Label(interaction.Value.pos,
                    interaction.Value.displayName, GUI.skin.textArea);
            }
        }
    }
}
