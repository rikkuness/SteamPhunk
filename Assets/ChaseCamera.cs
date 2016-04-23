using UnityEngine;
using System.Collections;
using XInputDotNetPure;

public class ChaseCamera : MonoBehaviour {
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
        target = targetObject.transform;
        offset = new Vector3(0, height, -distance);
    }
	
	// Update is called once per frame
	void LateUpdate () {
        GamePadState controlState = GamePad.GetState(PlayerIndex.One);
        offset = Quaternion.AngleAxis(controlState.ThumbSticks.Right.X * panSpeed, Vector3.up) * offset;
        transform.position = target.position + offset;
        transform.LookAt(target.position);
    }
}
