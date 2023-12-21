using System.Collections.Generic;
using UnityEngine;

public class CheckpointManager : MonoBehaviour
{
    public static CheckpointManager Instance;

    public List<Transform> checkpoints;
    public Dictionary<GameObject, int> lastPassedCheckpointIndex = new Dictionary<GameObject, int>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void UpdateCheckpoint(GameObject racer, int checkpointIndex)
    {
        lastPassedCheckpointIndex[racer] = checkpointIndex;

        // Additional logic when a checkpoint is passed (e.g., lap counting)
        Debug.Log(racer.name + " passed checkpoint " + checkpointIndex);
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

    // New method to get the last passed checkpoint index for a specific racer
    public int GetLastPassedCheckpointIndex(GameObject racer)
    {
        if (lastPassedCheckpointIndex.ContainsKey(racer))
        {
            return lastPassedCheckpointIndex[racer];
        }
        else
        {
            return 0;
        }
    }
}