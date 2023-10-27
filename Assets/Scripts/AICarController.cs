using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AICarController : MonoBehaviour
{
    public Transform[] waypoints; 
    public WaypointContainer waypointContainer;
    private int currentWaypointIndex = 0;
    

    public float rotationSpeed = 6f;
    public float speed = 20f;

    public Rigidbody theRB;

    private float groundRayLength = .5f;
    public Transform groundRayPoint;

    public float stuckTimer = 0f;
    public float maxStuckTime = 0.5f; 
    
    
    void Start()
    {
        waypoints = waypointContainer.waypoints.ToArray();
        theRB.transform.parent = null;
        currentWaypointIndex = 0;
    }

    
    void Update()
    {
        if (currentWaypointIndex < waypoints.Length)
        {
            Vector3 targetPosition = waypoints[currentWaypointIndex].position;
            Vector3 moveDirection = (targetPosition - transform.position).normalized;

            // rotates the CPU towards the next waypoint
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

            
            RaycastHit hit;
            if (Physics.Raycast(groundRayPoint.position, -transform.up, out hit, groundRayLength))
            {
                
                transform.Translate(Vector3.forward * speed * Time.deltaTime);
            }
            else
            {
                //
            }

            if (Vector3.Distance(transform.position, targetPosition) < 1f)
            {
                currentWaypointIndex++;
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
