using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Path : MonoBehaviour
{

    public Vector3[] Waypoints;
    public LineRenderer LineRenderer;
    public float Radius = 2f;
    public float TotalPathLength = 0;
    private float m_radiusSqr;
    private List<Vector2> m_path = new List<Vector2>(), m_normals = new List<Vector2>();
    private List<float> m_lengths = new List<float>();

    void Start()
    {
        LineRenderer.GetPositions(Waypoints);
        Init();
    }

    public void Init()
    {
        m_radiusSqr = Radius * Radius;
        m_lengths.Add(0);
        m_normals.Add(Vector2.zero);
        foreach (var point in Waypoints)
        {
            m_path.Add(new Vector2(point.x, point.z));
        }

        for (int i = 1; i < m_path.Count; i++)
        {
            Vector2 normal = m_path[i] - m_path[i - 1];
            float length = normal.magnitude;
            m_lengths.Add(length);
            m_normals.Add(normal / length);
            TotalPathLength += length;
        }
    }

    public float GetDistanceByMapPoint(Vector2 point, out bool isOutOfPath, out Vector2 segmentNomal)
    {
        float pathDistance = 0, segmentLengthTotal = 0;
        float minDistSqr = float.MaxValue;
        Vector2 mapPoint;
        segmentNomal = Vector2.zero;
        for (int i = 1; i < m_path.Count; i++)
        {
            float projectionLength = 0;
            float distSqr = SteerManager.GetPointToSegmentDistanceSqr(point, m_path[i - 1], m_path[i], m_normals[i], m_lengths[i], out mapPoint, out projectionLength);
            if (distSqr < minDistSqr)
            {
                minDistSqr = distSqr;
                segmentNomal = m_normals[i];
                pathDistance = segmentLengthTotal + projectionLength;
            }
            segmentLengthTotal += m_lengths[i];
        }
        isOutOfPath = minDistSqr > m_radiusSqr;
        return pathDistance;
    }

    public Vector2 GetPathPointByDistance(float distance)
    {
        if (distance < 0)
            return m_path[0];
        if (distance >= TotalPathLength)
            return m_path[m_path.Count - 1];

        float remainLength = distance;
        for (int i = 1; i < m_path.Count; i++)
        {
            float segmentLength = m_lengths[i];
            if (segmentLength < remainLength)
                remainLength -= segmentLength;
            else
                return Vector2.Lerp(m_path[i - 1], m_path[i], remainLength / segmentLength);
        }
        return m_path[0];
    }
}
