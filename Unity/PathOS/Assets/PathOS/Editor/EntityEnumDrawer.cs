using UnityEngine;
using UnityEditor;
using PathOS;

/*
EntityEnumDrawer.cs
EntityEnumDrawer (c) Nine Penguins (Samantha Stahlke) 2019

Used to display user-friendly enumerator names for level entity markup.
*/

[CustomPropertyDrawer(typeof(EntityDisplayAttribute))]
public class EntityEnumDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        property.enumValueIndex = EditorGUI.Popup(position, label.text,
            property.enumValueIndex, UI.entityPopupList);
    }
}