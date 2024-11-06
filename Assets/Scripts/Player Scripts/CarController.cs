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

    private bool isDrifting = false;
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

    public bool isSteering = false;
    public bool canDrift = false;


    public WheelCollider leftWheel;
    public WheelCollider rightWheel;

    private float driftForce = 10f;

    private int driftDirection = 0;
    
    private void Awake()
    {
        inputAsset = this.GetComponent<PlayerInput>().actions;
        gameplay = inputAsset.FindActionMap("Gameplay");

        gameplay.FindAction("MoveForward").started += ctx => speedInput = ctx.ReadValue<float>();
        gameplay.FindAction("Turn").started += ctx => turnInput = ctx.ReadValue<float>();
        gameplay.FindAction("Drift").started += ctx => StartDrift();

        gameplay.FindAction("MoveForward").canceled += ctx => speedInput = 0f;
        gameplay.FindAction("Turn").canceled += ctx => turnInput = 0f;
        gameplay.FindAction("Drift").canceled += ctx => StopDrift();
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

        animator = GetComponentInChildren<Animator>();
        animator.enabled = true;
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

        DriftEnable();
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
            driftDirection = turnInput < 0 ? -1 : 1;
        }

        /*if (currentSpeed > 20f && (turnInput < -0.1f || turnInput > 0.1f))
        {
            isDrifting = true;
            driftTimer = 0f;

            //PlayDriftEffects();
        }*/
        else
        {
            //StopDrift();
        }

    }


    private void StopDrift()
    {
        isDrifting = false;
        driftTimer = 0f;
        driftDirection = 0;
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
            driftTimer = 0.0f; //reset time when drift stop or not drifting
            
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


    

    void PowerSlide()
    {

        float dynamicDriftForce = driftForce * Mathf.Abs(turnInput); // Modulate force with turnInput

        if (driftDirection == -1) {
            theRB.AddForce(-transform.right * dynamicDriftForce, ForceMode.Acceleration);
            turnStrength = 7f;
            ApplyDriftFriction(leftWheel);
            dL = 1;
            dR = 0;
        } 
        else if (driftDirection == 1) {
            theRB.AddForce(transform.right * dynamicDriftForce, ForceMode.Acceleration);
            turnStrength = 7f;
            ApplyDriftFriction(rightWheel);
            dL = 0;
            dR = 1;

            /*if (turnInput < 0)
            {
                ApplyLeftDriftForce();
                dL = 1;
                dR = 0;

                
            }
            else if (turnInput > 0)
            {
                
                ApplyRightDriftForce();
                dL = 0;
                dR = 1;

               
            }
            else
            {
                StopDrift();
                dL = 0;
                dR = 0;
            }*/
            
        }
    }
    
    

    

    /*void ApplyLeftDriftForce()
    {
        theRB.AddForce(-transform.right * driftForce, ForceMode.Acceleration);
        turnStrength = 7f;

        
        ApplyDriftFriction(leftWheel);
    }

    void ApplyRightDriftForce()
    {
        theRB.AddForce(transform.right * driftForce, ForceMode.Acceleration);
        turnStrength = 7f;
        
        
    
        ApplyDriftFriction(rightWheel);
        
    }*/

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
                //bool isLeftWheel = wheel.wheelMesh.localPosition.x < 0;
                //wheel.wheelCollider.motorTorque = 0;
                //wheel.UpdateWheel(isLeftWheel);
            }
            else
            {
                wheel.wheelCollider.brakeTorque = Mathf.Abs(speedInput) * wheel.wheelCollider.brakeTorque;
                wheel.wheelCollider.motorTorque = 0;
            }
        }

        foreach (WheelMove wheel in wheels)
        {
            //bool isLeftWheel = wheel.wheelMesh.localPosition.x < 0;
            //wheel.wheelCollider.brakeTorque = 0;
            wheel.UpdateWheel();
        }

        foreach (WheelMove wheel in steeringWheels)
        {
            //bool isLeftWheel = wheel.wheelMesh.localPosition.x < 0;
            wheel.wheelCollider.steerAngle = turnInput * currentSteerRange;
            //wheel.UpdateWheel(isLeftWheel);
        }
    } 

    
    void ApplyDriftFriction(WheelCollider wheel)
    {
        if (wheel.GetGroundHit(out var hit))
        {
            wheel.forwardFriction = UpdateFriction(wheel.forwardFriction);
            wheel.sidewaysFriction = UpdateFriction(wheel.sidewaysFriction);
        }
    }

    WheelFrictionCurve UpdateFriction(WheelFrictionCurve friction) 
    {
        friction.stiffness = 10f;
        //friction.
        return friction;
    }

    WheelFrictionCurve DefaultFriction (WheelFrictionCurve friction) 
    {
        friction.stiffness = 1f;
        //friction.
        return friction;
    }
    
    public WheelFrictionCurve originalForwardFriction;
    public WheelFrictionCurve originalSidewaysFriction;
    
    void ResetDriftFriction(WheelCollider wheel)
    {
        wheel.forwardFriction = DefaultFriction(wheel.forwardFriction);
        wheel.sidewaysFriction = DefaultFriction(wheel.sidewaysFriction);
    }

}
