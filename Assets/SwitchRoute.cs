using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//using Dreamteck.Splines;

public class SwitchRoute : MonoBehaviour
{
    /*private SplineFollower follower;

    void Start()
    {
        follower = GetComponent<SplineFollower>();
        follower.onNode += OnNodePassed;
    }

    private void OnNodePassed(List<SplineTracer.NodeConnection> passed)
    {
        SplineTracer.NodeConnection nodeConnection = passed[0];
        Debug.Log(nodeConnection.node.name + "at point " + nodeConnection.point);
        double nodePercent = (double)nodeConnection.point / (follower.spline.pointCount - 1);
        double followerPercent = follower.UnclipPercent(follower.result.percent);
        float distancePastNode = follower.spline.CalculateLength(nodePercent, followerPercent);
        Debug.Log(nodePercent);

        Node.Connection[] connections = nodeConnection.node.GetConnections();
        int rnd = Random.Range(0, connections.Length);
        follower.spline = connections[rnd].spline;
        double newNodePercent = (double)connections[rnd].pointIndex / (connections[rnd].spline.pointCount - 1);
        double newPercent = connections[rnd].spline.Travel(newNodePercent, distancePastNode, follower.direction);
        follower.SetPercent(newPercent);
    }*/


}
