using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

public class SpectatorCamera : MonoBehaviour
{
     
    public RaceCompletion raceCompletion;
    public SpectatorHandler spectatorHandler;
    public int spectatorCameraIndex; // The index of the spectator camera to enable

    public bool finishLine = false;

    //public List<CinemachineVirtualCamera> spectatorCameras;
   //private int currentCameraIndex = -1;

    void Start()
    {
        spectatorHandler.SwitchToCamera(-1);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (raceCompletion.finishRace && other.CompareTag("Player"))
        {
           spectatorHandler.HandleSpectatorCamera(spectatorCameraIndex);
           
            /*if (!finishLine)
            {
                spectatorHandler.HandleSpectatorCamera(spectatorCameraIndex);

            }
            else
            {
                spectatorHandler.SwitchToCamera(-2);
            }*/
        }
    }

}
