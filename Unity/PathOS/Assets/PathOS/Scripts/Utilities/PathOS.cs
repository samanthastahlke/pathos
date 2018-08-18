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
    //This list is in flux based on the tagging system/typology review.
    //Right now the proof-of-concept just uses GOAL_OPTIONAL, HAZARD_ENEMY,
    //and POI.
    public enum EntityType
    {
        ET_NONE = 0,
        ET_GOAL_OPTIONAL = 100,
        ET_GOAL_MANDATORY = 110,
        ET_GOAL_COMPLETION = 120,
        ET_RESOURCE_ACHIEVEMENT = 150,
        ET_RESOURCE_PRESERVATION = 160,
        ET_HAZARD_ENEMY = 200,
        ET_HAZARD_ENVIRONMENT = 250,
        ET_POI = 300
    };

    //Representation of entity objects defined in the PathOS manager.
    [System.Serializable]
    public class LevelEntity
    {
        public GameObject entityRef;
        public EntityType entityType;
        public Renderer rend;

        //Not used yet. Will be used to simulate compass/map availability.
        public bool omniscientDirection;
        public bool omniscientPosition;
    }

    /* PLAYER PERCEPTION */
    //How an entity is represented in the agent's world model.
    public class PerceivedEntity
    {
        public GameObject entityRef;
        //Used for identification/comparison.
        protected int instanceID;

        public EntityType entityType;
        public Vector3 pos;

        public PerceivedEntity(GameObject entityRef, EntityType entityType,
            Vector3 pos)
        {
            this.entityRef = entityRef;
            this.instanceID = entityRef.GetInstanceID();
            this.entityType = entityType;
            this.pos = pos;
        }

        //Equality operators are overriden to make array search/comparison easier.
        public static bool operator==(PerceivedEntity lhs, PerceivedEntity rhs)
        {
            if(object.ReferenceEquals(lhs, null))
                return object.ReferenceEquals(rhs, null);

            if (object.ReferenceEquals(rhs, null))
                return object.ReferenceEquals(lhs, null);

            return lhs.instanceID == rhs.instanceID;
        }

        public static bool operator!=(PerceivedEntity lhs, PerceivedEntity rhs)
        {
            if (object.ReferenceEquals(lhs, null))
                return !object.ReferenceEquals(rhs, null);

            if (object.ReferenceEquals(rhs, null))
                return object.ReferenceEquals(lhs, null);

            return lhs.instanceID != rhs.instanceID;
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;

            PerceivedEntity objAsEntity = obj as PerceivedEntity;

            if (objAsEntity == null)
                return false;

            return this == objAsEntity;
        }

        public override int GetHashCode()
        {
            return instanceID;
        }

        public int GetInstanceID()
        {
            return instanceID;
        }
    }

    //How the memory of an object is represented in the agent's world model.
    public class EntityMemory : PerceivedEntity
    {
        public bool visited = false;
        public float impressionTime = 0.0f;

        public EntityMemory(GameObject entityRef, EntityType entityType,
            Vector3 pos) : base(entityRef, entityType, pos) { }

        public EntityMemory(PerceivedEntity data) : 
            base(data.entityRef, data.entityType, data.pos) { }
    }

    public class PerceivedInfo
    {
        //What in-game objects are visible?
        public List<PerceivedEntity> entities;

        //Set of vectors representing directions the environment
        //will allow us to travel.
        //Not used yet.
        public List<Vector3> navDirections;

        public PerceivedInfo()
        {
            entities = new List<PerceivedEntity>();
            navDirections = new List<Vector3>();
        }
    }
}
