using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class PerformanceLogger : MonoBehaviour
{
    [SerializeField] Transform obj;
    [SerializeField] float rotationSpeed;

    RayTracingManager manager;

    private void Awake()
    {
        manager = RayTracingManager.instance;
    }

    private void Update()
    {

        //Rotate object
        obj.Rotate(Vector3.up * rotationSpeed * Time.deltaTime);
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
