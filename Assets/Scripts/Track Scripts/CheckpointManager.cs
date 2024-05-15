using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class CheckpointManager : MonoBehaviour
{
    public static CheckpointManager Instance;

    public List<Transform> checkpoints;
    public Dictionary<GameObject, int> lastPassedCheckpointIndex = new Dictionary<GameObject, int>();
    public Dictionary<GameObject, int> lapsCompleted = new Dictionary<GameObject, int>();
    public Dictionary<GameObject, bool> raceFinished = new Dictionary<GameObject, bool>();

    public Dictionary<GameObject, List<float>> lapTimes = new Dictionary<GameObject, List<float>>();

    private float lapTime;

    public Dictionary<GameObject, float> lapStartTimes = new Dictionary<GameObject, float>();

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

        if (GetLapCount(racer) >= 1)
        {
            MarkRaceFinished(racer);
        }
    }

    public void IncrementLap(GameObject racer)
    {
        if (lapsCompleted.ContainsKey(racer))
        {

        lapTime = CalculateLapTime(racer);

        RecordLapTime(racer, lapTime);

        // lap time cannot be logged inside this BAD THIS ONE BAD

        lapsCompleted[racer]++;
        lapStartTimes[racer] = Time.time;
        }
        else // LAP 1 ONLY
        {
            lapStartTimes[racer] = Time.time;

            Debug.Log(racer.name + " completed lap 1 in " + FormatLapTime(Time.time));

        
            lapsCompleted[racer] = 1;

        }

        if (lapsCompleted[racer] > 1)
        {
            Debug.Log(racer.name + " completed lap " + lapsCompleted[racer] + " in " + FormatLapTime(lapTime));
        }

        //THIS ONE BAD FOR LAP 1 ONLY Debug.Log(racer.name + " completed lap " + lapsCompleted[racer] + " in " + FormatLapTime(lapTime));
        
        
        Debug.Log(racer.name + " completed lap " + lapsCompleted[racer] + ". Race time is currently " + FormatLapTime(Time.time));

    
    }

    public int GetLapCount(GameObject racer) //never checks to set lap count to 0 at start of race
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

    public void RecordLapTime(GameObject racer, float lapTime)
    {
        if (!lapTimes.ContainsKey(racer))
        {
            lapTimes[racer] = new List<float>();
        }
        lapTimes[racer].Add(lapTime);

        // this doesnt count lap 1 fsr 
    }

    private string FormatLapTime(float lapTime)
    {
        int minutes = Mathf.FloorToInt(lapTime / 60f);
        int seconds = Mathf.FloorToInt(lapTime % 60f);
        int milliseconds = Mathf.FloorToInt((lapTime * 1000f) % 1000f);

        return string.Format("{0:00}:{1:00}:{2:000}", minutes, seconds, milliseconds);
    }

    private float CalculateLapTime(GameObject racer)
    {
        if (lapStartTimes.ContainsKey(racer))
        {
            float lapStartTime = lapStartTimes[racer];
            return Time.time - lapStartTime;
        }
        else
        {
            return 0f;
        }
    }

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

    public float DistanceToNextCheckpoint(GameObject racer) //all racers should have checkpoint 0 has their current checkpoint without passing it
    {
        if (lastPassedCheckpointIndex.ContainsKey(racer))
        {
            int nextCheckpointIndex = (lastPassedCheckpointIndex[racer] + 1) % checkpoints.Count;
            Transform nextCheckpoint = checkpoints[nextCheckpointIndex];
            Vector3 racerPosition = racer.transform.position;
            return Vector3.Distance(racerPosition, nextCheckpoint.position);
        }
        else
        {
            //Debug.LogError("Racer has not passed any checkpoints.");
            //return Mathf.Infinity;
            return 100;
        }
    }
  
}
