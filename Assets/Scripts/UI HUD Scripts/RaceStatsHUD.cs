using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using System;

public class RaceStatsHUD : MonoBehaviour
{
    public TextMeshProUGUI totalRaceTime;
    public TextMeshProUGUI currentLapText;
    public TextMeshProUGUI lapTimeText;
    public TextMeshProUGUI lapTimesListText;
    //public TextMeshProUGUI racerPositionsText;
    public TextMeshProUGUI playerPositionText;

    public float popScale = 1.2f;
    public float popDuration = 0.2f;

    private float raceTime;
    private int previousLapCount;
    private bool lap1TimeSet = false;
    private float lap1Time;
    private List<float> lapTimesList = new List<float>();

    private CheckpointManager checkpointManager;
    private RaceManager raceManager;

    public GameObject playerObject;

    public bool raceFrozen = false;

    void Start()
    {
        checkpointManager = CheckpointManager.Instance;
        raceManager = FindObjectOfType<RaceManager>();

        previousLapCount = checkpointManager.GetLapCount(playerObject); 

        raceTime = 0;
    }

    void Update()
    {
        if (!raceFrozen)
        {
            raceTime = raceTime + Time.deltaTime;
            
            TimeSpan time = TimeSpan.FromSeconds(raceTime);

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
    }

    public void FreezeRaceStats()
    {
        raceFrozen = true;
    }

    void UpdateRacerPositions()
    {
        if (raceManager != null)
        {
            List<GameObject> racers = raceManager.racers; 
            racers.Sort(raceManager.CompareRacers);

            string positionsListText = "Racer Positions:\n";
            for (int i = 0; i < racers.Count; i++)
            {
                positionsListText += $"Position {i + 1}: {racers[i].name}\n";
            }

            //racerPositionsText.text = positionsListText;
        }
    }

    private int previousPlayerIndex = -1; // Variable to store the previous player index

    private void UpdatePlayerPosition()
    {
        if (raceManager != null)
        {
            List<GameObject> racers = raceManager.racers;
            racers.Sort(raceManager.CompareRacers);

            int playerIndex = racers.FindIndex(racer => racer == playerObject);

            if (playerIndex != previousPlayerIndex) // Check if player position has changed
            {
                string ordinalPosition = GetOrdinal(playerIndex + 1);
                AnimatePositionText(ordinalPosition); // Call the new method to animate position text
                previousPlayerIndex = playerIndex; // Update previous player index
            }
            else
            {
                playerPositionText.text = GetOrdinal(playerIndex + 1); // Update position text without animation
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

    private void AnimatePositionText(string ordinalPosition)
    {
        playerPositionText.transform.localScale = Vector3.one;
        playerPositionText.transform.DOScale(popScale, popDuration).SetEase(Ease.OutQuad).OnComplete(() =>
        {
            playerPositionText.transform.DOScale(1f, popDuration).SetEase(Ease.OutQuad);
        }).SetLoops(1);

        playerPositionText.text = ordinalPosition;
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