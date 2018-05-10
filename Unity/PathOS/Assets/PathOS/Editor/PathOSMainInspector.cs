using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/*
PathOSMainInspector.cs 
PathOSMainInspector (c) Nine Penguins (Samantha Stahlke) 2018
*/

[CustomEditor(typeof(PathOSManager))]
public class PathOSMainInspector : Editor
{
    private PathOSManager manager;
    private SerializedObject serial;
    private SerializedProperty entityList;
    private SerializedProperty playerProxy;

    private void OnEnable()
    {
        manager = (PathOSManager)target;
        serial = new SerializedObject(manager);
        entityList = serial.FindProperty("levelEntities");
        playerProxy = serial.FindProperty("playerProxy");

    }

    public override void OnInspectorGUI()
    {
        serial.Update();

        //Player proxy object.
        EditorGUILayout.PropertyField(playerProxy);

        //Level entity list.
        if(EditorGUILayout.PropertyField(entityList))
        {
            for(int i = 0; i < manager.levelEntities.Count; ++i)
            {
                GUILayout.BeginHorizontal();

                PathOSManager.LevelEntity entity = manager.levelEntities[i];

                entity.entityRef = (GameObject)EditorGUILayout.ObjectField(entity.entityRef, typeof(GameObject), true);
                entity.entityType = (PathOSManager.EntityType)EditorGUILayout.EnumPopup(entity.entityType);

                //Append/delete entities in place.
                if (GUILayout.Button("+", NPGUI.microBtn))
                    manager.AddEntity(i + 1);
                if (GUILayout.Button("-", NPGUI.microBtn))
                    manager.RemoveEntity(i);

                GUILayout.EndHorizontal();
            }

            //Append a new level entity to the end of the list.
            if (GUILayout.Button("Add Entity"))
                manager.AddEntity(manager.levelEntities.Count);

            //Clear the entities list. Safeguard to prevent accidental deletion.
            if(GUILayout.Button("Clear Entities"))
            {
                if (EditorUtility.DisplayDialog("This will delete all PathOS level entities!",
                    "Are you sure you want to do this?",
                    "Oh yeah, do it", "No, wait!"))
                    manager.ClearEntities();
            }
        }

        serial.ApplyModifiedProperties();
    }
}
