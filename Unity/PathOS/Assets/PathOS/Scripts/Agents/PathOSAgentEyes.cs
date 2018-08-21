using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PathOS;

/*
PathOSAgentEyes.cs 
PathOSAgentEyes (c) Nine Penguins (Samantha Stahlke) 2018
*/

public class PathOSAgentEyes : MonoBehaviour 
{
    public PathOSAgent agent;
    private static PathOSManager manager;

    //The agent's "eyes" - i.e., the camera the player would use.
    public Camera cam;

    //What can the agent "see" currently?
    public List<PerceivedEntity> visible { get; set; }

    //Timer to handle visual processing checks. Roll for perception.
    private float perceptionTimer = 0.0f;
    
	void Awake()
	{
        visible = new List<PerceivedEntity>();

        if (null == manager)
            manager = PathOSManager.instance;
    }

    public void ProcessPerception()
    {
        Plane[] frustum = GeometryUtility.CalculateFrustumPlanes(cam);

        visible.Clear();

        for (int i = 0; i < manager.levelEntities.Count; ++i)
        {
            LevelEntity entity = manager.levelEntities[i];
            Vector3 entityPos = entity.entityRef.transform.position;
            Vector3 ray = cam.transform.position - entityPos;

            //Visisbility check.
            //If object's renderer is in bounds of camera...
            //And we can draw a ray to the camera from that object without
            //hitting anything. (The raycast right now is naive and uses
            //only the object's centre, will need to be improved.)
            if (entity.rend != null
                && GeometryUtility.TestPlanesAABB(frustum, entity.rend.bounds)
                && !Physics.Raycast(entityPos, ray.normalized, ray.magnitude))
                visible.Add(new PerceivedEntity(entity.entityRef, entity.entityType, entityPos));
        }

        for (int i = 0; i < visible.Count; ++i)
        {
            agent.memory.Memorize(visible[i]);
        }
    }


	
	void Update() 
	{
        perceptionTimer += Time.deltaTime;

        //Visual processing update.
        if (perceptionTimer >= agent.perceptionComputeTime)
        {
            perceptionTimer = 0.0f;
            ProcessPerception();
        }
    }
}
