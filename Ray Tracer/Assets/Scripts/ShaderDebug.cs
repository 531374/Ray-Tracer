using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;

public class ShaderDebug : MonoBehaviour
{
    [SerializeField] Transform obj;
    [SerializeField] float rotationSpeed;

    [SerializeField] TextMeshProUGUI debugText;

    RayTracingManager manager;

    float startTime;
    int frames = 0;

    const float testTime = 300f;

    int maxBounces;
    int raysPerPixel;

    float currentFPS;
    float currentFPSTime;
    int currentFPSFrames;

    private void Awake()
    {
        startTime = Time.time;
        manager = RayTracingManager.instance;

        currentFPSTime = Time.time;
        currentFPSFrames = 0;
        currentFPS = 0f;

        maxBounces = manager.MaxBounceCount;
        raysPerPixel = manager.numRaysPerPixel;
    }

    private void Update()
    {
        obj.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);

        frames++;
        float totalTime = Time.time - startTime;
        float averageFps = frames / totalTime;

        float averageTimePerFrame = totalTime / frames;

        currentFPSFrames++; 
        if(Time.time - currentFPSTime > 1f)
        {
            currentFPSTime = Time.time;
            currentFPS = currentFPSFrames;
            currentFPSFrames = 0;
        }

        debugText.text = $"Current FPS: {currentFPS:F0}\n Avg FPS: {averageFps:F2}\nAvg/Frame: {averageTimePerFrame:F2}\nMax Bounces: {maxBounces}\nRays Per Pixel: {raysPerPixel}";

        if(Time.time - startTime > testTime)
        {
            Time.timeScale = 0.0f;
            EditorApplication.isPaused = true;
            Debug.Log($"Settings: Rays per Pixel={raysPerPixel}, Max Bounces={maxBounces}\nAverage FPS over {testTime} seconds: {averageFps}");
        }
    }

    void CheckInputs()
    {
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            manager.MaxBounceCount++;
        }
        else if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            manager.MaxBounceCount = Mathf.Max(1, --manager.MaxBounceCount);
        }

        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            int delta = Input.GetKey(KeyCode.LeftShift) ? 5 : 1;
            manager.numRaysPerPixel = Mathf.Max(1, manager.numRaysPerPixel - delta);
        }

        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            int delta = Input.GetKey(KeyCode.LeftShift) ? 5 : 1;
            manager.numRaysPerPixel += delta;
        }
    }
}
