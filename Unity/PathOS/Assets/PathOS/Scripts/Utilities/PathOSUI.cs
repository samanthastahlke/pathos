using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
PathOSUI.cs 
PathOSUI (c) Nine Penguins (Samantha Stahlke) 2019
*/

namespace PathOS
{
    public class UI
    {
        /* Style Constraints */
        public static float shortLabelWidth = 24.0f;
        public static float shortFloatfieldWidth = 40.0f;

        public static Color mapUnknown = Color.black;
        public static Color mapSeen = Color.blue;
        public static Color mapVisited = Color.green;
        public static Color mapObstacle = Color.red;

        //Cut off the initial part of a string.
        //(Used for displaying filepaths).
        public static void TruncateStringHead(string longText,
            ref string shortText, int maxLen)
        {
            shortText = longText.Substring(
                        Mathf.Max(0, longText.Length - maxLen));

            if (longText.Length > maxLen)
                shortText = "..." + shortText;
        }

        //Used to truncate floating-point values in input fields.
        public static float RoundFloatfield(float val)
        {
            return Mathf.Round(val * 1000.0f) / 1000.0f;
        }
    }
}