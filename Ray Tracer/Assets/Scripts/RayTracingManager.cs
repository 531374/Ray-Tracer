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

        if (!bvhCreated)
        {
            CreateMeshes();
            bvhCreated = true;
        }

        UpdateModels();
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

        rayTracingMaterial.SetFloat("boxDebugScale", boxDebugScale);
        rayTracingMaterial.SetFloat("triangleDebugScale", triangleDebugScale);
        rayTracingMaterial.SetInt("debugMode", debugMode);
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

            BVH bvh = new BVH(mesh.vertices, mesh.triangles, mesh.normals, t.position, t.rotation, t.lossyScale);
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
            if (objects[i] == null) objects.Remove(objects[i]);
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
    }

}
