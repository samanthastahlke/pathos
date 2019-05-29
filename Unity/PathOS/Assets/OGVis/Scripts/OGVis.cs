using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace OGVis
{
    //Utility class for defining custom scales.
    [System.Serializable]
    public struct TimeRange
    {
        public float min;
        public float max;
    }

    [System.Serializable]
    public struct Extents
    {
        public Vector3 min;
        public Vector3 max;
    }

    //Utility class for loading/storing player logfiles.
    public class PlayerLog
    {
        public struct VectorT
        {
            public Vector3 pos;
            public float timestamp;
        }

        public struct QuaternionT
        {
            public Quaternion rot;
            public float timestamp;
        }

        //Shell class for timestamped events.
        public abstract class EventRecord
        {
            public float timestamp;

            //"Real position" of the event data.
            public Vector3 realPos;

            //"Display position" updated by path resampling to account for 
            //axis flattening.
            public Vector3 pos;

            public EventRecord(float timestamp, Vector3 pos)
            {
                this.timestamp = timestamp;
                this.realPos = new Vector3(pos.x, pos.y, pos.z);
                this.pos = new Vector3(pos.x, pos.y, pos.z);
            }
        }

        //Interaction events.
        public class InteractionEvent : EventRecord
        {
            public string objectName;

            public InteractionEvent(float timestamp, Vector3 pos, string objectName)
                : base(timestamp, pos)
            {
                this.objectName = objectName;
            }

            public string GetStringHash()
            {
                return GetStringHash(realPos, objectName);
            }

            public static string GetStringHash(Vector3 pos, string objectName)
            {
                return objectName + "(" +
                    (Mathf.Round(pos.x * 10.0f) / 10.0f).ToString() + "," +
                    (Mathf.Round(pos.y * 10.0f) / 10.0f).ToString() + "," +
                    (Mathf.Round(pos.z * 10.0f) / 10.0f).ToString() + ")";
            }
        }

        public string playerID;

        //Time-series position/orientation data.
        public List<VectorT> positions;
        public List<QuaternionT> orientations;

        public List<InteractionEvent> interactionEvents;

        public List<Vector3> pathPoints;
        public int displayStartIndex;
        public int displayEndIndex;

        public Dictionary<PathOS.Heuristic, float> heuristics;
        public float experience;

        public float sampleRate = -1.0f;

        public bool visInclude = true;
        public Color pathColor = Color.white;

        public PlayerLog(string playerID)
        {
            this.playerID = playerID;

            positions = new List<VectorT>();
            orientations = new List<QuaternionT>();
            interactionEvents = new List<InteractionEvent>();

            pathPoints = new List<Vector3>();
        }

        public void AddPosition(float timestamp, Vector3 pos)
        {
            positions.Add(new VectorT { timestamp = timestamp, pos = pos });
        }

        public void AddOrientation(float timestamp, Quaternion rot)
        {
            orientations.Add(new QuaternionT { timestamp = timestamp, rot = rot });
        }

        public void UpdateDisplayPath(float displayHeight)
        {
            pathPoints.Clear();

            if (positions.Count == 0)
                return;

            foreach (VectorT p in positions)
            {
                pathPoints.Add(new Vector3(
                        p.pos.x, displayHeight, p.pos.z));
            }

            //Resample event positions to account for axis flattening.
            foreach (InteractionEvent e in interactionEvents)
            {
                e.pos.x = e.realPos.x;
                e.pos.y = displayHeight;
                e.pos.z = e.realPos.z;
            }
        }

        public void SliceDisplayPath(TimeRange range)
        {
            int startEstimate = 0;
            int endEstimate = positions.Count - 1;

            if (sampleRate > 0.0f)
            {
                startEstimate = Mathf.Max(0,
                    (int)Mathf.Floor(range.min * sampleRate));

                endEstimate = Mathf.Min(positions.Count - 1,
                    (int)Mathf.Floor(range.max * sampleRate));
            }

            bool foundIndex = false;

            while (!foundIndex)
            {
                if (startEstimate < positions.Count - 1
                    && positions[startEstimate].timestamp < range.min)
                {
                    ++startEstimate;
                    continue;
                }

                if (startEstimate > 0
                    && positions[startEstimate - 1].timestamp >= range.min)
                {
                    --startEstimate;
                    continue;
                }

                foundIndex = true;
                break;
            }

            foundIndex = false;

            while (!foundIndex && endEstimate < positions.Count)
            {
                if (endEstimate > 0
                   && positions[endEstimate].timestamp > range.max)
                {
                    --endEstimate;
                    continue;
                }

                if (endEstimate < positions.Count - 1
                    && positions[endEstimate + 1].timestamp <= range.max)
                {
                    ++endEstimate;
                    continue;
                }

                foundIndex = true;
                break;
            }

            displayStartIndex = startEstimate;
            displayEndIndex = endEstimate;
        }

        public void AddInteractionEvent(float timestamp, Vector3 pos, string objectName)
        {
            interactionEvents.Add(new InteractionEvent(timestamp, pos, objectName));
        }
    }
}
