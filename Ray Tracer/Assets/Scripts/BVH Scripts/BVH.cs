using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UIElements;

//A BVH or Bounding Volume Hierarchy is a structure used to optimize ray tracing performance
//It does this by dividing the mesh into bounding boxes, a triangle does not need to hittest if ray does not hit box
public class BVH
{
    public List<Node> AllNodes;
    public List<BVHTriangle> AllTriangles;

    public int maxDepth = 10;

    //Construct the bvh
    public BVH(Vector3[] vertices, int[] indices, Vector3 pos, Quaternion rot, Vector3 scale)
    {
        AllNodes ??= AllNodes = new List<Node>();
        AllNodes.Clear();

        AllTriangles ??= AllTriangles = new List<BVHTriangle>();
        AllTriangles.Clear();

        BoundingBox bounds = new();

        //Create initial bounding box
        foreach(Vector3 vert in vertices)
        {
            Vector3 worldVert = PointLocalToWorld(vert, pos, rot, scale);
            bounds.GrowToInclude(worldVert);
        }

        //Add all world space triangles to list
        for(int i = 0; i < indices.Length; i+=3)
        {
            Vector3 a = PointLocalToWorld(vertices[indices[i + 0]], pos, rot, scale);
            Vector3 b = PointLocalToWorld(vertices[indices[i + 1]], pos, rot, scale);
            Vector3 c = PointLocalToWorld(vertices[indices[i + 2]], pos, rot, scale);

            Vector3 normalA = DirectionLocalToWorld(vertices[indices[i + 0]], rot);
            Vector3 normalB = DirectionLocalToWorld(vertices[indices[i + 1]], rot);
            Vector3 normalC = DirectionLocalToWorld(vertices[indices[i + 2]], rot);

            AllTriangles.Add(new BVHTriangle(a, b, c, normalA, normalB, normalC));
        }

        //Start recursively splitting the mesh
        Node root = new Node(bounds, 0, AllTriangles.Count, 0);
        AllNodes.Add(root);

        Split(0, 0, AllTriangles.Count);
    }

    void Split(int parentIndex, int start, int numTris, int depth = 0)
    {
        Node parent = AllNodes[parentIndex];

        //Find the best way to split the node
        (int splitAxis, float splitPos, float bestCost) = ChooseSplit(parent, start, numTris);
        float nodeCost = NodeCost(parent.bounds.CalculateSize(), numTris);

        //Make node a leaf
        if (depth == maxDepth || nodeCost < bestCost)
        {
            //Debug.Log($"Made leaf with {numTris} triangles");
            parent.firstTriangleIndex = start;
            parent.triangleCount = numTris;
            AllNodes[parentIndex] = parent;
            return;
        }

        BoundingBox boundsA = new();
        BoundingBox boundsB = new();

        int numTrisA = 0;
        int numTrisB = 0;
        
        //Sort triangles into correct new child
        for (int i = start; i < start + numTris; i++)
        {
            BVHTriangle tri = AllTriangles[i];
            bool isSideA = tri.centre[splitAxis] < splitPos;

            if (isSideA)
            {
                boundsA.GrowToInclude(tri);
                
                BVHTriangle swap = AllTriangles[parent.firstTriangleIndex + numTrisA];
                AllTriangles[parent.firstTriangleIndex + numTrisA] = tri;
                AllTriangles[i] = swap;

                numTrisA++;
            }
            else
            {
                boundsB.GrowToInclude(tri);
                numTrisB++;
            }
        }

        parent.childIndex = AllNodes.Count;
        AllNodes[parentIndex] = parent;

        AllNodes.Add(new Node(boundsA, start));
        AllNodes.Add(new Node(boundsB, start + numTrisA));

        //Debug.Log(numTrisA + " " + numTrisB);

        //Split again
        Split(parent.childIndex + 0, start, numTrisA, depth + 1);
        Split(parent.childIndex + 1, start + numTrisA, numTrisB, depth + 1);
    }

    //Functions finds the best axis and position to split over
    (int, float, float) ChooseSplit(Node parent, int start, int count)
    {
        if (count <= 1) return (0, 0, float.PositiveInfinity);

        float bestSplitPos = 0;
        int bestSplitAxis = 0;
        const int splitTests = 5;

        float bestCost = float.MaxValue;

        for (int axis = 0; axis < 3; axis++)
        {
            for (int i = 0; i < splitTests; i++)
            {
                //Try different split points and save the best
                float splitT = (i + 1) / (splitTests + 1f);
                float splitPos = Mathf.Lerp(parent.bounds.Min[axis], parent.bounds.Max[axis], splitT);
                float cost = EvaluateSplit(axis, splitPos, start, count);

                if(cost < bestCost)
                {
                    bestCost = cost;
                    bestSplitPos = splitPos;
                    bestSplitAxis = axis;
                }
            }
        }

        return (bestSplitAxis, bestSplitPos, bestCost);

    }

    //Function calculates the cost of splitting at a certain point
    float EvaluateSplit(int splitAxis, float splitPos, int start, int count)
    {
        BoundingBox boundsLeft = new();
        BoundingBox boundsRight = new();
        int numOnLeft = 0;
        int numOnRight = 0;

        for (int i = start; i < start + count; i++)
        {
            BVHTriangle tri = AllTriangles[i];
            if (tri.centre[splitAxis] < splitPos)
            {
                boundsLeft.GrowToInclude(tri);
                numOnLeft++;
            }
            else
            {
                boundsRight.GrowToInclude(tri);
                numOnRight++;
            }
        }

        float costA = NodeCost(boundsLeft.CalculateSize(), numOnLeft);
        float costB = NodeCost(boundsRight.CalculateSize(), numOnRight);
        return costA + costB;

    }

    //Simple function to calculate how good a split is
    static float NodeCost(Vector3 size, int numTriangles)
    {
        float halfArea = size.x * size.y + size.x * size.z + size.y * size.z;
        return halfArea * numTriangles;
    }

    static Vector3 PointLocalToWorld(Vector3 point, Vector3 position, Quaternion rotation, Vector3 scale)
    {
        return rotation * Vector3.Scale(point, scale) + position;
    }

    static Vector3 DirectionLocalToWorld(Vector3 direction, Quaternion rotation)
    {
        return rotation * direction;
    }
}
