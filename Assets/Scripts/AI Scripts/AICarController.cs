using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AICarController : MonoBehaviour
{
    public Transform[] waypoints; 
    public WaypointContainer waypointContainer;
    public int currentWaypointIndex = 0;

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

    private CheckpointManager checkpointManager;
    public int currentCheckpointIndex = 0;
    
    //private Alpha_2D_Character_In_3D_World SpriteAnim;




    void Start()
    {
        waypoints = waypointContainer.waypoints.ToArray();
        aiRB.transform.parent = null;
        currentWaypointIndex = 0;
        currentSpeed = 0f;

        checkpointManager = CheckpointManager.Instance;
        currentCheckpointIndex = checkpointManager.GetLastPassedCheckpointIndex(gameObject);

        //SpriteAnim = GetComponent<Alpha_2D_Character_In_3D_World>();
    }

    void Update()
    {
        currentSpeed = aiRB.velocity.magnitude;
        
        if (cpuSpeed < maxSpeed)
        {
            float accelerationRate = maxSpeed / accelerationTime;
            accelerationTimer += Time.deltaTime;

            if (accelerationTimer > accelerationTime)
            {
                accelerationTimer = accelerationTime;
            }

            cpuSpeed += accelerationRate * Time.deltaTime;
            cpuSpeed = Mathf.Min(cpuSpeed, maxSpeed);
        }

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
            currentWaypointIndex = 0;
        }

        if (currentWaypointIndex < waypoints.Length)
        {
            currentCheckpointIndex = checkpointManager.GetLastPassedCheckpointIndex(gameObject);

            //float distanceToCheckpoint = Vector3.Distance(transform.position, checkpointManager.checkpoints[currentCheckpointIndex].position);
            float distanceToCheckpoint = checkpointManager.DistanceToNextCheckpoint(gameObject);

            if (distanceToCheckpoint < .5f)
            {
                checkpointManager.UpdateCheckpoint(gameObject, currentCheckpointIndex);
                currentCheckpointIndex++;
            }
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
        //SpriteAnim.enabled = true;

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
                //SpriteAnim.enabled = false;
            }

            aiRB.AddForce(forwardForce, ForceMode.Force);
        }
        else
        {
            HandleMidAir();
            //SpriteAnim.enabled = true;
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

        if (hit.collider != null && hit.collider.CompareTag("Opp"))
        {
            AvoidObstacle(hit);
        }
        else
        {
            
        }
    }

    void HandleMidAir()
    {
        aiRB.drag = 0.1f;
        aiRB.AddForce(Vector3.up * -gravityForce * 50f);
    }   

    void AvoidObstacles()
    {
        Debug.DrawRay(groundRayPoint.position, transform.forward * obstacleDetectionDistance, Color.red); // Ray in front
        Debug.DrawRay(groundRayPoint.position, -transform.right * obstacleDetectionDistance / 2, Color.red); // Ray to the left
        Debug.DrawRay(groundRayPoint.position, transform.right * obstacleDetectionDistance / 2, Color.red); // Ray to the right

        RaycastHit hitFront, hitLeft, hitRight;

        if (Physics.Raycast(groundRayPoint.position, transform.forward, out hitFront, obstacleDetectionDistance))
        {
            if (!IsRamp(hitFront.collider))
            {
                AvoidObstacle(hitFront);
            }
        }

        if (Physics.Raycast(groundRayPoint.position, -transform.right, out hitLeft, obstacleDetectionDistance / 2))
        {
            if (!IsRamp(hitLeft.collider))
            {
                AvoidObstacle(hitLeft);
            }
        }

        if (Physics.Raycast(groundRayPoint.position, transform.right, out hitRight, obstacleDetectionDistance / 2))
        {
            if (!IsRamp(hitRight.collider))
            {
                AvoidObstacle(hitRight);
            }
        }
    }

    bool IsRamp(Collider collider)
    {
        return collider.CompareTag("Ramp");
    }

    void AvoidObstacle(RaycastHit hit)
    {
        Vector3 avoidanceDirection = Vector3.Cross(hit.normal, Vector3.up).normalized;
        Vector3 newDirection = transform.forward + avoidanceDirection * obstacleAvoidanceForce;
        transform.rotation = Quaternion.LookRotation(newDirection);
        aiRB.AddForce(transform.forward * cpuSpeed, ForceMode.Force);
    }




}