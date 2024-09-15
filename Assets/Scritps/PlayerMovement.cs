using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("CnetroDeMasa")]
    [SerializeField] private Vector3 centerOfMass;
    
    [Header("WheelsModels")]
    [SerializeField] private WheelCollider C_frontRightWheel;
    [SerializeField] private WheelCollider C_frontLeftWheel;

    [Header("ControlMovement")]

    [SerializeField] private float speed = 100;
    [SerializeField] private int MaxStearAngle = 30;

    private bool hasWheelContact = false;
    private float torque;
    
    private Rigidbody rb;

    public void HasContact()
    {
        hasWheelContact = true;
    }


    void Start()
    {
       rb = GetComponent<Rigidbody>(); 
      // rb.centerOfMass = centerOfMass;
    }


    private void FixedUpdate()
    {
        torque = Input.GetAxis("Vertical") *  speed;
        if (hasWheelContact)
        {
            C_frontLeftWheel.motorTorque = torque;
            C_frontRightWheel.motorTorque = torque;
        }
        

        float rotation = Input.GetAxis("Horizontal") * MaxStearAngle;
        
        C_frontRightWheel.steerAngle = rotation;
        C_frontLeftWheel.steerAngle = rotation;

       hasWheelContact = false;
    }
}
