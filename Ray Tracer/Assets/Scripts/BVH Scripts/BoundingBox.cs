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
            Min.x = min.x < Min.x ? min.x : Min.x;
            Min.y = min.y < Min.y ? min.y : Min.y;
            Min.z = min.z < Min.z ? min.z : Min.z;

            Max.x = max.x > Max.x ? max.x : Max.x;
            Max.y = max.y > Max.y ? max.y : Max.y;
            Max.z = max.z > Max.z ? max.z : Max.z;
        }

    }
}
