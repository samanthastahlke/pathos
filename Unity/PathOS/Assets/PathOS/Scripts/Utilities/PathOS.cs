using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
PathOS.cs 
PathOS (c) Nine Penguins (Samantha Stahlke) 2018
*/

namespace PathOS
{
    /* GAME ENTITIES */
    public enum EntityType
    {
        ET_NONE = 0,
        ET_GOAL = 100,
        ET_ENEMY = 200,
        ET_POI = 300
    };

    [System.Serializable]
    public class LevelEntity
    {
        public GameObject entityRef;
        public EntityType entityType;
        public bool omniscientDirection;
        public bool omniscientPosition;
        public Renderer rend;
    }

    public class PerceivedEntity
    {
        public GameObject entityRef;
        private int instanceID;
        public EntityType entityType;
        public Vector3 pos;
        public bool visited = false;

        public PerceivedEntity(GameObject entityRef, EntityType entityType,
            Vector3 pos)
        {
            this.entityRef = entityRef;
            this.instanceID = entityRef.GetInstanceID();
            this.entityType = entityType;
            this.pos = pos;
        }

        public static bool operator==(PerceivedEntity lhs, PerceivedEntity rhs)
        {
            return lhs.instanceID == rhs.instanceID;
        }

        public static bool operator!=(PerceivedEntity lhs, PerceivedEntity rhs)
        {
            return lhs.instanceID != rhs.instanceID;
        }
    }

    public class TargetAnalysis
    {
        public float explorationScore;
        public float goalScore;
        public float dangerScore;
    }

    /* PLAYER PERCEPTION */
    public class PerceivedInfo
    {
        public List<PerceivedEntity> entities;
        public List<Vector3> navDirections;

        public PerceivedInfo()
        {
            entities = new List<PerceivedEntity>();
            navDirections = new List<Vector3>();
        }
    }
}
