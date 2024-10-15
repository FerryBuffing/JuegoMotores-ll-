using UnityEngine;
using System.Collections;

public class IANave : MonoBehaviour 
{
	private Rigidbody rb;
	private AudioSource audioSource;



	[SerializeField] private Vector3 centerOfMass = Vector3.zero;
    [SerializeField] private WheelCollider frontLeftWheel;
    [SerializeField] private WheelCollider frontRightWheel;

    //public float[] GearRatio = {4.31f, 2.71f, 1.88f, 1.41f, 1.13f, 0.93f};
    //public float[] GearRatio = {2.66f, 1.78f, 1.30f, 1.00f, 0.74f, 0.50f};
    [SerializeField] private float[] GearRatio = { 4.31f, 2.71f, 1.88f, 1.41f, 1.13f, 0.93f };
    [SerializeField] private int CurrentGear = 0;

    [SerializeField] private float EngineTorque = 600;
    [SerializeField] private float MaxEngineRPM = 3000;
    [SerializeField] private float MinEngineRPM = 1000;
	[SerializeField] private  float EngineRPM = 0;
	
	private bool hasWheelContact = false;
	private float motorForce = 0;
	private float steerAngle = 0;
	[SerializeField] private float maxSteerAngle = 35;

    [SerializeField] private GameObject waypointContainer;
	private Transform[] waypoints;
	private int currentWaypoint = 1;
	

	// Use this for initialization
	void Start () {
		//waypointContainer = GameObject.Find("CollidersScenary");
		GetWaypoints();
        audioSource = GetComponent<AudioSource>();
        rb = GetComponent<Rigidbody>();
		rb.centerOfMass = centerOfMass;
    }
	
	// Update is called once per frame
	void FixedUpdate () {

		
        rb.drag = rb.velocity.magnitude / 250;
		
		NavigateTowardsWaypoint();
		
		EngineRPM = (frontLeftWheel.rpm + frontRightWheel.rpm)/2 * GearRatio[CurrentGear];
		// Cambio de marchas.
		ShiftGears();

       /* audioSource.pitch = Mathf.Abs(EngineRPM/MaxEngineRPM) + 1.0f;
		if(audioSource.pitch > 2.0f)
            audioSource.pitch = 2.0f;*/
		
		frontLeftWheel.motorTorque = EngineTorque * GearRatio[CurrentGear] * motorForce;
		frontRightWheel.motorTorque = EngineTorque * GearRatio[CurrentGear] * motorForce;
		
		//float rotation = Input.GetAxis("Horizontal") * maxSteerAngle;
		frontLeftWheel.steerAngle = maxSteerAngle * steerAngle;
		frontRightWheel.steerAngle = maxSteerAngle * steerAngle;
	}
	
	
	void ShiftGears(){
		if(EngineRPM >= MaxEngineRPM){
			int AppropriateGear = CurrentGear;
			for(int i = 0; i < GearRatio.Length; i++){
				if(frontLeftWheel.rpm * GearRatio[i] < MaxEngineRPM){
					AppropriateGear = i;
					break;
				}
			}
			CurrentGear = AppropriateGear;
		}
		
		
		if(EngineRPM <= MinEngineRPM){
			int AppropriateGear = CurrentGear;
			for(int j = GearRatio.Length -1; j >= 0 ; j--){
				if(frontLeftWheel.rpm * GearRatio[j] > MinEngineRPM){
					AppropriateGear = j;
					break;
				}
			}
			CurrentGear = AppropriateGear;
		}
	}
	
	
	void GetWaypoints(){
		Transform[] potencialWaypoints = waypointContainer.GetComponentsInChildren<Transform>();
		waypoints = new Transform[potencialWaypoints.Length-1];
		
		int i = 0;
		foreach(Transform potencialWaypoint in potencialWaypoints)
			if(potencialWaypoint != waypointContainer.transform)
				waypoints[i++] = potencialWaypoint;
	}
	
	void NavigateTowardsWaypoint(){
		Vector3 RelativeWaypointPosition = transform.InverseTransformPoint(new Vector3(
																			waypoints[currentWaypoint].position.x,
																			transform.position.y,
																			waypoints[currentWaypoint].position.z));
		
		steerAngle = RelativeWaypointPosition.x / RelativeWaypointPosition.magnitude;



		// El coche frena en las curvas.
		if (steerAngle < 0.3f)
		{
			motorForce = RelativeWaypointPosition.z / RelativeWaypointPosition.magnitude - Mathf.Abs(steerAngle);
        
        }
		else
		{
			motorForce = 0;       
        }
		
		if(RelativeWaypointPosition.magnitude < 20) {
			currentWaypoint++;
			if(currentWaypoint >= waypoints.Length)
				currentWaypoint = 0;
		}
	}



	void Ray()
	{
        // Bit shift the index of the layer (8) to get a bit mask
        int layerMask = 1 << 8;

        // This would cast rays only against colliders in layer 8.
        // But instead we want to collide against everything except layer 8. The ~ operator does this, it inverts a bitmask.
        layerMask = ~layerMask;

        RaycastHit hit;
        // Does the ray intersect any objects excluding the player layer
        if (Physics.Raycast(transform.position, transform.TransformDirection(Vector3.forward), out hit, Mathf.Infinity, layerMask))
        {
            Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.forward) * hit.distance, Color.yellow);
            Debug.Log("Did Hit");
        }
        else
        {
            Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.forward) * 1000, Color.white);
            Debug.Log("Did not Hit");
			transform.position = waypoints[currentWaypoint].position;
        }
    }
}
