using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CamTest : MonoBehaviour
{
    public Vector2Int debugPointCount;
    public float debugRadius;

    void CameraRayTest()
    {
        Camera cam = Camera.main;
        Transform camT = cam.transform;

        float planeHeight = cam.nearClipPlane * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad) * 2;
        float planeWidth = planeHeight * cam.aspect;

        Vector3 localBottomLeft = new Vector3(-planeWidth / 2f, -planeHeight / 2f, cam.nearClipPlane);

        for(int x = 0; x < debugPointCount.x; x++)
        {
            for(int y = 0; y < debugPointCount.y; y++)
            {
                float tx = x / (debugPointCount.x - 1f);
                float ty = y / (debugPointCount.y - 1f);

                Vector3 localPoint = localBottomLeft + new Vector3(planeWidth * tx, planeHeight * ty);
                Vector3 point = camT.position + camT.right * localPoint.x + camT.up * localPoint.y + camT.forward * localPoint.z;
                Vector3 direction = (point - camT.position).normalized;

                Gizmos.DrawSphere(point, debugRadius);
                Gizmos.DrawLine(point, point + direction * 10f);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        CameraRayTest();
    }


}
