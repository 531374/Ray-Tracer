using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct Node
{
    public Vector3 boundsMin;
    public Vector3 boundsMax;
    public int firstTriangleIndex;
    public int triangleCount;
    public int childIndex;

    public Node(BoundingBox bounds, int firstTriangleIndex = 0, int triangleCount = 0, int childIndex = 0)
    {
        this.boundsMin = bounds.Min;
        this.boundsMax = bounds.Max;

        this.firstTriangleIndex = firstTriangleIndex;
        this.triangleCount = triangleCount;
        this.childIndex = childIndex;
    }

    public Vector3 CalculateCentre() => (boundsMin + boundsMax) * 0.5f;
    public Vector3 CalculateSize() => boundsMax - boundsMin;
}


