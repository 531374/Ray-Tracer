using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways, ImageEffectAllowedInSceneView]
public class RayTracingManager : MonoBehaviour
{
    [SerializeField] bool useShaderInSceneView;
    [SerializeField] Shader rayTracingShader;
    Material rayTracingMaterial;

    [SerializeField, Range(1, 10)] int MaxBounceCount;

    ComputeBuffer sphereBuffer;
    RenderTexture resultTexture;

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        bool isSceneCam = Camera.current.name == "SceneCamera";

        if (isSceneCam)
        {
            if (useShaderInSceneView)
            {
                InitializeFrame();
                Graphics.Blit(null, destination, rayTracingMaterial);
            }
            else
            {
                Graphics.Blit(source, destination);
            }
        }
        else
        {
            InitializeFrame();
            Graphics.Blit(null, destination, rayTracingMaterial);


            //RenderTexture previousFrameCopy = RenderTexture.GetTemporary(source.width, source.height, 0, ShaderHelper.RGBA_SFloat);
            //Graphics.Blit(resultTexture, previousFrameCopy);

            //RenderTexture currentFrame = RenderTexture.GetTemporary(source.width, source.height, 0, ShaderHelper.RGBA_SFloat);

            //Graphics.Blit(null, resultTexture, rayTracingMaterial);

            //Graphics.Blit(resultTexture, destination);

            //RenderTexture.ReleaseTemporary(currentFrame);
            //RenderTexture.ReleaseTemporary(previousFrameCopy);
        }
    }

    void InitializeFrame()
    {
        ShaderHelper.InitMaterial(rayTracingShader, ref rayTracingMaterial);

        ShaderHelper.CreateRenderTexture(ref resultTexture, Screen.width, Screen.height, FilterMode.Bilinear, ShaderHelper.RGBA_SFloat, "Result");

        UpdateCameraParams(Camera.current);
        CreateSpheres();
        UpdateShaderParams();
    }

    void UpdateCameraParams(Camera cam)
    {
        float planeHeight = cam.nearClipPlane * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
        float planeWidth = planeHeight * cam.aspect;

        rayTracingMaterial.SetVector("ViewParams", new Vector3(planeWidth, planeHeight, cam.nearClipPlane));
        rayTracingMaterial.SetMatrix("CamLocalToWorldMatrix", cam.transform.localToWorldMatrix);
    }

    void UpdateShaderParams()
    {
        rayTracingMaterial.SetInt("MaxBounceCount", MaxBounceCount);
    }

    void CreateSpheres()
    {
        SphereObject[] sphereObjects = FindObjectsOfType<SphereObject>();
        Sphere[] spheres = new Sphere[sphereObjects.Length];

        for (int i = 0; i < sphereObjects.Length; i++)
        {
            spheres[i] = new Sphere()
            {
                position = sphereObjects[i].transform.position,
                radius = sphereObjects[i].transform.localScale.x * 0.5f,
                material = sphereObjects[i].material
            };
        }

        ShaderHelper.CreateStructuredBuffer(ref sphereBuffer, spheres);
        rayTracingMaterial.SetBuffer("Spheres", sphereBuffer);
        rayTracingMaterial.SetInt("numSpheres", sphereBuffer.count);
    }

    private void OnDisable()
    {
        ShaderHelper.Release(sphereBuffer);
        ShaderHelper.Release(resultTexture);
    }

}
