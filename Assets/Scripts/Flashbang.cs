using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Flashbang : MonoBehaviour
{
    public GameObject objThatSpawns;
    public Transform startPos;

    public Transform opponentTarget;

    public float rotationSpeed = 6f;
    public float paparazziSpeed = 20f;
    public float dragOnGround = 5f;
    public float gravityForce = 10f;

    public float matchSpeedDistance = 5f;

    public float groundRayLength = .5f;
    public Transform groundRayPoint;

    public Rigidbody aiRB;

    



    // Start is called before the first frame update
    void Start()
    {
        //
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnEnable()
    {
        CallCameras();
        //objThatSpawns.SetActive(true);
        //reference to racestatshud racer positions and player position, compare
        MoveTowards(opponentTarget.position);

        //instantiate, select target, move to target
    }

    public void OnDisable()
    {
        //destory
    }

    public void CallCameras()
    {
       
       Instantiate(objThatSpawns, startPos.position, Quaternion.identity);
    }

    void MoveTowards(Vector3 targetPosition)
    {
        Vector3 moveDirection = (targetPosition - opponentTarget.position).normalized;
        
        
        RotateTowards(moveDirection);

        
        float distanceToTarget = Vector3.Distance(transform.position, targetPosition);
            
        if (distanceToTarget < matchSpeedDistance)
        {
            //aiRB.velocity = plRB.GetComponent<Rigidbody>().velocity;
        }
        else
        {
            ApplyForwardForce();
        }
        
    
    }

    void RotateTowards(Vector3 moveDirection)
    {
        Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }

    void ApplyForwardForce()
    {
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
        }
        else
        {
            HandleMidAir();
        }
    }

    void HandleMidAir()
    {
        aiRB.drag = 0.1f;
        aiRB.AddForce(Vector3.up * -gravityForce * 50f);
    }   

    

    //objThatSpawns follows paparazzi state system but needs to know how to detect other cars
    //maybe do position number in front of player and if player is first dont do anything

    //flashbanging mechanic
    //instantiate paparazzi cars 
    //-spawn^ a group of cars that spawn behind the player camera
    //-they follow a half circle trail in front of player
    //-they target closest racer in front of the player
    //-targeted racer gets slowed/stuned for .5 seconds
    //-particle blast
    //-by this point the player should have passed the target

}
