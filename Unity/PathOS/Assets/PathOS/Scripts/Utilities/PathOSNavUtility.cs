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
    [System.Serializable]
    public class NavmeshBoundsXZ
    {
        public float altitudeSampleHeight { get; set; }
        public Vector3 centre { get; set; }
        public Vector3 min;
        public Vector3 max;
        public Vector3 size { get; set; }

        public NavmeshBoundsXZ()
        {
            altitudeSampleHeight = 0.0f;
            min = new Vector3(float.MaxValue, 0.0f, float.MaxValue);
            max = new Vector3(float.MinValue, 0.0f, float.MinValue);
            size = Vector3.zero;
            centre = Vector3.zero;
        }

        public void RecomputeCentreAndSize()
        {
            size = new Vector3(max.x - min.x, 0.0f, max.z - min.z);
            centre = 0.5f * (max + min);
        }
    }

    //Maintains a (for now) yes-no "visited map" of the environment.
    public class NavmeshMemoryMapper
    {
        public struct NavmeshMemoryMapperCastHit
        {
            public int numUnexplored;
            public float portionUnexplored;
            public float distance;
        }

        public enum NavmeshMapCode
        {
            NM_DNE = -1,
            NM_UNVISITED = 0,
            NM_VISITED = 1,
            NM_OBSTACLE = 10
        };

        NavmeshBoundsXZ bounds;
        float sampleGridSize;
        public const int maxCastSamples = 64;

        Vector3 gridOrigin;
        NavmeshMapCode[,] visitedGrid;

        Texture2D visualGrid;

        public NavmeshMemoryMapper(float sampleGridSize, float extents, float sampleHeight)
        {
            this.sampleGridSize = sampleGridSize;

            //Grab the NavMesh bounds.
            SetBounds(GetNavmeshBounds(sampleHeight, extents));
        }

        public NavmeshMemoryMapper(float sampleGridSize, NavmeshBoundsXZ bounds)
        {
            this.sampleGridSize = sampleGridSize;
            bounds.RecomputeCentreAndSize();
            SetBounds(bounds);
        }

        private void SetBounds(NavmeshBoundsXZ bounds)
        {
            this.bounds = bounds;
            gridOrigin = bounds.min;

            //Calculate the grid size based on NavMesh extents and grid sampling edge.
            int sizeX = (int)(bounds.size.x / sampleGridSize) + 1;
            int sizeZ = (int)(bounds.size.z / sampleGridSize) + 1;

            visitedGrid = new NavmeshMapCode[sizeX, sizeZ];

            //Create a texture to represent the grid for on-screen display.
            visualGrid = new Texture2D(sizeX, sizeZ, TextureFormat.ARGB32, false, true);
            visualGrid.filterMode = FilterMode.Point;

            for (int i = 0; i < sizeX; ++i)
            {
                for (int j = 0; j < sizeZ; ++j)
                {
                    visitedGrid[i, j] = NavmeshMapCode.NM_UNVISITED;
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

        private void GetGridCoords(Vector3 point, ref int gridX, ref int gridZ)
        {
            //Calculate a vector from the grid's origin to the sample point.
            Vector3 diff = point - gridOrigin;

            //Calculate grid indices based on sampling size.
            gridX = (int)(diff.x / sampleGridSize);
            gridZ = (int)(diff.z / sampleGridSize);
        }

        private NavmeshMapCode SampleMap(Vector3 point)
        {
            //Calculate grid indices.
            int gridX = 0, gridZ = 0;
            GetGridCoords(point, ref gridX, ref gridZ);

            if (gridX >= 0 && gridZ >= 0
                && gridX < visitedGrid.GetLength(0)
                && gridZ < visitedGrid.GetLength(1))
                return visitedGrid[gridX, gridZ];
            else
                return NavmeshMapCode.NM_DNE;
        }

        //In-progress memory raycast.
        //Right now the distance will be an estimation of the straight-line distance 
        //traversable in that direction, and unexplored tiles will stop being counted
        //if the ray samples from an obstacle tile.
        public void RaycastMemoryMap(Vector3 origin, Vector3 dir, float maxDistance, out NavmeshMemoryMapperCastHit hit)
        {
            Vector3 point = origin;

            Vector3 d = new Vector3(dir.x, 0.0f, dir.z);
            d.Normalize();

            //What is our sampling distance?
            //Depending on the angle between the direction and the grid lines,
            //this will fluctuate - we effectively want to sample so 
            //we'll hit in one-tile increments.
            //This could be improved later to be less approximate and hit
            //every tile the ray would cross.
            float theta = Vector3.Angle(Vector3.forward, d);
            theta = Mathf.Abs(theta);

            //Debug.Log(string.Format("Theta: {0:0.000}", theta));

            theta -= (int)(theta / 90.0f) * 90.0f;

            if (theta > 45.0f)
                theta = 90.0f - theta;

            //Debug.Log(string.Format("Clamped Theta: {0:0.000}", theta));

            float sampleDistance = sampleGridSize / Mathf.Cos(Mathf.Deg2Rad * theta);
            //Debug.Log(string.Format("Grid Size: {0:0.000}, Sampling distance: {1:0.000}", sampleGridSize, sampleDistance));

            d = sampleDistance * d;

            int numUnexplored = 0, totalSampled = 0;
            float totalDistance = 0.0f;
            int obstacleCount = 0;
            NavmeshMapCode sample = NavmeshMapCode.NM_DNE;

            for(int i = 1; (i * sampleDistance) < maxDistance && i < maxCastSamples; ++i)
            {
                sample = SampleMap(point);

                if (sample == NavmeshMapCode.NM_UNVISITED)
                    ++numUnexplored;
                else if (sample == NavmeshMapCode.NM_OBSTACLE)
                    ++obstacleCount;
                //Stop if we reach the edge of the grid or we've crossed more than 
                //one obstacle tile (avoid mistaking corners for walls).
                //(This "hit detection" for obstacles should be improved as well.)
                else if (sample == NavmeshMapCode.NM_DNE || obstacleCount > 1)
                    break;

                ++totalSampled;
                point += d;

                totalDistance += sampleDistance;
            }

            hit.numUnexplored = numUnexplored;
            hit.distance = totalDistance;
            hit.portionUnexplored = (totalSampled > 0) ? (float)numUnexplored / (float)totalSampled : 0.0f;
        }

        public void Fill(Vector3 point, NavmeshMapCode code = NavmeshMapCode.NM_VISITED)
        {
            //Calculate grid indices.
            int gridX = 0, gridZ = 0;
            GetGridCoords(point, ref gridX, ref gridZ);

            if(gridX < 0 || gridZ < 0
                || gridX > visitedGrid.GetLength(0) 
                || gridZ > visitedGrid.GetLength(1))
            {
                NPDebug.LogError("Sample location outside of grid bounds!", 
                    typeof(NavmeshMemoryMapper));

                return;
            }

            visitedGrid[gridX, gridZ] = code;

            Color fillColor = Color.black;
            switch(code)
            {
                case NavmeshMapCode.NM_VISITED:
                    fillColor = Color.green;
                    break;

                case NavmeshMapCode.NM_OBSTACLE:
                    fillColor = Color.red;
                    break;
            }

            visualGrid.SetPixel(gridX, gridZ, fillColor);
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
