using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using XInputDotNetPure;

public class MapShow : MonoBehaviour {

    public bool IsRunningOnMono;

    public GameObject MapCanvas; // Assign in inspector
    private bool ToggleMapOn;
    private bool ToggleMapOff;

    // Use this for initialization
    void Start () {
        IsRunningOnMono = (Application.platform == RuntimePlatform.OSXEditor);
        ToggleMapOn = false;
        ToggleMapOff = false;
        MapCanvas.SetActive(false);
    }

    // Update is called once per frame
    void Update () {

        object controlState = null;
		if (!IsRunningOnMono) {
			controlState = GamePad.GetState (PlayerIndex.One);

			if (((GamePadState)controlState).Buttons.Back == ButtonState.Pressed) {
				ToggleMapOn = true;
			}

			if (((GamePadState)controlState).Buttons.Back == ButtonState.Released) {
				if (ToggleMapOn == true) {
					ToggleMapOff = !ToggleMapOff;
				}
				ToggleMapOn = false;
				MapCanvas.SetActive (ToggleMapOff);
			}
		} else {
			if (Input.GetKeyDown ("space")) {
				ToggleMapOn = true;
			}

			if (Input.GetKeyUp ("space")) {
				ToggleMapOff = !ToggleMapOff;

			}
			ToggleMapOn = false;
			MapCanvas.SetActive (ToggleMapOff);
		}
	}
}
