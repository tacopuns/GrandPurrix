using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

public class SpectatorHandler : MonoBehaviour
{
    public RaceCompletion raceCompletion;

    public Animator animator;

    public int camIndex;
    private int currentCameraIndex;

    public List<CinemachineVirtualCamera> spectatorCameras;
    public List<Collider> spectatorTriggers;

    void Start()
    {
        SwitchToCamera(-1);
        //animator.SetInteger("cameraIndex",camIndex);
    }

    public void SwitchToCamera(int cameraIndex)
    {
        //camIndex = cameraIndex;

        raceCompletion.playerCamera.enabled = false;
        raceCompletion.finishCamera.enabled = false;
        foreach (var cam in spectatorCameras)
        {
            cam.enabled = false;
        }

        if (cameraIndex == -1)
        {
            raceCompletion.playerCamera.enabled = true;
            Debug.Log("Player camera enabled.");
        }
        else if (cameraIndex == -2)
        {
            raceCompletion.finishCamera.enabled = true;
            raceCompletion.playerCamera.enabled = false;
            Debug.Log("Finish camera enabled.");
        }
        else if (cameraIndex >= 0 && cameraIndex < spectatorCameras.Count)
        {
            spectatorCameras[cameraIndex].enabled = true;
            Debug.Log($"Spectator camera {cameraIndex} enabled: {spectatorCameras[cameraIndex].name}");
        }

        currentCameraIndex = cameraIndex;
        
    }

    public void HandleSpectatorCamera(int cameraIndex)
    {
        if (raceCompletion.finishRace)
        {
            SwitchToCamera(cameraIndex);
        }
    }

}
