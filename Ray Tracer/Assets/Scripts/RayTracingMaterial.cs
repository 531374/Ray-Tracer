using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct RayTracingMaterial
{
    public Color color;
    public Color emissionColor;
    public float emissionStrength;
    
    public void SetDefaults()
    {
        color = Color.white;
        emissionColor = Color.black;
        emissionStrength = 0f;
    }
}
