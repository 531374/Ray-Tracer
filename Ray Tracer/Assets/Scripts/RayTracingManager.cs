using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways, ImageEffectAllowedInSceneView]
public class RayTracingManager : MonoBehaviour
{
    public static int TriangleLimit = 1500;

    [Header("Enable Shaders")]
    [SerializeField] bool useRayTracingInSceneView;
    [SerializeField] bool useAccumulator;

    [Header("Settings")]
    public int MaxBounceCount;
    public int numRaysPerPixel;
    [SerializeField] Color skyColor;

    [Header("References")]
    [SerializeField] Shader rayTracingShader;
    [SerializeField] Shader accumulatorShader;


    Material rayTracingMaterial;
    Material accumulatorMaterial;
    RenderTexture resultTexture;


    ComputeBuffer sphereBuffer;
    ComputeBuffer triangleBuffer;
    ComputeBuffer meshInfoBuffer;

    List<Triangle> allTriangles;
    List<MeshInfo> allMeshInfo;

    [Header("Info")]
    [SerializeField] int numRenderedFrames;
    [SerializeField] int numMeshChunks;
    [SerializeField] int numTriangles;

    public static RayTracingManager instance;

    private void Awake()
    {
        if (instance == null) instance = this;
    }

    private void Start()
    {
        numRenderedFrames = 0;
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (!Application.isPlaying)
        {
            if (useRayTracingInSceneView)
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

            if (!useAccumulator)
            {
                Graphics.Blit(null, destination, rayTracingMaterial);
                return;
            }

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
        CreateMeshes();

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

    

    void CreateMeshes()
    {
        RayTracingMesh[] meshes = FindObjectsOfType<RayTracingMesh>();

        allTriangles ??= new List<Triangle>();
        allMeshInfo ??= new List<MeshInfo>();
        allTriangles.Clear();
        allMeshInfo.Clear();

        for(int i = 0; i < meshes.Length; i++)
        {
            MeshChunk[] chunks = meshes[i].GetSubMeshes();

            foreach(MeshChunk chunk in chunks)
            {
                RayTracingMaterial material = meshes[i].GetMaterial(chunk.subMeshIndex);
                allMeshInfo.Add(new MeshInfo(allTriangles.Count, chunk.triangles.Length, material, chunk.bounds.min, chunk.bounds.max));
                allTriangles.AddRange(chunk.triangles);
            }
        }

        numMeshChunks = allMeshInfo.Count;
        numTriangles = allTriangles.Count;  

        ShaderHelper.CreateStructuredBuffer(ref triangleBuffer, allTriangles);
        ShaderHelper.CreateStructuredBuffer(ref meshInfoBuffer, allMeshInfo);
        rayTracingMaterial.SetBuffer("AllTriangles", triangleBuffer);
        rayTracingMaterial.SetBuffer("AllMeshInfo", meshInfoBuffer);
        rayTracingMaterial.SetInt("numMeshes", allMeshInfo.Count);
        
    }

    private void OnDisable()
    {
        ShaderHelper.Release(sphereBuffer);
        ShaderHelper.Release(triangleBuffer);
        ShaderHelper.Release(meshInfoBuffer);

        ShaderHelper.Release(resultTexture);
    }

    private void OnValidate()
    {
        MaxBounceCount = Mathf.Max(0, MaxBounceCount);
        numRaysPerPixel = Mathf.Max(1, numRaysPerPixel);
    }

}
