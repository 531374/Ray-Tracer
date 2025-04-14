using UnityEditor.Profiling.Memory.Experimental;
using UnityEngine;

//This class basically takes the work usually done for every pixel on the GPU
//And does it on the CPU for one ray for easy debugging and visualizing
public class BVHDebug : MonoBehaviour
{
    [SerializeField] BVHObject obj;
    BVH bvh;

    Mesh mesh;

    [Header("Debug Settings")]
    [SerializeField] bool enableDebugBVHRay = false;
    [SerializeField] bool enableDebugBVHNodes = false;
    [SerializeField] int bvhDebugDepth = 0;

    [Header("References")]
    [SerializeField] Transform rayT;

    private void OnDrawGizmos()
    {
        if (bvh == null)
        {
            if (mesh == null) mesh = GetComponent<MeshFilter>().sharedMesh;
            bvh = new BVH(mesh.vertices, mesh.triangles, mesh.normals);
        }

        if (enableDebugBVHRay)
        {
            if (rayT == null) return;
            Gizmos.color = Color.red;
            BVHResult result = RayTriangleTestBVH(bvh.AllNodes[0], new Ray(rayT.position, rayT.forward), new BVHResult());

            if (result.didHit)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawCube(result.node.CalculateCentre(), result.node.boundsMax - result.node.boundsMin);
            }

            Gizmos.color = Color.white;
            Gizmos.DrawLine(rayT.position, rayT.position + rayT.forward * (result.didHit ? result.closestDistance : 10f));
        }

        if (enableDebugBVHNodes)
        {
            DrawNodes(bvh.AllNodes[0]);
        }

    }

    //AI generated function for debugging the BVH
    void DrawNodes(Node node, int depth = 0)
    {

        // Color based on depth, more visually distinct
        Color col = Color.HSVToRGB((depth * 0.13f) % 1f, 0.8f, 1f);
        Gizmos.color = col;

        //Get bounds properties
        Vector3 center = node.CalculateCentre();
        Vector3 size = node.CalculateSize();

        //Draw bounding boxes of debug depth
        Gizmos.color = col;
        
        if(depth == bvhDebugDepth)
        {
            Gizmos.DrawCube(center, size);
        }
        else
        {
            Gizmos.DrawWireCube(center, size);
        }


        if (node.childIndex == 0 || depth + 1 > bvhDebugDepth) return;

        // Recurse into children
        DrawNodes(bvh.AllNodes[node.childIndex + 0], depth + 1);
        DrawNodes(bvh.AllNodes[node.childIndex + 1], depth + 1);
    }

    bool RayBoundingBox(Ray ray, Vector3 boundsMin, Vector3 boundsMax)
    {
        Vector3 invDir = new Vector3(1f / ray.direction.x, 1f / ray.direction.y, 1f / ray.direction.z);
        Vector3 tMin = Vector3.Scale(boundsMin - ray.origin, invDir);

        Vector3 tMax = Vector3.Scale(boundsMax - ray.origin, invDir);
        Vector3 t1 = Vector3.Min(tMin, tMax);
        Vector3 t2 = Vector3.Max(tMin, tMax);
        float tNear = Mathf.Max(Mathf.Max(t1.x, t1.y), t1.z);
        float tFar = Mathf.Min(Mathf.Min(t2.x, t2.y), t2.z);
        return tNear <= tFar;
    }

    HitInfo HitTriangle(Ray ray, Triangle tri)
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
        bool boundsHit = RayBoundingBox(ray, node.boundsMin, node.boundsMax);

        if (boundsHit)
        {
            Gizmos.color *= 0.8f;
            Gizmos.DrawWireCube(node.CalculateCentre(), node.CalculateSize());

            //Leaf node
            int childIndex = node.childIndex;
            if (childIndex == 0)
            {
                for (int i = node.firstTriangleIndex; i < node.firstTriangleIndex + node.triangleCount; i++)
                {
                    Triangle tri = bvh.AllTriangles[i];

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
        //if (transform.position != lastPosition || transform.rotation != lastRotation || transform.lossyScale != lastScale)
        //{
        //    if (mesh == null) mesh = GetComponent<MeshFilter>().sharedMesh;
        //    //bvh = new BVH(mesh.vertices, mesh.triangles, transform.position, transform.rotation, transform.lossyScale);
        //    bvh = GetComponent<BVHObject>().bvh;
        //    lastPosition = transform.position;
        //    lastRotation = transform.rotation;
        //    lastScale = transform.lossyScale;
        //}

        //bvh = GetComponent<BVHObject>().bvh;
    }
}

public struct BVHResult
{
    public bool didHit;
    public float closestDistance;
    public Node node;
    public Triangle triangle;
}

struct HitInfo
{
    public bool didHit;
    public float distance;
    public Vector3 hitPoint;
    //public Vector3 normal;
}
