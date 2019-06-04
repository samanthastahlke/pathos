using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
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
    private OGLogVisualizer vis;
    private SerializedObject serial;

    //Logfile management.
    private static bool fileFoldout = true;
    private string lblFileFoldout = "Manage Log Files";

    private const int pathDisplayLength = 32;
    private GUIStyle errorStyle = new GUIStyle();

    private string logDirectoryDisplay;

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

    private SerializedProperty propHeatmapGradient;
    private SerializedProperty propHeatmapAlpha;
    private SerializedProperty propHeatmapTileSize;
    private SerializedProperty propHeatmapAggregate;
    private SerializedProperty propHeatmapTimeSlice;

    //Path display settings.
    private static bool pathFoldout = false;
    private string lblPathFoldout = "Individual Paths";

    private PathOSAgent agentReference;
    private List<PathOS.Heuristic> heuristics = new List<PathOS.Heuristic>();

    private SerializedProperty propShowIndividual;
    private SerializedProperty propShowIndividualInteractions;
    private Texture2D polylinetex;

    //Interaction display settings.
    private static bool interactionFoldout = false;
    private string lblInteractionFoldout = "Entity Interactions";

    private SerializedProperty propShowEntities;
    private SerializedProperty propEntityGradient;
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

        propHeatmapGradient = serial.FindProperty("heatmapGradient");
        propHeatmapAlpha = serial.FindProperty("heatmapAlpha");
        propHeatmapTileSize = serial.FindProperty("tileSize");
        propHeatmapAggregate = serial.FindProperty("heatmapAggregateActiveOnly");
        propHeatmapTimeSlice = serial.FindProperty("heatmapUseTimeSlice");

        propShowIndividual = serial.FindProperty("showIndividualPaths");
        propShowIndividualInteractions = serial.FindProperty("showIndividualInteractions");

        propShowEntities = serial.FindProperty("showEntities");
        propEntityGradient = serial.FindProperty("interactionGradient");
        propEntityAggregate = serial.FindProperty("interactionAggregateActiveOnly");
        propEntityTimeSlice = serial.FindProperty("interactionUseTimeSlice");

        PathOS.UI.TruncateStringHead(vis.logDirectory,
            ref logDirectoryDisplay, pathDisplayLength);

        errorStyle.normal.textColor = Color.red;

        if(expansionToggleStyle != null)
        {
            expansionToggleStyle.fixedHeight = 16.0f;
            expansionToggleStyle.fixedWidth = 32.0f;
        }

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
                    vis.logDirectory : Application.dataPath;

                string selectedPath = EditorUtility.OpenFolderPanel("Select Folder...",
                    defaultDirectory, "");

                if (selectedPath != "")
                    vis.logDirectory = selectedPath;

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
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.MinMaxSlider("Time Range",
                ref vis.displayTimeRange.min,
                ref vis.displayTimeRange.max,
                vis.fullTimeRange.min,
                vis.fullTimeRange.max);

            vis.displayTimeRange.min = EditorGUILayout.FloatField(
                PathOS.UI.RoundFloatfield(vis.displayTimeRange.min),
                GUILayout.Width(PathOS.UI.shortFloatfieldWidth));

            EditorGUILayout.LabelField("<->",
                    GUILayout.Width(PathOS.UI.shortLabelWidth));

            vis.displayTimeRange.max = EditorGUILayout.FloatField(
                PathOS.UI.RoundFloatfield(vis.displayTimeRange.max),
                GUILayout.Width(PathOS.UI.shortFloatfieldWidth));

            EditorGUILayout.EndHorizontal();

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

            if (null == expansionToggleStyle)
            {
                expansionToggleStyle = EditorStyles.miniButton;
                expansionToggleStyle.fixedHeight = 16.0f;
                expansionToggleStyle.fixedWidth = 32.0f;
            }

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
                pLog.visInclude = GUILayout.Toggle(pLog.visInclude, pLog.playerID);

                if (oldFilter != pLog.visInclude)
                    refreshFilter = true;

                pLog.showDetail = GUILayout.Toggle(pLog.showDetail, expansionLabel, expansionToggleStyle);

                GUILayout.EndHorizontal();

                if (pLog.showDetail)
                {
                    agentReference = EditorGUILayout.ObjectField("Agent to update: ",
                        agentReference, typeof(PathOSAgent), true) as PathOSAgent;

                    if (GUILayout.Button("Copy heuristics to agent")
                        && agentReference != null)
                    {
                        Undo.RecordObject(agentReference, "Copy heuristics to agent");

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

            EditorGUILayout.PropertyField(propHeatmapGradient);
            EditorGUILayout.PropertyField(propHeatmapAlpha);
            EditorGUILayout.PropertyField(propHeatmapTileSize);
            EditorGUILayout.PropertyField(propHeatmapAggregate);
            EditorGUILayout.PropertyField(propHeatmapTimeSlice);

            if (GUILayout.Button("Apply Heatmap Settings"))
                vis.ApplyHeatmapSettings();
        }

        //Collapsible pane for path display settings.
        pathFoldout = EditorGUILayout.Foldout(pathFoldout, lblPathFoldout);

        if (pathFoldout)
        {
            //Global path display settings.
            EditorGUILayout.PropertyField(propShowIndividual);

            EditorGUILayout.PropertyField(propShowIndividualInteractions);

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

            EditorGUILayout.PropertyField(propEntityGradient);
            EditorGUILayout.PropertyField(propEntityAggregate);
            EditorGUILayout.PropertyField(propEntityTimeSlice);

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

                    if (vis.showIndividualInteractions)
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
                            Handles.Label(curEvent.pos, curEvent.objectName);
                        }
                    }
                }
            }
        } 
        
        if(vis.showEntities)
        {
            foreach(KeyValuePair<string, OGLogVisualizer.AggregateInteraction> interaction 
                in vis.aggregateInteractions)
            {
                Handles.color = interaction.Value.displayColor;
                Handles.DrawSolidDisc(interaction.Value.pos, Vector3.up, interaction.Value.displaySize);
                Handles.Label(interaction.Value.pos, interaction.Value.displayName);
            }

            foreach (KeyValuePair<string, OGLogVisualizer.AggregateInteraction> interaction
                in vis.aggregateInteractions)
            { 
                Handles.Label(interaction.Value.pos, interaction.Value.displayName);
            }
        }
    }
}
