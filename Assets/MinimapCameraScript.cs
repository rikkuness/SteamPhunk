using UnityEngine;
using System.Collections;

public class MinimapCameraScript : MonoBehaviour {

    public GameObject targetObject;
    private Transform target;

    // Use this for initialization
    void Start () {
        target = targetObject.transform;
    }
	
	// Update is called once per frame
	void Update () {
	
	}

    void LateUpdate()
    {
        transform.position = new Vector3(target.position.x, 60f, target.position.z);
    }
}
