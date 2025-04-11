using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct BoundingBox
{
    public Vector3 Min;
    public Vector3 Max;
    public Vector3 CalculateCentre() => (Min + Max) * 0.5f;
    public Vector3 CalculateSize() => Max - Min;

    bool hasPoint;

    public void GrowToInclude(Vector3 point)
    {
        if (!hasPoint)
        {
            hasPoint = true;
            Min = point;
            Max = point;
        }
        else
        {
            Min = Vector3.Min(Min, point);
            Max = Vector3.Max(Max, point);
        }

    }

    public void GrowToInclude(BVHTriangle tri)
    {
        GrowToInclude(tri.posA);
        GrowToInclude(tri.posB);
        GrowToInclude(tri.posC);
    }
}
