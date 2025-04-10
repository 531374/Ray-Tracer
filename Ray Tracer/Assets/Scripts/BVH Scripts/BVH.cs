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

            AllTriangles.Add(new BVHTriangle(a, b, c));
        }

        //Start recursively splitting the mesh
        Node root = new Node() { bounds = bounds, firstTriangleIndex = 0, triangleCount = AllTriangles.Count};
        AllNodes.Add(root);

        Split(root);
    }

    void Split(Node parent, int depth = 0)
    {
        if (depth == maxDepth) return;

        //Find the best way to split the node
        (int splitAxis, float splitPos) = ChooseSplit(parent, parent.firstTriangleIndex, parent.triangleCount);

        //Create children
        parent.childIndex = AllNodes.Count;
        Node childA = new Node() { firstTriangleIndex = parent.firstTriangleIndex };
        Node childB = new Node() { firstTriangleIndex = parent.firstTriangleIndex };
        AllNodes.Add(childA);
        AllNodes.Add(childB);
        
        //Sort triangles into correct new child
        for (int i = parent.firstTriangleIndex; i < parent.firstTriangleIndex + parent.triangleCount; i++)
        {
            bool isSideA = AllTriangles[i].centre[splitAxis] < splitPos;

            Node child = isSideA ? childA : childB;
            child.bounds.GrowToInclude(AllTriangles[i]);
            child.triangleCount++;

            //Sort the indices of the triangles correctly
            if (isSideA)
            {
                int swap = child.firstTriangleIndex + child.triangleCount - 1;
                (AllTriangles[i], AllTriangles[swap]) = (AllTriangles[swap], AllTriangles[i]);
                childB.firstTriangleIndex++;
            }
        }

        //Split again
        Split(AllNodes[parent.childIndex + 0], depth + 1);
        Split(AllNodes[parent.childIndex + 1], depth + 1);
    }

    //Functions finds the best axis and position to split over
    (int, float) ChooseSplit(Node parent, int start, int count)
    {
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

        return (bestSplitAxis, bestSplitPos);

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

        float costA = NodeCost(boundsLeft.Size, numOnLeft);
        float costB = NodeCost(boundsRight.Size, numOnRight);
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
