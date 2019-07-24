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
        ET_POI = 300,
        ET_POI_NPC = 350
    };

    /* AGENT HEURISTICS */
    //Like the list of entities, this list is subject to change based on
    //the typology review (ongoing).
    public enum Heuristic
    {
        CURIOSITY = 0,
        ACHIEVEMENT = 10,
        COMPLETION = 15,
        AGGRESSION = 20,
        ADRENALINE = 25,
        CAUTION = 30,
        EFFICIENCY = 35
    };

    [System.Serializable]
    public struct FloatRange
    {
        public float min;
        public float max;
    }

    [System.Serializable]
    public class EntityWeight
    {
        public EntityType entype;
        public float weight;

        public EntityWeight(EntityType m_entype, float m_weight = 0.0f)
        {
            entype = m_entype;
            weight = m_weight;
        }
    }

    [System.Serializable]
    public class HeuristicWeightSet
    {
        public Heuristic heuristic;
        public List<EntityWeight> weights;

        public HeuristicWeightSet()
        {
            heuristic = Heuristic.ACHIEVEMENT;
            weights = new List<EntityWeight>();
        }

        public HeuristicWeightSet(Heuristic m_heuristic)
        {
            heuristic = m_heuristic;
            weights = new List<EntityWeight>();
        }
    }

    [System.Serializable]
    public class HeuristicScale
    {
        public Heuristic heuristic;
        public float scale;

        public HeuristicScale()
        {
            heuristic = Heuristic.ACHIEVEMENT;
            scale = 0.0f;
        }

        public HeuristicScale(Heuristic m_heuristic, float m_scale)
        {
            heuristic = m_heuristic;
            scale = m_scale;
        }
    }

    [System.Serializable]
    public class HeuristicRange
    {
        public Heuristic heuristic;
        public FloatRange range;

        public HeuristicRange()
        {
            heuristic = Heuristic.ACHIEVEMENT;
            range = new FloatRange
            {
                min = 0.0f,
                max = 1.0f
            };
        }

        public HeuristicRange(Heuristic m_heuristic, 
            float m_min = 0.0f, float m_max = 1.0f)
        {
            heuristic = m_heuristic;
            range = new FloatRange
            {
                min = m_min,
                max = m_max
            };
        }
    }

    [System.Serializable]
    public class AgentProfile
    {
        public string name;
        
        [SerializeField]
        public List<HeuristicRange> heuristicRanges;
        public FloatRange expRange;

        public AgentProfile()
        {
            name = "Unnamed Profile";
            heuristicRanges = new List<HeuristicRange>();

            expRange = new FloatRange { min = 0.0f, max = 1.0f };

            foreach(Heuristic heuristic in System.Enum.GetValues(typeof(Heuristic)))
            {
                heuristicRanges.Add(new HeuristicRange(heuristic));
            }
        }

        public AgentProfile(AgentProfile other)
        {
            heuristicRanges = new List<HeuristicRange>();
            Copy(other);
        }

        public void Copy(AgentProfile other)
        {
            name = other.name;
            heuristicRanges.Clear();

            foreach (HeuristicRange hr in other.heuristicRanges)
            {
                heuristicRanges.Add(new HeuristicRange(hr.heuristic,
                    hr.range.min, hr.range.max));
            }

            expRange = new FloatRange
            {
                min = other.expRange.min,
                max = other.expRange.max
            };
        }

        public void Clear()
        {
            name = "Unnamed Profile";

            expRange.min = 0.0f;
            expRange.max = 1.0f;

            foreach (HeuristicRange hr in heuristicRanges)
            {
                hr.range.min = 0.0f;
                hr.range.max = 1.0f;
            }
        }
    }

    //Representation of entity objects defined in the PathOS manager.
    [System.Serializable]
    public class LevelEntity
    {
        public string name;

        public GameObject objectRef;

        [EntityDisplay]
        public EntityType entityType;

        //Simulates compass/map availability.
        public bool alwaysKnown;

        //Individual per-object visitation radius.
        [Tooltip("Should this object have a custom threshold " +
            "for whether it has been visited?")]
        public bool overrideVisitRadius;
        [Tooltip("How close does the agent need to get to this object" +
            "to mark it as visited? (If override is enabled)")]
        public float objectVisitRadius = 1.0f;
        public float visitRadiusSqr { get; private set; }

        //For visibility checking.
        private List<Renderer> rend;
        public Bounds bounds { get; private set; }
        private Vector3[] boxVerts;
        private bool initBounds = false;

        public LevelEntity(GameObject objectRef, EntityType entityType)
        {
            this.objectRef = objectRef;
            name = objectRef.name;

            this.entityType = entityType;
        }

        public void Init()
        {
            visitRadiusSqr = objectVisitRadius * objectVisitRadius;
            FetchRenderers();
        }

        private void FetchRenderers()
        {
            rend = new List<Renderer>();
            rend.AddRange(objectRef.GetComponentsInChildren<Renderer>());

            bounds = new Bounds();
            UpdateBounds();
        }

        public void UpdateBounds()
        {
            if (initBounds && objectRef.isStatic)
                return;

            Bounds tempBounds = new Bounds();

            if (rend.Count > 0)
            {
                tempBounds = rend[0].bounds;

                for (int i = 1; i < rend.Count; ++i)
                {
                    tempBounds.Encapsulate(rend[i].bounds);
                }
            }
            else
            {
                tempBounds.SetMinMax(objectRef.transform.position,
                    objectRef.transform.position);
            }

            bounds = tempBounds;

            boxVerts = new Vector3[8];

            boxVerts[0] = new Vector3(bounds.min.x, bounds.min.y, bounds.min.z);
            boxVerts[1] = new Vector3(bounds.min.x, bounds.min.y, bounds.max.z);
            boxVerts[2] = new Vector3(bounds.min.x, bounds.max.y, bounds.min.z);
            boxVerts[3] = new Vector3(bounds.min.x, bounds.max.y, bounds.max.z);

            boxVerts[4] = new Vector3(bounds.max.x, bounds.min.y, bounds.min.z);
            boxVerts[5] = new Vector3(bounds.max.x, bounds.min.y, bounds.max.z);
            boxVerts[6] = new Vector3(bounds.max.x, bounds.max.y, bounds.min.z);
            boxVerts[7] = new Vector3(bounds.max.x, bounds.max.y, bounds.max.z);

            initBounds = true;
        }

        public bool SizeVisibilityCheck(Camera cam, float normThres)
        {
            //For objects with no calculated bounds, we don't have
            //an accurate impression of the renderers.
            //Return true to avoid false "blindness".
            if (bounds.size.sqrMagnitude < 0.01f)
                return true;

            Vector3 vProj = Vector3.zero;

            float minX, minY, maxX, maxY;

            minX = minY = float.MaxValue;
            maxX = maxY = float.MinValue;

            //Coordinates are normalized in the camera's viewport.
            //By Unity's specs, lower left is (0.0f, 0.0f) and upper
            //right is (1.0f, 1.0f).
            for(int i = 0; i < boxVerts.Length; ++i)
            {
                vProj = cam.WorldToViewportPoint(boxVerts[i]);

                if (vProj.x < minX)
                    minX = vProj.x;
                if (vProj.x > minX)
                    maxX = vProj.x;
                if (vProj.y < minY)
                    minY = vProj.y;
                if (vProj.y > maxY)
                    maxY = vProj.y;
            }

            float hFac = 1.0f / cam.aspect;

            if ((maxY - minY) * hFac >= normThres
                || (maxX - minX) >= normThres)
                return true;

            return false;
        }
    }

    public class ScoringUtility
    {
        //Picks a target from the list based on a given stochasticity threshold.
        public static TargetDest PickTarget(List<TargetDest> destList, float maxScore)
        {
            float totalScore = 0.0f;

            TargetDest best = destList[0];

            for(int i = destList.Count - 1; i >= 0; --i)
            {
                //Clean out everything below our threshold.
                if (destList[i].score < maxScore - PathOS.Constants.Behaviour.SCORE_UNCERTAINTY_THRESHOLD)
                {
                    destList.RemoveAt(i);
                    continue;
                }
                
                //Keep a "best" target in case of rounding/floating-point errors
                //during RNG.
                if (destList[i].score > best.score)
                    best = destList[i];

                //Normalized score on [0, 1] 
                //converted from [maxScore - threshold, maxScore].
                destList[i].normScore = 1.0f - (maxScore - destList[i].score) /
                    PathOS.Constants.Behaviour.SCORE_UNCERTAINTY_THRESHOLD;

                totalScore += destList[i].normScore;
            }

            //Roll within the total normalized score range.
            float roll = Random.Range(0.0f, totalScore);
            float accumulated = 0.0f;

            for(int i = 0; i < destList.Count; ++i)
            {
                accumulated += destList[i].normScore;

                if (roll < accumulated)
                    return destList[i];
            }

            return best;
        }
    }

    /* PLAYER PERCEPTION */
    //How an entity is represented in the agent's world model.
    public class PerceivedEntity
    {
        public LevelEntity entityRef;
  
        //Used for identification/comparison.
        protected int instanceID;

        public EntityType entityType;
        public Vector3 perceivedPos;

        public bool visible = false;
        public float visibilityTimer = 0.0f;

        public int impressionCount = 0;
        public bool impressionMade = false;

        public PerceivedEntity(LevelEntity entityRef)
        {
            this.entityRef = entityRef;
            this.instanceID = entityRef.objectRef.GetInstanceID();
            this.entityType = entityRef.entityType;
            this.perceivedPos = entityRef.objectRef.transform.position;
        }

        public Vector3 ActualPosition()
        {
            return entityRef.objectRef.transform.position;
        }

        public static bool SameEntity(PerceivedEntity lhs, EntityMemory rhs)
        {
            if (object.ReferenceEquals(lhs, null))
                return object.ReferenceEquals(rhs, null);

            if (object.ReferenceEquals(rhs, null))
                return object.ReferenceEquals(lhs, null);

            return lhs.instanceID == rhs.entity.instanceID;
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
                return !object.ReferenceEquals(lhs, null);

            return lhs.instanceID != rhs.instanceID;
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;

            PerceivedEntity objAsEntity = obj as PerceivedEntity;

            if (objAsEntity == default(PerceivedEntity))
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

    public class TargetDest
    {
        public PerceivedEntity entity = null;
        public Vector3 pos = Vector3.zero;
        public bool accurate = true;
        public float score = 0.0f;
        public float normScore = 0.0f;

        public TargetDest() { }

        public TargetDest(TargetDest data)
        {
            entity = data.entity;
            pos = data.pos;
            accurate = data.accurate;
        }   
    }

    //How the memory of an object is represented in the agent's world model.
    //Entity memory = POI which represents an in-game object.
    public class EntityMemory
    {
        public PerceivedEntity entity;
        public bool visited = false;
        public bool unreachable = false;

        //Whether the memory should be considered long-term.
        public bool ltm = false;

        //Whether the memory can be forgotten.
        //(Only false for objects that are "always known" to the player.
        public bool forgettable = true;

        //How old the current impression of the object is.
        //i.e., how long since it was visible.
        public float impressionTime = 0.0f;

        public EntityMemory(LevelEntity entity)
        {
            this.entity = new PerceivedEntity(entity);
        }

        public EntityMemory(PerceivedEntity entity)
        {
            this.entity = entity;
        }

        public void MakeUnforgettable()
        {
            this.ltm = true;
            this.forgettable = false;
        }

        public void Visit()
        {
            if(!visited)
                MakeUnforgettable();

            visited = true;
        }

        public void Visit(GameObject caller, OGLogManager logger)
        {
            if(!visited)
            {
                MakeUnforgettable();

                if(logger != null)
                    logger.FireInteractionEvent(caller, entity.entityRef.objectRef);
            }

            visited = true;
        }

        public void MakeUnreachable()
        {
            if(!unreachable)
                MakeUnforgettable();

            unreachable = true;
        }

        public Vector3 RecallPos()
        {
            Vector3 pos = entity.ActualPosition();

            //Add noise to position recall if the object is not in sight 
            //and not always known.
            if(!entity.visible && !entity.entityRef.alwaysKnown)
            {
                float rGen = Mathf.Sqrt(Random.Range(0.0f, 1.0f)) 
                    * PathOS.Constants.Memory.POS_VARIANCE;

                float thetaGen = Random.Range(0.0f, 1.0f) * 2.0f * Mathf.PI;

                pos.x += rGen * Mathf.Cos(thetaGen);
                pos.z += rGen * Mathf.Sin(thetaGen);
            }

            return pos;
        }

        public Vector3 XZActualPos()
        {
            Vector3 actualPos = entity.ActualPosition();
            actualPos.y = 0.0f;

            return actualPos;
        }
    }

    //How the memory of a path/explore point is represented in the agent's world model.
    //Explore memory = POI which doesn't represent an actual game object - "made up" by the player.
    public class ExploreMemory
    {
        public float score = 0.0f;

        public Vector3 originPoint;
        public Vector3 direction;
        public Vector3 estimatedDest;

        public float impressionTime = 0.0f;

        public ExploreMemory(Vector3 originPoint, Vector3 direction, Vector3 estimatedDest, float score)
        {
            this.originPoint = originPoint;
            this.direction = direction;
            this.estimatedDest = estimatedDest;
            this.score = score;
        }

        public void UpdateScore(float score)
        {
            this.score = score;
        }

        public bool EqualsSimilar(ExploreMemory rhs, PathOSAgent agent)
        {
            return (originPoint - rhs.originPoint).magnitude 
                <= PathOS.Constants.Navigation.EXPLORE_PATH_POS_THRESHOLD_FAC
                   * agent.exploreThreshold
                && Vector3.Angle(direction, rhs.direction) 
                <= PathOS.Constants.Navigation.EXPLORE_PATH_DEG_THRESHOLD_FAC
                   * agent.exploreDegrees;
        }
    }
}
