using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BVHObject : MonoBehaviour
{
    public RayTracingMaterial material;

    [SerializeField, HideInInspector] int materialObjectID;
    [SerializeField, HideInInspector] bool materialInitFlag;

    //References
    Mesh mesh;
    MeshRenderer meshRenderer;
    MeshFilter meshFilter;

    void OnValidate()
    {
        if(mesh == null || meshRenderer == null || meshFilter == null)
        {
            meshRenderer = GetComponent<MeshRenderer>();
            meshFilter = GetComponent<MeshFilter>();
            mesh = meshFilter.sharedMesh;
        }

        if (!materialInitFlag)
        {
            materialInitFlag = true;
            material.SetDefaults();
        }

        if (meshRenderer != null)
        {
            if (materialObjectID != gameObject.GetInstanceID())
            {
                meshRenderer.sharedMaterial = new Material(meshRenderer.sharedMaterial);
                materialObjectID = gameObject.GetInstanceID();
            }
            meshRenderer.sharedMaterial.color = material.color;
        }
    }
}
