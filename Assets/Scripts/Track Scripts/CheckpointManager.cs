/*using System.Collections.Generic;
using UnityEngine;

public class CheckpointManager : MonoBehaviour
{
    public static CheckpointManager Instance;

    public List<Transform> checkpoints;
    public Dictionary<GameObject, int> lastPassedCheckpointIndex = new Dictionary<GameObject, int>();
    public Dictionary<GameObject, int> lapsCompleted = new Dictionary<GameObject, int>();

    public Dictionary<GameObject, bool> raceFinished = new Dictionary<GameObject, bool>();

    public Dictionary<GameObject, List<float>> lapTimes = new Dictionary<GameObject, List<float>>();

    private float lapTime;

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

            // Calculate lap time
            lapTime = CalculateLapTime(racer);

            // Record lap time
            RecordLapTime(racer, lapTime);


        }
        else
        {
            lapsCompleted[racer] = 1; // Start counting laps from here
            
        }

        Debug.Log(racer.name + " completed lap " + lapsCompleted[racer]);
        Debug.Log(racer.name + " completed lap " + lapsCompleted[racer] + " in " + FormatLapTime(lapTime));

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

    private void RecordLapTime(GameObject racer, float lapTime)
    {
        if (!lapTimes.ContainsKey(racer))
        {
            lapTimes[racer] = new List<float>();
        }
        lapTimes[racer].Add(lapTime);
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
        // Calculate lap time here (e.g., using Time.time - startTime)
        // For simplicity, let's just return a dummy value for now
        return 60.0f; // Dummy lap time (replace with actual calculation)
    }


    // vvv referenced in checkpoint trigger and individual racers vvv
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

}*/

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

    //public RaceStatsHUD raceStatsHUD;
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

        if (GetLapCount(racer) >= 2)
        {
            MarkRaceFinished(racer);
        }
    }

    public void IncrementLap(GameObject racer)
    {
        if (lapsCompleted.ContainsKey(racer))
        {
        // Calculate lap time
        lapTime = CalculateLapTime(racer);

        // Record lap time
        RecordLapTime(racer, lapTime);

        // Log lap time THIS ONE BAD
        //Debug.Log(racer.name + " completed lap " + lapsCompleted[racer] + " in " + FormatLapTime(lapTime));

        // Update lap count
        lapsCompleted[racer]++;
        lapStartTimes[racer] = Time.time; // Update lap start time
        }
        else // LAP 1 ONLY
        {
        // Initialize lap start time when the racer completes the first lap
        lapStartTimes[racer] = Time.time;

        // Log lap time for the first lap THIS ONE GOOD
        Debug.Log(racer.name + " completed lap 1 in " + FormatLapTime(Time.time));

        // Initialize lap count for the racer
        lapsCompleted[racer] = 1;

        }

        if (lapsCompleted[racer] > 1)
        {
            Debug.Log(racer.name + " completed lap " + lapsCompleted[racer] + " in " + FormatLapTime(lapTime));
        }

        //THIS ONE BAD FOR LAP 1 ONLY Debug.Log(racer.name + " completed lap " + lapsCompleted[racer] + " in " + FormatLapTime(lapTime));
        
        //Current Race time
        Debug.Log(racer.name + " completed lap " + lapsCompleted[racer] + ". Race time is currently " + FormatLapTime(Time.time));

    
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

    public void RecordLapTime(GameObject racer, float lapTime)
    {
        if (!lapTimes.ContainsKey(racer))
        {
            lapTimes[racer] = new List<float>();
        }
        lapTimes[racer].Add(lapTime);

        // this doesnt count lap 1 fsr Debug.Log(racer.name + " lap times: " + string.Join(", ", lapTimes[racer].Select(t => FormatLapTime(t))));
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
  
}
