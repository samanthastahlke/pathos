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
    }
}