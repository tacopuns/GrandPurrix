using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Splines;

public class Junction : MonoBehaviour
{

    [Header("Junction Splines")]
    public SplineContainer splineContainer;
    [SerializeField] private List<int> splines;

    [Tooltip("Indices of splines that can be chosen at this junction")]
    public List<int> availableSplines = new List<int>();


    void Start()
    {

        GetSplines();
    }

    public void GetSplines()
    {
        if (splineContainer != null)
        {
            // Get all splines as a read-only list
            IReadOnlyList<Spline> allSplines = splineContainer.Splines;

            int count = allSplines.Count;

            Debug.Log($"Total splines in container: {count}");

            for (int i = 0; i < count; i++)
            {
                //splines.Add(i);
                Spline currentSpline = allSplines[i];
                Debug.Log($"Spline index {i} has {currentSpline.Count} knots.");

                splines.Add(i);
            }
        }
        
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Opp") && !other.CompareTag("Player"))
            return;

        var racer = other.GetComponent<CPUController>();
        if (racer == null)
            return;

        racer.EnterJunction(this);
    }

    public Spline GetSpline(int index)
    {
        return splineContainer.Splines[index];
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, 1.5f);

        if (splineContainer == null)
            return;

        foreach (int i in availableSplines)
        {
            Vector3 p = splineContainer.transform.TransformPoint(
                splineContainer.Splines[i].EvaluatePosition(0f)
            );
            Gizmos.DrawLine(transform.position, p);
        }
    }




    /*[SerializeField] private SplineContainer splineContainer;




    [SerializeField] private int currentSplineIndex;


    

    void Start()
    {

        GetSplines();
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("hit by " + other.gameObject.name);

        if (other.CompareTag("Player") || other.CompareTag("Opp"))
        {
            
        }
    }

    public void GetSplines()
    {
        if (splineContainer != null)
        {
            // Get all splines as a read-only list
            IReadOnlyList<Spline> allSplines = splineContainer.Splines;

            int count = allSplines.Count;

            Debug.Log($"Total splines in container: {count}");

            for (int i = 0; i < count; i++)
            {
                //splines.Add(i);
                Spline currentSpline = allSplines[i];
                Debug.Log($"Spline index {i} has {currentSpline.Count} knots.");

                splines.Add(i);
            }
        }
        
    }*/

}
