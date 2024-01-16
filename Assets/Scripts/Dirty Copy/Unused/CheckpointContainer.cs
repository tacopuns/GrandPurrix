using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CheckpointContainer : MonoBehaviour
{
    public List<Transform> checkpoints;
    
    void Awake()
    {
        foreach(Transform tr in gameObject.GetComponentsInChildren<Transform>())
        {
            checkpoints.Add(tr);
        }
        checkpoints.Remove(checkpoints[0]);
    
    }

}