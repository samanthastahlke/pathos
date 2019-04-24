using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PathOS;

/*
PathOSAgentMemory.cs 
PathOSAgentMemory (c) Nine Penguins (Samantha Stahlke) and Atiya Nova 2018
*/

public class PathOSAgentMemory : MonoBehaviour 
{
    public PathOSAgent agent;
    private static PathOSManager manager;

    //Remembered entities.
    public List<EntityMemory> entities { get; set; }

    //Remembered paths/directions.
    public List<ExploreMemory> paths { get; set; }

    //Traversal history.
    public List<WaypointMemory> waypoints { get; set; }
    public int waypointCacheSize = 64;
    public float waypointRegisterTime = 5.0f;
    private float waypointTimer = 0.0f;

    //The agent's memory model of the complete navmesh.
    [Header("Navmesh Memory Model")]
    public bool autogenerateMapExtents = true;
    public PathOSNavUtility.NavmeshBoundsXZ navmeshBounds;
    public float worldExtentRadius = 200.0f;
    public float worldBorderMargin = 25.0f;
    public float gridSampleSize = 1.0f;
    public float gridSampleElevation = 10.0f;

    public PathOSNavUtility.NavmeshMemoryMapper memoryMap { get; set; }

    //Check to see if there are any goals left
    protected bool goalsLeft = true;

    //for hazardous area
    private List<Vector3> nearbyEnemies = new List<Vector3>();
    private float hazardRadius = 0;
    private int hazardLimit = 2;
    private float hazardRange = 6f;

    private void Awake()
    {
        entities = new List<EntityMemory>();
        paths = new List<ExploreMemory>();
        waypoints = new List<WaypointMemory>();

        if (null == manager)
            manager = PathOSManager.instance;

        //Initialize the (blank) model of the agent's internal "map".
        if (autogenerateMapExtents)
            memoryMap = new PathOSNavUtility.NavmeshMemoryMapper(gridSampleSize, worldExtentRadius, gridSampleElevation);
        else
            memoryMap = new PathOSNavUtility.NavmeshMemoryMapper(gridSampleSize, navmeshBounds);

        //Commit any "always-known" entities to memory.
        foreach (LevelEntity entity in manager.levelEntities)
        {
            if(entity.alwaysKnown)
            {
                EntityMemory newMemory = new EntityMemory(new PerceivedEntity(
                    entity, entity.entityType, entity.objectRef.transform.position));
                newMemory.MakeUnforgettable();

                entities.Add(newMemory);
            }
        }
    }

    private void Start()
    {
        PushWaypoint(agent.GetPosition());
    }

    private void Update()
    {
        for(int i = entities.Count - 1; i > 0; --i)
        {
            entities[i].impressionTime += Time.deltaTime;

            if (entities[i].forgettable && !entities[i].visited && entities[i].impressionTime >= agent.forgetTime)
                entities.RemoveAt(i);
        }

        for(int i = paths.Count - 1; i > 0; --i)
        {
            paths[i].impressionTime += Time.deltaTime;

            if (paths[i].impressionTime >= agent.forgetTime)
                paths.RemoveAt(i);
        }

        waypointTimer += Time.deltaTime;

        if (waypointTimer >= waypointRegisterTime)
        {
            waypointTimer = 0.0f;
            PushWaypoint(agent.GetPosition());
        }       
    }

    //Push an entity into the agent's memory.
    public void Memorize(PerceivedEntity entity)
    {
        for(int i = 0; i < entities.Count; ++i)
        {
            if (entity == entities[i])
            {
                entities[i].impressionTime = 0.0f;
                entities[i].perceivedPos = entity.perceivedPos;
                return;
            }             
        }

        entities.Add(new EntityMemory(entity));
    }

    public void CommitLTM(PerceivedEntity entity)
    {
        for(int i = 0; i < entities.Count; ++i)
        {
            if(entity == entities[i])
            {
                entities[i].ltm = true;
                return;
            }
        }

        entities.Add(new EntityMemory(entity));
        entities[entities.Count - 1].ltm = true;
    }

    //Has a visible entity been visited?
    public bool Visited(PerceivedEntity entity)
    {
        for(int i = 0; i < entities.Count; ++i)
        {
            if (entity == entities[i])
                return entities[i].visited;
        }

        return false;
    }

    //Adds a new path to memory
    public void AddPath(ExploreMemory thePath)
    {
        if (!CheckIfPathExists(thePath))
        {
            paths.Add(new ExploreMemory(thePath.originPoint, thePath.direction, thePath.dEstimate));
        }
    }

    //Checks to see if the path already exists in memory
    bool CheckIfPathExists(ExploreMemory thePath)
    {
        for (int i = paths.Count - 1; i > 0; i--)
        {
            if (thePath == paths[i]) return true;
        }
        return false;
    }

    //gets the last traversed path
    public int GetLastPath()
    {
        Vector3 originPoint = paths[paths.Count - 1].originPoint;

        for (int i = paths.Count - 1; i > 0; i--)
        {
            if (paths[i].originPoint != originPoint)
            {
                return i;
            }
        }
        return 0;
    }

    public void PushWaypoint(Vector3 pos, bool wasTarget = false)
    {
        waypoints.Add(new WaypointMemory(pos, wasTarget));

        if (waypoints.Count > waypointCacheSize)
            waypoints.RemoveAt(0);
    }

    public Vector3 GetLastWaypoint()
    {
        if(waypoints.Count == 0)
        {
            NPDebug.LogError("Attempted to access waypoint memory before initialization!",
                typeof(PathOSAgentMemory));

            return Vector3.zero;
        }

        return waypoints[waypoints.Count - 1].pos;
    }

    //Are there any goals left? 
    bool GoalsRemaining()
    {
        for (int i = 0; i < entities.Count; i++)
        {
            //this really, really, really needs to be cleaned up pleasedonthateme
            if ((entities[i].entityType == EntityType.ET_GOAL_OPTIONAL || entities[i].entityType == EntityType.ET_GOAL_MANDATORY
                || entities[i].entityType == EntityType.ET_RESOURCE_ACHIEVEMENT || entities[i].entityType == EntityType.ET_RESOURCE_PRESERVATION)
                && entities[i].visited == false)
            {
                return true;
            }
        }
        return false;
    }

    public void CheckGoals()
    {
        goalsLeft = GoalsRemaining(); //calculates whether or not there are goals it still needs to go to
    }

    public bool GetGoalsLeft()
    {
        return goalsLeft; //returns whether or not there are goals left
    }

    //checks the number of hazards in close proximity
    public bool CheckHazards(Vector3 currentDestination)
    {
        int hazardCounter = 0;
        nearbyEnemies.Clear();

        for (int i = 0; i < entities.Count; i++)
        {
            //if the hazard is within range... (the range is just a placeholder for now as well, I'm worried that it's too short?)
            if ((entities[i].perceivedPos - currentDestination).magnitude < hazardRange && (entities[i].entityType == EntityType.ET_HAZARD_ENEMY || entities[i].entityType == EntityType.ET_HAZARD_ENVIRONMENT))
            {
                nearbyEnemies.Add(entities[i].perceivedPos);
                //we increment the counter to see how many hazards are close by
                hazardCounter++;

                //the 2 is just a placeholder to test that it works
                if (hazardCounter >= hazardLimit)
                {
                    //if it's hazardous it returns true
                    return true;
                }
            }
        }
        
        //else return false
        return false;
    }

    //Takes the centroid, calculates a new path based off of it
    //So far this hasn't been giving me issues, but I'll keep iterating on it
    public Vector3 CalculateNewPath(int startingIndex) 
    {
        Vector3 centroid = CalculateCentroid();
        CalculateHazardRadius(centroid);
        Vector3 newPath = CalculateNewDirection(centroid, startingIndex);
        return newPath;
    }

    //Chooses path away from the area where the enemies are
    public Vector3 CalculateNewDirection(Vector3 centerPoint, int startingIndex)
    {
        for (int i = startingIndex; i > 0; i--)
        {
            //Uses centerpoint of where the enemies are to pick paths that fall outside of that radius
            if (Vector3.Distance(CalculatePathDestination(i), centerPoint) > (hazardRadius))
            {
                return CalculatePathDestination(i);
            }
        }

        return CalculatePathDestination(0);
    }

    //Calculates the destination for that path
    public Vector3 CalculatePathDestination(int pathIndex)
    {
        //returns a point on the navmesh that the agent can reach with the chosen path
       return PathOSNavUtility.GetClosestPointWalkable(
                agent.transform.position + paths[pathIndex].dEstimate * paths[pathIndex].direction, worldBorderMargin);
    }

    //based off of what we set the limit to hazards in the area to be,
    //it calculates the center
    //that we can then use to determine the approximate area of hazards
    public Vector3 CalculateCentroid()
    {
        Vector3 centroid = Vector3.zero;

        for (int i = 0; i < hazardLimit; i++)
        {
            centroid.x += nearbyEnemies[i].x;
            centroid.y += nearbyEnemies[i].y;
            centroid.z += nearbyEnemies[i].z;
        }

        centroid = centroid / hazardLimit;

        return centroid;
    }

    //This gets the radius of the area
    public void CalculateHazardRadius(Vector3 centroid)
    {
        for (int i = 0; i < hazardLimit; i++)
        {
            if (Vector3.Distance(nearbyEnemies[i], centroid) > hazardRadius)
                hazardRadius = Vector3.Distance(nearbyEnemies[i], centroid);
        }
    }
}
