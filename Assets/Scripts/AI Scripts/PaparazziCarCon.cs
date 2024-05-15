using UnityEngine;

public class PaparazziCarCon : MonoBehaviour
{
    public enum PaparazziState { Waiting, FollowingPlayer, Leaving, Interviewing }

    public PaparazziState currentState;

    public Transform player;

    public Transform paparazzi;

    public Transform waitAreaInitial;
    public Transform waitAreaFinal;

    public float rotationSpeed = 6f;
    public float paparazziSpeed = 20f;
    public float dragOnGround = 5f;
    public float gravityForce = 10f;

    public float matchSpeedDistance = 5f;

    public float groundRayLength = .5f;
    public Transform groundRayPoint;

    public Rigidbody aiRB;
    public Rigidbody plRB;

    public Collider triggerZone;

    public float interviewDuration = 10f;
    public float interviewTimer;
    public bool startInterview = false;
    public bool followingTarget = false;

    public InterviewHolder interviewHolder;


    void Start()
    {
        currentState = PaparazziState.Waiting;
        interviewTimer = interviewDuration;
        
        interviewHolder = GetComponent<InterviewHolder>();
        
    }

    void FixedUpdate()
    {
        switch (currentState)
        {
            case PaparazziState.Waiting:
                UpdateWaitingState();
                break;
            case PaparazziState.FollowingPlayer:
                MoveTowards(player.position);
                followingTarget = true;
                break;
            case PaparazziState.Leaving:
                MoveTowards(waitAreaFinal.position);
                GoHome();
                break;
            case PaparazziState.Interviewing:
                MoveTowards(player.position);
                Interview();
                TalkingTimer();
                break;
        }

    }


    void UpdateWaitingState()
    {

        if (triggerZone != null && triggerZone.bounds.Contains(player.position))
        {
            aiRB.isKinematic = false;
            ChangeState(PaparazziState.FollowingPlayer);
            Destroy(triggerZone);
        }

        if (waitAreaFinal.GetComponent<Collider>().bounds.Contains(aiRB.position))
        {
            aiRB.isKinematic = true;
        }
        
    }

    void MoveTowards(Vector3 targetPosition)
    {
        Vector3 moveDirection = (targetPosition - transform.position).normalized;
        
        
        RotateTowards(moveDirection);

        
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (currentState == PaparazziState.FollowingPlayer || currentState == PaparazziState.Interviewing)
        {
            
            if (distanceToPlayer < matchSpeedDistance)
            {
                aiRB.velocity = plRB.GetComponent<Rigidbody>().velocity;

                
                if (!startInterview)
                {
                    startInterview = true;
                    ChangeState(PaparazziState.Interviewing);

                    Interview();
                }
            }
            else
            {
                ApplyForwardForce();
            }
        }

        if(currentState == PaparazziState.Leaving)
        {
            aiRB.velocity = aiRB.GetComponent<Rigidbody>().velocity;
            ApplyForwardForce();
            
            GoHome();
            

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


    void Interview()
    {
        if (interviewTimer <= 0f)
        {
            ChangeState(PaparazziState.Leaving);
            Debug.Log("Interview ended.");
        }
        else
        {
            TalkingTimer();
        }
        
        /*TalkingTimer();
        
        Debug.Log("interviewing");

        if (interviewTimer <= 0f)
        {
            Debug.Log("am done");
            ChangeState(PaparazziState.Leaving);
            Debug.Log("ok am done");
        }
        else
        {
            //TalkingTimer();
            //interviewTimer -= Time.deltaTime;
            // displaying interview UI, handling dialogue, etc.
        }*/
    }

    void TalkingTimer()
    {
        interviewTimer -= Time.deltaTime;
    }

    void GoHome()
    {
        
        float distanceToFinal = Vector3.Distance(aiRB.position, waitAreaFinal.position);

        if (distanceToFinal < 3f)
        {
            ChangeState(PaparazziState.Waiting);
        }
    }
   

    public void EndInterview()
    {
        interviewTimer = 0f;
        ChangeState(PaparazziState.Leaving);
        Debug.Log("Interview ended early.");
    }

    


    void ChangeState(PaparazziState newState)
    {
        currentState = newState;
    }
    
}
