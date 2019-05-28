using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Malee.Editor;

/*
OGLogManagerEditor.cs
OGLogManagerEditor (c) Nine Penguins (Samantha Stahlke) 2019
*/

[CustomEditor(typeof(OGLogManager))]
public class OGLogManagerEditor : Editor
{
    private OGLogManager manager;
    private SerializedObject serial;

    private const int pathDisplayLength = 32;
    private GUIStyle errorStyle = new GUIStyle();

    private string logDirectoryDisplay;

    private SerializedProperty enableLogging;
    private SerializedProperty logFilePrefix;
    private SerializedProperty sampleRate;

    private void OnEnable()
    {
        manager = (OGLogManager)target;
        serial = new SerializedObject(manager);

        enableLogging = serial.FindProperty("enableLogging");
        logFilePrefix = serial.FindProperty("logFilePrefix");
        sampleRate = serial.FindProperty("sampleRate");

        PathOS.UI.TruncateStringHead(manager.logDirectory,
            ref logDirectoryDisplay, pathDisplayLength);

        errorStyle.normal.textColor = Color.red;
    }

    public override void OnInspectorGUI()
    {
        serial.Update();

        EditorGUILayout.PropertyField(enableLogging);
        EditorGUILayout.LabelField("Log Directory: ", logDirectoryDisplay);

        if(GUILayout.Button("Browse..."))
        {
            string defaultDirectory = (manager.LogDirectoryValid()) ?
                manager.logDirectory : Application.dataPath;

            string selectedPath = EditorUtility.OpenFolderPanel("Select Folder...",
                defaultDirectory, "");

            if (selectedPath != "")
                manager.logDirectory = selectedPath;

            PathOS.UI.TruncateStringHead(manager.logDirectory,
                ref logDirectoryDisplay, pathDisplayLength);
        }

        if(!manager.LogDirectoryValid())
        {
            EditorGUILayout.LabelField("Error! You must choose a " +
                "valid folder on this computer.", errorStyle);
        }

        EditorGUILayout.PropertyField(logFilePrefix);
        EditorGUILayout.PropertyField(sampleRate);

        serial.ApplyModifiedProperties();
    }
}
