using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways, ImageEffectAllowedInSceneView]
public class RayTracingManager : MonoBehaviour
{
    [Header("Enable Shaders")]
    [SerializeField] bool useRayTracingInSceneView;
    [SerializeField] bool useAccumulator;

    [Header("Debug")]
    [SerializeField, Range(0, 2)] int debugMode;
    [SerializeField] float boxDebugScale = 100f;
    [SerializeField] float triangleDebugScale = 100f;

    [Header("Settings")]
    public int MaxBounceCount;
    public int numRaysPerPixel;

    [Header("Camera Settings")]
    [SerializeField] float divergeStrength;
    [SerializeField] float defocusStrength;
    [SerializeField] float focusDistance;

    [Header("Environment Settings")]
    [SerializeField] bool enableEnvironment;
    [SerializeField] Color skyHorizonColor;
    [SerializeField] Color groundColor;
    [SerializeField] float sunFocus;

    [Header("References")]
    [SerializeField] Shader rayTracingShader;
    [SerializeField] Shader accumulatorShader;
    [SerializeField] Light sun;


    Material rayTracingMaterial;
    Material accumulatorMaterial;
    RenderTexture resultTexture;


    ComputeBuffer triangleBuffer;
    ComputeBuffer nodeBuffer;
    ComputeBuffer modelBuffer;

    List<Triangle> allTriangles;
    List<Node> allNodes;
    List<Model> allModels;

    List<Transform> objects;

    [Header("Info")]
    [SerializeField] int numRenderedFrames;
    [SerializeField] int numNodes;
    [SerializeField] int numTriangles;

    public static RayTracingManager instance;

    [SerializeField] bool bvhCreated = false;

    private void Awake()
    {
        if (instance == null) instance = this;
        
    }

    private void OnEnable()
    {
        bvhCreated = false;
    }

    private void Start()
    {
        numRenderedFrames = 0;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            numRenderedFrames = 0;
        }

        if (Input.GetKeyDown(KeyCode.S))
        {
            string path = System.IO.Path.Combine(Application.persistentDataPath, "Screenshot.png");
            ScreenCapture.CaptureScreenshot(path);
            Debug.Log("Took screenshot");
        }
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

        if (allModels == null || FindObjectsOfType<BVHObject>().Length != allModels.Count) bvhCreated = false;

        if (!bvhCreated)
        {
            CreateMeshes();
            bvhCreated = true;
        }

        UpdateModels();
        UpdateCameraParams(Camera.main);
        UpdateEnvironmentParams();
        UpdateShaderParams();
    }

    void UpdateCameraParams(Camera cam)
    {
        float planeHeight = cam.nearClipPlane * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad) * 2f;
        float planeWidth = planeHeight * cam.aspect;
        cam.nearClipPlane = focusDistance;

        rayTracingMaterial.SetVector("ViewParams", new Vector3(planeWidth, planeHeight, focusDistance));
        rayTracingMaterial.SetMatrix("CamLocalToWorldMatrix", cam.transform.localToWorldMatrix);

        rayTracingMaterial.SetFloat("divergeStrength", divergeStrength);
        rayTracingMaterial.SetFloat("defocusStrength", defocusStrength);

    }

    void UpdateShaderParams()
    {
        rayTracingMaterial.SetInt("MaxBounceCount", MaxBounceCount);
        rayTracingMaterial.SetInt("NumRaysPerPixel", numRaysPerPixel);

        rayTracingMaterial.SetFloat("boxDebugScale", boxDebugScale);
        rayTracingMaterial.SetFloat("triangleDebugScale", triangleDebugScale);
        rayTracingMaterial.SetInt("debugMode", debugMode);
    }

    void UpdateEnvironmentParams()
    {
        if (sun == null) enableEnvironment = false;

        rayTracingMaterial.SetColor("skyColor", enableEnvironment ? sun.color : Color.black);
        rayTracingMaterial.SetColor("skyHorizonColor", enableEnvironment ? skyHorizonColor : Color.black);
        rayTracingMaterial.SetColor("groundColor", enableEnvironment ? groundColor : Color.black);

        if (sun == null) return;

        rayTracingMaterial.SetVector("sunLightDirection", sun.transform.forward);
        rayTracingMaterial.SetFloat("sunFocus", sunFocus);
        rayTracingMaterial.SetFloat("sunIntensity", sun.intensity);
    }
   

    void CreateMeshes()
    {
        BVHObject[] bvhObjects = FindObjectsOfType<BVHObject>();


        allNodes = new List<Node>();
        allTriangles = new List<Triangle>();
        allModels = new List<Model>();
        objects = new List<Transform>();


        for(int i = 0; i < bvhObjects.Length; i++)
        {
            BVHObject obj = bvhObjects[i];
            Mesh mesh = obj.GetComponent<MeshFilter>().sharedMesh;
            Transform t = obj.transform;

            if (mesh == null) return;

            BVH bvh = new BVH(mesh.vertices, mesh.triangles, mesh.normals);
            Model model = new Model(allNodes.Count, allTriangles.Count, obj.material);

            allNodes.AddRange(bvh.AllNodes);
            allTriangles.AddRange(bvh.AllTriangles);
            allModels.Add(model);
            objects.Add(t);
        }

        ShaderHelper.CreateStructuredBuffer(ref nodeBuffer, allNodes);
        ShaderHelper.CreateStructuredBuffer(ref triangleBuffer, allTriangles);
        ShaderHelper.CreateStructuredBuffer(ref modelBuffer, allModels);

        rayTracingMaterial.SetBuffer("Nodes", nodeBuffer);
        rayTracingMaterial.SetBuffer("Triangles", triangleBuffer);
        rayTracingMaterial.SetBuffer("Models", modelBuffer);
        rayTracingMaterial.SetInt("numModels", allModels.Count);

        numTriangles = allTriangles.Count;
        numNodes = allNodes.Count;

        UpdateModels();
    }

    void UpdateModels()
    {
        for (int i = 0; i < allModels.Count; i++)
        {
            if (objects[i] == null) 
            {
                objects.Remove(objects[i]);
                allModels.Remove(allModels[i]);
                return;
            }
            Model model = allModels[i];
            model.worldToLocalMatrix = objects[i].worldToLocalMatrix;
            model.localToWorldMatrix = objects[i].localToWorldMatrix;
            allModels[i] = model;
        }

        modelBuffer.SetData(allModels);
        rayTracingMaterial.SetBuffer("Models", modelBuffer);
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
        focusDistance = Mathf.Max(0.1f, focusDistance);
    }

}
