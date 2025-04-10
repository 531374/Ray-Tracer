using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoundingBox
{
    public Vector3 Min = Vector3.one * float.PositiveInfinity;
    public Vector3 Max = Vector3.one * float.NegativeInfinity;
    public Vector3 Centre => (Min + Max) * 0.5f;
    public Vector3 Size => Max - Min;

    public void GrowToInclude(Vector3 point)
    {
        Min = Vector3.Min(Min, point);
        Max = Vector3.Max(Max, point);
    }

    public void GrowToInclude(BVHTriangle tri)
    {
        GrowToInclude(tri.posA);
        GrowToInclude(tri.posB);
        GrowToInclude(tri.posC);
    }
}
