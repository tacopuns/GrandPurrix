using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class CarController : MonoBehaviour
{
    public Rigidbody theRB;

    public float reverseAccel = 50f, turnStrength = 15f, gravityForce = 10f, dragOnGround = 7f;
    
    public LayerMask whatIsGround; //anything set as ground, the car will be able to move on
    private float groundRayLength = .5f;
    public Transform groundRayPoint;
    
    private float speedInput, turnInput;
    
    private bool grounded;
    
    [SerializeField]
    private float currentSpeed = 0f;
    private float accelerationRate;
    public float maxSpeed = 260f;

    
    private float accelerationTimer = 0.0f;
    [SerializeField]
    private float accelerationTime = 2.0f; //time needed to reach max speed

    public bool isDrifting = false;
    //private float driftDuration = 2.0f;
    private float driftTimer = 0.0f;

    //private ParticleSystem driftXF;
    //private GameObject skidXF;
    
    private bool isBoosting = false;
    private float boostForce = 50;
    private float boostStrength = 100f;

    public List<WheelMove> wheels = new List<WheelMove>();

    public List<WheelMove> steeringWheels = new List<WheelMove>();
    public List<WheelMove> drivingWheels = new List<WheelMove>();

    private Vector3 centerOfMass;
    
    private InputActionAsset inputAsset;
    private InputActionMap gameplay;
    private InputAction MoveForward;
    private InputAction Turn;
    private InputAction Drift;

    private CheckpointManager checkpointManager;
    private int currentCheckpointIndex = 0;
    
    public Animator animator;
    int dL = 0;
    int dR = 0;
    int fB = 0;

    public bool isSteering = false;
    public bool canDrift = false;
    

    public WheelCollider leftWheel;
    public WheelCollider rightWheel;

    private float driftForce = 10f;

    private int driftDirection = 0;

    public Camera flashCamera; 
    public float detectionRange = 10f;

    public GameObject camUI;
    
    private void Awake()
    {
        inputAsset = this.GetComponent<PlayerInput>().actions;
        gameplay = inputAsset.FindActionMap("Gameplay");

        gameplay.FindAction("MoveForward").started += ctx => speedInput = ctx.ReadValue<float>();
        gameplay.FindAction("Turn").started += ctx => turnInput = ctx.ReadValue<float>();
        gameplay.FindAction("Drift").started += ctx => StartDrift();
        gameplay.FindAction("Flashbang").started += ctx => Flashbang();

        gameplay.FindAction("MoveForward").canceled += ctx => speedInput = 0f;
        gameplay.FindAction("Turn").canceled += ctx => turnInput = 0f;
        gameplay.FindAction("Drift").canceled += ctx => StopDrift();
        gameplay.FindAction("Flashbang").canceled += ctx => CamOff();
    }

    private void OnEnable()
    {
        gameplay.Enable();

        gameplay.FindAction("Turn").started += ctx => turnInput = ctx.ReadValue<float>();
    }

    private void OnDisable()
    {
        gameplay.Disable();
    }
    
    void Start()
    {
        //theRB.transform.parent = null;

        checkpointManager = CheckpointManager.Instance;
        currentCheckpointIndex = checkpointManager.GetLastPassedCheckpointIndex(gameObject);

        //driftXF = GetComponentInChildren<ParticleSystem>();
        //skidXF.SetActive(false);
        StartWheelMove();
        //animator = GetComponent<Animator>();

        animator = GetComponentInChildren<Animator>(); //might change this to set manually. idk if its possible to have 2 diff animators? one for player one for npc
        animator.enabled = true;

        camUI.SetActive(false);
    }

    void Update()
    {
        currentSpeed = theRB.velocity.magnitude;

        accelerationRate = maxSpeed / accelerationTime;
        accelerationTimer += Time.deltaTime;


        if (accelerationTimer > accelerationTime)
        {
            accelerationTimer = accelerationTime;
        }

        if (speedInput > 0)
        {
            speedInput = accelerationRate * accelerationTimer;
            theRB.drag = dragOnGround;
        }
        else if (speedInput < 0)
        {
            speedInput = -reverseAccel;
        }
        else if (speedInput == 0)
        {
            accelerationTimer = 0f;
            
            if(currentSpeed > 15)
            {
                theRB.drag = 2;
            }
            else
            {
                speedInput = 0;
            }
        }

        //float distanceToCheckpoint = Vector3.Distance(transform.position, checkpointManager.checkpoints[currentCheckpointIndex].position);
        float distanceToCheckpoint = checkpointManager.DistanceToNextCheckpoint(gameObject);
        currentCheckpointIndex = checkpointManager.GetLastPassedCheckpointIndex(gameObject);

        if (distanceToCheckpoint < .5f)
        {
            checkpointManager.UpdateCheckpoint(gameObject, currentCheckpointIndex);
            currentCheckpointIndex++;
        }

        animator.SetInteger("driftL",dL);
        animator.SetInteger("driftR",dR);
        animator.SetInteger("flashBang", fB);

        DriftEnable();

        Debug.DrawRay(flashCamera.transform.position, flashCamera.transform.forward * detectionRange, Color.red);


    }

    private void FixedUpdate() 
    {
        
        grounded = false;
        RaycastHit hit;

        if (Physics.Raycast(groundRayPoint.position, -transform.up, out hit, groundRayLength, whatIsGround))
        {
            grounded = true;
            transform.rotation = Quaternion.Euler(transform.rotation.eulerAngles + new Vector3(0f, turnInput * turnStrength * Time.deltaTime, 0f));
                if (turnInput < -0.1f || turnInput > 0.1f)
                {
                    isSteering = true;
                    
                }
                else
                {
                    isSteering = false;
                    //canDrift = false;
                }
        
            Quaternion newRotation = Quaternion.FromToRotation(transform.up, hit.normal) * transform.rotation;
            transform.rotation = Quaternion.Slerp(transform.rotation, newRotation, 100f);
        }

        if (grounded)
        {   
            //theRB.drag = dragOnGround;
            

            if (Mathf.Abs(speedInput) > 0)
            {
                theRB.AddForce(transform.forward * speedInput, ForceMode.Acceleration);
            }
        }
        else
        {
            theRB.drag = 0.1f;
            theRB.AddForce(Vector3.up * -gravityForce * 50f);
        }

        //UpdateDriftBoost();
        if (isDrifting)
        {
            PowerSlide();
        }

        DoWheelMove();
    }


    private void DriftEnable()
    {
    
        if(currentSpeed > 20f)
        {
            canDrift = true;
        }
        
    }

    private void StartDrift()
    {
        if (canDrift)
        {
            isDrifting = true;
            
        }

        /*if (currentSpeed > 20f && (turnInput < -0.1f || turnInput > 0.1f))
        {
            isDrifting = true;
            driftTimer = 0f;

            //PlayDriftEffects();
        }*/
        else
        {
            StopDrift();
        }

    }


    

    /*private void PlayDriftEffects()
    {
        if (!driftXF.isPlaying)
        {
            driftXF.Play();
        }

        if (!skidXF.activeSelf)
        {
            skidXF.SetActive(true);
        }
    }

    private void StopDriftEffects()
    {
        if (driftXF.isPlaying)
        {
            driftXF.Stop();
        }

        if (skidXF.activeSelf)
        {
            skidXF.SetActive(false);
        }
    }*/

    /*void UpdateDriftBoost()
    {
        if (isDrifting)
        {
            driftTimer += Time.deltaTime; 
            

            if (driftTimer >= driftDuration)
            {
                driftTimer = driftDuration;
                //ApplySpeedBoost();
            }
        }
        else
        {
            isBoosting = false;
            driftTimer = 0.0f;
            
        }
    }

    
    void ApplySpeedBoost()
    {
        if (!isBoosting)
        {
            isBoosting = true;
            Debug.Log("boost");
        }
    } */


    
    private float driftSwitchBuffer = 0.3f;
    private float driftSwitchTimer = 0f;


    public float currentDriftForce = 0f; 
    private float driftForceIncreaseRate = 1f;
    private float maxDriftForce = 15f;

    public float driftFriction = 10f;
    public float defaultFriction = 1f;

    void PowerSlide()
    {
        
        driftSwitchTimer -= Time.deltaTime;

        
        if (turnInput != 0 && driftSwitchTimer <= 0f)
        {
           
            int newDriftDirection = turnInput < 0 ? -1 : 1;

            
            if (newDriftDirection != driftDirection)
            {
                driftDirection = newDriftDirection;
                driftSwitchTimer = driftSwitchBuffer; 
            }

            
           // theRB.AddForce(transform.right * driftForce * driftDirection, ForceMode.Acceleration);

            
            currentDriftForce += driftForceIncreaseRate * Time.deltaTime;
            currentDriftForce = Mathf.Clamp(currentDriftForce, 0f, maxDriftForce);

            
            theRB.AddForce(transform.right * currentDriftForce * driftDirection, ForceMode.Acceleration);

            
            dL = driftDirection == -1 ? 1 : 0;
            dR = driftDirection == 1 ? 1 : 0;

            
        if (driftDirection != 0)
        {
            //Debug.Log($"Drift Direction: {driftDirection}");

            if (driftDirection == -1)
            {
                //Debug.Log("Applying drift friction to left wheel.");
                ApplyDriftFriction(leftWheel, driftFriction);
                ResetDriftFriction(rightWheel);
            }
            else if (driftDirection == 1)
            {
                //Debug.Log("Applying drift friction to right wheel.");
                ApplyDriftFriction(rightWheel, driftFriction);
                ResetDriftFriction(leftWheel);
            }
        }
        else
        {
            //Debug.Log("No drift detected, resetting both wheels.");
            ResetDriftFriction(leftWheel);
            ResetDriftFriction(rightWheel);
        }

            turnStrength = 7f; 
        }
        
        else if (turnInput == 0)
        {
            
            StopDrift();
        }
        
        
        /*if (turnInput != 0)
        {
            
            driftDirection = turnInput < 0 ? -1 : 1;

            
            theRB.AddForce(transform.right * driftForce * driftDirection, ForceMode.Acceleration);

            
            dL = driftDirection == -1 ? 1 : 0;
            dR = driftDirection == 1 ? 1 : 0;

            
            turnStrength = 7f;

            
            if (driftDirection == -1)
            {
                ApplyDriftFriction(leftWheel);
                ResetDriftFriction(rightWheel); 
            }
            else
            {
                ApplyDriftFriction(rightWheel);
                ResetDriftFriction(leftWheel); 
            }
        }
        else
        {
            
            StopDrift();
        }*/
            
        
    }

    private void StopDrift()
    {
        currentDriftForce = 0f;
        isDrifting = false;
        driftTimer = 0f;
        turnStrength = 15f;
        dL = 0;
        dR = 0;

        
        ResetDriftFriction(leftWheel);
        ResetDriftFriction(rightWheel);

        

        //StopDriftEffects();

        if (isBoosting)
        {
            Vector3 boostForce = transform.forward * boostStrength;
            theRB.AddForce(boostForce, ForceMode.VelocityChange);
        }

    }

    
    
    private void Flashbang()
    {
        fB = 1;

        Ray ray = new Ray(flashCamera.transform.position, flashCamera.transform.forward);
        //Debug.DrawRay(ray.origin, ray.direction * detectionRange, Color.red, 2f); // Draws a red ray for 2 seconds

        if (Physics.Raycast(ray, out RaycastHit hit, detectionRange))
        {
            Debug.Log("FlashCamera Detected Object: " + hit.collider.gameObject.name);
        }
        
        camUI.SetActive(true);

    }

    private void CamOff()
    {
        camUI.SetActive(false);

        fB = 0;

    }
    
    



    void StartWheelMove()
    {
        theRB.centerOfMass = centerOfMass;

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

    void DoWheelMove()
    {
        float forwardSpeed = Vector3.Dot(transform.forward, theRB.velocity);
        float speedFactor = Mathf.InverseLerp(0, maxSpeed, forwardSpeed);
        
        float currentMotorTorque = Mathf.Lerp(reverseAccel, 0, speedFactor);
        float currentSteerRange = Mathf.Lerp(turnStrength, turnStrength / 2, speedFactor);
        bool isAccelerating = Mathf.Sign(speedInput) == Mathf.Sign(forwardSpeed);

        foreach (WheelMove wheel in drivingWheels)
        {
            if (isAccelerating)
            {
                wheel.wheelCollider.motorTorque = speedInput * currentMotorTorque;
                wheel.wheelCollider.brakeTorque = 0;
                
            }
            else
            {
                wheel.wheelCollider.brakeTorque = Mathf.Abs(speedInput) * wheel.wheelCollider.brakeTorque;
                wheel.wheelCollider.motorTorque = 0;
            }
        }

        foreach (WheelMove wheel in wheels)
        {
            
            wheel.UpdateWheel();
        }

        foreach (WheelMove wheel in steeringWheels)
        {
            
            wheel.wheelCollider.steerAngle = turnInput * currentSteerRange;
            
        }
    } 

    
    void ApplyDriftFriction(WheelCollider wheel, float targetFriction)
    {
        //Debug.Log($"Applying drift friction to: {wheel.name} with target friction: {targetFriction}");
        if (wheel.GetGroundHit(out var hit))
        {
            wheel.forwardFriction = UpdateFriction(wheel.forwardFriction, targetFriction);
            wheel.sidewaysFriction = UpdateFriction(wheel.sidewaysFriction, targetFriction);
        }
        else
        {
            Debug.Log($"Wheel {wheel.name} is not grounded.");
        }
    }

    WheelFrictionCurve UpdateFriction(WheelFrictionCurve friction, float targetFriction)
    {
        //Debug.Log($"Current stiffness: {friction.stiffness}, Target stiffness: {targetFriction}");
        friction.stiffness = Mathf.Lerp(friction.stiffness, targetFriction, Time.deltaTime * 5f);
        return friction;
    }

    void ResetDriftFriction(WheelCollider wheel)
    {
        wheel.forwardFriction = DefaultFriction(wheel.forwardFriction);
        wheel.sidewaysFriction = DefaultFriction(wheel.sidewaysFriction);
    }

    WheelFrictionCurve DefaultFriction(WheelFrictionCurve friction)
    {
        friction.stiffness = defaultFriction;
        return friction;
    }


}
