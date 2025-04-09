using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct RayTracingMaterial
{
    public Color color;
    public Color emissionColor;
    public float emissionStrength;
    [Range(0, 1f)] public float smoothness;
    [Range(0, 1f)] public float specularProbability;
    
    public void SetDefaults()
    {
        color = Color.white;
        emissionColor = Color.black;
        emissionStrength = 0f;
        smoothness = 0f;
        specularProbability = 1f;
    }
}
