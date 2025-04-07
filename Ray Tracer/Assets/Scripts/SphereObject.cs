using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

[ExecuteAlways]
public class SphereObject : MonoBehaviour
{
    public RayTracingMaterial material;
    public Color color;
    public Color emissionColor;
    public float emissionStrength;

    private void OnValidate()
    {
        material.SetDefaults();
        material.color = color;
        material.emissionColor = emissionColor;
        material.emissionStrength = emissionStrength;

        GetComponent<MeshRenderer>().sharedMaterial.color = color;
    }
}

public struct Sphere
{
    public Vector3 position;
    public float radius;
    public RayTracingMaterial material;
}
