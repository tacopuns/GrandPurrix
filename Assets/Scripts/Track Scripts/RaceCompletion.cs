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

    public List<CinemachineVirtualCamera> spectatorCameras;
    private int currentCameraIndex = -1;
    public List<Collider> spectatorTriggers;




    void Start()
    {
        checkpointManager = CheckpointManager.Instance;
        finishRace = false;
        
        SwitchToCamera(-1);
    }

    
    private void SwitchToCamera(int cameraIndex)
    {

        playerCamera.enabled = false;
        finishCamera.enabled = false;
        foreach (var cam in spectatorCameras)
        {
            cam.enabled = false;
        }

        // Enable the appropriate camera based on the index
        if (cameraIndex == -1)
        {
            playerCamera.enabled = true;
            Debug.Log("Player camera enabled.");
        }
        else if (cameraIndex == -2)
        {
            finishCamera.enabled = true;
            Debug.Log("Finish camera enabled.");
        }
        else if (cameraIndex >= 0 && cameraIndex < spectatorCameras.Count)
        {
            spectatorCameras[cameraIndex].enabled = true;
            Debug.Log($"Spectator camera {cameraIndex} enabled: {spectatorCameras[cameraIndex].name}");
        }

        currentCameraIndex = cameraIndex;
        
    }

    private void CheckRaceCompletion(GameObject racer)
    {
        if (checkpointManager.raceFinished.ContainsKey(racer))
        {
            SwitchToCamera(-2);
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

    public void HandleSpectatorCamera(int cameraIndex)
    {
        if (finishRace)
        {
            SwitchToCamera(cameraIndex);
        }
    }

}
