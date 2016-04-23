using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using XInputDotNetPure;

public class MapShow : MonoBehaviour {

    public bool IsRunningOnMono;

    public GameObject MapCanvas; // Assign in inspector
    public bool ToggleMapOn;
    //public bool ToggleMapOff;

    // Use this for initialization
    void Start () {
        IsRunningOnMono = (Application.platform == RuntimePlatform.OSXEditor);
        ToggleMapOn = false;
        //ToggleMapOff = false;
    }
	
	// Update is called once per frame
	void Update () {

        object controlState = null;
        if (!IsRunningOnMono)
        {
            controlState = GamePad.GetState(PlayerIndex.One);
        }

        if (ToggleMapOn == true)
        {
            MapCanvas.SetActive(true);
        } else
        {
            MapCanvas.SetActive(false);
        }

        if (((GamePadState)controlState).Buttons.Back == ButtonState.Pressed)
        {
            ToggleMapOn = true;
        }
        if (((GamePadState)controlState).Buttons.Back == ButtonState.Released)
        {
            ToggleMapOn = false;
        }
    }
}
