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
    private static bool fileFoldout = false;
    private string lblFileFoldout = "Manage Log Files";

    private const int pathDisplayLength = 32;
    private GUIStyle errorStyle = new GUIStyle();

    private string logDirectoryDisplay;

    private SerializedProperty propLogDirectory;

    //Filter/display management.
    private static bool filterFoldout = false;
    private string lblFilterFoldout = "Filtering/Display Options";

    private SerializedProperty propDisplayHeight;

    //Path display settings.
    private static bool pathFoldout = false;
    private string lblPathFoldout = "Agent Navigation Data";

    private SerializedProperty propShowHeatmap;
    private SerializedProperty propHeatmapGradient;
    private SerializedProperty propShowIndividual;
    private SerializedProperty propHeatmapGridSize;

    private Texture2D polylinetex;

    //Interaction display settings.
    private static bool interactionFoldout = false;
    private string lblInteractionFoldout = "Entity Interactions";

    private SerializedProperty propShowEntities;

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

        propShowIndividual = serial.FindProperty("showIndividualPaths");
        propShowHeatmap = serial.FindProperty("showHeatmap");
        propHeatmapGradient = serial.FindProperty("heatmapGradient");

        propDisplayHeight = serial.FindProperty("displayHeight");
        propHeatmapGridSize = serial.FindProperty("gridSize");

        propShowEntities = serial.FindProperty("showEntities");

        PathOS.UI.TruncateStringHead(vis.logDirectory,
            ref logDirectoryDisplay, pathDisplayLength);

        errorStyle.normal.textColor = Color.red;
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
            //Used to see if the player filters have been updated.
            //If filters are updated, the vis will be refreshed.
            bool refreshFilter = false;
            bool oldFilter = false;

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

            //Chekck for an update to global aggregation settings.
            oldFilter = vis.aggregateActiveOnly;
            vis.aggregateActiveOnly = GUILayout.Toggle(vis.aggregateActiveOnly, "Aggregate from enabled agents only");

            if (oldFilter != vis.aggregateActiveOnly)
                refreshFilter = true;

            EditorGUILayout.PropertyField(propDisplayHeight);

            //If the user wants to commit axis-flattening settings, update the vis
            //accordingly.
            if (GUILayout.Button("Apply Display Height"))
            {
                vis.ApplyDisplayHeight();
                vis.ReclusterEvents();
            }

            //Collapsible pane for path display settings.
            pathFoldout = EditorGUILayout.Foldout(pathFoldout, lblPathFoldout);

            if(pathFoldout)
            {
                //Global path display settings.
                EditorGUI.BeginChangeCheck();
                vis.showHeatmap = EditorGUILayout.Toggle("Show Heatmap", vis.showHeatmap);

                if (EditorGUI.EndChangeCheck())
                    vis.UpdateHeatmapVisibility();

                EditorGUILayout.PropertyField(propHeatmapGradient);
                EditorGUILayout.PropertyField(propShowIndividual);
                
                if(vis.pLogs.Count > 0)
                    GUILayout.Label("Filter Data by Player ID:");

                //Filter options.
                //Enable/disable players, set path colour by player ID.
                foreach (PlayerLog pLog in vis.pLogs)
                {
                    GUILayout.BeginHorizontal();

                    oldFilter = pLog.visInclude;
                    pLog.visInclude = GUILayout.Toggle(pLog.visInclude, pLog.playerID);

                    if (oldFilter != pLog.visInclude && vis.aggregateActiveOnly)
                        refreshFilter = true;

                    pLog.pathColor = EditorGUILayout.ColorField(pLog.pathColor);
                    GUILayout.EndHorizontal();
                }
                
                //Shortcut to enable all PIDs in the vis.
                if(GUILayout.Button("Select All"))
                {
                    foreach (PlayerLog pLog in vis.pLogs)
                    {
                        pLog.visInclude = true;
                    }

                    if(vis.aggregateActiveOnly)
                        refreshFilter = true;
                }

                //Shortcut to exclude all PIDs from the vis.
                if(GUILayout.Button("Select None"))
                {
                    foreach (PlayerLog pLog in vis.pLogs)
                    {
                        pLog.visInclude = false;
                    }

                    if(vis.aggregateActiveOnly)
                        refreshFilter = true;
                }             
            }

            //If we've detected a change that requires re-aggregation, do so.
            if (refreshFilter)
            {
                vis.ReclusterEvents();
            }
            
            //Collapsible pane for managing display of gameplay events.
            interactionFoldout = EditorGUILayout.Foldout(interactionFoldout, lblInteractionFoldout);

            if(interactionFoldout)
            {
                EditorGUILayout.PropertyField(propShowEntities);

            }
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
                        Handles.DrawAAPolyLine(polylinetex, OGLogVisualizer.MIN_PATH_WIDTH, points);
                    }

                    if (vis.showIndividualInteractions)
                    {
                        for (int i = 0; i < pLog.interactionEvents.Count; ++i)
                        {
                            PlayerLog.InteractionEvent curEvent = pLog.interactionEvents[i];

                            if (curEvent.timestamp < vis.displayTimeRange.min)
                                continue;
                            else if (curEvent.timestamp > vis.displayTimeRange.max)
                                break;

                            Handles.color = Color.white;
                            Handles.DrawSolidDisc(curEvent.pos, Vector3.up, 0.1f);
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
