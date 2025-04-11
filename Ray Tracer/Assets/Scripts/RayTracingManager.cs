using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways, ImageEffectAllowedInSceneView]
public class RayTracingManager : MonoBehaviour
{
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


    ComputeBuffer triangleBuffer;
    ComputeBuffer nodeBuffer;
    ComputeBuffer modelBuffer;

    List<BVHTriangle> allTriangles;
    List<Node> allNodes;
    List<Model> allModels;

    [Header("Info")]
    [SerializeField] int numRenderedFrames;
    [SerializeField] int numMeshChunks;
    [SerializeField] int numTriangles;

    [HideInInspector] public int debugVisRays = 2000;
    [HideInInspector] public bool enableDebugVisRays = false;

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
                rayTracingMaterial.SetInt("Frame", numRenderedFrames);
                numRenderedFrames++;
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
   

    void CreateMeshes()
    {
        BVHObject[] bvhObjects = FindObjectsOfType<BVHObject>();


        allNodes ??= allNodes = new List<Node>();
        allTriangles ??= allTriangles = new List<BVHTriangle>();
        allModels ??= allModels = new List<Model>();

        allNodes.Clear();
        allTriangles.Clear();
        allModels.Clear();


        for(int i = 0; i < bvhObjects.Length; i++)
        {
            BVHObject obj = bvhObjects[i];
            BVH bvh = obj.bvh;
            if (obj == null || bvh == null) continue;

            Model model = new Model(allNodes.Count, allTriangles.Count, obj.material);

            allNodes.AddRange(bvh.AllNodes);
            allTriangles.AddRange(bvh.AllTriangles);
            allModels.Add(model);

        }

        ShaderHelper.CreateStructuredBuffer(ref nodeBuffer, allNodes);
        ShaderHelper.CreateStructuredBuffer(ref triangleBuffer, allTriangles);
        ShaderHelper.CreateStructuredBuffer(ref modelBuffer, allModels);

        rayTracingMaterial.SetBuffer("Nodes", nodeBuffer);
        rayTracingMaterial.SetBuffer("Triangles", triangleBuffer);
        rayTracingMaterial.SetBuffer("Models", modelBuffer);
        rayTracingMaterial.SetInt("numModels", allModels.Count);

        rayTracingMaterial.SetFloat("debugVisRays", debugVisRays);
        rayTracingMaterial.SetInt("enableDebugVisRays", enableDebugVisRays ? 1 : 0);
    }

    private void OnDisable()
    {
        ShaderHelper.Release(triangleBuffer);
        ShaderHelper.Release(nodeBuffer);
        ShaderHelper.Release(modelBuffer);

        ShaderHelper.Release(resultTexture);
    }

    private void OnValidate()
    {
        MaxBounceCount = Mathf.Max(0, MaxBounceCount);
        numRaysPerPixel = Mathf.Max(1, numRaysPerPixel);
    }

}
