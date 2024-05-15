using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SwitchToAI : MonoBehaviour
{
    private RaceCompletion raceCompletion;

    private CarController carController;
    //private PlayerInput playerInput;
    private AICarController aiCarController;

    void Start()
    {
        raceCompletion = FindObjectOfType<RaceCompletion>();
        carController = GetComponent<CarController>();
        //playerInput = GetComponent<PlayerInput>();
        aiCarController = GetComponent<AICarController>();
    }

    
    void Update()
    {
        if (raceCompletion != null && raceCompletion.finishRace)
        {
            SwapController();
        }
    }

    private void SwapController()
    {
        carController.enabled = false;
        //playerInput.enabled = false;
        aiCarController.enabled = true;
    }
}
