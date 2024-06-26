using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

public class RaceCompletion : MonoBehaviour
{
    public CinemachineVirtualCamera playerCamera;
    public CinemachineVirtualCamera finishCamera;

    public bool finishRace = false;

    private CheckpointManager checkpointManager;

    public RaceManager raceManager;

    public Collider finishCollider;

    void Start()
    {
        checkpointManager = CheckpointManager.Instance;
        finishRace = false;
        
        SwitchToCamera(playerCamera);
    }

    
    private void SwitchToCamera(CinemachineVirtualCamera targetCamera)
    {
        /*foreach (CinemachineVirtualCamera camera in virtualCameras)
        {
            camera.enabled = camera == targetCamera;
        }*/

        playerCamera.enabled = targetCamera == playerCamera;
        finishCamera.enabled = targetCamera == finishCamera;
        
    }

    private void CheckRaceCompletion(GameObject racer)
    {
        if (checkpointManager.raceFinished.ContainsKey(racer))
        {
            SwitchToCamera(finishCamera);
            finishRace = true;
            FindObjectOfType<RaceStatsHUD>().FreezeRaceStats();
            RemoveCollider();
            raceManager.FinishRace();
        }
    }

    private void OnTriggerEnter(Collider other) //this updates even tho the race is completed. needs to happen only once
    {
        if (other.CompareTag("Player"))
        {
            
            CheckRaceCompletion(other.gameObject);
            
        }
    }

    private void RemoveCollider()
    {
        finishCollider.enabled = false;
    }
}
