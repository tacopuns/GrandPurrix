using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PaparazziCarCon : MonoBehaviour
{
    public Transform target;
    public GameObject playerObject;

    public float rotationSpeed = 6f;

    public float paparazziSpeed = 20f;
    public float accelerationRate = 5.0f;
    public float maxSpeed = 40.0f;
    public float matchSpeedDistance = 5f;
    public Rigidbody aiRB;

    private float groundRayLength = .5f;
    public Transform groundRayPoint;

    public float dragOnGround = 7f, gravityForce = 10f;

    private bool isSticking = false;
    private float stickingTimer = 0f;

    public float currentSpd = 0f;

    void Start()
    {
        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                target = player.transform;
            }
            else
            {
                Debug.LogError("Paparazzi target is not assigned, and no player found in the scene.");
            }
        }

        aiRB.transform.parent = null;
    }

    void Update()
    {
        currentSpd = aiRB.velocity.magnitude;
    }

    void FixedUpdate()
    {
        if (target != null)
    {
        Vector3 moveDirection = (target.position - transform.position).normalized;
        Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

        float distanceToPlayer = Vector3.Distance(transform.position, target.position);

        RaycastHit hit;

        if (Physics.Raycast(groundRayPoint.position, -transform.up, out hit, groundRayLength))
        {
            aiRB.drag = dragOnGround;

            Vector3 forwardForce = transform.forward * paparazziSpeed * 50f;

            if (hit.normal != Vector3.up)
            {
                forwardForce += hit.normal * 20f;
            }

            aiRB.AddForce(forwardForce, ForceMode.Force);

    
            if (distanceToPlayer < matchSpeedDistance)
            {
                MatchPlayerSpeed();
                rotationSpeed = 10f;
            }
            else
            {
                AccelerateTowardsPlayer();
                rotationSpeed = 200f;
            }
        }
        else
        {
            HandleMidAir();
        }

        Debug.DrawRay(transform.position, target.position - transform.position, Color.yellow);
    }
    }

    void AccelerateTowardsPlayer()
    {
        Vector3 desiredVelocity = (target.position - transform.position).normalized * maxSpeed;
        Vector3 velocityChange = desiredVelocity - aiRB.velocity;

        aiRB.AddForce(velocityChange * accelerationRate, ForceMode.Acceleration);
    }

    void StartSticking()
    {
        isSticking = true;
        stickingTimer = 0f;
        Debug.Log("sticking");
        StickToPlayer();
    }

    void StickToPlayer()
    {
        stickingTimer += Time.deltaTime;
    }

    void HandleMidAir()
    {
        aiRB.drag = 0.1f;
        aiRB.AddForce(Vector3.up * -gravityForce * 50f);
    }

    void MatchPlayerSpeed()
    {
        aiRB.velocity = playerObject.GetComponent<Rigidbody>().velocity;
        StartSticking();
    }
}