using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class CarController : MonoBehaviour
{
    public Rigidbody theRB;

    public float reverseAccel = 50f, turnStrength = 20f, gravityForce = 10f, dragOnGround = 7f;
    
    public LayerMask whatIsGround; //anything set as ground, the car will be able to move on
    private float groundRayLength = .5f;
    public Transform groundRayPoint;
    
    private float speedInput, turnInput;
    
    private bool grounded;
    
    [SerializeField]
    private float currentSpeed = 0f;
    private float accelerationRate;
    public float maxSpeed = 260f;

    [SerializeField]
    private float accelerationTimer = 0.0f;
    private float accelerationTime = 2.0f; //time needed to reach max speed

    private bool isDrifting = false;
    //private float driftDuration = 2.0f;
    private float driftTimer = 0.0f;

    public ParticleSystem driftXF;
    public GameObject skidXF;
    
    private bool isBoosting = false;
    public float boostForce = 50;
    public float boostStrength = 100f;
    
    private InputActionAsset inputAsset;
    private InputActionMap gameplay;
    private InputAction MoveForward;
    private InputAction Turn;
    private InputAction Drift;

    private CheckpointManager checkpointManager;
    public int currentCheckpointIndex = 0;
    

    
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
        theRB.transform.parent = null;

        checkpointManager = CheckpointManager.Instance;
        currentCheckpointIndex = checkpointManager.GetLastPassedCheckpointIndex(gameObject);

        driftXF = GetComponentInChildren<ParticleSystem>();
        skidXF.SetActive(false);
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
    }


    private void StartDrift()
    {
        if (currentSpeed > 20f && (turnInput < -0.1f || turnInput > 0.1f))
        {
            isDrifting = true;
            driftTimer = 0f;

            PlayDriftEffects();
        }
        else
        {
            StopDrift();
        }

    }

    private void StopDrift()
    {
        isDrifting = false;
        driftTimer = 0f;
        turnStrength = 40f;

         StopDriftEffects();

        if (isBoosting)
        {
            Vector3 boostForce = transform.forward * boostStrength;
            theRB.AddForce(boostForce, ForceMode.VelocityChange);
        }

    }

    private void PlayDriftEffects()
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
}

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


    public float driftForce = 10f;

    void PowerSlide()
    {

            if (turnInput < 0)
            {
                ApplyLeftDriftForce();

                
            }
            else if (turnInput > 0)
            {
                
                ApplyRightDriftForce();

               
            }
            else
            {
                StopDrift();
            }
        
    }
    

    void ApplyLeftDriftForce()
    {
        theRB.AddForce(-transform.right * driftForce, ForceMode.Acceleration);
        turnStrength = 50f;
    }

    void ApplyRightDriftForce()
    {
        theRB.AddForce(transform.right * driftForce, ForceMode.Acceleration);
        turnStrength = 50f;

    }

        
        

}
