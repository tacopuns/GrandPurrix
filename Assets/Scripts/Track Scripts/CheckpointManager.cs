using System.Collections.Generic;
using UnityEngine;

public class CheckpointManager : MonoBehaviour
{
    public static CheckpointManager Instance;

    public List<Transform> checkpoints;
    public Dictionary<GameObject, int> lastPassedCheckpointIndex = new Dictionary<GameObject, int>();
    public Dictionary<GameObject, int> lapsCompleted = new Dictionary<GameObject, int>();

    public Dictionary<GameObject, bool> raceFinished = new Dictionary<GameObject, bool>();

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
        int lastPassedIndex = GetLastPassedCheckpointIndex(racer);

        lastPassedCheckpointIndex[racer] = checkpointIndex;

        if (checkpointIndex == 0 && lastPassedIndex == checkpoints.Count - 1)
        {
            IncrementLap(racer);
        }

        if (GetLapCount(racer) >= 2)
        {
            MarkRaceFinished(racer);
        }

    }


    public void IncrementLap(GameObject racer)
    {
        if (lapsCompleted.ContainsKey(racer))
        {
            lapsCompleted[racer]++;

        }
        else
        {
            lapsCompleted[racer] = 1; // Start counting laps from here
            
        }

        Debug.Log(racer.name + " completed lap " + lapsCompleted[racer]);

    }

    public int GetLapCount(GameObject racer)
    {
        if (lapsCompleted.ContainsKey(racer))
        {
            return lapsCompleted[racer];
        }
        else
        {
            return 0;
        }
    }

    private void MarkRaceFinished(GameObject racer)
    {
        if (!raceFinished.ContainsKey(racer))
        {
            raceFinished[racer] = true;
            Debug.Log(racer.name + " finished the race!");
        }
    }


    // vvv referenced in checkpoint trigger and individual racers vvv
    /*private int GetCheckpointIndex()
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
    }*/

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