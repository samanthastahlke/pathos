/*
PathOSConstants.cs
PathOSConstants (c) Nine Penguins (Samantha Stahlke) 2018
*/

namespace PathOS
{
    namespace Constants
    {
        struct Memory
        {
            public const float IMPRESSION_TIME_MIN = 0.5f;
            public const float IMPRESSION_CONVERT_LTM = 5.0f;

            public const float FORGET_TIME_MIN = 15.0f;
            public const float FORGET_TIME_MAX = 30.0f;

            //Cowan (2000)
            //Limitations of short-term memory for basic recall tasks.
            public const int MEM_CAPACITY_MIN = 3;
            public const int MEM_CAPACITY_MAX = 5;
        }

        struct Behaviour
        {
            public const float LOOK_TIME_MAX = 20.0f;
            public const float LOOK_TIME_MIN_EXPLORE = 6.0f;
            public const float LOOK_TIME_MIN_CAUTION = 2.0f;

            public const float ENEMY_RADIUS = 8.0f;
            public const float ENEMY_RADIUS_SQR = ENEMY_RADIUS * ENEMY_RADIUS;
            public const int ENEMY_COUNT_THRESHOLD = 4;

            public const float HEURISTIC_EPSILON = 0.1f;

            //How many tiles must a "memory path" to a goal cover to be
            //used instead of regular NavMeshAgent navigation?
            public const int MIN_A_STAR_MEMORY_LENGTH = 3;

            //How often will the agent try to navigate somewhere based on 
            //memory rather than navmesh logic (optimal/exploring)?
            public const float BASE_MEMORY_NAV_CHANCE = 0.5f;
            public const float MEMORY_NAV_CHANCE_MIN = 0.0f;
            public const float MEMORY_NAV_CHANCE_MAX = 1.0f;

            //How close do two goals need to be to be considered the same?
            public const float GOAL_EPSILON_SQR = 0.1f;

            //How close do we need to be to a waypoint to have crossed it?
            public const float WAYPOINT_EPSILON_SQR = 1.0f;
        }
    }
}