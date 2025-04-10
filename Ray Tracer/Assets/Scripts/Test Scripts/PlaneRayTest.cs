using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlaneRayTest : MonoBehaviour
{
    public Transform rayT;
    public Transform planeT;

    private void OnDrawGizmos()
    {
        Gizmos.color = HitPlane() ? Color.green : Color.red;
        Gizmos.DrawLine(rayT.position, rayT.position + rayT.forward * 15f);    
    }

    bool HitPlane()
    {
        float dot = Vector3.Dot(rayT.forward, planeT.up);
        
        //Ray is parralel
        if (Mathf.Abs(dot) < Mathf.Epsilon)
        {
            return false;
        }

        float t = Vector3.Dot(planeT.position - rayT.position, planeT.up) / dot;

        //Ray is behind plane
        if (t < 0) return false;

        Vector3 hitPoint = rayT.position + t * rayT.forward;
        Vector3 toHit = hitPoint - planeT.position;

        float uDist = Vector3.Dot(toHit, planeT.right);
        float vDist = Vector3.Dot(toHit, planeT.forward);

        float halfWidth = planeT.localScale.x * 5f;
        float halfHeight = planeT.localScale.z * 5f;

        //Ray is outside of plane   
        if (Mathf.Abs(uDist) > halfWidth || Mathf.Abs(vDist) > halfHeight) return false;

        return true;
    }

}
