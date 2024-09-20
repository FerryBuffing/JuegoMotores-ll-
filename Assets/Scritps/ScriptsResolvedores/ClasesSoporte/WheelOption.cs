using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;


[System.Serializable]
public class WheelOption
{
    [Range(0.1f, 1.0f)] public float wheelRadious = 0.52f;
    public bool steerFourWheels = false;
    public bool isForwardMotorized = true;
    public bool isFourMotorized = false;
    // These variables allow the script to power the wheels of the car.
    public WheelCollider FrontLeftWheel;
    public WheelCollider FrontRightWheel;
    public WheelCollider BackLeftWheel;
    public WheelCollider BackRightWheel;
}