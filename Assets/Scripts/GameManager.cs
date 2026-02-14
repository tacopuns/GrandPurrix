using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public RaceInitializer raceInitializer;

    public PreRaceHandler preRaceHandler;

    public void StartNewRace()
    {
        preRaceHandler.ResetRace();
        preRaceHandler.StartCoroutine(preRaceHandler.StartRaceCountdown());
    
    }

    public void StartNewGameMode()
    {
        raceInitializer.ResetRaceData();
        StartNewRace();

    }
}
