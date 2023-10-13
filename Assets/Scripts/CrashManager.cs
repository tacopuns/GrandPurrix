using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CrashManager : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private Transform respawnPoint;

    public float currentSpeed = 0f;
    public Rigidbody theRB;

    void Update() 
    {
        currentSpeed = theRB.velocity.magnitude;    
    }
    
    void OnTriggerEnter(Collider other)
    {
        if(currentSpeed > 20f)
        {
            player.transform.position = respawnPoint.transform.position;
            currentSpeed = 0f;
            Debug.Log("hit");
            Input.ResetInputAxes();
        }
        else
        {

        }
        
        
    }

    /* check speed, if higher than x speed, respawn
    when respawn, set speed to 0 */
}
