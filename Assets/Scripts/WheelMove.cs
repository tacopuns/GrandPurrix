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

    //public bool isLeftWheel;

    public void UpdateWheel()
    {
        Vector3 position;
        Quaternion rotation;
        wheelCollider.GetWorldPose(out position, out rotation);

        wheelMesh.position = position;
        wheelMesh.rotation = rotation;

        // Apply rotation correction based on whether the wheel is on the left or right
        /*if (isLeftWheel)
        {
            // Adjust for left wheels
            wheelMesh.Rotate(new Vector3(0f, -90f, 0f));
            //wheelMesh.transform.position += new Vector3(-.5f, 0f, 0f); // Adjust as needed
        }
        else
        {
            // Adjust for right wheels
            wheelMesh.Rotate(new Vector3(0f, 90f, 0f));
        } */
    }
}
