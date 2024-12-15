using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

public class SpectatorCamera : MonoBehaviour
{
     
    public RaceCompletion raceCompletion;
    public int spectatorCameraIndex; // The index of the spectator camera to enable

    private void OnTriggerEnter(Collider other)
    {
        // Ensure the race is finished and the collider is a racer
        if (raceCompletion.finishRace && other.CompareTag("Player"))
        {
            raceCompletion.HandleSpectatorCamera(spectatorCameraIndex);
        }
    }

}
