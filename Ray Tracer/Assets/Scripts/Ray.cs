using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ray
{
    public Vector3 origin;
    public Vector3 direction;


    public Ray() { }

    public Ray(Vector3 pOrigin, Vector3 pDirection) 
    {
        this.origin = pOrigin;
        this.direction = pDirection;
    }

    //Returns the position of the ray at t
    public Vector3 at(float t)
    {
        return origin + t * direction;
    }


}
