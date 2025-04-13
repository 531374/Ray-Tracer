using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct BVHTriangle
{
    public Vector3 Centre;
    public Vector3 Min;
    public Vector3 Max;
    public int Index;

    public BVHTriangle(Vector3 centre, Vector3 min, Vector3 max, int index)
    {
        this.Centre = centre;
        this.Min = min;
        this.Max = max;
        this.Index = index;
    }
}
