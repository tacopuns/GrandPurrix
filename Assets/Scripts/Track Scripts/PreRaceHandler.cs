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
        //StartCoroutine(StartRaceCountdown());
        raceStatsHUD.raceFrozen = true;
        DisableRacerMovement();
        raceStarted = false;
    }


    public IEnumerator StartRaceCountdown()
    {
        
        // Initialize race positions
        raceInitializer.InitializeStartingPositions();
        

        // Countdown loop
        float countdown = countdownTime;
        while (countdown > 0)
        {
            countdownText.text = countdown.ToString("F0");
            yield return new WaitForSeconds(1.0f);
            countdown--;
        }

        // Start the race
        countdownText.text = "GO!";
        
        EnableRacerMovement();
        yield return new WaitForSeconds(1.0f); // Display "GO!" for a moment
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

        /*AICarController aiCarController = racer.GetComponent<AICarController>();
        if (aiCarController != null)
        {
            aiCarController.enabled = false;
            return;
        }*/

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

        /*AICarController aiCarController = racer.GetComponent<AICarController>();
        if (aiCarController != null)
        {
            aiCarController.enabled = true;
            return;
        }*/

        CPUController cPUController = racer.GetComponent<CPUController>();
        if (cPUController != null)
        {
            cPUController.enabled = true;
            return;
        }
    }

    public bool IsRaceStarted()
    {
        return raceStarted;
    }
}
