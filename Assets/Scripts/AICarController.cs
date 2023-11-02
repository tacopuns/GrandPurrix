using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AICarController : MonoBehaviour
{
    public Transform[] waypoints; 
    public WaypointContainer waypointContainer;
    private int currentWaypointIndex = 0;
    

    public float rotationSpeed = 6f;
    public float currentSpeed = 20f;

    public Rigidbody aiRB;

    private float groundRayLength = .5f;
    public Transform groundRayPoint;

    public float stuckTimer = 0f;
    public float maxStuckTime = 0.5f; 

    public float accelerationRate = 5.0f;
    public float maxSpeed = 40.0f;

    public float dragOnGround = 7f, gravityForce = 10f;

    
    void Start()
    {
        waypoints = waypointContainer.waypoints.ToArray();
        aiRB.transform.parent = null;
        currentWaypointIndex = 0;

    }

    
    void Update()
    {

        /*if (currentWaypointIndex < waypoints.Length)
        {
            Vector3 targetPosition = waypoints[currentWaypointIndex].position;
            Vector3 moveDirection = (targetPosition - transform.position).normalized;

            // rotates the CPU towards the next waypoint
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

            
            RaycastHit hit;
            if (Physics.Raycast(groundRayPoint.position, -transform.up, out hit, groundRayLength))
            {
                
                transform.Translate(Vector3.forward * currentSpeed * Time.deltaTime);
            }
            else
            {
                //
            }

            if (Vector3.Distance(transform.position, targetPosition) < 1f)
            {
                currentWaypointIndex++;
            }

            if (currentSpeed < maxSpeed)
            {
                currentSpeed += accelerationRate * Time.deltaTime;
                currentSpeed = Mathf.Min(currentSpeed, maxSpeed); // Ensure speed doesn't exceed the maximum
            }
        }*/

        if (currentWaypointIndex < waypoints.Length)
        {
            Vector3 targetPosition = waypoints[currentWaypointIndex].position;
            Vector3 moveDirection = (targetPosition - transform.position).normalized;

            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

            RaycastHit hit;
            
            if (Physics.Raycast(groundRayPoint.position, -transform.up, out hit, groundRayLength))
            {
                aiRB.drag = dragOnGround;
                
                Vector3 forwardForce = transform.forward * currentSpeed;
                
                if (hit.normal != Vector3.up)
                    { 
                        // Adjust vertical movement on ramps
                        //transform.position = Vector3.Lerp(transform.position, hit.point + Vector3.up * 2f, Time.deltaTime);
                        
                        //Vector3 newPosition = Vector3.Lerp(transform.position, hit.point + Vector3.up * 2f, Time.deltaTime);
                        //transform.position = new Vector3(newPosition.x, transform.position.y, newPosition.z);
                        forwardForce += hit.normal * 10f;
                    }

                // Move forward on both regular track and ramps
                //transform.Translate(Vector3.forward * currentSpeed * Time.deltaTime);
                // Apply the forward force to the Rigidbody
                aiRB.AddForce(forwardForce, ForceMode.Force);

            }
            else
            {
                // Handling for when the car is not on the ground
                //(steal dragonground logic from carcontroller)

                aiRB.drag = 0.1f;
                aiRB.AddForce(Vector3.up * -gravityForce * 10f);
            }

            // Waypoint handling
            float distanceToWaypoint = Vector3.Distance(transform.position, targetPosition);
            if (distanceToWaypoint < 1f)
            {
                currentWaypointIndex++;
            }
            else if (distanceToWaypoint < currentSpeed * Time.deltaTime)
            {
                // Continue moving until very close to the waypoint
            }

            // Acceleration logic
            if (currentSpeed < maxSpeed)
            {
                currentSpeed += accelerationRate * Time.deltaTime;
                currentSpeed = Mathf.Min(currentSpeed, maxSpeed);
            }

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
        
    
     Debug.DrawRay(transform.position, waypoints[currentWaypointIndex].position - transform.position, Color.yellow);

    }
}
