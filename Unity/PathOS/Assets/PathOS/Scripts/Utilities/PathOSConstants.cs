/*
PathOSConstants.cs
PathOS (c) Nine Penguins (Samantha Stahlke) 2018
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
    }
}