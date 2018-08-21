using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

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

    private SerializedProperty propLogDirectory;

    //Filter/display management.
    private static bool filterFoldout = false;
    private string lblFilterFoldout = "Filtering/Display Options";

    //Path display settings.
    private static bool pathFoldout = false;
    private string lblPathFoldout = "Player Telemetry";

    private SerializedProperty propFlattenCoordinates;
    private SerializedProperty propDisplaySampleRate;
    private SerializedProperty propAggregateGridSize;
    private SerializedProperty propAggregateEdgeScale;

    private Texture2D polylinetex;

    //Input event display settings.
    private static bool inputFoldout = false;
    private string lblInputFoldout = "Input Events";

    private SerializedProperty propInputClusterRadius;
    private SerializedProperty propInputLinearScale;

    //Game event display settings.
    private static bool gameFoldout = false;
    private string lblGameFoldout = "Game Events";

    private SerializedProperty propGameClusterRadius;
    private SerializedProperty propGameLinearScale;

    //Called when the inspector pane is initialized.
    private void OnEnable()
    {
        //Debug.Log("Waking OGVisEditor...");

        //Grab our sharp line texture - this looks nicer on screen than the default.
        polylinetex = Resources.Load("polylinetex") as Texture2D;

        //Grab a reference to the serialized representation of the visualization.
        vis = (OGLogVisualizer)target;
        serial = new SerializedObject(vis);

        //These serialized properties will let us skip some of the legwork in displaying
        //interactive widgets in the inspector.
        propLogDirectory = serial.FindProperty("logDirectory");
        propFlattenCoordinates = serial.FindProperty("flattenCoordinates");
        propDisplaySampleRate = serial.FindProperty("displaySampleRate");
        propAggregateGridSize = serial.FindProperty("aggregateGridEdge");
        propAggregateEdgeScale = serial.FindProperty("aggregateEdgeExtents");

        propInputClusterRadius = serial.FindProperty("inputClusterRadius");
        propGameClusterRadius = serial.FindProperty("gameClusterRadius");
        propInputLinearScale = serial.FindProperty("inputExtents");
        propGameLinearScale = serial.FindProperty("gameExtents");
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
            EditorGUILayout.PropertyField(propLogDirectory);

            //Log file loading/management.
            if (GUILayout.Button("Add Files from Assets/" + propLogDirectory.stringValue + "/"))
            {
                //Add log files, if the provided directory is valid.
                vis.LoadLogs();
            }

            if(GUILayout.Button("Clear All Data"))
            {
                //Clear all logs and records from the current session.
                vis.ClearData();
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

            //Chekck for an update to global aggregation settings.
            oldFilter = vis.aggregateActiveOnly;
            vis.aggregateActiveOnly = GUILayout.Toggle(vis.aggregateActiveOnly, "Aggregate from enabled players only");

            if (oldFilter != vis.aggregateActiveOnly)
                refreshFilter = true;

            //Axis flattening settings.
            EditorGUILayout.PropertyField(propFlattenCoordinates);
            GUILayout.Label("Enable Axis Flattening");
            GUILayout.BeginHorizontal();
            vis.flattenX = GUILayout.Toggle(vis.flattenX, "X");
            vis.flattenY = GUILayout.Toggle(vis.flattenY, "Y");
            vis.flattenZ = GUILayout.Toggle(vis.flattenZ, "Z");
            GUILayout.EndHorizontal();

            //If the user wants to commit axis-flattening settings, update the vis
            //accordingly.
            if (GUILayout.Button("Apply Axis Flattening Settings"))
            {
                vis.ResamplePaths();
                vis.ReaggregatePath();
                vis.ReclusterEvents();
            }

            //Collapsible pane for path display settings.
            pathFoldout = EditorGUILayout.Foldout(pathFoldout, lblPathFoldout);

            if(pathFoldout)
            {
                //Global path display settings.
                vis.showIndividualPaths = GUILayout.Toggle(vis.showIndividualPaths, "Show Individual Paths");
                vis.showAggregatePath = GUILayout.Toggle(vis.showAggregatePath, "Show Aggregate Path");

                EditorGUILayout.PropertyField(propDisplaySampleRate);
                EditorGUILayout.PropertyField(propAggregateGridSize);
                EditorGUILayout.PropertyField(propAggregateEdgeScale, true);
                
                if(vis.pLogs.Count > 0)
                    GUILayout.Label("Filter Data by Player ID:");

                //Filter options.
                //Enable/disable players, set path colour by player ID.
                foreach (KeyValuePair<string, OGLogVisualizer.PlayerLog> pLog in vis.pLogs)
                {
                    GUILayout.BeginHorizontal();

                    oldFilter = pLog.Value.visInclude;
                    pLog.Value.visInclude = GUILayout.Toggle(pLog.Value.visInclude, pLog.Key);

                    if (oldFilter != pLog.Value.visInclude && vis.aggregateActiveOnly)
                        refreshFilter = true;

                    pLog.Value.pathColor = EditorGUILayout.ColorField(pLog.Value.pathColor);
                    GUILayout.EndHorizontal();
                }
                
                //Shortcut to enable all PIDs in the vis.
                if(GUILayout.Button("Select All"))
                {
                    foreach (KeyValuePair<string, OGLogVisualizer.PlayerLog> pLog in vis.pLogs)
                    {
                        pLog.Value.visInclude = true;
                    }

                    if(vis.aggregateActiveOnly)
                        refreshFilter = true;
                }

                //Shortcut to exclude all PIDs from the vis.
                if(GUILayout.Button("Select None"))
                {
                    foreach (KeyValuePair<string, OGLogVisualizer.PlayerLog> pLog in vis.pLogs)
                    {
                        pLog.Value.visInclude = false;
                    }

                    if(vis.aggregateActiveOnly)
                        refreshFilter = true;
                }             
            }

            //If we've detected a change that requires re-aggregation, do so.
            if (refreshFilter)
            {
                vis.ReclusterEvents();
                vis.ReaggregatePath();
            }

            //Collapsible pane for managing display of input events.
            inputFoldout = EditorGUILayout.Foldout(inputFoldout, lblInputFoldout);

            if(inputFoldout)
            {
                //Toggle showing input events at all.
                vis.showIndividualInputEvents = GUILayout.Toggle(vis.showIndividualInputEvents, "Show Individual Input Events");
                vis.showAggregateInputEvents = GUILayout.Toggle(vis.showAggregateInputEvents, "Show Aggregate Input Events");

                EditorGUILayout.PropertyField(propInputClusterRadius);
                EditorGUILayout.PropertyField(propInputLinearScale, true);

                //Filters.
                if (vis.enabledInputEvents.Count > 0)
                    GUILayout.Label("Filter by Key Code:");

                //Same paradigm as Player ID enabling/colour settings.
                foreach(KeyCode key in vis.allInputEvents)
                {
                    GUILayout.BeginHorizontal();

                    vis.enabledInputEvents[key] = 
                        GUILayout.Toggle(vis.enabledInputEvents[key], key.ToString());
                    vis.aggregateInputColors[key] =
                        EditorGUILayout.ColorField(vis.aggregateInputColors[key]);

                    GUILayout.EndHorizontal();
                }

                if(GUILayout.Button("Select All"))
                {
                    foreach (KeyCode key in vis.allInputEvents)
                    {
                        vis.enabledInputEvents[key] = true;
                    }
                }

                if(GUILayout.Button("Select None"))
                {
                    foreach (KeyCode key in vis.allInputEvents)
                    {
                        vis.enabledInputEvents[key] = false;
                    }
                }
            }

            //Collapsible pane for managing display of gameplay events.
            gameFoldout = EditorGUILayout.Foldout(gameFoldout, lblGameFoldout);

            if(gameFoldout)
            {
                //Toggle showing game events at all.
                vis.showIndividualGameEvents = GUILayout.Toggle(vis.showIndividualGameEvents, "Show Individual Game Events");
                vis.showAggregateGameEvents = GUILayout.Toggle(vis.showAggregateGameEvents, "Show Aggregate Game Events");
                EditorGUILayout.PropertyField(propGameClusterRadius);
                EditorGUILayout.PropertyField(propGameLinearScale, true);

                if (vis.enabledInputEvents.Count > 0)
                    GUILayout.Label("Filter by Event ID:");

                //Same paradigm as PID/input filters and colour settings.
                foreach(string eventID in vis.allGameEvents)
                {
                    GUILayout.BeginHorizontal();

                    vis.enabledGameEvents[eventID] =
                        GUILayout.Toggle(vis.enabledGameEvents[eventID], eventID);
                    vis.aggregateEventColors[eventID] =
                        EditorGUILayout.ColorField(vis.aggregateEventColors[eventID]);

                    GUILayout.EndHorizontal();
                }

                if (GUILayout.Button("Select All"))
                {
                    foreach (string eventID in vis.allGameEvents)
                    {
                        vis.enabledGameEvents[eventID] = true;
                    }
                }

                if (GUILayout.Button("Select None"))
                {
                    foreach (string eventID in vis.allGameEvents)
                    {
                        vis.enabledGameEvents[eventID] = false;
                    }
                }

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

        //Draw the aggregate path using the aggregate edges created by the vis.
        if (vis.showAggregatePath)
        {
            Handles.color = Color.white;

            foreach (OGLogVisualizer.PathAggregateEdge edge in vis.aggregatePath)
            {
                Handles.DrawAAPolyLine(polylinetex, (float)edge.displayWidth, edge.points);
            }
        }

        //Draw individual player paths.
        if (vis.showIndividualPaths)
        {
            foreach (KeyValuePair<string, OGLogVisualizer.PlayerLog> pLog in vis.pLogs)
            {
                if (pLog.Value.visInclude)
                {
                    //Draw path trace.
                    Vector3[] points = pLog.Value.pathPoints.ToArray();
                    Handles.color = pLog.Value.pathColor;
                    Handles.DrawAAPolyLine(polylinetex, OGLogVisualizer.MIN_PATH_WIDTH, points);                 
                }
            }
        } 

        //For input and game events, showing aggregate/individual events are mutually
        //exclusive operations - to avoid confusion/excessive occlusion with this 
        //form of representation.
        if(vis.showIndividualInputEvents)
        {
            foreach (KeyValuePair<string, OGLogVisualizer.PlayerLog> pLog in vis.pLogs)
            {
                if (pLog.Value.visInclude)
                {
                    //Draw input events for enabled player IDs.
                    foreach(OGLogVisualizer.PlayerLog.InputEvent input in pLog.Value.inputEvents)
                    {
                        if (vis.enabledInputEvents[input.key])
                        {
                            Handles.color = vis.aggregateInputColors[input.key];
                            Handles.DrawSolidDisc(input.pos, Vector3.up, OGLogVisualizer.MIN_EVENT_RADIUS);
                            Handles.Label(input.pos, input.key.ToString());
                        }
                    }
                }
            }
        }
        else if(vis.showAggregateInputEvents)
        {
            foreach(KeyValuePair<KeyCode, List<OGLogVisualizer.InputAggregateEvent>>
                aggregation in vis.aggregateInputEvents)
            {
                if(vis.enabledInputEvents[aggregation.Key])
                {
                    //Draw aggregates of enabled input event IDs.
                    foreach(OGLogVisualizer.InputAggregateEvent aggregate in 
                        aggregation.Value)
                    {
                        if (aggregate.displaySize == 0.0f)
                            continue;

                        Handles.color = vis.aggregateInputColors[aggregation.Key];
                        Handles.DrawSolidDisc(aggregate.pos, Vector3.up, aggregate.displaySize);
                        Handles.Label(aggregate.pos, aggregate.key.ToString());
                    }
                }
            }
        }

        if(vis.showIndividualGameEvents)
        {
            foreach (KeyValuePair<string, OGLogVisualizer.PlayerLog> pLog in vis.pLogs)
            {
                if (pLog.Value.visInclude)
                {
                    //Draw visible input events.
                    foreach (OGLogVisualizer.PlayerLog.GameEvent gameEvent in pLog.Value.gameEvents)
                    {
                        if (vis.enabledGameEvents[gameEvent.eventID])
                        {
                            Handles.color = vis.aggregateEventColors[gameEvent.eventID];
                            Handles.DrawSolidDisc(gameEvent.pos, Vector3.up, OGLogVisualizer.MIN_EVENT_RADIUS);
                            Handles.Label(gameEvent.pos, gameEvent.eventID);
                        }
                    }
                }
            }
        }
        else if(vis.showAggregateGameEvents)
        {
            foreach(KeyValuePair<string, List<OGLogVisualizer.GameAggregateEvent>>
                aggregation in vis.aggregateGameEvents)
            {
                if(vis.enabledGameEvents[aggregation.Key])
                {
                    //Draw aggregates of enabled gameplay event IDs.
                    foreach(OGLogVisualizer.GameAggregateEvent aggregate in 
                        aggregation.Value)
                    {
                        if (aggregate.displaySize == 0.0f)
                            continue;

                        Handles.color = vis.aggregateEventColors[aggregation.Key];
                        Handles.DrawSolidDisc(aggregate.pos, Vector3.up, aggregate.displaySize);
                        Handles.Label(aggregate.pos, aggregate.eventID);
                    }
                }
            }
        }
    }
}
