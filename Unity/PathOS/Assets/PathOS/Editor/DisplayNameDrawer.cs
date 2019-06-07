using UnityEngine;
using UnityEditor;

[CustomPropertyDrawer(typeof(DisplayNameAttribute))]
public class DisplayNameDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        label.text = (attribute as DisplayNameAttribute).displayName;
        EditorGUI.PropertyField(position, property, label);
    }
}
