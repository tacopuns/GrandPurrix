using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CheckpointTrigger : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") || other.CompareTag("Opp"))
        {
            int checkpointIndex = GetCheckpointIndex();
            //Debug.Log("CheckpointTrigger OnTriggerEnter: " + other.name + ", Checkpoint Index: " + checkpointIndex);

            CheckpointManager.Instance.UpdateCheckpoint(other.gameObject, checkpointIndex);
            //Debug.Log("CheckpointTrigger: Checkpoint Updated");
        }
    }

    private int GetCheckpointIndex()
    {

        int checkpointIndex = -1;

        for (int i = 0; i < CheckpointManager.Instance.checkpoints.Count; i++)
        {
            if (transform.position == CheckpointManager.Instance.checkpoints[i].position)
            {
                checkpointIndex = i;
                break;
            }
        }

        return checkpointIndex;
    }


}