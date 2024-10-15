using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DrawWaypointGizmos : MonoBehaviour
{
    private void OnDrawGizmos()
    {
        Transform[] waypoint = transform.GetComponentsInChildren<Transform>();
        foreach (Transform t in waypoint)
        {
            Gizmos.DrawSphere(t.position, 1);
        }
    }
}
