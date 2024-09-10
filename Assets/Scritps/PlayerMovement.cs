using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("CnetroDeMasa")]
    [SerializeField] private Vector3 centerOfMass = Vector3.zero;
    //[Header("WheelsModels")]
    //[SerializeField] private Transform M_frontRightWheel;
    //[SerializeField] private Transform M_frontLeftWheel;
    [Header("WheelsModels")]
    [SerializeField] private WheelCollider C_frontRightWheel;
    [SerializeField] private WheelCollider C_frontLeftWheel;

    [Header("ControlMovement")]

    [SerializeField] private float speed = 100;
    [SerializeField] private int MaxStearAngle = 30;

    private float torque;
    
    private Rigidbody rb;

    // Start is called before the first frame update
    void Start()
    {
       rb = GetComponent<Rigidbody>(); 
       //rb.centerOfMass = centerOfMass;
    }

    private void FixedUpdate()
    {
        torque = Input.GetAxis("Vertical") *  speed;
        rb.AddForce(0, 0, torque);

        float rotation = Input.GetAxis("Horizontal") * MaxStearAngle;
        //Rotacion de los modelos de las ruedas
        //M_frontRightWheel.eulerAngles = new Vector3(0, rotation, 90);
        //M_frontLeftWheel.eulerAngles = new Vector3(0, rotation, 90);
        //Rotacion de los WheelsColliders
        C_frontRightWheel.steerAngle = rotation;
        C_frontLeftWheel.steerAngle = rotation;



    }

}
