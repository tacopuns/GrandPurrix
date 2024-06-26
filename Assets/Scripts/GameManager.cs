using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    //public RaceManager raceManager;
    public RaceInitializer raceInitializer;

    public PreRaceHandler preRaceHandler;

    public SavePlayerData savePlayerData;

    public RaceManager raceManager;

    public void StartNewRace()
    {
        //savePlayerData.LoadRacerPositions(raceManager.racers);
        preRaceHandler.StartCoroutine(preRaceHandler.StartRaceCountdown());
    
    }

    /*public void EndRace()
    {
        raceManager.FinishRace();
        // Any additional logic to end the race
    }*/

    public void StartNewGameMode()
    {
        raceInitializer.ResetRaceData();
        StartNewRace();

    }
}
