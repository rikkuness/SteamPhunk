using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class poi_collector : MonoBehaviour {

	Text hud_title;
	Text hud_body;
	GameObject disp;

	// Use this for initialization
	void Start () {
		hud_title = GameObject.Find("hud_title").GetComponent<Text> ();
		hud_body = GameObject.Find ("hud_body").GetComponent<Text> ();
		disp = GameObject.Find("hud_panel");
		disp.SetActive(false);
	}
	
	// Update is called once per frame
	void Update () {
	
	}


	void OnTriggerEnter (Collider col)
	{
        Debug.Log("hey there");
		POI_marker m = col.gameObject.GetComponent < POI_marker> ();
		hud_title.text = m.title;
		hud_body.text = m.body;
		disp.SetActive (true);
	}

	void OnTriggerExit (Collider col)
	{
        Debug.Log("hey there beef");
        hud_title.text = "null";
		hud_body.text = "null";
		disp.SetActive (false);
	}

}
