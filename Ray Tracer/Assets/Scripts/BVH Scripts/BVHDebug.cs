using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

//This class basically takes the work usually done for every pixel on the GPU
//And does it on the CPU for one ray for easy debugging and visualizing
public class BVHDebug : MonoBehaviour
{
    BVH bvh;
    public Transform rayT;

    Mesh mesh;
    public int debugDepth = 0;

    private void OnDrawGizmos()
    {
        if(bvh == null)
        {
            if(mesh == null) mesh = GetComponent<MeshFilter>().sharedMesh;
            bvh = new(mesh.vertices, mesh.triangles, transform.position, transform.rotation, transform.lossyScale);
        }

        Gizmos.color = Color.red;
        BVHResult result = RayTriangleTestBVH(bvh.AllNodes[0], new Ray(rayT.position, rayT.forward), new BVHResult());

        if (result.didHit)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawCube(result.node.bounds.CalculateCentre(), result.node.bounds.Max - result.node.bounds.Min);
        }

        Gizmos.color = Color.white;
        Gizmos.DrawLine(rayT.position, rayT.position + rayT.forward * 15f);

        //DrawNodes(bvh.AllNodes[0]);

    }

    //AI generated function for debugging the BVH
    void DrawNodes(Node node, int depth = 0)
    {
        if (depth > debugDepth) return;

        // Color based on depth, more visually distinct
        Color col = Color.HSVToRGB((depth * 0.13f) % 1f, 0.8f, 1f);
        col.a = depth == debugDepth ? 0.5f : 0.1f;
        Gizmos.color = col;

        Vector3 center = node.bounds.CalculateCentre();
        Vector3 size = node.bounds.Max - node.bounds.Min;

        // Slight size boost for visibility
        Vector3 paddedSize = size + Vector3.one * 0.01f;

        if (depth == debugDepth) Gizmos.DrawCube(center, paddedSize);
        else Gizmos.DrawWireCube(center, paddedSize);

        // Leaf check
        if (node.childIndex == 0)
        {
            // Optional: mark leaves with a small sphere
            Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.4f);
            Gizmos.DrawSphere(center, 0.02f);
            return;
        }

        // Recurse into children
        DrawNodes(bvh.AllNodes[node.childIndex + 0], depth + 1);
        DrawNodes(bvh.AllNodes[node.childIndex + 1], depth + 1);
    }

    bool RayBoundingBox(Ray ray, BoundingBox bounds)
    {
        Vector3 invDir = new Vector3(1f/ray.direction.x, 1f/ray.direction.y, 1f/ray.direction.z);
        Vector3 tMin = Vector3.Scale(bounds.Min - ray.origin, invDir);

        Vector3 tMax = Vector3.Scale(bounds.Max - ray.origin, invDir);
        Vector3 t1 = Vector3.Min(tMin, tMax);
        Vector3 t2 = Vector3.Max(tMin, tMax);
        float tNear = Mathf.Max(Mathf.Max(t1.x, t1.y), t1.z);
        float tFar = Mathf.Min(Mathf.Min(t2.x, t2.y), t2.z);
        return tNear <= tFar;
    }

    HitInfo HitTriangle(Ray ray, BVHTriangle tri)
    {
        Vector3 edgeAB = tri.posB - tri.posA;
        Vector3 edgeAC = tri.posC - tri.posA;
        Vector3 normalVector = Vector3.Cross(edgeAB, edgeAC);
        Vector3 ao = ray.origin - tri.posA;
        Vector3 dao = Vector3.Cross(ao, ray.direction);

        float determinant = -Vector3.Dot(ray.direction, normalVector);
        float invDet = 1.0f / determinant;

        float dst = Vector3.Dot(ao, normalVector) * invDet;
        float u = Vector3.Dot(edgeAC, dao) * invDet;
        float v = -Vector3.Dot(edgeAB, dao) * invDet;
        float w = 1 - u - v;

        HitInfo hitInfo;
        hitInfo.didHit = determinant >= 1e-6 && dst >= 0 && u >= 0 && v >= 0 && w >= 0;
        hitInfo.hitPoint = ray.origin + ray.direction * dst;
        //hitInfo.normal = Vector3.Normalize(tri.normalA * w + tri.normalB * u + tri.normalC * v);
        hitInfo.distance = dst;
        return hitInfo;
    }

    BVHResult RayTriangleTestBVH(Node node, Ray ray, BVHResult state)
    {
        bool boundsHit = RayBoundingBox(ray, node.bounds);

        if (boundsHit)
        {
            Gizmos.color *= 0.8f;
            Gizmos.DrawWireCube(node.bounds.CalculateCentre(), node.bounds.Max - node.bounds.Min);

            //Leaf node
            int childIndex = node.childIndex;
            if (childIndex == 0)
            {
                for(int i = node.firstTriangleIndex; i < node.firstTriangleIndex + node.triangleCount; i++)
                {
                    BVHTriangle tri = bvh.AllTriangles[i];

                    HitInfo hitInfo = HitTriangle(ray, tri);
                    if (hitInfo.didHit && hitInfo.distance < state.closestDistance)
                    {
                        state.closestDistance = hitInfo.distance;
                        state.node = node;
                        state.triangle = tri;
                    }
                }
            }
            else
            {
                state = RayTriangleTestBVH(bvh.AllNodes[childIndex + 0], ray, state);
                state = RayTriangleTestBVH(bvh.AllNodes[childIndex + 1], ray, state);
            }
        }

        return state;
    }

    private void OnValidate()
    {
        bvh = new(mesh.vertices, mesh.triangles, transform.position, transform.rotation, transform.lossyScale);
    }
}

public struct BVHResult
{
    public bool didHit;
    public float closestDistance;
    public Node node;
    public BVHTriangle triangle;
}

struct HitInfo
{
    public bool didHit;
    public float distance;
    public Vector3 hitPoint;
    //public Vector3 normal;
}
