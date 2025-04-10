using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TriangleRayTest : MonoBehaviour
{
    Mesh mesh;

    public Transform vert1;
    public Transform vert2;
    public Transform vert3;

    public Transform rayT;

    bool HitTriangle()
    {
        Vector3 rayOrigin = rayT.position;
        Vector3 rayDir = rayT.forward;

        Vector3 edge1 = vert2.position - vert1.position;
        Vector3 edge2 = vert3.position - vert1.position;

        Vector3 crossRayEdge2 = Vector3.Cross(rayDir, edge2);
        float determinant = Vector3.Dot(edge1, crossRayEdge2);

        if (Mathf.Abs(determinant) < Mathf.Epsilon) return false;

        float inverseDeterminant = 1.0f / determinant;
        Vector3 s = rayOrigin - vert1.position;
        float u = inverseDeterminant * Vector3.Dot(s, crossRayEdge2);

        if(u < 0 && Mathf.Abs(u) > Mathf.Epsilon || (u > 1 && Mathf.Abs(u-1) > Mathf.Epsilon)) return false;

        Vector3 crossSEdge1 = Vector3.Cross(s, edge1);
        float v = inverseDeterminant * Vector3.Dot(rayDir, crossSEdge1);

        if(v < 0 && Mathf.Abs(v) > Mathf.Epsilon || (u + v > 1 && Mathf.Abs(u + v - 1) > Mathf.Epsilon)) return false;

        float t = inverseDeterminant * Vector3.Dot(edge2, crossSEdge1);

        if(t > Mathf.Epsilon)
        {
            return true;
        }
        return false;

    }

    private void OnDrawGizmos()
    {
        if(mesh == null) mesh = new Mesh();

        Gizmos.color = HitTriangle() ? Color.green : Color.red;
        Gizmos.DrawLine(rayT.position, rayT.position + rayT.forward * 10f);

        Gizmos.DrawSphere(vert1.position, 0.1f);
        Gizmos.DrawSphere(vert2.position, 0.1f);
        Gizmos.DrawSphere(vert3.position, 0.1f);

        Vector3[] triangle = new Vector3[] { vert1.position, vert2.position, vert3.position };
        int[] indices = new int[] { 0, 1, 2 };

        mesh.SetVertices(triangle);
        mesh.SetTriangles(indices, 0);

        GetComponent<MeshFilter>().mesh = mesh; 
    }
}
