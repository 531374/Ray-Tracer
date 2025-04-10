using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Node
{
    public BoundingBox bounds = new();
    public int firstTriangleIndex;
    public int triangleCount;
    public int childIndex = -1;
}


