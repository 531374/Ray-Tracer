using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct Node
{
    public BoundingBox bounds;
    public int firstTriangleIndex;
    public int triangleCount;
    public int childIndex;

    public Node(BoundingBox bounds, int firstTriangleIndex = 0, int triangleCount = 0, int childIndex = 0)
    {
        this.bounds = bounds;
        this.firstTriangleIndex = firstTriangleIndex;
        this.triangleCount = triangleCount;
        this.childIndex = childIndex;
    }
}


