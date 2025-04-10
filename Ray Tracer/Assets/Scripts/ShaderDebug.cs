using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ShaderDebug : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI debugText;

    RayTracingManager manager;

    float startTime;
    int frames = 0;


    private void Awake()
    {
        startTime = Time.time;
        manager = RayTracingManager.instance;
    }

    private void Update()
    {
        CheckInputs();

        frames++;
        float totalTime = Time.time - startTime;
        float averageFps = frames / totalTime;

        float averageTimePerFrame = totalTime / frames;

        int maxBounces = manager.MaxBounceCount;

        int raysPerPixel = manager.numRaysPerPixel;

        debugText.text = $"Avg FPS: {averageFps:F0}\nAvg/Frame: {averageTimePerFrame:F2}\nMax Bounces: {maxBounces}\nRays Per Pixel: {raysPerPixel}";
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
