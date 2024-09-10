using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public WheelCollider[] wheel = new WheelCollider[4];
    public float torque = 200f;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    private void FixedUpdate()
    {
        if(Input.GetKey(KeyCode.W))
        {
            for(int i = 0; i < wheel.Length; i++)
            {
                wheel[i].motorTorque = torque;
            }
        }
        else
        {
            for (int i = 0; i < wheel.Length; i++)
            {
                wheel[i].motorTorque = 0;
            }
        }
    }
}
