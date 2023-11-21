using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestCarCon : MonoBehaviour
{
    public Transform[] waypoints; 
    public WaypointContainer waypointContainer;
    private int currentWaypointIndex = 0;
    

    public float rotationSpeed = 6f;

    public float currentSpeed = 0f;
    public float cpuSpeed = 20f;

    public Rigidbody aiRB;

    private float groundRayLength = .5f;
    public Transform groundRayPoint;

    public float stuckTimer = 0f;
    public float maxStuckTime = 0.5f; 

    public float accelerationRate = 5.0f;
    public float maxSpeed = 40.0f;

    public float dragOnGround = 7f, gravityForce = 10f;

    [SerializeField]
    private float accelerationTimer = 0.0f;
    public float accelerationTime = 5.0f; 

    public float obstacleDetectionDistance = 0f;
    public float obstacleAvoidanceForce = 0f;

    
    void Start()
    {
        waypoints = waypointContainer.waypoints.ToArray();
        aiRB.transform.parent = null;
        currentWaypointIndex = 0;
        currentSpeed = 0f;
    }


    void Update()
    {
        currentSpeed = aiRB.velocity.magnitude;
        
        if (cpuSpeed < maxSpeed)
        {
            // Calculate the rate of acceleration
            float accelerationRate = maxSpeed / accelerationTime;

            // Increment the acceleration timer
            accelerationTimer += Time.deltaTime;

            // If acceleration timer exceeds the acceleration time, reset it
            if (accelerationTimer > accelerationTime)
            {
                accelerationTimer = accelerationTime;
            }

            // Apply acceleration to CPU speed
            cpuSpeed += accelerationRate * Time.deltaTime;
            cpuSpeed = Mathf.Min(cpuSpeed, maxSpeed); // Ensure speed doesn't exceed the maximum
        }


        //Stuck timer
        if (Vector3.Distance(transform.position, waypoints[currentWaypointIndex].position) < waypoints.Length)
        {
            stuckTimer += Time.deltaTime;
            if (stuckTimer > maxStuckTime)
            {
                currentWaypointIndex++; 
                stuckTimer = 0f; 
            }
        }
        else
        {
            stuckTimer = 0f;
        }   
        

        if (currentWaypointIndex >= waypoints.Length)
        {
            currentWaypointIndex = 0;  // Reset the waypoint index to begin a new lap
            // Increment a lap count if needed
        }
    
     Debug.DrawRay(transform.position, waypoints[currentWaypointIndex].position - transform.position, Color.yellow);
    
    }
    
    void FixedUpdate()
    {
        if (currentWaypointIndex < waypoints.Length)
    {
        MoveTowardsWaypoint();
        AvoidObstacles();
    }
}

void MoveTowardsWaypoint()
{
    Vector3 targetPosition = waypoints[currentWaypointIndex].position;
    Vector3 moveDirection = (targetPosition - transform.position).normalized;
    Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
    transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

    RaycastHit hit;

    if (Physics.Raycast(groundRayPoint.position, -transform.up, out hit, groundRayLength))
    {
        aiRB.drag = dragOnGround;

        Vector3 forwardForce = transform.forward * cpuSpeed * 50f;

        if (hit.normal != Vector3.up)
        {
            forwardForce += hit.normal * 10f;
        }

        aiRB.AddForce(forwardForce, ForceMode.Force);
    }
    else
    {
        aiRB.drag = 0.1f;
        aiRB.AddForce(Vector3.up * -gravityForce * 50f);
    }

    // Waypoint handling
    float distanceToWaypoint = Vector3.Distance(transform.position, targetPosition);
    if (distanceToWaypoint < 1f)
    {
        currentWaypointIndex++;
    }
    else if (distanceToWaypoint < cpuSpeed * Time.deltaTime)
    {
        // Continue moving until very close to the waypoint
    }

    if (hit.collider.CompareTag("Opp"))  // Adjust the tag based on your obstacles
        {
            AvoidObstacle(hit);
        }
    else
    {
        //other case handling logic 
    }
}

void AvoidObstacles()
{
    Debug.DrawRay(groundRayPoint.position, transform.forward * obstacleDetectionDistance, Color.red); // Ray in front
    Debug.DrawRay(groundRayPoint.position, -transform.right * obstacleDetectionDistance / 2, Color.red); // Ray to the left
    Debug.DrawRay(groundRayPoint.position, transform.right * obstacleDetectionDistance / 2, Color.red); // Ray to the right

    RaycastHit hitFront, hitLeft, hitRight;

    // Check for obstacles in front
    if (Physics.Raycast(groundRayPoint.position, transform.forward, out hitFront, obstacleDetectionDistance))
    {
        if (!IsRamp(hitFront.collider))
        {
            Debug.Log("Obstacle detected in front!");
            AvoidObstacle(hitFront);
        }
    }

    // Check for obstacles to the left
    if (Physics.Raycast(groundRayPoint.position, -transform.right, out hitLeft, obstacleDetectionDistance / 2))
    {
        if (!IsRamp(hitLeft.collider))
        {
            Debug.Log("Obstacle detected to the left!");
            AvoidObstacle(hitLeft);
        }
    }

    // Check for obstacles to the right
    if (Physics.Raycast(groundRayPoint.position, transform.right, out hitRight, obstacleDetectionDistance / 2))
    {
        if (!IsRamp(hitRight.collider))
        {
            Debug.Log("Obstacle detected to the right!");
            AvoidObstacle(hitRight);
        }
    }
}

bool IsRamp(Collider collider)
{
    // Add logic to check if the collider belongs to a ramp (e.g., by tag or layer)
    // Return true if it's a ramp, false otherwise
    // Example using tag:
    return collider.CompareTag("Ramp");
    // Example using layer:
    // return collider.gameObject.layer == rampLayer;
}

void AvoidObstacle(RaycastHit hit)
{
    // Calculate a steering force perpendicular to the obstacle's normal
    Vector3 avoidanceDirection = Vector3.Cross(hit.normal, Vector3.up).normalized;
    
    // Apply the steering force to the CPU's forward direction
    Vector3 newDirection = transform.forward + avoidanceDirection * obstacleAvoidanceForce;

    // Rotate the CPU towards the new direction
    transform.rotation = Quaternion.LookRotation(newDirection);

    // Move forward (you can adjust the speed)
    aiRB.AddForce(transform.forward * cpuSpeed, ForceMode.Force);
}
}



    

