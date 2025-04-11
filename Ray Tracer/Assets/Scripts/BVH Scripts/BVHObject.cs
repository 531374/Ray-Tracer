using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BVHObject : MonoBehaviour
{
    public RayTracingMaterial material;
    public BVH bvh;

    [SerializeField, HideInInspector] int materialObjectID;
    [SerializeField, HideInInspector] bool materialInitFlag;

    //References
    Mesh mesh;
    MeshRenderer meshRenderer;
    MeshFilter meshFilter;

    //Check for changes
    Vector3 lastPosition;
    Quaternion lastRotation;
    Vector3 lastScale;

    void OnValidate()
    {
        if(mesh == null || meshRenderer == null || meshFilter == null)
        {
            meshRenderer = GetComponent<MeshRenderer>();
            meshFilter = GetComponent<MeshFilter>();
            mesh = meshFilter.sharedMesh;
        }

        if(bvh == null) bvh = new BVH(mesh.vertices, mesh.triangles, mesh.normals, transform.position, transform.rotation, transform.lossyScale);

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

        if(transform.position != lastPosition || transform.rotation != lastRotation || transform.lossyScale != lastScale)
        {
            bvh = new BVH(mesh.vertices, mesh.triangles, mesh.normals, transform.position, transform.rotation, transform.lossyScale);
            lastPosition = transform.position;
            lastRotation = transform.rotation;
            lastScale = transform.lossyScale;
        }

    }
}
