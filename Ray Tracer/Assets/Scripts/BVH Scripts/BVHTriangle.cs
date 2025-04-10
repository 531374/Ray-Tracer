using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct BVHTriangle
{
    public Vector3 posA;
    public Vector3 posB;
    public Vector3 posC;

    public Vector3 centre;

    public BVHTriangle(Vector3 posA, Vector3 posB, Vector3 posC)
    {
        this.posA = posA;   
        this.posB = posB;
        this.posC = posC;

        centre = (posA + posB + posC) / 3f; 
    }
}
