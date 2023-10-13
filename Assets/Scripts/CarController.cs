using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class CarController : MonoBehaviour
{
    public Rigidbody theRB;

    public float forwardAccel = 50f, reverseAccel = 10f, turnStrength = 40f, gravityForce = 10f, dragOnGround = 7f;
    
    public LayerMask whatIsGround; 
    private float groundRayLength = .5f;
    public Transform groundRayPoint;
    
    private float speedInput, turnInput;
    
    private bool grounded;
    
    [SerializeField]
    private float currentSpeed = 0f;
    public float accelerationRate;
    public float maxSpeed = 530f;
    [SerializeField]
    private float accelerationTimer = 0.0f;
    public float accelerationTime = 5.0f;

    private bool isDrifting = false;
    public float driftDuration = 3.0f;
    public float driftTimer = 0.0f;
    
    private bool isBoosting = false;
    public float boostForce = 50f;
    public float boostStrength = 10f;
    
    

    private PlayerControls controls;

    private void Awake()
    {
        controls = new PlayerControls();
        controls.Gameplay.MoveForward.performed += ctx => speedInput = ctx.ReadValue<float>();
        controls.Gameplay.MoveForward.canceled += ctx => speedInput = 0f;

        controls.Gameplay.Turn.performed += ctx => turnInput = ctx.ReadValue<float>();
        controls.Gameplay.Turn.canceled += ctx => turnInput = 0f;

        controls.Gameplay.Drift.started += ctx => StartDrift();
        controls.Gameplay.Drift.canceled += ctx => StopDrift();
    }

    private void OnEnable()
    {
        controls.Enable();
    }

    private void OnDisable()
    {
        controls.Disable();
    }
    
    void Start()
    {
        theRB.transform.parent = null;
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
        }
        else if (speedInput < 0)
        {
            speedInput = -reverseAccel * 1000f * Time.deltaTime;
        }
        else
        {
            // If no input, reset acceleration
            accelerationTimer = 0f;
        }
        
    }

    private void FixedUpdate() 
    {
        grounded = false;
        RaycastHit hit;

        if (Physics.Raycast(groundRayPoint.position, -transform.up, out hit, groundRayLength, whatIsGround))
        {
            grounded = true;
            transform.rotation = Quaternion.Euler(transform.rotation.eulerAngles + new Vector3(0f, turnInput * turnStrength * Time.deltaTime, 0f));
        
            Quaternion newRotation = Quaternion.FromToRotation(transform.up, hit.normal) * transform.rotation;
            transform.rotation = Quaternion.Slerp(transform.rotation, newRotation, 100f);
        }

        if (grounded)
        {   
            theRB.drag = dragOnGround;

            if (Mathf.Abs(speedInput) > 0)
            {
                theRB.AddForce(transform.forward * speedInput, ForceMode.Acceleration);
            }
        }
        else
        {
            theRB.drag = 0.1f;
            theRB.AddForce(Vector3.up * -gravityForce * 100f);
        }

        UpdateDriftBoost();
    }

    private void HandleMoveForwardInput(float value)
    {
        speedInput = value;
    }

    private void HandleTurnInput(float value)
    {
        turnInput = value;
    }

    private void StartDrift()
    {
        isDrifting = true;
        driftTimer = 0f;
        turnStrength = 100f;

    }

    private void StopDrift()
    {
        isDrifting = false;
        driftTimer = 0f;
        turnStrength = 40f;

        if (isBoosting)
        {
            Vector3 boostForce = transform.forward * boostStrength;
            theRB.AddForce(boostForce, ForceMode.VelocityChange);
        }

    }

    void UpdateDriftBoost()
    {
        if (isDrifting)
        {
            driftTimer += Time.deltaTime; 

            if (driftTimer >= driftDuration)
            {
                driftTimer = driftDuration;
                ApplySpeedBoost();
            }
        }
        else
        {
            isBoosting = false;
            driftTimer = 0.0f; // Reset driftTimer when not drifting
        }
    }

    
    void ApplySpeedBoost()
    {
        if (!isBoosting)
        {
            isBoosting = true;
            Debug.Log("boost");
        }
    }

}
