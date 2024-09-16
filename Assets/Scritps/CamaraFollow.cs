using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class CamaraFollow : MonoBehaviour
{
    [SerializeField] Transform player;
    [SerializeField] Vector3 pov = Vector3.zero;
    
    // Update is called once per frame
    void FixedUpdate()
    {
        transform.position = player.position + pov;
        transform.LookAt(player.localPosition);
    }
    
}
