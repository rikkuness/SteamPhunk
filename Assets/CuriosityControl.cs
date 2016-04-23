using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using XInputDotNetPure;

[System.Serializable]
public class ControlArm
{
    public GameObject controlArm;
    public bool inverse;
    public bool leftSide;
}

[System.Serializable]
public class WheelMotor
{
    public GameObject motor;
    public bool leftSide;
}

public class CuriosityControl : MonoBehaviour {
	public bool IsRunningOnMono;

    public List<WheelMotor> motors;
    public List<ControlArm> controlArms;

    public float motorSpeed = 100f;
    public float steeringAngle = 45.0f;
    public float rollSpeed = 1f;
    public float skidCompensation = 1f;

    void Start()
    { 
        IsRunningOnMono = (Application.platform == RuntimePlatform.OSXEditor);
    }

    // Update is called once per frame
    void Update () {
		object controlState = null;
		if (! IsRunningOnMono) {
			controlState = GamePad.GetState (PlayerIndex.One);
		}
		// Forward and backwards movement
		foreach (WheelMotor motor in motors)
		{
			HingeJoint hinge = motor.motor.GetComponent<HingeJoint>();
			JointMotor thisMotor = hinge.motor;
			if (IsRunningOnMono || !((GamePadState)controlState).IsConnected)
			{
				thisMotor.targetVelocity = motorSpeed * Input.GetAxis("Vertical");
			}
			else
			{
				thisMotor.targetVelocity = motorSpeed * (((GamePadState)controlState).Triggers.Right + -((GamePadState)controlState).Triggers.Left);
			}
			hinge.motor = thisMotor;
		}

		// Steering control
		foreach(ControlArm controlArm in controlArms)
		{
			HingeJoint hinge = controlArm.controlArm.GetComponent<HingeJoint>();
			JointSpring spring = hinge.spring;
			if (IsRunningOnMono || !((GamePadState)controlState).IsConnected)
			{
				spring.targetPosition = steeringAngle * Input.GetAxis("Horizontal");
			}else
			{
				spring.targetPosition = steeringAngle * ((GamePadState)controlState).ThumbSticks.Left.X; 
			}

			// Forklift steering
			if (controlArm.inverse)
			{
				spring.targetPosition = -spring.targetPosition;
			}

			hinge.spring = spring;
		}

		// Skid compensate
		foreach (WheelMotor motor in motors)
		{
			HingeJoint wheelhinge = motor.motor.GetComponent<HingeJoint>();
			JointMotor thisMotor = wheelhinge.motor;
			if (motor.leftSide && Input.GetAxis("Horizontal") < 0)
			{
				thisMotor.targetVelocity = thisMotor.targetVelocity / skidCompensation;
			}
			else if(!(motor.leftSide) && Input.GetAxis("Horizontal") > 0)
			{
				thisMotor.targetVelocity = thisMotor.targetVelocity * skidCompensation;
			}
			wheelhinge.motor = thisMotor;
		}

		// Self righting
		transform.localEulerAngles = new Vector3(0, transform.localEulerAngles.y, transform.localEulerAngles.z);
    }
}