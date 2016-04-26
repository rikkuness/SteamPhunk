using UnityEngine;
using System.Collections;
using XInputDotNetPure;

public class ChaseCamera : MonoBehaviour {
    public bool IsRunningOnMono;

    public GameObject targetObject;
    public float panSpeed = 1f;
    public float heightMax = 10f;
    public float heightMin = 1f;
    public float zoomMax = 10f;
    public float zoomMin = 1f;

    private float height = 1f;
    private float distance = 6f;

    private Transform target;
    private Vector3 offset;

    // Use this for initialization
    void Start () {
        IsRunningOnMono = (Application.platform == RuntimePlatform.OSXEditor);
        target = targetObject.transform;
        offset = new Vector3(0, height, -distance);
    }
	
	// Update is called once per frame
	void LateUpdate () {
		object controlState = null;
		if (!IsRunningOnMono) {
			controlState = GamePad.GetState (PlayerIndex.One);
		}
		float mouseRatioX = 0f;
		if (IsRunningOnMono || ! ((GamePadState)controlState).IsConnected) {
			const float sensitivity_boost = 3f;
			mouseRatioX = (float)((Input.mousePosition.x - (0.5 * Screen.width)) / Screen.width) * sensitivity_boost;

		} else {
			mouseRatioX = ((GamePadState)controlState).ThumbSticks.Right.X;
		}
		offset = Quaternion.AngleAxis ( mouseRatioX * panSpeed, Vector3.up) * offset;

		transform.position = target.position + offset;
        transform.LookAt(target.position);

    }
}
