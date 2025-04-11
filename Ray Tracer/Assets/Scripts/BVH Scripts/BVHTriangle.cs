using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct BVHTriangle
{
    public Vector3 posA;
    public Vector3 posB;
    public Vector3 posC;

    public Vector3 normalA;
    public Vector3 normalB;
    public Vector3 normalC;

    public Vector3 centre;

    public BVHTriangle(Vector3 posA, Vector3 posB, Vector3 posC, Vector3 normalA, Vector3 normalB, Vector3 normalC)
    {
        this.posA = posA;   
        this.posB = posB;
        this.posC = posC;

        this.normalA = normalA;
        this.normalB = normalB;
        this.normalC = normalC;

        centre = (posA + posB + posC) / 3f; 
    }
}
