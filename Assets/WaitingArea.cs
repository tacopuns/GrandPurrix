using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaitingArea : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // When the player enters the waiting area, trigger the Paparazzi NPC to start following the player
            PaparazziCarCon paparazzi = GetComponentInParent<PaparazziCarCon>();
            if (paparazzi != null)
            {
                //paparazzi.StartFollowingPlayer();
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // When the player exits the waiting area, trigger the Paparazzi NPC to stop following the player
            PaparazziCarCon paparazzi = GetComponentInParent<PaparazziCarCon>();
            if (paparazzi != null)
            {
                //paparazzi.StopFollowingPlayer();
            }
        }
    }
}
