using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class WheelMove
{
    public WheelCollider wheelCollider;
    public Transform wheelMesh;
    public bool canSteer;
    public bool canDrive;

    

    public void UpdateWheel()
    {
        Vector3 position;
        Quaternion rotation;
        wheelCollider.GetWorldPose(out position, out rotation);

        wheelMesh.position = position;
        wheelMesh.rotation = rotation;

        
    }
}
