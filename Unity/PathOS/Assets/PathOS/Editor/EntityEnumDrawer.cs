using UnityEngine;
using UnityEditor;
using PathOS;

[CustomPropertyDrawer(typeof(EntityDisplayAttribute))]
public class EntityEnumDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        property.enumValueIndex = EditorGUI.Popup(position, label.text,
            property.enumValueIndex, UI.entityPopupList);
    }
}