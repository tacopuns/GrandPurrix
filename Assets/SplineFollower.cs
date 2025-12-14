using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//using UnityEngine.Splines;

public class SplineFollower : MonoBehaviour
{
    /*public SplineContainer splineContainer;
    public float speed = 5f;
    private float distanceTravelled = 0f;

    void Update()
    {
        if (splineContainer != null)
        {
            // Calculate the distance travelled this frame
            distanceTravelled += speed * Time.deltaTime;

            // Determine the total length of the spline
            float totalLength = splineContainer.CalculateLength();

            // Calculate the normalized position (0 to 1) along the entire spline path
            // Use the modulo operator for looping behavior
            float normalizedDistance = Mathf.Repeat(distanceTravelled, totalLength);

            // Get the position and tangent (forward direction) at the current distance
            Vector3 position = splineContainer.EvaluatePosition(normalizedDistance);
            Vector3 forward = splineContainer.EvaluateTangent(normalizedDistance);

            // Update the object's position and rotation
            transform.position = position;
            // Use Quaternion.LookRotation to align the object's forward direction with the spline's tangent
            transform.rotation = Quaternion.LookRotation(forward);
        }
    }*/
}
