using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class SphereObject : MonoBehaviour
{
    public RayTracingMaterial material;

	[SerializeField, HideInInspector] int materialObjectID;
	[SerializeField, HideInInspector] bool materialInitFlag;

	void OnValidate()
	{
		if (!materialInitFlag)
		{
			materialInitFlag = true;
			material.SetDefaults();
		}

		MeshRenderer renderer = GetComponent<MeshRenderer>();
		if (renderer != null)
		{
			if (materialObjectID != gameObject.GetInstanceID())
			{
				renderer.sharedMaterial = new Material(renderer.sharedMaterial);
				materialObjectID = gameObject.GetInstanceID();
			}
			renderer.sharedMaterial.color = material.color;
		}
	}
}

public struct Sphere
{
    public Vector3 position;
    public float radius;
    public RayTracingMaterial material;
}
