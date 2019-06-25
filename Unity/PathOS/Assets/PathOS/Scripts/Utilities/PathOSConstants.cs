/*
PathOSConstants.cs
PathOSConstants (c) Nine Penguins (Samantha Stahlke) 2018
*/

namespace PathOS
{
    namespace Constants
    {
        struct Perception
        {
            //How often does the agent's "visual system" update?
            public const float PERCEPTION_COMPUTE_TIME = 0.1f;
        }

        struct Memory
        {
            public const float IMPRESSION_TIME_MIN = 0.1f;
            public const float IMPRESSION_TIME_CONVERT_LTM = 5.0f;
            public const int IMPRESSIONS_MAX = 16;
            public const int IMPRESSIONS_CONVERT_LTM = 4;

            public const float FORGET_TIME_MIN = 15.0f;
            public const float FORGET_TIME_MAX = 30.0f;

            //Cowan (2000)
            //Limitations of short-term memory for basic recall tasks.
            public const int MEM_CAPACITY_MIN = 3;
            public const int MEM_CAPACITY_MAX = 5;

            //The radius with which to vary the remembered position of
            //entities not in view.
            public const float POS_VARIANCE = 3.0f;

            //The time taken to "retrieve" an object from memory.
            //Used to help scale goal recompute time.
            public const float RETRIEVAL_TIME = 0.03f;
        }

        struct Navigation
        {
            //What should the base minimum time for recomputing a destination be?
            public const float ROUTE_COMPUTE_BASE = 0.5f;

            //How close do two goals need to be to be considered the same?
            public const float GOAL_EPSILON_SQR = 0.1f;

            //How close do we need to be to a waypoint to have crossed it?
            public const float WAYPOINT_EPSILON_SQR = 1.0f;

            //How close do two "path" memories need to be to be considered
            //equivalent?
            public const float EXPLORE_PATH_POS_THRESHOLD = 5.0f;
            public const float EXPLORE_PATH_DEG_THRESHOLD = 15.0f;

            //How proportionally large should the hazard movement penalty
            //be when passing next to a hazard while constructing a path from
            //memory?
            public const float HAZARD_PENALTY_MAX = 8.0f;
        }

        struct Behaviour
        {
            public const float LOOK_TIME_MAX = 30.0f;
            public const float LOOK_TIME_MIN_EXPLORE = 10.0f;
            public const float LOOK_TIME_MIN_CAUTION = 5.0f;

            public const float ENEMY_RADIUS = 8.0f;
            public const float ENEMY_RADIUS_SQR = ENEMY_RADIUS * ENEMY_RADIUS;
            public const int ENEMY_COUNT_THRESHOLD = 4;

            //How many tiles must a "memory path" to a goal cover to be
            //used instead of regular NavMeshAgent navigation?
            public const int MIN_A_STAR_MEMORY_LENGTH = 3;

            //How often will the agent try to navigate somewhere based on 
            //memory rather than navmesh logic (optimal/exploring)?
            public const float BASE_MEMORY_NAV_CHANCE = 0.5f;
            public const float MEMORY_NAV_CHANCE_MIN = 0.0f;
            public const float MEMORY_NAV_CHANCE_MAX = 1.0f;

            //Bias for preferring the existing goal.
            public const float EXISTING_GOAL_BIAS = 0.5f;

            //Adjustment factor for calculating the role of distance in 
            //entity scoring.
            public const float DIST_SCORE_FACTOR = 2.0f;
            public const float DIST_SCORE_FACTOR_SQR = DIST_SCORE_FACTOR * DIST_SCORE_FACTOR;

            //Govern the "ending" behaviour of the agent.
            public const float FINAL_GOAL_BONUS_FACTOR = 5.0f;
            //Penalty used to reduce score of the end goal if optional goals remain.
            public const float FINAL_GOAL_ACHIEVEMENT_PENALTY_FACTOR = 1.0f;
            //Penalty used to reduce score of the end goal for unexplored entities.
            public const float FINAL_GOAL_EXPLORATION_PENALTY_FACTOR = 1.0f;

            public const float SCORE_MAX = 10000.0f;
            public const float SCORE_UNCERTAINTY_THRESHOLD = 0.1f;
            public const float SCORE_UNCERTAINTY_HALF = 0.5f * SCORE_UNCERTAINTY_THRESHOLD;
        }
    }
}