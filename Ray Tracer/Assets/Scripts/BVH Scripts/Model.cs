using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct Model 
{
    public int nodeOffset;
    public int triangleOffset;
    public RayTracingMaterial material;

    public Model(int nodeOffset, int triangleOffset, RayTracingMaterial material)
    {
        this.nodeOffset = nodeOffset;
        this.triangleOffset = triangleOffset;
        this.material = material;
    }

}
