using UnityEngine;

public class IgnoreLayerBillboard : MonoBehaviour
{
    [SerializeField]
    private LayerMask targetLayerMask; // Specify the layer of the cameras to target

    [SerializeField]
    private LayerMask ignoreLayerMask; // Specify the layer to ignore

    [SerializeField]
    bool freezeXZAxis = true;

    private Camera targetCamera;

    private void Update()
    {
        // Find all cameras on the specified layer and not on the ignored layer
        targetCamera = FindCameraOnLayer(targetLayerMask, ignoreLayerMask);

        if (targetCamera != null)
        {
            // Make the object look at the target camera
            transform.LookAt(targetCamera.transform);

            if (freezeXZAxis)
            {
                Vector3 eulerAngles = transform.rotation.eulerAngles;
                eulerAngles.x = 0f;
                eulerAngles.z = 0f;
                transform.rotation = Quaternion.Euler(eulerAngles);
            }
        }
    }

    Camera FindCameraOnLayer(LayerMask layerMask, LayerMask ignoreMask)
    {
        Camera[] cameras = Camera.allCameras;
        Camera closestCamera = null;
        float closestDistance = float.MaxValue;

        foreach (var cam in cameras)
        {
            if (((layerMask & (1 << cam.gameObject.layer)) != 0) && ((ignoreMask & (1 << cam.gameObject.layer)) == 0))
            {
                float distance = Vector3.Distance(cam.transform.position, transform.position);

                if (distance < closestDistance)
                {
                    closestCamera = cam;
                    closestDistance = distance;
                }
            }
        }

        return closestCamera;
    }
}

