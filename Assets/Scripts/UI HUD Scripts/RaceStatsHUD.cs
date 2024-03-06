/* good version
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RaceStatsHUD : MonoBehaviour
{
    public Text totalRaceTime;
    public Text currentLapText;
    public Text lapTimeText;

    private float raceTime;
    private CheckpointManager checkpointManager;
    public GameObject playerObject;

    private bool lap1TimeSet = false;
    private float lap1Time;

    void Start()
    {
        checkpointManager = CheckpointManager.Instance;
    }

    void Update()
    {
        raceTime = Time.time;
        string formattedTime = RaceTimeFormat(raceTime);
        totalRaceTime.text = "Total Race Time: " + formattedTime;

        if (checkpointManager != null)
        {
            int currentLap = checkpointManager.GetLapCount(playerObject); // Retrieve current lap number
            currentLapText.text = "Lap: " + currentLap; // Display current lap number

            if (currentLap == 1 && !lap1TimeSet)
            {
                // Set lap time for lap 1
                lap1Time = Time.time;
                lapTimeText.text = "Lap Time: " + RaceTimeFormat(lap1Time);
                lap1TimeSet = true;
            }
            else if (currentLap > 1)
            {
                // Retrieve and display lap time for the current lap
                float currentLapTime = GetCurrentLapTime(playerObject);
                string formattedLapTime = RaceTimeFormat(currentLapTime);
                lapTimeText.text = "Lap Time: " + formattedLapTime; // Display lap time
            }
        }
    }

    // Retrieve lap time for the current lap from CheckpointManager's lapTimes dictionary
    float GetCurrentLapTime(GameObject racer)
    {
        if (checkpointManager.lapTimes.ContainsKey(racer))
        {
            List<float> racerLapTimes = checkpointManager.lapTimes[racer];
            if (racerLapTimes.Count > 0)
            {
                return racerLapTimes[racerLapTimes.Count - 1]; // Return the most recent lap time
            }
        }
        return 0f; // Return 0 if lap time is not found
    }

    string RaceTimeFormat(float timeInSeconds)
    {
        int minutes = Mathf.FloorToInt(timeInSeconds / 60f);
        int seconds = Mathf.FloorToInt(timeInSeconds % 60f);
        int milliseconds = Mathf.FloorToInt((timeInSeconds * 1000f) % 1000f);

        return string.Format("{0:00}:{1:00}:{2:000}", minutes, seconds, milliseconds);
    }
}
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RaceStatsHUD : MonoBehaviour
{
    public TextMeshProUGUI totalRaceTime;
    public TextMeshProUGUI currentLapText;
    public TextMeshProUGUI lapTimeText;
    public TextMeshProUGUI lapTimesListText;
    public TextMeshProUGUI racerPositionsText;
    public TextMeshProUGUI playerPositionText;

    private float raceTime;
    private int previousLapCount;
    private bool lap1TimeSet = false;
    private float lap1Time;
    private List<float> lapTimesList = new List<float>();

    private CheckpointManager checkpointManager;
    private RaceManager raceManager;

    public GameObject playerObject;

    void Start()
    {
        checkpointManager = CheckpointManager.Instance;
        raceManager = FindObjectOfType<RaceManager>();

        previousLapCount = checkpointManager.GetLapCount(playerObject); 
    }

    void Update()
    {
        raceTime = Time.time;
        totalRaceTime.text = RaceTimeFormat(raceTime);


        int currentLap = checkpointManager.GetLapCount(playerObject);
        currentLapText.text = "LAP " + currentLap;

        // Check if lap count has changed
        if (currentLap != previousLapCount)
        {
            if (currentLap == 1 && !lap1TimeSet)
            {
                lap1Time = raceTime;
                lapTimeText.text = "LAP TIME: + " + RaceTimeFormat(lap1Time);
                lap1TimeSet = true;
                lapTimesList.Add(lap1Time);
                UpdateLapTimesList();
            }
            else if (currentLap > 1)
            {
                float currentLapTime = GetCurrentLapTime(playerObject);
                lapTimeText.text = "LAP TIME: + " + RaceTimeFormat(currentLapTime);
                lapTimesList.Add(currentLapTime);
                UpdateLapTimesList();
            }

            // Update previous lap count
            previousLapCount = currentLap;
        }

        UpdateRacerPositions();
        UpdatePlayerPosition();
    }

    void UpdateRacerPositions()
    {
        // get positions from UpdateRacerPositions() racemanager and convert to string
        if (raceManager != null)
        {
            List<GameObject> racers = raceManager.racers; 

            
            racers.Sort(raceManager.CompareRacers);

            
            string positionsListText = "Racer Positions:\n";
            for (int i = 0; i < racers.Count; i++)
            {
                positionsListText += $"Position {i + 1}: {racers[i].name}\n";
            }

            
            racerPositionsText.text = positionsListText;
        }
    
    }

    private void UpdatePlayerPosition()
    {
        if (raceManager != null)
        {
            List<GameObject> racers = raceManager.racers;

            
            racers.Sort(raceManager.CompareRacers);

            int playerIndex = racers.FindIndex(racer => racer == playerObject);

            
            if (playerIndex >= 0)
            {
                string ordinalPosition = GetOrdinal(playerIndex + 1); 
                playerPositionText.text = $"{ordinalPosition}";
            }
            else
            {
                playerPositionText.text = "N/A";
            }
        }
    }

    private string GetOrdinal(int number)
    {
        if (number <= 0)
        {
            return number.ToString();
        }


        switch (number % 10)
        {
            case 1:
                return number + "ST";
            case 2:
                return number + "ND";
            case 3:
                return number + "RD";
            default:
                return number + "TH";
        }
    }

    void UpdateLapTimesList()
    {
        string lapTimesListString = "LAP TIMES:\n";
        for (int i = 0; i < lapTimesList.Count; i++)
        {
            lapTimesListString += "LAP " + (i + 1) + ": " + RaceTimeFormat(lapTimesList[i]) + "\n";
        }
        lapTimesListText.text = lapTimesListString;
    }

    float GetCurrentLapTime(GameObject racer)
    {
        if (checkpointManager.lapTimes.ContainsKey(racer))
        {
            List<float> racerLapTimes = checkpointManager.lapTimes[racer];
            if (racerLapTimes.Count > 0)
            {
                return racerLapTimes[racerLapTimes.Count - 1];
            }
        }
        return 0f;
    }

    string RaceTimeFormat(float timeInSeconds)
    {
        int minutes = Mathf.FloorToInt(timeInSeconds / 60f);
        int seconds = Mathf.FloorToInt(timeInSeconds % 60f);
        int milliseconds = Mathf.FloorToInt((timeInSeconds * 1000f) % 1000f);
        return string.Format("{0:00}:{1:00}:{2:000}", minutes, seconds, milliseconds);
    }
}
