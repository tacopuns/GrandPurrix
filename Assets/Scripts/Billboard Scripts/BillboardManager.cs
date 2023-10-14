using UnityEngine;
using System.Collections.Generic;

public class BillboardManager : MonoBehaviour
{

    [SerializeField] 
    bool freezeXZAxis = true;


    void LateUpdate()
    {
        // Find all cameras in the scene
        Camera[] allCameras = Camera.allCameras;

        if (allCameras.Length > 0)
        {
            // Find the closest camera from all cameras
            Camera closestCamera = FindClosestCamera(allCameras);

            // Check if you want to freeze the X and Z axis
            if (freezeXZAxis)
            {
                Vector3 eulerAngles = transform.rotation.eulerAngles;
                eulerAngles.y = closestCamera.transform.rotation.eulerAngles.y;
                transform.rotation = Quaternion.Euler(eulerAngles);
            }
            else
            {
                transform.rotation = closestCamera.transform.rotation;
            }
        }
    }

    Camera FindClosestCamera(Camera[] cameras)
    {
        Camera closest = null;
        float closestDistance = float.MaxValue;

        foreach (var camera in cameras)
        {
            float distance = Vector3.Distance(camera.transform.position, transform.position);

            if (distance < closestDistance)
            {
                closest = camera;
                closestDistance = distance;
            }
        }

        return closest;
    }


}
