using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class PlaneObject : MonoBehaviour
{
    public RayTracingMaterial material;

    [SerializeField, HideInInspector] int materialObjectID;
    [SerializeField, HideInInspector] bool materialInitFlag;

    void OnValidate()
    {
        if (!materialInitFlag)
        {
            materialInitFlag = true;
            material.SetDefaults();
        }

        MeshRenderer renderer = GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            if (materialObjectID != gameObject.GetInstanceID())
            {
                renderer.sharedMaterial = new Material(renderer.sharedMaterial);
                materialObjectID = gameObject.GetInstanceID();
            }
            renderer.sharedMaterial.color = material.color;
        }
    }
}



struct Plane
{
    public Vector3 position;

    public Vector3 normal;
    public Vector3 right;
    public Vector3 up;

    public Vector2 halfSize;

    public RayTracingMaterial material;
}
