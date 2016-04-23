using UnityEngine;
using System.Collections;

public class poi_collector : MonoBehaviour {

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}


	void OnTriggerEnter (Collider col)
	{
		POI_marker m = col.gameObject.GetComponent < POI_marker> ();
		Debug.Log(m.poi_name);
	}
}
