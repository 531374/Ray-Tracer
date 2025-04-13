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

    public Triangle[] AllTriangles;
    public BVHTriangle[] BuildTriangles;

    //Construct the bvh
    public BVH(Vector3[] vertices, int[] indices, Vector3[] normals, Vector3 pos, Quaternion rot, Vector3 scale)
    {
        AllNodes = new List<Node>();


        BuildTriangles = new BVHTriangle[indices.Length / 3];
        BoundingBox bounds = new();

        for (int i = 0; i < indices.Length; i += 3)
        {   
            Vector3 a = vertices[indices[i + 0]];
            Vector3 b = vertices[indices[i + 1]];
            Vector3 c = vertices[indices[i + 2]];

            Vector3 centre = (a + b + c) / 3f;

            Vector3 max = Vector3.Max(a, Vector3.Max(b, c));
            Vector3 min = Vector3.Min(a, Vector3.Min(b, c));

            BuildTriangles[i / 3] = new BVHTriangle(centre, min, max, i);
            bounds.GrowToInclude(min, max);
        }


        //Start recursively splitting the mesh
        Node root = new Node(bounds);
        AllNodes.Add(root);

        Split(0, 0, BuildTriangles.Length);


        AllTriangles = new Triangle[BuildTriangles.Length];

        for(int i = 0; i < AllTriangles.Length; i++)
        {
            BVHTriangle buildTriangle = BuildTriangles[i];

            Vector3 a = vertices[indices[buildTriangle.Index + 0]];
            Vector3 b = vertices[indices[buildTriangle.Index + 1]];
            Vector3 c = vertices[indices[buildTriangle.Index + 2]];

            Vector3 normalA = normals[indices[buildTriangle.Index + 0]];
            Vector3 normalB = normals[indices[buildTriangle.Index + 1]];
            Vector3 normalC = normals[indices[buildTriangle.Index + 2]];

            AllTriangles[i] = new Triangle(a, b, c, normalA, normalB, normalC);
        }      
    }

    void Split(int parentIndex, int start, int numTris, int depth = 0)
    {
        Node parent = AllNodes[parentIndex];
        Vector3 size = parent.CalculateSize();
        const int maxDepth = 32;

        float parentCost = NodeCost(size, numTris);

        //Find the best way to split the node
        (int splitAxis, float splitPos, float bestCost) = ChooseSplit(parent, start, numTris);

        if (bestCost < parentCost && depth < maxDepth)
        {
            BoundingBox boundsA = new();
            BoundingBox boundsB = new();
            int numTrisA = 0;

            for(int i = start; i < start + numTris; i++)
            {
                BVHTriangle triangle = BuildTriangles[i];
                if(triangle.Centre[splitAxis] < splitPos)
                {
                    boundsA.GrowToInclude(triangle.Min, triangle.Max);

                    BVHTriangle swap = BuildTriangles[start + numTrisA];
                    BuildTriangles[start + numTrisA] = triangle;
                    BuildTriangles[i] = swap;

                    numTrisA++;
                }
                else
                {
                    boundsB.GrowToInclude(triangle.Min, triangle.Max);
                }
            }

            int numTrisB = numTris - numTrisA;

            int firstTriangleA = start;
            int firstTriangleB = start + numTrisA;

            int childIndex = AllNodes.Count;

            Node childA = new Node(boundsA, firstTriangleA, 0);
            Node childB = new Node(boundsB, firstTriangleB, 0);

            AllNodes.Add(childA);
            AllNodes.Add(childB);

            parent.childIndex = childIndex;
            parent.firstTriangleIndex = start;

            AllNodes[parentIndex] = parent;

            Split(childIndex + 0, firstTriangleA, numTrisA, depth + 1);
            Split(childIndex + 1, firstTriangleB, numTrisB, depth + 1);
        }
        else
        {
            parent.childIndex = 0;
            parent.triangleCount = numTris;
            AllNodes[parentIndex] = parent;
        }
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
                float splitPos = Mathf.Lerp(parent.boundsMin[axis], parent.boundsMax[axis], splitT);
                float cost = EvaluateSplit(axis, splitPos, start, count);

                if (cost < bestCost)
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
            BVHTriangle tri = BuildTriangles[i];
            if (tri.Centre[splitAxis] < splitPos)
            {
                boundsLeft.GrowToInclude(tri.Min, tri.Max);
                numOnLeft++;
            }
            else
            {
                boundsRight.GrowToInclude(tri.Min, tri.Max);
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
        return Vector3.Normalize(rotation * direction);
    }
}
