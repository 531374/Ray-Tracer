using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct Model 
{
    public int nodeOffset;
    public int triangleOffset;
    public Matrix4x4 worldToLocalMatrix;
    public Matrix4x4 localToWorldMatrix;
    public RayTracingMaterial material;

    public Model(int nodeOffset, int triangleOffset, RayTracingMaterial material)
    {
        this.nodeOffset = nodeOffset;
        this.triangleOffset = triangleOffset;
        this.material = material;
        this.worldToLocalMatrix = Matrix4x4.identity;
        this.localToWorldMatrix = Matrix4x4.identity;
    }

}
