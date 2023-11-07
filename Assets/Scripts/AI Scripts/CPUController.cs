using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class CPUController : MonoBehaviour
{
    public NavMeshAgent agent;
    public WaypointContainer waypointContainer;
    public Transform[] waypoints;

    public Rigidbody aiRB;
    public float dragOnGround = 7f;
    public float gravityForce = 10f;

    private float groundRayLength = 0.5f;
    public Transform groundRayPoint;

    private int currentWaypointIndex = 0;

    void Start()
    {
        aiRB.transform.parent = null;
        agent = GetComponent<NavMeshAgent>();

        waypoints = waypointContainer.waypoints.ToArray();

        agent.SetDestination(waypoints[0].position);
    }

    void FixedUpdate()
    {
        if (!agent.pathPending && agent.remainingDistance < 0.5f)
        {
            currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;
            agent.SetDestination(waypoints[currentWaypointIndex].position);
        }

        RaycastHit hit;
        if (Physics.Raycast(groundRayPoint.position, -transform.up, out hit, groundRayLength))
        {
            aiRB.drag = dragOnGround;

            // Rotation handling eventually
        }
        else
        {
            aiRB.drag = 0.1f;
            aiRB.AddForce(Vector3.up * -gravityForce * 50f);
        }
    }
}

