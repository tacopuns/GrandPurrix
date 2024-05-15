using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StopMoving : MonoBehaviour
{
    private RaceCompletion raceCompletion;

    public Rigidbody vehicleRB;
    public float delayBeforeStop = 2.0f;
    
    void Start()
    {
        raceCompletion = FindObjectOfType<RaceCompletion>();
    }

    
    //when racecompletion bool finishrace = true, then vehicleRB.isKinematic = true;
    void Update()
    {
        if (raceCompletion != null && raceCompletion.finishRace)
        {
            vehicleRB.drag = 1;
            StartCoroutine(StopVehicleAfterDelay());
        }
    }

    private IEnumerator StopVehicleAfterDelay()
    {
        yield return new WaitForSeconds(delayBeforeStop);
        
        
        vehicleRB.isKinematic = false; //set to true
        
    }
}
