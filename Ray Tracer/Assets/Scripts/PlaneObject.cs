using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class PlaneObject : MonoBehaviour
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



struct Plane
{
    public Vector3 position;

    public Vector3 normal;
    public Vector3 right;
    public Vector3 up;

    public Vector2 size;

    public RayTracingMaterial material;
}
