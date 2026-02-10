using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SwitchToAI : MonoBehaviour
{
    private RaceCompletion raceCompletion;

    private CarController carController;
    //private PlayerInput playerInput;
    private AICarController aiCarController;

    private CPUController cPUController;

    private Alpha_2D_Character_In_3D_World spriteSwaps;

    void Start()
    {
        raceCompletion = FindObjectOfType<RaceCompletion>();
        carController = GetComponent<CarController>();
        //playerInput = GetComponent<PlayerInput>();
        aiCarController = GetComponent<AICarController>();
        cPUController = GetComponent<CPUController>();

        spriteSwaps = GetComponent<Alpha_2D_Character_In_3D_World>();
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
        //aiCarController.enabled = true;
        cPUController.enabled = true;
        spriteSwaps.enabled = true;
    }

    
}
