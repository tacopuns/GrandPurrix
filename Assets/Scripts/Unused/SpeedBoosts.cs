using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpeedBoosts : MonoBehaviour
{
    public float boostForce = 50; // Adjust this value as needed

    private void OnTriggerEnter(Collider other)
    {
        // Check if the colliding object is the player's car
        if (other.CompareTag("Player"))
        {
            Rigidbody playerRigidbody = other.GetComponent<Rigidbody>();
            
            if (playerRigidbody != null)
            {
                // Apply the boost force to the player's car
                playerRigidbody.AddForce(transform.forward * boostForce, ForceMode.VelocityChange);
            }
        }
    }

}
