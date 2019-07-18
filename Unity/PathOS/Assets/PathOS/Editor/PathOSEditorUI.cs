using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace PathOS
{
    public class EditorUI
    {
        public static void FullMinMaxSlider(string label, ref float min, ref float max, 
            float absMin = 0.0f, float absMax = 1.0f)
        {
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.MinMaxSlider(label,
                ref min, ref max, absMin, absMax);

            min = Mathf.Clamp(EditorGUILayout.FloatField(
                PathOS.UI.RoundFloatfield(min),
                GUILayout.Width(PathOS.UI.shortFloatfieldWidth)), 
                absMin, absMax);

            EditorGUILayout.LabelField("<->",
                GUILayout.Width(PathOS.UI.shortLabelWidth));

            max = Mathf.Clamp(EditorGUILayout.FloatField(
                PathOS.UI.RoundFloatfield(max),
                GUILayout.Width(PathOS.UI.shortFloatfieldWidth)),
                absMin, absMax);

            EditorGUILayout.EndHorizontal();
        }
    }
}