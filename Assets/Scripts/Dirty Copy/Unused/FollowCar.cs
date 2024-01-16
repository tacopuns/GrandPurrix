using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowCar : MonoBehaviour
{
    public Transform target; // Assign the car's transform to this in the inspector.
    public float smoothingFactor = 5.0f;

    private void FixedUpdate()
    {
    //Vector3 targetPosition = target.position;
    //Vector3 smoothedPosition = Vector3.Lerp(transform.position, targetPosition, smoothingFactor * Time.fixedDeltaTime);
    //transform.position = smoothedPosition;

    // Similarly, you can interpolate rotation for smooth camera rotation.
    Quaternion targetRotation = target.rotation;
    Quaternion smoothedRotation = Quaternion.Slerp(transform.rotation, targetRotation, smoothingFactor * Time.fixedDeltaTime);
    transform.rotation = smoothedRotation;
    }
}
