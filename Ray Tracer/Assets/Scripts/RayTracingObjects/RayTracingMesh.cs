using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

public class RayTracingMesh : MonoBehaviour
{
    public RayTracingMaterial[] materials;


    public MeshRenderer meshRenderer;
    public MeshFilter meshFilter;
    public int triangleCount;

    [SerializeField, HideInInspector] int materialObjectID;
    [SerializeField] Mesh mesh;
    [SerializeField] MeshChunk[] localChunks;
    MeshChunk[] worldChunks;

    public MeshChunk[] GetSubMeshes()
    {
        //if (mesh.triangles.Length / 3 > RayTracingManager.TriangleLimit)
        //{
        //    throw new System.Exception($"Too many triangles :(");
        //}

        if (meshFilter != null && (mesh != meshFilter.sharedMesh || localChunks == null || localChunks.Length == 0))
        {
            mesh = meshFilter.sharedMesh;
            localChunks = CreateAllMeshChunks(mesh);
        }

        if (worldChunks == null || worldChunks.Length != localChunks.Length)
        {
            worldChunks = new MeshChunk[localChunks.Length];
        }

        Vector3 pos = transform.position;
        Quaternion rot = transform.rotation;
        Vector3 scale = transform.lossyScale;

        for (int i = 0; i < worldChunks.Length; i++)
        {
            MeshChunk localChunk = localChunks[i];

            if (worldChunks[i] == null || worldChunks[i].triangles.Length != localChunk.triangles.Length)
            {
                worldChunks[i] = new MeshChunk(new Triangle[localChunk.triangles.Length], localChunk.bounds, localChunk.subMeshIndex);
            }

            UpdateWorldChunkFromLocalChunk(worldChunks[i], localChunk, pos, rot, scale);
        }

        return worldChunks;
    }

    void UpdateWorldChunkFromLocalChunk(MeshChunk worldChunk, MeshChunk localChunk, Vector3 pos, Quaternion rot, Vector3 scale)
    {
        Triangle[] localTriangles = localChunk.triangles;

        Vector3 boundsMin = PointLocalToWorld(localTriangles[0].posA, pos, rot, scale);
        Vector3 boundsMax = boundsMin;

        for (int i = 0; i < localTriangles.Length; i++)
        {
            //Transform triangle from local to world space
            Vector3 worldA = PointLocalToWorld(localTriangles[i].posA, pos, rot, scale);
            Vector3 worldB = PointLocalToWorld(localTriangles[i].posB, pos, rot, scale);
            Vector3 worldC = PointLocalToWorld(localTriangles[i].posC, pos, rot, scale);

            Vector3 worldNormalA = DirectionLocalToWorld(localTriangles[i].normalA, rot);
            Vector3 worldNormalB = DirectionLocalToWorld(localTriangles[i].normalB, rot);
            Vector3 worldNormalC = DirectionLocalToWorld(localTriangles[i].normalC, rot);

            //Add new triangle to chunk
            Triangle worldTriangle = new Triangle(worldA, worldB, worldC, worldNormalA, worldNormalB, worldNormalC);
            worldChunk.triangles[i] = worldTriangle;

            //Set the bounds
            boundsMin = Vector3.Min(boundsMin, worldA);
            boundsMax = Vector3.Max(boundsMax, worldA);
            boundsMin = Vector3.Min(boundsMin, worldB);
            boundsMax = Vector3.Max(boundsMax, worldB);
            boundsMin = Vector3.Min(boundsMin, worldC);
            boundsMax = Vector3.Max(boundsMax, worldC);
        }

        worldChunk.bounds = new Bounds((boundsMin + boundsMax) / 2, boundsMax - boundsMin);
        worldChunk.subMeshIndex = localChunk.subMeshIndex;
    }

    MeshChunk CreateMeshChunk(Vector3[] vertices, Vector3[] normals, int[] indices, int subMeshIndex)
    {
        Triangle[] triangles = new Triangle[indices.Length / 3];
        Bounds bounds = new Bounds(vertices[indices[0]], Vector3.one * 0.001f);


        for (int i = 0; i < indices.Length; i += 3)
        {
            int a = indices[i];
            int b = indices[i + 1];
            int c = indices[i + 2];

            Vector3 posA = vertices[a];
            Vector3 posB = vertices[b];
            Vector3 posC = vertices[c];

            bounds.Encapsulate(posA);
            bounds.Encapsulate(posB);
            bounds.Encapsulate(posC);

            Vector3 normalA = normals[a];
            Vector3 normalB = normals[b];
            Vector3 normalC = normals[c];

            Triangle triangle = new Triangle(posA, posB, posC, normalA, normalB, normalC);
            triangles[i / 3] = triangle;
        }

        return new MeshChunk(triangles, bounds, subMeshIndex);

    }

    MeshChunk[] CreateAllMeshChunks(Mesh mesh)
    {
        MeshChunk[] allChunks = new MeshChunk[mesh.subMeshCount];

        for (int i = 0; i < mesh.subMeshCount; i++)
        {
            allChunks[i] = CreateMeshChunk(mesh.vertices, mesh.normals, mesh.GetTriangles(i), i);
        }

        return allChunks;
    }

    static Vector3 PointLocalToWorld(Vector3 point, Vector3 position, Quaternion rotation, Vector3 scale)
    {
        return rotation * Vector3.Scale(point, scale) + position;
    }

    static Vector3 DirectionLocalToWorld(Vector3 direction, Quaternion rotation)
    {
        return rotation * direction;
    }

    public RayTracingMaterial GetMaterial(int subMeshIndex)
    {
        return materials[Mathf.Min(subMeshIndex, materials.Length - 1)];
    }

    void OnValidate()
    {

        if (materials == null ||  materials.Length == 0)
        {
            materials = new RayTracingMaterial[1];
            materials[0].SetDefaults(); 
        }

        if (meshRenderer == null || meshFilter == null)
        {
            meshRenderer = GetComponent<MeshRenderer>();
            meshFilter = GetComponent<MeshFilter>();
        }


        SetUpMaterialDisplay();
        triangleCount = meshFilter.sharedMesh.triangles.Length / 3;
    }

    void SetUpMaterialDisplay()
    {
        if (gameObject.GetInstanceID() != materialObjectID)
        {
            materialObjectID = gameObject.GetInstanceID();
            Material[] originalMaterials = meshRenderer.sharedMaterials;
            Material[] newMaterials = new Material[originalMaterials.Length];
            Shader shader = Shader.Find("Standard");
            for (int i = 0; i < meshRenderer.sharedMaterials.Length; i++)
            {
                newMaterials[i] = new Material(shader);
            }
            meshRenderer.sharedMaterials = newMaterials;
        }

        for (int i = 0; i < meshRenderer.sharedMaterials.Length; i++)
        {
            RayTracingMaterial mat = materials[Mathf.Min(i, materials.Length-1)];
            bool displayEmissiveCol = mat.color.maxColorComponent < mat.emissionColor.maxColorComponent * mat.emissionStrength;
            Color displayCol = displayEmissiveCol ? mat.emissionColor * mat.emissionStrength : mat.color;
            meshRenderer.sharedMaterials[i].color = displayCol;
        }
    }

}
[System.Serializable]
public struct Triangle
{
    public Vector3 posA, posB, posC;
    public Vector3 normalA, normalB, normalC;

    public Triangle(Vector3 posA, Vector3 posB, Vector3 posC, Vector3 normalA, Vector3 normalB, Vector3 normalC)
    {
        this.posA = posA;
        this.posB = posB;
        this.posC = posC;

        this.normalA = normalA;
        this.normalB = normalB;
        this.normalC = normalC;
    }
}
