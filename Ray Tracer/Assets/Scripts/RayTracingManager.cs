using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways, ImageEffectAllowedInSceneView]
public class RayTracingManager : MonoBehaviour
{
    [SerializeField] bool useShaderInSceneView;
    [SerializeField] Shader rayTracingShader;
    [SerializeField] Shader accumulatorShader;

    Material rayTracingMaterial;
    Material accumulatorMaterial;

    [SerializeField] int MaxBounceCount;
    [SerializeField] int numRaysPerPixel;

    [SerializeField] Color skyColor;

    ComputeBuffer sphereBuffer;
    ComputeBuffer planeBuffer;

    RenderTexture resultTexture;

    int numRenderedFrames;

    private void Start()
    {
        numRenderedFrames = 0;
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (!Application.isPlaying)
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

            //Create copy of the previous rendered frame
            RenderTexture previousFrameCopy = RenderTexture.GetTemporary(source.width, source.height, 0, ShaderHelper.RGBA_SFloat);
            Graphics.Blit(resultTexture, previousFrameCopy);

            //Run the ray tracer and save result to a texture
            rayTracingMaterial.SetInt("Frame", numRenderedFrames);
            RenderTexture currentFrame = RenderTexture.GetTemporary(source.width, source.height, 0, ShaderHelper.RGBA_SFloat);
            Graphics.Blit(null, currentFrame, rayTracingMaterial);

            //Accumulate frames
            accumulatorMaterial.SetInt("Frame", numRenderedFrames);
            accumulatorMaterial.SetTexture("_MainTexOld", previousFrameCopy);
            Graphics.Blit(currentFrame, resultTexture, accumulatorMaterial);

            //Draw to the screen
            Graphics.Blit(resultTexture, destination);

            //Release the temporary buffers
            RenderTexture.ReleaseTemporary(currentFrame);
            RenderTexture.ReleaseTemporary(previousFrameCopy);

            numRenderedFrames++;
        }
    }

    void InitializeFrame()
    {
        ShaderHelper.InitMaterial(rayTracingShader, ref rayTracingMaterial);
        ShaderHelper.InitMaterial(accumulatorShader, ref accumulatorMaterial);

        ShaderHelper.CreateRenderTexture(ref resultTexture, Screen.width, Screen.height, FilterMode.Bilinear, ShaderHelper.RGBA_SFloat, "Result");

        CreateSpheres();
        CreatePlanes();

        UpdateCameraParams(Camera.current);
        UpdateShaderParams();
    }

    void UpdateCameraParams(Camera cam)
    {
        float planeHeight = cam.nearClipPlane * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad) * 2f;
        float planeWidth = planeHeight * cam.aspect;

        rayTracingMaterial.SetVector("ViewParams", new Vector3(planeWidth, planeHeight, cam.nearClipPlane));
        rayTracingMaterial.SetMatrix("CamLocalToWorldMatrix", cam.transform.localToWorldMatrix);
        rayTracingMaterial.SetColor("skyColor", skyColor);
    }

    void UpdateShaderParams()
    {
        rayTracingMaterial.SetInt("MaxBounceCount", MaxBounceCount);
        rayTracingMaterial.SetInt("NumRaysPerPixel", numRaysPerPixel);
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

    void CreatePlanes()
    {
        PlaneObject[] planeObjects = FindObjectsOfType<PlaneObject>();
        Plane[] planes = new Plane[planeObjects.Length];

        for(int i = 0; i < planeObjects.Length; i++)
        {
            planes[i] = new Plane()
            {
                position = planeObjects[i].transform.position,

                normal = planeObjects[i].transform.up.normalized,
                right = planeObjects[i].transform.right.normalized,
                up = planeObjects[i].transform.forward.normalized,

                halfSize = new Vector2(planeObjects[i].transform.localScale.x, planeObjects[i].transform.localScale.z) * 5f,

                material = planeObjects[i].material
            };
        }

        ShaderHelper.CreateStructuredBuffer(ref planeBuffer, planes);
        rayTracingMaterial.SetBuffer("Planes", planeBuffer);
        rayTracingMaterial.SetInt("numPlanes", planeBuffer.count);
    }
    private void OnDisable()
    {
        ShaderHelper.Release(sphereBuffer);
        ShaderHelper.Release(planeBuffer);

        ShaderHelper.Release(resultTexture);
    }

    private void OnValidate()
    {
        MaxBounceCount = Mathf.Max(0, MaxBounceCount);
        numRaysPerPixel = Mathf.Max(1, numRaysPerPixel);
    }

}
