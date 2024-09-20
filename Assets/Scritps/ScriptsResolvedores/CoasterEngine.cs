

using UnityEngine;
using System.Collections;
using System.Collections.Generic;


public class CoasterEngine : MonoBehaviour {
	private Rigidbody rb;
	private AudioSource audioSource;
	[Header("_______AnguloNave_______")]
	[SerializeField] private float maxAngleZ;
    [SerializeField] private float maxAngleY;
    [SerializeField] private GameObject nave;
	[Space(20)]

    public Vector3 centerOfMass = new Vector3(0, -1.5f, 0);
	private Vector3 centerOfMassAux;
	
	
	public COMOption centerOfMassOptions = new COMOption();	
	
	public SuspensionOption suspensionOptions = new SuspensionOption();

	public WheelOption wheelOptions = new WheelOption();
	
	private float wheelRPM = 0.0f;	
	
	public WhellcurveOption fwdWheelcurveOptions = new WhellcurveOption();	
	
	public WhellcurveOption backWheelcurveOptions = new WhellcurveOption();	
	
	public FrictionOption wheelFrictionOptions = new FrictionOption();
	
	
	
	// These variables are for the gears, the array is the list of ratios. The script
	// uses the defined gear ratios to determine how much torque to apply to the wheels.
	// Gear ratio Calculator: http://www.grimmjeeper.com/gears.html
	//public float[] GearRatio = {4.31f, 2.71f, 1.88f, 1.41f, 1.13f, 0.93f};
	
	public GearOption gearOptions = new GearOption();
	
	
	// These variables are just for applying torque to the wheels and shifting gears.
	// using the defined Max and Min Engine RPM, the script can determine what gear the
	// car needs to be in.
	public float EngineTorque = 600.0f;
	public float torqueRatio = 6;
	public float audioPitchRatio = 6;
	public AnimationCurve engineTorqueCurve = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1.0f, 0.3f));
	
	public float maxSteerAngle = 30;
	public float steerRatio = 2;
	public AnimationCurve velSteerCurve = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1.0f, 0.3f));
	
	public float maxSpeed = 200;
	public float normalBrakeForce = 1500;
	public float handBrakeForce = 1000;
	public float airResistance = 0.5f;
	public float downForce = 1000; // It's really necesary?
	
	public bool useAntiRoll = false;
	public float antiRollStiffness = 5000;
	
	public float LimitEngineRPM = 5000.0f;
	public float MaxEngineRPM = 3000.0f;
	public float MinEngineRPM = 1000.0f;
	
	public Light[] breakLights;

	
	public GuiOption guiOptions = new GuiOption();


	// Private insternal vars.
	private float accelPedal = 0;
	private float myHorizontal = 0;
	private bool isHandBrake = false;
	private float brakeForce = 0.0f;
	private float carSpeed = 0;
	private Vector3 lastVelocity;
	private float motorTorque = 0;
	private float motorTorque_Ant = 0;
	private float EngineRPM = 0.0f;
	
	private bool isGrounded = false;

    public Rigidbody Rb { get => rb; set => rb = value; }

    void Start()
	{
        rb = GetComponent<Rigidbody>();
        audioSource = GetComponent<AudioSource>();

        // I usually alter the center of mass to make the car more stable. I'ts less likely to flip this way.
        rb.centerOfMass = centerOfMass;
        centerOfMassAux = centerOfMass;

		



		UpdateWheelColliderData();

    }


    void Update()
    {

        accelPedal = Input.GetAxis("Vertical");
        myHorizontal = Input.GetAxis("Horizontal");
        isHandBrake = Input.GetButton("Jump");
		nave.transform.localEulerAngles = new Vector3(0, maxAngleY *myHorizontal, maxAngleZ * myHorizontal);


        // calculate current speed in km/h
        carSpeed = rb.velocity.magnitude * 3.6f; // ( 3600/1000 )
        //guiOptions.uiSpeedo.SendMessage("SetSpeed", carSpeed);

        // set the audio pitch to the percentage of RPM to the maximum RPM plus one, this makes the sound play
        // up to twice it's pitch, where it will suddenly drop when it switches gears.
        float pitchAux = Mathf.Abs(EngineRPM / MaxEngineRPM) + 1.0f;
        pitchAux = Mathf.Clamp(pitchAux, 0.0f, 2.0f);
        //audioSource.pitch = Mathf.Lerp(audioSource.pitch, pitchAux, audioPitchRatio * Time.deltaTime);
        //Debug.Log(pitchAux);

        //=======================================================================================================
        // Compute the engine RPM based on the average RPM of the two wheels, then call the shift gear function
        // 
        // MaxTorque = EngineTorqueCurve(Engine RPM)
        // EngineTorque = ThrottlePosition * MaxTorque
        // DriveTorque = EngineTorque * TransmissionRatio * TransmissionEfficiency

        // finally, apply the values to the wheels.	The torque applied is divided by the current gear, and
        // multiplied by the user input variable.

        // Get the transmission ratio.
        float TransmissionRatio = gearOptions.GearRatio[gearOptions.CurrentGear] * gearOptions.DifferentialRatio;

        // Get wheel rpm
        if (wheelOptions.isFourMotorized)
            wheelRPM = (wheelOptions.FrontLeftWheel.rpm + wheelOptions.FrontRightWheel.rpm +
                        wheelOptions.BackLeftWheel.rpm + wheelOptions.BackRightWheel.rpm) * 0.25f;
        else
        if (wheelOptions.isForwardMotorized)
            wheelRPM = (wheelOptions.FrontLeftWheel.rpm + wheelOptions.FrontRightWheel.rpm) * 0.5f;
        else
            wheelRPM = (wheelOptions.BackLeftWheel.rpm + wheelOptions.BackRightWheel.rpm) * 0.5f;

        // The engine (the motor itself) rpm
        EngineRPM = wheelRPM * TransmissionRatio;
        /*if(!gearOptions.isAutomatic && EngineRPM > LimitEngineRPM)
			EngineRPM = LimitEngineRPM;*/

        //guiOptions.uiRPM.SendMessage("SetRPM", EngineRPM);

        // Get motor Toque from the parabola and limit the maximun rpm the motor can reach. 
        float Torquepower = LimitedTorque(EngineRPM);
        float power = engineTorqueCurve.Evaluate(EngineRPM / MaxEngineRPM) * Torquepower;

        motorTorque = 0;
        brakeForce = 0;

        // En las marchas normales:
        // Solo acelera hacia adelante si se pulsa el acelerador en las marchas normales
        // si se pulsa el freno, no va marcha atras que el cambio llegue a esa marcha.
        if (gearOptions.CurrentGear < gearOptions.GearRatio.Length - 1)
        {
            if (accelPedal < 0)
                brakeForce = normalBrakeForce;
            else
                motorTorque = power * accelPedal * TransmissionRatio;
        }
        else
        // En las marcha atras:
        // Solo acelera hacia adelante si se pulsa el freno en la marcha atras
        // si se pulsa el acelerador, no va hacia adelante hasta que el cambio llegue a esa marcha.
        if (gearOptions.CurrentGear == gearOptions.GearRatio.Length - 1)
        {
            if (accelPedal > 0)
                brakeForce = normalBrakeForce;
            else
                motorTorque = power * accelPedal * TransmissionRatio;
        }

        if (isHandBrake)
            brakeForce += handBrakeForce;

        //Debug.Log("wheelRPM: "+wheelRPM+", EngineRPM: "+EngineRPM+" ,Torquepower:"+Torquepower+" ,power:"+power+" ,motorTorque:"+motorTorque+" ,Velocity: "+carSpeed);

        //
        //======================================================================================================
        // Update the gears if the EngineRPM is reaching the maxRPM.
        if (gearOptions.isAutomatic)
            ShiftGears();
        else
        {
            if (Input.GetButtonDown("Fire1"))
            {
                if (gearOptions.CurrentGear < gearOptions.GearRatio.Length - 2)
                    gearOptions.CurrentGear++;
                else
                if (gearOptions.CurrentGear == gearOptions.GearRatio.Length - 1 && carSpeed < 20)
                    gearOptions.CurrentGear = 0;
            }
            else
            if (Input.GetButtonDown("Fire2"))
            {
                if (gearOptions.CurrentGear > 0 && gearOptions.CurrentGear < gearOptions.GearRatio.Length - 1)
                    gearOptions.CurrentGear--;
                else
                // Solo permite cambiar marchga atras si la velocidad es muy baja.
                if (gearOptions.CurrentGear == 0 && carSpeed < 20)
                    gearOptions.CurrentGear = gearOptions.GearRatio.Length - 1;
            }
        }

        //
        //======================================================================================================
        // steer the wheels colliders.
        float currentSteerAngle = velSteerCurve.Evaluate(carSpeed / maxSpeed) * maxSteerAngle;
        // the steer angle is an arbitrary value multiplied by the user input.
        wheelOptions.FrontLeftWheel.steerAngle = Mathf.Lerp(wheelOptions.FrontLeftWheel.steerAngle, currentSteerAngle * myHorizontal, steerRatio * Time.deltaTime);
        wheelOptions.FrontRightWheel.steerAngle = Mathf.Lerp(wheelOptions.FrontRightWheel.steerAngle, currentSteerAngle * myHorizontal, steerRatio * Time.deltaTime);
        if (wheelOptions.steerFourWheels)
        {
            wheelOptions.BackLeftWheel.steerAngle = currentSteerAngle * -myHorizontal;
            wheelOptions.BackRightWheel.steerAngle = currentSteerAngle * -myHorizontal;
        }

        // Turn On/off the break lights.
        if (brakeForce > 0)
        {
            foreach (Light myLight in breakLights)
                if (!myLight.enabled) myLight.enabled = true;
        }
        else
        {
            foreach (Light myLight in breakLights)
                if (myLight.enabled) myLight.enabled = false;
        }


        //
        //======================================================================================================
        // GUIData to screen
        if (guiOptions.uiSpeed != null)
            guiOptions.uiSpeed.text = carSpeed.ToString("f0") + "\tkm/h";
        if (guiOptions.uiMotorRpm != null)
            guiOptions.uiMotorRpm.text = Mathf.Abs(EngineRPM).ToString("f0") + "\trpm";
        if (guiOptions.uiMotorGear != null)
        {
            string gearStr = "";
            if (gearOptions.CurrentGear > 0 && gearOptions.CurrentGear < gearOptions.GearRatio.Length - 1)
                gearStr = (gearOptions.CurrentGear + 1).ToString();
            else
            if (gearOptions.CurrentGear == gearOptions.GearRatio.Length - 1)
                gearStr = "Reverse";
            else
            if (gearOptions.CurrentGear == 0 && Mathf.Abs(carSpeed) > 1)
                gearStr = (gearOptions.CurrentGear + 1).ToString();
            else
            if (gearOptions.CurrentGear == 0 && Mathf.Abs(carSpeed) <= 1)
                gearStr = "NONE";

            guiOptions.uiMotorGear.text = gearStr;
        }
        if (guiOptions.uiMotorTorque != null)
        {
            if (wheelOptions.isFourMotorized)
                guiOptions.uiMotorTorque.text = (motorTorque * 0.25f).ToString("f0");
            else
                guiOptions.uiMotorTorque.text = (motorTorque * 0.5f).ToString("f0");
        }
    }

    void FixedUpdate()
    {
        //
        //======================================================================================================
        // if not modify the ceterof mass depending on velocity, then we can
        // allow the user to change it in inspector (testing purposes).
        if (!centerOfMassOptions.modifyCenterOfMass)
		{
            rb.centerOfMass = centerOfMass;
        }
            
        UpdateWheelColliderData();

		//
		//======================================================================================================
		// This is to limith the maximum speed of the car, using the velocity as a variable.
		// Think of it as air resistance and a force going down (to the floor).
		if (Mathf.RoundToInt(carSpeed) > 0)
		{
			rb.drag = carSpeed / maxSpeed * airResistance;
		}
		else
		{
            rb.drag = 0;
		}

        Vector3 airForce = -transform.up * (carSpeed / maxSpeed) * downForce;
        rb.AddForce(airForce);


        //
        //======================================================================================================
        // Adjust stiffness and slipcurve in the whells.
        if (wheelFrictionOptions.adjustStiffness)
            UpdateWheelStiffness(carSpeed);
        if (wheelFrictionOptions.adjustFrictionCurve)
            AdjustFriction(carSpeed, myHorizontal);
        if (isHandBrake && wheelFrictionOptions.useHandBreake)
            ApplyHandBrake(carSpeed);

        //
        //======================================================================================================
        // Generate force in suspension.
        if (useAntiRoll)
        {
            StabilizerBarsUpdate(wheelOptions.FrontLeftWheel, wheelOptions.FrontRightWheel);
            StabilizerBarsUpdate(wheelOptions.BackLeftWheel, wheelOptions.BackRightWheel);
        }

        IsCarGrounded();

        //
        //======================================================================================================
        // Apply the MotorTorque to the wheels
        if (wheelOptions.isFourMotorized)
        {
            wheelOptions.FrontLeftWheel.motorTorque = Mathf.Lerp(wheelOptions.FrontLeftWheel.motorTorque, motorTorque * 0.25f, torqueRatio * Time.deltaTime);
            wheelOptions.FrontRightWheel.motorTorque = Mathf.Lerp(wheelOptions.FrontRightWheel.motorTorque, motorTorque * 0.25f, torqueRatio * Time.deltaTime);
            wheelOptions.BackLeftWheel.motorTorque = Mathf.Lerp(wheelOptions.BackLeftWheel.motorTorque, motorTorque * 0.25f, torqueRatio * Time.deltaTime);
            wheelOptions.BackRightWheel.motorTorque = Mathf.Lerp(wheelOptions.BackRightWheel.motorTorque, motorTorque * 0.25f, torqueRatio * Time.deltaTime);
        }
        else
        if (wheelOptions.isForwardMotorized)
        {
            wheelOptions.FrontLeftWheel.motorTorque = Mathf.Lerp(wheelOptions.FrontLeftWheel.motorTorque, motorTorque * 0.5f, torqueRatio * Time.deltaTime);
            wheelOptions.FrontRightWheel.motorTorque = Mathf.Lerp(wheelOptions.FrontRightWheel.motorTorque, motorTorque * 0.5f, torqueRatio * Time.deltaTime);
        }
        else
        {
            wheelOptions.BackLeftWheel.motorTorque = Mathf.Lerp(wheelOptions.BackLeftWheel.motorTorque, motorTorque * 0.5f, torqueRatio * Time.deltaTime);
            wheelOptions.BackRightWheel.motorTorque = Mathf.Lerp(wheelOptions.BackRightWheel.motorTorque, motorTorque * 0.5f, torqueRatio * Time.deltaTime);
        }

        if (!isHandBrake)
        {
            wheelOptions.FrontLeftWheel.brakeTorque = brakeForce;
            wheelOptions.FrontRightWheel.brakeTorque = brakeForce;
        }
        wheelOptions.BackLeftWheel.brakeTorque = brakeForce;
        wheelOptions.BackRightWheel.brakeTorque = brakeForce;

        //
        //======================================================================================================
        // Change Center of mass in Z based on velocity.
        if (centerOfMassOptions.modifyCenterOfMass)
        {
            Vector3 acceleration = (rb.velocity - lastVelocity) / Time.fixedDeltaTime;
            lastVelocity = rb.velocity;
            //Debug.Log(acceleration.z);

            if (acceleration.z > 1)
                centerOfMassAux.z += centerOfMassOptions.fwdCOMRatio * Time.deltaTime;
            else
            if (acceleration.z < -1)
                centerOfMassAux.z -= centerOfMassOptions.fwdCOMRatio * Time.deltaTime;
            else
                centerOfMassAux.z = Mathf.Lerp(centerOfMassAux.z, centerOfMass.z, centerOfMassOptions.fwdCOMRatio * Time.deltaTime);
            centerOfMassAux.z = Mathf.Clamp(centerOfMassAux.z, centerOfMass.z - centerOfMassOptions.fwdMaxValue, centerOfMass.z + centerOfMassOptions.fwdMaxValue);

            // Chage center of mass in X, based in turning car keyboard.
            if (myHorizontal > 0.1f)
                centerOfMassAux.x += centerOfMassOptions.sideCOMRatio * Time.deltaTime;
            else
            if (myHorizontal < -0.1f)
                centerOfMassAux.x -= centerOfMassOptions.sideCOMRatio * Time.deltaTime;
            else
                centerOfMassAux.x = Mathf.Lerp(centerOfMassAux.x, centerOfMass.x, centerOfMassOptions.sideCOMRatio * Time.deltaTime);

            centerOfMassAux.x = Mathf.Clamp(centerOfMassAux.x, centerOfMass.x - centerOfMassOptions.sideMaxValue, centerOfMass.x + centerOfMassOptions.sideMaxValue);
            rb.centerOfMass = centerOfMassAux;
            //Debug.Log((carSpeed/maxSpeed)+", "+centerOfMassAux);
        }

        // Bajar el centro de masas si alguna de las ruedas está en el aire.
        // La idea que el coche nunca se vueque si no queremos y, si lo hace,
        // vuela a girarse para seguir conduciendo.
        if (centerOfMassOptions.modifyRollCOM)
        {
            if (!isGrounded)
            {
                centerOfMassAux.y -= centerOfMassOptions.rollRatio * Time.deltaTime;
                centerOfMassAux.y = Mathf.Clamp(centerOfMassAux.y, centerOfMass.y + centerOfMassOptions.rollCorrectionY, centerOfMass.y);
            }
            else
            if (isGrounded)
            {
                centerOfMassAux.y = Mathf.Lerp(centerOfMassAux.y, centerOfMass.y, centerOfMassOptions.rollRatio * Time.deltaTime);
                centerOfMassAux.y = Mathf.Clamp(centerOfMassAux.y, centerOfMass.y + centerOfMassOptions.rollCorrectionY, centerOfMass.y);
            }
            rb.centerOfMass = centerOfMassAux;
            //Debug.Log(centerOfMassAux+", "+isGrounded);
        }

    }










    void UpdateWheelColliderData(){
		JointSpring SpringAux = new JointSpring();
		SpringAux.spring = (float)suspensionOptions.fwdSuspensionSpring;
		SpringAux.damper = (float)suspensionOptions.fwdSpringDamper;
		
		WheelFrictionCurve fwdFrictionAux = new WheelFrictionCurve();
		fwdFrictionAux.extremumSlip = fwdWheelcurveOptions.fwdExtremumSlip;
		fwdFrictionAux.extremumValue = fwdWheelcurveOptions.fwdExtremumValue;
		fwdFrictionAux.asymptoteSlip = fwdWheelcurveOptions.fwdAsymptoteSlip;
		fwdFrictionAux.asymptoteValue = fwdWheelcurveOptions.fwdAsymptoteValue;
		fwdFrictionAux.stiffness = fwdWheelcurveOptions.fwdStiffness;
		
		WheelFrictionCurve sideFrictionAux = new WheelFrictionCurve();
		sideFrictionAux.extremumSlip = fwdWheelcurveOptions.sideExtremumSlip;
		sideFrictionAux.extremumValue = fwdWheelcurveOptions.sideExtremumValue;
		sideFrictionAux.asymptoteSlip = fwdWheelcurveOptions.sideAsymptoteSlip;
		sideFrictionAux.asymptoteValue = fwdWheelcurveOptions.sideAsymptoteValue;
		sideFrictionAux.stiffness = fwdWheelcurveOptions.sideStiffness;
		
		wheelOptions.FrontLeftWheel.radius = wheelOptions.wheelRadious;
		wheelOptions.FrontLeftWheel.suspensionDistance = suspensionOptions.fwdSuspensionDistance;
		wheelOptions.FrontLeftWheel.suspensionSpring = SpringAux;
		wheelOptions.FrontLeftWheel.forwardFriction = fwdFrictionAux;
		wheelOptions.FrontLeftWheel.sidewaysFriction = sideFrictionAux;
		
		wheelOptions.FrontRightWheel.radius = wheelOptions.wheelRadious;
		wheelOptions.FrontRightWheel.suspensionDistance = suspensionOptions.fwdSuspensionDistance;
		wheelOptions.FrontRightWheel.suspensionSpring = SpringAux;
		wheelOptions.FrontRightWheel.forwardFriction = fwdFrictionAux;
		wheelOptions.FrontRightWheel.sidewaysFriction = sideFrictionAux;
		
		// LLantas traseras
		SpringAux.spring = (float)suspensionOptions.backSuspensionSpring;
		SpringAux.damper = (float)suspensionOptions.backSpringDamper;
		
		fwdFrictionAux.extremumSlip = backWheelcurveOptions.fwdExtremumSlip;
		fwdFrictionAux.extremumValue = backWheelcurveOptions.fwdExtremumValue;
		fwdFrictionAux.asymptoteSlip = backWheelcurveOptions.fwdAsymptoteSlip;
		fwdFrictionAux.asymptoteValue = backWheelcurveOptions.fwdAsymptoteValue;
		fwdFrictionAux.stiffness = backWheelcurveOptions.fwdStiffness;
		
		sideFrictionAux.extremumSlip = backWheelcurveOptions.sideExtremumSlip;
		sideFrictionAux.extremumValue = backWheelcurveOptions.sideExtremumValue;
		sideFrictionAux.asymptoteSlip = backWheelcurveOptions.sideAsymptoteSlip;
		sideFrictionAux.asymptoteValue = backWheelcurveOptions.sideAsymptoteValue;
		sideFrictionAux.stiffness = backWheelcurveOptions.sideStiffness;
		
		// Assign the values.
		wheelOptions.BackLeftWheel.radius = wheelOptions.wheelRadious;
		wheelOptions.BackLeftWheel.suspensionDistance = suspensionOptions.backSuspensionDistance;
		wheelOptions.BackLeftWheel.suspensionSpring = SpringAux;
		wheelOptions.BackLeftWheel.forwardFriction = fwdFrictionAux;
		wheelOptions.BackLeftWheel.sidewaysFriction = sideFrictionAux;
		
		wheelOptions.BackRightWheel.radius = wheelOptions.wheelRadious;
		wheelOptions.BackRightWheel.suspensionDistance = suspensionOptions.backSuspensionDistance;
		wheelOptions.BackRightWheel.suspensionSpring = SpringAux;
		wheelOptions.BackRightWheel.forwardFriction = fwdFrictionAux;
		wheelOptions.BackRightWheel.sidewaysFriction = sideFrictionAux;
	}
	
	// Cambia el stiffnees (rigidez) de las ruedas dependiendo de la velocidad-
	void UpdateWheelStiffness(float _speed){
		
		WheelFrictionCurve fwdFrictionAux = new WheelFrictionCurve();
		fwdFrictionAux.extremumSlip = fwdWheelcurveOptions.fwdExtremumSlip;
		fwdFrictionAux.extremumValue = fwdWheelcurveOptions.fwdExtremumValue;
		fwdFrictionAux.asymptoteSlip = fwdWheelcurveOptions.fwdAsymptoteSlip;
		fwdFrictionAux.asymptoteValue = fwdWheelcurveOptions.fwdAsymptoteValue;
		fwdFrictionAux.stiffness = wheelFrictionOptions.frwStiffnessCurve.Evaluate(_speed/maxSpeed) * fwdWheelcurveOptions.fwdStiffness;
		
		WheelFrictionCurve sideFrictionAux = new WheelFrictionCurve();
		sideFrictionAux.extremumSlip = fwdWheelcurveOptions.sideExtremumSlip;
		sideFrictionAux.extremumValue = fwdWheelcurveOptions.sideExtremumValue;
		sideFrictionAux.asymptoteSlip = fwdWheelcurveOptions.sideAsymptoteSlip;
		sideFrictionAux.asymptoteValue = fwdWheelcurveOptions.sideAsymptoteValue;
		sideFrictionAux.stiffness = wheelFrictionOptions.sideStiffnessCurve.Evaluate(_speed/maxSpeed) * fwdWheelcurveOptions.sideStiffness;
		
		// Assign front wheels friction data.
		wheelOptions.FrontLeftWheel.forwardFriction = fwdFrictionAux;
		wheelOptions.FrontLeftWheel.sidewaysFriction = sideFrictionAux;
		wheelOptions.FrontRightWheel.forwardFriction = fwdFrictionAux;
		wheelOptions.FrontRightWheel.sidewaysFriction = sideFrictionAux;
		
		// Backward Whells
		fwdFrictionAux.extremumSlip = backWheelcurveOptions.fwdExtremumSlip;
		fwdFrictionAux.extremumValue = backWheelcurveOptions.fwdExtremumValue;
		fwdFrictionAux.asymptoteSlip = backWheelcurveOptions.fwdAsymptoteSlip;
		fwdFrictionAux.asymptoteValue = backWheelcurveOptions.fwdAsymptoteValue;
		fwdFrictionAux.stiffness = wheelFrictionOptions.frwStiffnessCurve.Evaluate(_speed/maxSpeed) * backWheelcurveOptions.fwdStiffness;
		
		sideFrictionAux.extremumSlip = backWheelcurveOptions.sideExtremumSlip;
		sideFrictionAux.extremumValue = backWheelcurveOptions.sideExtremumValue;
		sideFrictionAux.asymptoteSlip = backWheelcurveOptions.sideAsymptoteSlip;
		sideFrictionAux.asymptoteValue = backWheelcurveOptions.sideAsymptoteValue;
		sideFrictionAux.stiffness = wheelFrictionOptions.sideStiffnessCurve.Evaluate(_speed/maxSpeed) * backWheelcurveOptions.sideStiffness;

		// Assign rear wheels friction data.
		wheelOptions.BackLeftWheel.forwardFriction = fwdFrictionAux;
		wheelOptions.BackLeftWheel.sidewaysFriction = sideFrictionAux;
		wheelOptions.BackRightWheel.forwardFriction = fwdFrictionAux;
		wheelOptions.BackRightWheel.sidewaysFriction = sideFrictionAux;
	}
	
	// Cambia el los extremun y Asymtote values de las ruedas dependiendo de si se gira.
	void AdjustFriction(float _speed, float horizontalInput){
		
	
		if(horizontalInput != 0)
        {
			
			WheelFrictionCurve fwdFrictionAux = new WheelFrictionCurve();
			fwdFrictionAux.extremumSlip = fwdWheelcurveOptions.fwdExtremumSlip;
			fwdFrictionAux.extremumValue = fwdWheelcurveOptions.fwdExtremumValue;
			fwdFrictionAux.asymptoteSlip = fwdWheelcurveOptions.fwdAsymptoteSlip;
			fwdFrictionAux.asymptoteValue = fwdWheelcurveOptions.fwdAsymptoteValue;
			fwdFrictionAux.stiffness = wheelOptions.FrontRightWheel.sidewaysFriction.stiffness;
			
			WheelFrictionCurve sideFrictionAux = new WheelFrictionCurve();
			sideFrictionAux.extremumSlip = fwdWheelcurveOptions.sideExtremumSlip;
			sideFrictionAux.extremumValue = fwdWheelcurveOptions.sideExtremumValue;
			sideFrictionAux.asymptoteSlip = fwdWheelcurveOptions.sideAsymptoteSlip;
			sideFrictionAux.asymptoteValue = fwdWheelcurveOptions.sideAsymptoteValue;
			sideFrictionAux.stiffness = wheelOptions.FrontRightWheel.sidewaysFriction.stiffness;
		
			fwdFrictionAux.extremumValue = wheelFrictionOptions.frwFrictionCurve.Evaluate(_speed/maxSpeed) * fwdWheelcurveOptions.fwdExtremumValue;
			fwdFrictionAux.asymptoteValue = wheelFrictionOptions.frwFrictionCurve.Evaluate(_speed/maxSpeed) * fwdWheelcurveOptions.fwdAsymptoteValue;
			sideFrictionAux.extremumValue = wheelFrictionOptions.frwFrictionCurve.Evaluate(_speed/maxSpeed) * fwdWheelcurveOptions.sideExtremumValue;
			sideFrictionAux.asymptoteValue = wheelFrictionOptions.frwFrictionCurve.Evaluate(_speed/maxSpeed) * fwdWheelcurveOptions.sideAsymptoteValue;
			
			wheelOptions.FrontLeftWheel.forwardFriction = fwdFrictionAux;
			wheelOptions.FrontLeftWheel.sidewaysFriction = sideFrictionAux;
			wheelOptions.FrontRightWheel.forwardFriction = fwdFrictionAux;
			wheelOptions.FrontRightWheel.sidewaysFriction = sideFrictionAux;
			
			// Backward Whells
			fwdFrictionAux.extremumSlip = backWheelcurveOptions.fwdExtremumSlip;
			fwdFrictionAux.extremumValue = backWheelcurveOptions.fwdExtremumValue;
			fwdFrictionAux.asymptoteSlip = backWheelcurveOptions.fwdAsymptoteSlip;
			fwdFrictionAux.asymptoteValue = backWheelcurveOptions.fwdAsymptoteValue;
			fwdFrictionAux.stiffness = wheelOptions.BackRightWheel.sidewaysFriction.stiffness;
			
			sideFrictionAux.extremumSlip = backWheelcurveOptions.sideExtremumSlip;
			sideFrictionAux.extremumValue = backWheelcurveOptions.sideExtremumValue;
			sideFrictionAux.asymptoteSlip = backWheelcurveOptions.sideAsymptoteSlip;
			sideFrictionAux.asymptoteValue = backWheelcurveOptions.sideAsymptoteValue;
			sideFrictionAux.stiffness = wheelOptions.BackRightWheel.sidewaysFriction.stiffness;
		
			fwdFrictionAux.extremumValue = wheelFrictionOptions.sideFrictionCurve.Evaluate(_speed/maxSpeed) * backWheelcurveOptions.fwdExtremumValue;
			fwdFrictionAux.asymptoteValue = wheelFrictionOptions.sideFrictionCurve.Evaluate(_speed/maxSpeed) * backWheelcurveOptions.fwdAsymptoteValue;
			sideFrictionAux.extremumValue = wheelFrictionOptions.sideFrictionCurve.Evaluate(_speed/maxSpeed) * backWheelcurveOptions.sideExtremumValue;
			sideFrictionAux.asymptoteValue = wheelFrictionOptions.sideFrictionCurve.Evaluate(_speed/maxSpeed) * backWheelcurveOptions.sideAsymptoteValue;
			
			wheelOptions.BackLeftWheel.forwardFriction = fwdFrictionAux;
			wheelOptions.BackLeftWheel.sidewaysFriction = sideFrictionAux;
			wheelOptions.BackRightWheel.forwardFriction = fwdFrictionAux;
			wheelOptions.BackRightWheel.sidewaysFriction = sideFrictionAux;
		}
	}
	
	// Aplica un stiffness muy bajo dependiendo de si se pulsa el freno de mano.
	void ApplyHandBrake(float _speed){
		WheelFrictionCurve fwdFrictionAux = new WheelFrictionCurve();
		fwdFrictionAux.extremumSlip = wheelOptions.BackRightWheel.forwardFriction.extremumSlip;
		fwdFrictionAux.extremumValue = wheelOptions.BackRightWheel.forwardFriction.extremumValue;
		fwdFrictionAux.asymptoteSlip = wheelOptions.BackRightWheel.forwardFriction.asymptoteSlip;
		fwdFrictionAux.asymptoteValue = wheelOptions.BackRightWheel.forwardFriction.asymptoteValue;
		fwdFrictionAux.stiffness = wheelOptions.BackRightWheel.forwardFriction.stiffness;
		
		WheelFrictionCurve sideFrictionAux = new WheelFrictionCurve();
		sideFrictionAux.extremumSlip = wheelOptions.BackRightWheel.sidewaysFriction.extremumSlip;
		sideFrictionAux.extremumValue = wheelOptions.BackRightWheel.sidewaysFriction.extremumValue;
		sideFrictionAux.asymptoteSlip = wheelOptions.BackRightWheel.sidewaysFriction.asymptoteSlip;
		sideFrictionAux.asymptoteValue = wheelOptions.BackRightWheel.sidewaysFriction.asymptoteValue;
		sideFrictionAux.stiffness = wheelOptions.BackRightWheel.sidewaysFriction.stiffness;
		
		fwdFrictionAux.stiffness = wheelFrictionOptions.HandBrakeCurve.Evaluate(_speed/maxSpeed) * fwdFrictionAux.stiffness;
		sideFrictionAux.stiffness = wheelFrictionOptions.HandBrakeCurve.Evaluate(_speed/maxSpeed) * sideFrictionAux.stiffness;
		
		
		wheelOptions.BackLeftWheel.forwardFriction = fwdFrictionAux;
		wheelOptions.BackRightWheel.forwardFriction = fwdFrictionAux;
		wheelOptions.BackLeftWheel.sidewaysFriction = sideFrictionAux;
		wheelOptions.BackRightWheel.sidewaysFriction = sideFrictionAux;
	}
	

	
	

	// Limit the Engine power whrn EngineRPM Exceed the maximun RPM.
	float LimitedTorque(float rpm) {
		float torque = EngineTorque;
		if (rpm > MaxEngineRPM)
			torque -= (rpm - MaxEngineRPM) * 2f;
		
		if (rpm > LimitEngineRPM)
			torque -= (rpm - LimitEngineRPM) * 4f;
		
		//if(torque < 0) torque = 0;
		return torque;
    }
	
	
	void ShiftGears() {
		carSpeed = rb.velocity.magnitude * 3.6f;
		if(carSpeed < 20)
			gearOptions.CurrentGear = 0;
		
		// Solo permite cambiar marchga atras si la velocidad es muy baja.
		if(gearOptions.CurrentGear == 0 && accelPedal < 0 && carSpeed < 20)
			gearOptions.CurrentGear = gearOptions.GearRatio.Length-1;
		else
		if(gearOptions.CurrentGear == gearOptions.GearRatio.Length-1 && accelPedal > 0 && carSpeed < 20)
			gearOptions.CurrentGear = 0;
		else{
		
			// this funciton shifts the gears of the vehcile, it loops through all the gears, checking which will make
			// the engine RPM fall within the desired range. The gear is then set to this "appropriate" value.
			if ( EngineRPM >= MaxEngineRPM ) {
				int AppropriateGear = gearOptions.CurrentGear;
				for(int i = 0; i < gearOptions.GearRatio.Length-1; i ++ ) {
					float TransRatio = gearOptions.GearRatio[i] * gearOptions.DifferentialRatio;
					if (wheelOptions.FrontLeftWheel.rpm * TransRatio < MaxEngineRPM ) {
						AppropriateGear = i;
						break;
					}
				}
				
				gearOptions.CurrentGear = AppropriateGear;
			}
			
			if ( EngineRPM <= MinEngineRPM ) {
				int AppropriateGear = gearOptions.CurrentGear;
				
				for ( int j = gearOptions.GearRatio.Length-2; j >= 0; j -- ) {
					float TransRatio = gearOptions.GearRatio[j] * gearOptions.DifferentialRatio;
					if (wheelOptions.FrontLeftWheel.rpm * TransRatio > MinEngineRPM ) {
						AppropriateGear = j;
						break;
					}
				}
				
				gearOptions.CurrentGear = AppropriateGear;
			}
		}
	}
	
	void StabilizerBarsUpdate( WheelCollider WheelL,  WheelCollider WheelR){
	    WheelHit hit;
	    float travelL = 1.0f;
	    float travelR = 1.0f;
		
		bool groundedL = WheelL.GetGroundHit(out hit);
	    if (groundedL)
	        travelL = (-WheelL.transform.InverseTransformPoint(hit.point).y - WheelL.radius) / WheelL.suspensionDistance;
	
		bool groundedR = WheelR.GetGroundHit(out hit);
	    if (groundedR)
	        travelR = (-WheelR.transform.InverseTransformPoint(hit.point).y - WheelR.radius) / WheelR.suspensionDistance;
	
	    float antiRollForce = (travelL - travelR) * antiRollStiffness;
		if(groundedL && groundedR){
		    if (groundedL)
		        rb.AddForceAtPosition(WheelL.transform.up * -antiRollForce, WheelL.transform.TransformPoint(WheelL.center)); 
		    if (groundedR)
		        rb.AddForceAtPosition(WheelR.transform.up * antiRollForce, WheelR.transform.TransformPoint(WheelR.center)); 
		}
	}
		
	void IsCarGrounded(){
	    WheelHit hit;
		isGrounded = true;
		
		bool groundedFL = wheelOptions.FrontLeftWheel.GetGroundHit(out hit);
		bool groundedFR = wheelOptions.FrontRightWheel.GetGroundHit(out hit);
		bool groundedBL = wheelOptions.BackLeftWheel.GetGroundHit(out hit);
		bool groundedBR = wheelOptions.BackRightWheel.GetGroundHit(out hit);
		
		// Dos ruedas del coche están en el aire.
		if(!groundedFL && !groundedBL)
			isGrounded = false;
		else
		if(!groundedFR && !groundedBR)
			isGrounded = false;
	}
}
