using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;

using UnityEngine.Splines;

public class CPUController : MonoBehaviour
{

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

    public List<WheelMove> wheels = new List<WheelMove>();

    public List<WheelMove> steeringWheels = new List<WheelMove>();
    public List<WheelMove> drivingWheels = new List<WheelMove>();

    public Vector3 centerOfMass;
    public LayerMask whatIsGround;
    private bool grounded;
    public float weightMultiplier;

    private bool isSteering;

    //private Vector3 moveDirection;

    [Header("Spline")]
    public SplineContainer splineContainer;
    public float splineProgress = 0f;

    public float lookAheadDistance = 5f;

    public float splineLength;
    private float lastSplineProgress;


    [Header("Lane Offset")]
    public float laneOffset = 0f;        
    public float maxLaneOffset = 1.5f;   // Track half-width
    public bool randomizeLaneOnStart = true;

    [Header("Lane Switching")]
    public float currentLaneOffset = 0f;
    public float desiredLaneOffset = 0f;
    public float laneChangeSpeed = 2.5f;
    public float laneChangeCooldown = 1.5f;

    private float laneChangeTimer = 0f;

    [Header("Overtaking")]
    public float overtakeDistance = 8f;    //in spline meters
    public float laneCommitTime = 1.25f;    

    private float laneCommitTimer = 0f;

    public bool debugOvertaking = true;

    [Header("Overtake Boost")]
    public float boostSpeed = 800f;
    public float boostAccelerationTime = 0.6f;

    float boostTimer = 0f;
    float boostDuration = 1.2f;
    bool isOvertaking = false;


    public float overtakeStartDelay = 4f;
    public bool overtakeEnabled = false;

    public RaceManager raceManager;
    public RaceStatsHUD raceStatsHUD;

    //public static List<GameObject> Racers;
    public static List<CPUController> allRacers;

    [Header("Debug")]
    public bool drawDebug = true;




    void Start()
    {

        splineLength = splineContainer.Spline.GetLength();

        checkpointManager = CheckpointManager.Instance;
        currentCheckpointIndex = checkpointManager.GetLastPassedCheckpointIndex(gameObject);

        currentSpeed = 0f;
        lastSplineProgress = splineProgress;
        
        StartWheelMove();

        //SpriteAnim = GetComponent<Alpha_2D_Character_In_3D_World>();

        currentLaneOffset = Random.Range(-maxLaneOffset, maxLaneOffset);
        desiredLaneOffset = currentLaneOffset;

        allRacers = new List<CPUController>();

        foreach (GameObject racer in raceManager.racers)
        {
            CPUController ai = racer.GetComponent<CPUController>();
            if (ai != null)
                allRacers.Add(ai);
        }
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

            float distanceToCheckpoint = checkpointManager.DistanceToNextCheckpoint(gameObject);
        
            if (distanceToCheckpoint < .5f)
            {
                checkpointManager.UpdateCheckpoint(gameObject, currentCheckpointIndex);
                currentCheckpointIndex++;
            }
        
    
        HandleStuckLogic();
        
        laneChangeTimer -= Time.deltaTime;

        currentLaneOffset = Mathf.Lerp(currentLaneOffset, desiredLaneOffset, Time.deltaTime * laneChangeSpeed);

        /*if (Input.GetKeyDown(KeyCode.Q))
            RequestLaneChange(-maxLaneOffset);

        if (Input.GetKeyDown(KeyCode.E))
            RequestLaneChange(maxLaneOffset);*/

        if (RaceStatsHUD.Instance.raceTime >= overtakeStartDelay)
        {
            overtakeEnabled = true;
        }

        EvaluateOvertake();

        laneCommitTimer -= Time.deltaTime;

        if (isOvertaking)
        {
            cpuSpeed = boostSpeed;
            boostTimer -= Time.deltaTime;
            if (boostTimer <= 0f)
            {
                isOvertaking = false;
                cpuSpeed = maxSpeed;
            }
        }

        
        
    }

    void FixedUpdate()
    {
        
        AdvanceAlongSpline();
        MoveAlongSpline();
        AvoidObstacles();
        

        DoWheelMove();
    }

    Vector3 GetSplineForward(float distance)
    {
        float t = Mathf.Repeat(distance / splineLength, 1f);

        return splineContainer.transform.TransformDirection(
            splineContainer.Spline.EvaluateTangent(t)
        ).normalized;
    }

    void AdvanceAlongSpline()
    {
        splineProgress += currentSpeed * Time.fixedDeltaTime;
        splineProgress = Mathf.Repeat(splineProgress, splineLength);
        
    }

    Vector3 GetSplinePosition(float distance)
    {
        float t = Mathf.Repeat(distance / splineLength, 1f);

        var spline = splineContainer.Spline;

        Vector3 centerPos = splineContainer.transform.TransformPoint(
            spline.EvaluatePosition(t)
        );

        Vector3 tangent = splineContainer.transform.TransformDirection(
            spline.EvaluateTangent(t)
        ).normalized;

        Vector3 right = Vector3.Cross(Vector3.up, tangent).normalized;


        // --- CURVATURE CHECK ---
        float nextT = Mathf.Repeat(t + 0.02f, 1f);
        Vector3 nextTangent = splineContainer.transform.TransformDirection(
            spline.EvaluateTangent(nextT)
        ).normalized;

        float curvature = Vector3.Angle(tangent, nextTangent);

        // Pull toward center on tight corners
        float curvature01 = Mathf.InverseLerp(0f, 80f, curvature);
        float safeLaneOffset = Mathf.Lerp(laneOffset, 0f, curvature01);

        return centerPos + right * currentLaneOffset;
        
    }

    Vector3 GetSteerTarget()
    {
        return GetSplinePosition(splineProgress + lookAheadDistance);
    }

    void MoveAlongSpline()
    {
        grounded = false;

        Vector3 targetPosition = GetSteerTarget();
        Vector3 moveDirection = (targetPosition - transform.position).normalized;

        Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        

        RaycastHit hit;
        if (Physics.Raycast(groundRayPoint.position, -transform.up, out hit, groundRayLength, whatIsGround))
        {
            grounded = true;
            aiRB.drag = dragOnGround;

            Vector3 forwardForce = transform.forward * cpuSpeed * 50f;

            if (hit.normal != Vector3.up)
                forwardForce += hit.normal * 10f;

            aiRB.AddForce(forwardForce, ForceMode.Force);
        }

        if (!grounded)
        {
            aiRB.drag = 0.1f;
            aiRB.AddForce(Vector3.up * -gravityForce * weightMultiplier);
        }
    }

    public void RequestLaneChange(float newOffset)
    {
        if (laneChangeTimer > 0f)
            return;

        desiredLaneOffset = Mathf.Clamp(newOffset, -maxLaneOffset, maxLaneOffset);
        laneChangeTimer = laneChangeCooldown;
    }

    void EvaluateOvertake()
    {
        if (!overtakeEnabled)
        {

            if (laneChangeTimer > 0f || laneCommitTimer > 0f)
            return;

            CPUController closestAhead = null;
            float closestDelta = float.MaxValue;

            foreach (var other in allRacers)
            {
                if (other == this)
                    continue;

                float delta = Mathf.DeltaAngle(splineProgress, other.splineProgress);

                
                if (delta > 0f && delta < overtakeDistance)
                {
                    if (delta < closestDelta)
                    {
                        closestDelta = delta;
                        closestAhead = other;
                    }
                }
            }

            if (closestAhead == null)
                return;

            
            float desiredLane = closestAhead.currentLaneOffset > 0f
                ? -maxLaneOffset
                : maxLaneOffset;

            
            if (Mathf.Abs(currentLaneOffset - desiredLane) < 0.1f)
                return;

            RequestLaneChange(desiredLane);
            laneCommitTimer = laneCommitTime;

            isOvertaking = true;
            boostTimer = boostDuration;
        }
    }

    void HandleStuckLogic()
    {
        float delta = Mathf.Abs(splineProgress - lastSplineProgress);

        if (delta < 0.05f)
        {
            stuckTimer += Time.deltaTime;
            if (stuckTimer > maxStuckTime)
            {
                splineProgress += lookAheadDistance;
                stuckTimer = 0f;
            }
        }
        else
        {
            stuckTimer = 0f;
        }

        lastSplineProgress = splineProgress;
    }



    void AvoidObstacles() //pretty sure this doesnt work and makes them misbehave
    {
        RaycastHit hitFront, hitLeft, hitRight;

        if (Physics.Raycast(groundRayPoint.position, transform.forward, out hitFront, obstacleDetectionDistance))
            AvoidIfValid(hitFront);

        if (Physics.Raycast(groundRayPoint.position, -transform.right, out hitLeft, obstacleDetectionDistance / 2))
            AvoidIfValid(hitLeft);

        if (Physics.Raycast(groundRayPoint.position, transform.right, out hitRight, obstacleDetectionDistance / 2))
            AvoidIfValid(hitRight);
    }

    void AvoidIfValid(RaycastHit hit)
    {
        if (hit.collider.CompareTag("Ramp")) return;

        Vector3 avoidance = Vector3.Cross(hit.normal, Vector3.up).normalized;
        Vector3 newDirection = transform.forward + avoidance * obstacleAvoidanceForce;
        transform.rotation = Quaternion.LookRotation(newDirection);
        aiRB.AddForce(transform.forward * cpuSpeed, ForceMode.Force);
    }

    bool IsRamp(Collider collider)
    {
        return collider.CompareTag("Ramp");
    }

    /*void AvoidObstacle(RaycastHit hit)
    {
        Vector3 avoidanceDirection = Vector3.Cross(hit.normal, Vector3.up).normalized;
        Vector3 newDirection = transform.forward + avoidanceDirection * obstacleAvoidanceForce;
        transform.rotation = Quaternion.LookRotation(newDirection);
        aiRB.AddForce(transform.forward * cpuSpeed, ForceMode.Force);
    }*/



    void StartWheelMove()
    {
        centerOfMass = aiRB.centerOfMass;

        foreach (WheelMove wheels in wheels)
        {
            if(wheels.canSteer)
            {
                steeringWheels.Add(wheels);
            }
        }
        foreach (WheelMove wheels in wheels)
        {
            if(wheels.canDrive)
            {
                drivingWheels.Add(wheels);
            }
        }
    }


    public float steeringStrength = 1.2f;
    void DoWheelMove()
    {
        
        
        Vector3 steerTarget = GetSplinePosition(splineProgress + lookAheadDistance);
        Vector3 steerDirection = Vector3.ProjectOnPlane(steerTarget - transform.position,transform.up).normalized;
        float steeringAngle = Vector3.SignedAngle(transform.forward, steerDirection, Vector3.up);


        float forwardSpeed = Vector3.Dot(transform.forward, aiRB.velocity);
        float speedFactor = Mathf.InverseLerp(0, maxSpeed, forwardSpeed);
        
        float currentMotorTorque = Mathf.Lerp(cpuSpeed, 0, speedFactor);
        float currentSteerRange = Mathf.Lerp(steeringAngle, rotationSpeed / 2, speedFactor);
        bool isAccelerating = Mathf.Sign(cpuSpeed) == Mathf.Sign(forwardSpeed);
        
        float clampedSteeringAngle = Mathf.Clamp(steeringAngle, -40f, 40f);


        foreach (WheelMove wheel in drivingWheels)
        {
            if (isAccelerating)
            {
                wheel.wheelCollider.motorTorque = cpuSpeed * currentMotorTorque;
                wheel.wheelCollider.brakeTorque = 0;
        
            }
            else
            {
                wheel.wheelCollider.brakeTorque = Mathf.Abs(cpuSpeed) * wheel.wheelCollider.brakeTorque;
                wheel.wheelCollider.motorTorque = 0;
            }
        }

        foreach (WheelMove wheel in wheels)
        {
            wheel.UpdateWheel();
        }

        foreach (WheelMove wheel in steeringWheels)
        {
            
            //wheel.wheelCollider.steerAngle = rotationSpeed * currentSteerRange;

            //wheel.wheelCollider.steerAngle = clampedSteeringAngle;

            float steer = steeringAngle * steeringStrength;
            steer = Mathf.Clamp(steer, -40f, 40f);
            wheel.wheelCollider.steerAngle = steer;
            
        }
    }

    void OnDrawGizmos()
    {
        if (!drawDebug || splineContainer == null)
            return;

        var spline = splineContainer.Spline;
        if (spline == null)
            return;

        // Current spline position
        Vector3 currentPos = splineContainer.transform.TransformPoint(
            spline.EvaluatePosition(splineProgress / splineLength)
        );

        Gizmos.color = Color.green;
        Gizmos.DrawSphere(currentPos, 0.5f);

        // Look-ahead target
        float lookT = Mathf.Repeat((splineProgress + lookAheadDistance) / splineLength, 1f);
        Vector3 lookAheadPos = splineContainer.transform.TransformPoint(
            spline.EvaluatePosition(lookT)
        );

        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(lookAheadPos, 0.5f);

        // Line from car to target
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, lookAheadPos);

        if (Application.isPlaying)
        {
            Gizmos.color = Color.white;
            Gizmos.DrawLine(
                GetSplinePosition(splineProgress),
                splineContainer.transform.TransformPoint(
                    splineContainer.Spline.EvaluatePosition(
                        splineProgress / splineLength
                    )
                )
            );
        }

        if (!debugOvertaking || !Application.isPlaying)
        return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 3f);

        foreach (var other in allRacers)
        {
            if (other == null || other == this)
                continue;

            float delta = Mathf.DeltaAngle(splineProgress, other.splineProgress);

            if (delta > 0f && delta < overtakeDistance)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(transform.position, other.transform.position);
            }
        }


    }

}

