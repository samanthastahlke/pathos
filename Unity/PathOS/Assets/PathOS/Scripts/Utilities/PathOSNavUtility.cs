using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/*
PathOSNavUtility.cs 
PathOSNavUtility (c) Nine Penguins (Samantha Stahlke) 2018
*/

public class PathOSNavUtility 
{
    //Simple class for defining the boundaries of a NavMesh in the XZ plane.
    public class NavmeshBoundsXZ
    {
        public float altitudeSampleHeight = 0.0f;
        public Vector3 centre;
        public Vector3 min;
        public Vector3 max;
        public Vector3 size;

        public NavmeshBoundsXZ()
        {
            min = new Vector3(float.MaxValue, 0.0f, float.MaxValue);
            max = new Vector3(float.MinValue, 0.0f, float.MinValue);
            size = Vector3.zero;
            centre = Vector3.zero;
        }

        public void RecomputeCentreAndSize()
        { 
            size.x = max.x - min.x;
            size.z = max.z - min.z;
            centre = 0.5f * (max + min);
        }
    }

    //Maintains a (for now) yes-no "visited map" of the environment.
    public class NavmeshMemoryMapper
    {
        NavmeshBoundsXZ bounds;
        float sampleGridSize;

        Vector3 gridOrigin;
        bool[,] visitedGrid;

        Texture2D visualGrid;

        public NavmeshMemoryMapper(float sampleGridSize, float extents)
        {
            //Grab the NavMesh bounds.
            bounds = GetNavmeshBounds(0.0f, extents);

            this.sampleGridSize = sampleGridSize;
            gridOrigin = bounds.min;

            //Calculate the grid size based on NavMesh extents and grid sampling edge.
            int sizeX = (int)(bounds.size.x / sampleGridSize) + 1;
            int sizeZ = (int)(bounds.size.z / sampleGridSize) + 1;

            visitedGrid = new bool[sizeX, sizeZ];

            //Create a texture to represent the grid for on-screen display.
            visualGrid = new Texture2D(sizeX, sizeZ, TextureFormat.ARGB32, false, true);
            visualGrid.filterMode = FilterMode.Point;

            for(int i = 0; i < sizeX; ++i)
            {
                for(int j = 0; j < sizeZ; ++j)
                {
                    visitedGrid[i, j] = false;
                    visualGrid.SetPixel(i, j, Color.black);
                }
            }

            visualGrid.Apply();
        }

        public Vector3 GetBoundsSize()
        {
            return bounds.size;
        }

        public float GetAspect()
        {
            return (float)visualGrid.width / (float)visualGrid.height;
        }

        public Texture2D GetVisualGrid()
        {
            return visualGrid;
        }

        public void Traverse(Vector3 point)
        {
            //Calculate a vector from the grid's origin to the sample point.
            Vector3 diff = point - gridOrigin;

            //Calculate grid indices based on sampling size.
            int gridX = (int)(diff.x / sampleGridSize);
            int gridZ = (int)(diff.z / sampleGridSize);

            if(gridX < 0 || gridZ < 0
                || gridX > visitedGrid.GetLength(0) 
                || gridZ > visitedGrid.GetLength(1))
            {
                NPDebug.LogError("Sample location outside of grid bounds!", 
                    typeof(NavmeshMemoryMapper));

                return;
            }

            visitedGrid[gridX, gridZ] = true;
            visualGrid.SetPixel(gridX, gridZ, Color.green);
            visualGrid.Apply();
        }
    }

    public static NavmeshBoundsXZ GetNavmeshBounds(float altitudeSampleHeight,
        float extents)
    {
        NavmeshBoundsXZ result = new NavmeshBoundsXZ();

        //Create our worldspace "corners" based on the search extents.
        Vector3[] corners = new Vector3[4];
        corners[0] = new Vector3(extents, altitudeSampleHeight, extents);
        corners[1] = new Vector3(extents, altitudeSampleHeight, -extents);
        corners[2] = new Vector3(-extents, altitudeSampleHeight, extents);
        corners[3] = new Vector3(-extents, altitudeSampleHeight, -extents);

        NavMeshHit castResult = new NavMeshHit();

        //Get the closest points on the navmesh to each of the worldspace corners.
        for (int i = 0; i < corners.Length; ++i)
        {
            NavMesh.SamplePosition(corners[i], out castResult, extents * 2.0f, NavMesh.AllAreas);
            corners[i] = castResult.position;
        }

        //Grab the min/max XZ coordinates of the navmesh.
        for(int i = 0; i < corners.Length; ++i)
        {
            if (corners[i].x > result.max.x)
                result.max.x = corners[i].x;
            if (corners[i].x < result.min.x)
                result.min.x = corners[i].x;
            if (corners[i].z > result.max.z)
                result.max.z = corners[i].z;
            if (corners[i].z < result.min.z)
                result.min.z = corners[i].z;
        }

        result.RecomputeCentreAndSize();

        return result;
    }


}
