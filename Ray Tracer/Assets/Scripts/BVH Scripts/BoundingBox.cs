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

    public void GrowToInclude(Vector3 min, Vector3 max)
    {
        if (!hasPoint)
        {
            hasPoint = true;
            Min = min;
            Max = max;
        }
        else
        {
            Min = Vector3.Min(Min, min);
            Max = Vector3.Max(Max, max);
        }

    }
}
