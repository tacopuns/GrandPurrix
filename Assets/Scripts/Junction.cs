using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;

public class Junction : MonoBehaviour
{
    [SerializeField] private SplineContainer spline;

    [SerializeField] private List<int> splines;

    [SerializeField] private int currentSpline;

    private void OnTriggerEnter(Collider other)
    {
        
    }
}
