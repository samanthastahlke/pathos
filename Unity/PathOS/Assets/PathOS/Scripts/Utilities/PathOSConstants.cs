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
            public const int IMPRESSIONS_CONVERT_UNFORGETTABLE = 8;

            public const float FORGET_TIME_MIN = 30.0f;
            public const float FORGET_TIME_MAX = 60.0f;

            //Cowan (2000)
            //Limitations of short-term memory for basic recall tasks.
            public const int MEM_CAPACITY_MIN = 3;
            public const int MEM_CAPACITY_MAX = 5;

            //The radius with which to vary the remembered position of
            //entities not in view.
            public const float POS_VARIANCE = 2.0f;

            //The time taken to "retrieve" an object from memory.
            //Used to help scale goal recompute time.
            public const float RETRIEVAL_TIME = 0.03f;
        }

        struct Navigation
        {
            //What should the base minimum time for recomputing a destination be?
            public const float ROUTE_COMPUTE_BASE = 0.5f;

            //How close do two goals need to be to be considered the same?
            public const float GOAL_EPSILON_SQR = 0.01f;

            //Multiplier of the agent's height used for sample radius when
            //snapping target positions from entities onto the navmesh.
            //(Unity recommends 2 as a starting point).
            public const float NAV_SEARCH_RADIUS_FAC = 3.0f;

            //How close do we need to be to a waypoint to have crossed it?
            public const float WAYPOINT_EPSILON_SQR = 1.0f;
            //How far apart do we set memory path waypoints?
            public const float WAYPOINT_DIST_MIN = 8.0f;
            public const float WAYPOINT_DIST_MIN_SQR = 
                WAYPOINT_DIST_MIN * WAYPOINT_DIST_MIN;

            //How close do two "path" memories need to be to be considered
            //equivalent?
            //(Applied as multipliers to agent exploration thresholds).
            public const float EXPLORE_PATH_POS_THRESHOLD_FAC = 3.0f;
            public const float EXPLORE_PATH_DEG_THRESHOLD_FAC = 3.0f;

            //For checking when exploration targets are unreachable.
            //(R - the threshold for whether a point should be used as a reference
            //to an unreachable area.)
            public const float UNREACHABLE_POS_SIMILARITY_RAD = 2.0f;

            //For optimized checking, use R^2.
            public const float UNREACHABLE_POS_SIMILARITY_SQR =
                UNREACHABLE_POS_SIMILARITY_RAD *
                UNREACHABLE_POS_SIMILARITY_RAD;

            //To guarantee a point is not covered by an unreachable reference - 
            //(R * 2)^2 = R^2 * 4.
            public const float UNREACHABLE_POS_CHECK_SQR = 
                4 * UNREACHABLE_POS_SIMILARITY_SQR;

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

            //How often will the agent try to navigate somewhere based on 
            //memory rather than navmesh logic (optimal/exploring)?
            public const float BASE_MEMORY_NAV_CHANCE = 0.5f;
            public const float MEMORY_NAV_CHANCE_MIN = 0.0f;
            public const float MEMORY_NAV_CHANCE_MAX = 1.0f;

            //How many times in a row can the agent change its
            //mind without reaching a destination before it will lock on?
            public const float GOAL_INDECISION_COUNT_THRESHOLD = 5;
            public const float GOAL_INDECISION_CHANCE = 1.0f / GOAL_INDECISION_COUNT_THRESHOLD;

            //Bias for preferring the existing goal.
            public const float EXISTING_GOAL_BIAS = 0.5f;

            //Bias for preferring entity targets.
            public const float INTERACTIVITY_BIAS = 0.5f;

            //Adjustment factor for calculating the role of distance in 
            //entity scoring.
            //Factor is calculated as the squared factor over the squared distance.
            //As a reference, the score factor is the distance in units
            //that will result in an adjustment factor of 1.0.
            public const float DIST_SCORE_FACTOR = 2.0f;
            public const float DIST_SCORE_FACTOR_SQR = DIST_SCORE_FACTOR * DIST_SCORE_FACTOR;

            //Govern the "ending" behaviour of the agent.
            public const float FINAL_GOAL_BONUS_MIN = 1.0f;
            public const float FINAL_GOAL_BONUS_MAX = 5.0f;

            public const float SCORE_MAX = 10000.0f;
            public const float SCORE_UNCERTAINTY_THRESHOLD = 0.2f;
            public const float SCORE_UNCERTAINTY_HALF = 0.5f * SCORE_UNCERTAINTY_THRESHOLD;
        }
    }
}