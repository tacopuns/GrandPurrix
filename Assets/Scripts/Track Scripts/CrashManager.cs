/*using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CrashManager : MonoBehaviour
{
    public List<Rigidbody> racerRigidbodies = new List<Rigidbody>();

    private Transform respawnPoint;

    private CheckpointManager checkpointManager;

    void Start()
    {
        
        checkpointManager = CheckpointManager.Instance;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") || other.CompareTag("Opp"))
        {
            Rigidbody racerRB = other.GetComponent<Rigidbody>();

            if (racerRB != null)
            {
                // adds the Rigidbody to the list if not already present
                if (!racerRigidbodies.Contains(racerRB))
                {
                    racerRigidbodies.Add(racerRB);
                }

                float currentSpeed = racerRB.velocity.magnitude;

                if (currentSpeed > 20f)
                {
                    // Retrieve the racer's last passed checkpoint index
                    int lastCheckpointIndex = checkpointManager.GetLastPassedCheckpointIndex(other.gameObject);

                    // Reset the racer's position to the most recent checkpoint
                    racerRB.transform.position = checkpointManager.checkpoints[lastCheckpointIndex].position;
                    //theres a bug where if you drive backwards into the hazard you respawn backwards.

                    racerRB.velocity = Vector3.zero;

        
                }
        
                if (other.CompareTag("Player"))
                {
                    Input.ResetInputAxes();
                }

            }
        }
    }
}*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CrashManager : MonoBehaviour
{
    public List<Rigidbody> racerRigidbodies = new List<Rigidbody>();

    private Transform respawnPoint;

    private CheckpointManager checkpointManager;

    // Define the waypoint indices for each checkpoint
    public int[] waypointIndicesPerCheckpoint = { };

    void Start()
    {
        checkpointManager = CheckpointManager.Instance;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") || other.CompareTag("Opp"))
        {
            Rigidbody racerRB = other.GetComponent<Rigidbody>();

            if (racerRB != null)
            {
                // adds the Rigidbody to the list if not already present
                if (!racerRigidbodies.Contains(racerRB))
                {
                    racerRigidbodies.Add(racerRB);
                }

                float currentSpeed = racerRB.velocity.magnitude;

                if (currentSpeed > 20f)
                {
                    // Retrieve the racer's last passed checkpoint index
                    int lastCheckpointIndex = checkpointManager.GetLastPassedCheckpointIndex(other.gameObject);

                    // Reset the racer's position to the most recent checkpoint
                    racerRB.transform.position = checkpointManager.checkpoints[lastCheckpointIndex].position;
                    

                    // Reset the racer's velocity and rotation
                    racerRB.velocity = Vector3.zero;
                    racerRB.angularVelocity = Vector3.zero;
                    //racerRB.transform.rotation = Quaternion.identity;

                    // Get the corresponding waypoint index for the checkpoint
                    int nextWaypointIndex = waypointIndicesPerCheckpoint[lastCheckpointIndex];

                    // Update the waypoint index for AI racers
                    if (other.CompareTag("Opp"))
                    {
                        AICarController aiCar = other.GetComponent<AICarController>();
                        if (aiCar != null)
                        {
                            // Reset AI's speed and acceleration
                            aiCar.currentSpeed = 0f;
                            aiCar.cpuSpeed = 0f;
                            aiCar.currentWaypointIndex = nextWaypointIndex;
                        }
                    }
                }
        
                if (other.CompareTag("Player"))
                {
                    Input.ResetInputAxes();
                }

            }
        }
    }
}


