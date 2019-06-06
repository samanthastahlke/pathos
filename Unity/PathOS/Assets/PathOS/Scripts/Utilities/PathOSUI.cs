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

        public static SortedList<Heuristic, string> heuristicLabels =
            new SortedList<Heuristic, string>()
            {
                { Heuristic.ACHIEVEMENT,    "Achievement" },
                { Heuristic.ADRENALINE,     "Adrenaline" },
                { Heuristic.AGGRESSION,     "Aggression" },
                { Heuristic.CAUTION,        "Caution" },
                { Heuristic.COMPLETION,     "Completion" },
                { Heuristic.CURIOSITY,      "Curiosity" },
                { Heuristic.EFFICIENCY,     "Efficiency" }
            };

        public static Dictionary<string, Heuristic> heuristicLookup =
            new Dictionary<string, Heuristic>()
            {
                { "Achievement",    Heuristic.ACHIEVEMENT },
                { "Adrenaline",     Heuristic.ADRENALINE },
                { "Aggression",     Heuristic.AGGRESSION },
                { "Caution",        Heuristic.CAUTION    },
                { "Completion",     Heuristic.COMPLETION },
                { "Curiosity",      Heuristic.CURIOSITY  },
                { "Efficiency",     Heuristic.EFFICIENCY }
            };

        public static SortedList<EntityType, string> entityLabels =
            new SortedList<EntityType, string>()
            {
                { EntityType.ET_NONE,                   "Null Type" },
                { EntityType.ET_GOAL_OPTIONAL,          "Optional Goal" },
                { EntityType.ET_GOAL_MANDATORY,         "Mandatory Goal" },
                { EntityType.ET_GOAL_COMPLETION,        "Final Goal" },
                { EntityType.ET_RESOURCE_ACHIEVEMENT,   "Collectable" },
                { EntityType.ET_RESOURCE_PRESERVATION,  "Self-Preservation" },
                { EntityType.ET_HAZARD_ENEMY,           "Enemy" },
                { EntityType.ET_HAZARD_ENVIRONMENT,     "Environment Hazard" },
                { EntityType.ET_POI,                    "POI" },
                { EntityType.ET_POI_NPC,                "NPC" }
            };

        public static Dictionary<string, EntityType> entityLookup =
            new Dictionary<string, EntityType>()
            {
                { "Null Type",          EntityType.ET_NONE },
                { "Optional Goal",      EntityType.ET_GOAL_OPTIONAL },
                { "Mandatory Goal",     EntityType.ET_GOAL_MANDATORY },
                { "Final Goal",         EntityType.ET_GOAL_COMPLETION },
                { "Collectable",        EntityType.ET_RESOURCE_ACHIEVEMENT },
                { "Self-Preservation",  EntityType.ET_RESOURCE_PRESERVATION },
                { "Enemy",              EntityType.ET_HAZARD_ENEMY },
                { "Environment Hazard", EntityType.ET_HAZARD_ENVIRONMENT },
                { "POI",                EntityType.ET_POI },
                { "NPC",                EntityType.ET_POI_NPC }
            };
    }
}