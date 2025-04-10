using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using UnityEngine;
using UnityEngine.UIElements;

public class BVH
{
    public List<Node> allNodes;

    public int maxDepth = 10;

    public BVH(Vector3[] vertices, int[] indices, Vector3 pos, Quaternion rot, Vector3 scale)
    {
        allNodes ??= allNodes = new List<Node>();
        allNodes.Clear();


        BoundingBox bounds = new();

        foreach(Vector3 vert in vertices)
        {
            bounds.GrowToInclude(vert);
        }

        BVHTriangle[] triangles = new BVHTriangle[indices.Length / 3];

        for(int i = 0; i < indices.Length; i+=3)
        {
            Vector3 a = PointLocalToWorld(vertices[indices[i + 0]], pos, rot, scale);
            Vector3 b = PointLocalToWorld(vertices[indices[i + 1]], pos, rot, scale);
            Vector3 c = PointLocalToWorld(vertices[indices[i + 2]], pos, rot, scale);

            triangles[i/3] = new BVHTriangle(a, b, c);
        }

        Node root = new Node() { bounds = bounds, triangles = new List<BVHTriangle>(triangles)};
        allNodes.Add(root);

        Split(root);
    }

    void Split(Node node, int depth = 0)
    {
        if (depth >= maxDepth) return;

        node.childIndex = allNodes.Count;
        allNodes.Add(new());
        allNodes.Add(new());

        float sizeX = node.bounds.Max.x - node.bounds.Min.x;
        float sizeY = node.bounds.Max.y - node.bounds.Min.y;


        foreach(BVHTriangle tri in node.triangles)
        {
            bool inA;

            //Check whether to split vertically or horizontally
            if(sizeX > sizeY)
            {
                inA = tri.centre.x < node.bounds.Centre.x;
            }
            else
            {
                inA = tri.centre.y < node.bounds.Centre.y;
            }

            Node child = inA ? allNodes[node.childIndex] : allNodes[node.childIndex + 1];
            child.triangles.Add(tri);
            child.bounds.GrowToInclude(tri);
        }

        Split(allNodes[node.childIndex + 0], depth + 1);
        Split(allNodes[node.childIndex + 1], depth + 1);
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
