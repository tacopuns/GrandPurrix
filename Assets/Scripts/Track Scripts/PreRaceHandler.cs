using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PreRaceHandler : MonoBehaviour
{
    public RaceInitializer raceInitializer;
    public RaceManager raceManager;
    public float countdownTime = 3.0f;
    public TextMeshProUGUI countdownText;

    public RaceStatsHUD raceStatsHUD;

    private bool raceStarted = false;

    void Start()
    {
        ResetRace();
    }
    
    public void ResetRace()
    {
        raceStatsHUD.raceFrozen = true;
        raceStatsHUD.raceTime = 0;
        ResetSpeed();
        DisableRacerMovement();
        ResetSplinePath();
        raceStarted = false;
    }


    public IEnumerator StartRaceCountdown()
    {
        
        raceInitializer.InitializeStartingPositions();
        

        float countdown = countdownTime;
        while (countdown > 0)
        {
            countdownText.text = countdown.ToString("F0");
            yield return new WaitForSeconds(1.0f);
            countdown--;
        }

        countdownText.text = "GO!";
        
        EnableRacerMovement();
        yield return new WaitForSeconds(1.0f);
        raceStarted = true;
        raceStatsHUD.raceFrozen = false;
        countdownText.text = "";
    }

    private void DisableRacerMovement()
    {
        foreach (GameObject racer in raceManager.racers)
        {
            DisableController(racer);
        }
    }

    private void EnableRacerMovement()
    {
        foreach (GameObject racer in raceManager.racers)
        {
            EnableController(racer);
        }
    }

    private void DisableController(GameObject racer)
    {
        CarController carController = racer.GetComponent<CarController>();
        if (carController != null)
        {
            carController.enabled = false;
            return;
        }

        CPUController cPUController = racer.GetComponent<CPUController>();
        if (cPUController != null)
        {
            cPUController.enabled = false;
            return;
        }
    }

    private void EnableController(GameObject racer)
    {
        CarController carController = racer.GetComponent<CarController>();
        if (carController != null)
        {
            carController.enabled = true;
            return;
        }

        CPUController cPUController = racer.GetComponent<CPUController>();
        if (cPUController != null)
        {
            cPUController.enabled = true;
            return;
        }

    }

    
     private void ResetSplinePath()
    {
        foreach (GameObject racer in raceManager.racers)
        {
            ResetSplineProgress(racer);
        }
    }
    
    private void ResetSplineProgress(GameObject racer)
    {
        CPUController cPUController = racer.GetComponent<CPUController>();
        if (cPUController != null)
        {
            cPUController.splineProgress = 0;
            return;
        }

    }

    private void ResetSpeed()
    {
        foreach (GameObject racer in raceManager.racers)
        {
            SetRacerSpeed(racer);
        }
    }
   

    private void SetRacerSpeed(GameObject racer)
    {
        CPUController cPUController = racer.GetComponent<CPUController>();
        if (cPUController != null)
        {
            cPUController.currentSpeed = 0;
            //cPUController.accelerationTimer = 0;
            return;
        }

    }



    public bool IsRaceStarted()
    {
        return raceStarted;
    }
}
