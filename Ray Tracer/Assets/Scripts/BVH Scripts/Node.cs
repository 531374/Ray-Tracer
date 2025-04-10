using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Node
{
    public BoundingBox bounds = new();
    public List<BVHTriangle> triangles = new();
    public int childIndex = -1;
}


