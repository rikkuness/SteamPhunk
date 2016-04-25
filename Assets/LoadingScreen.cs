using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class LoadingScreen : MonoBehaviour
{
    //The static loading loading screen texture to be assigned
    public Texture2D texture;
    //We make a static variable to our LoadingScreen instance
    static LoadingScreen instance;

    //When the object awakens, we assign the static variable if its a new instance and
    void Awake()
    {
        //destroy the already existing instance, if any
        if (instance)
        {
            Destroy(gameObject);
            hide();                                         //call hide function to hide the 'loading texture'
            return;
        }

        instance = this;
        gameObject.AddComponent<GUITexture>().enabled = false;  //disable the texture on start of the scene
        GetComponent<GUITexture>().texture = texture;                           //assign the texture
        transform.position = new Vector3(0.5f, 0.5f, 1f);       //position the texture to the center of the screen
        DontDestroyOnLoad(this);                                //make this object persistent between scenes
    }


    void Update()
    {
        //hide the loading screen if the scene is loaded
        if (!Application.isLoadingLevel)
            hide();
    }

    //function to enable the loading screen
    public static void show()
    {
        //if instance does not exists return from this function
        if (!InstanceExists())
        {
            return;
        }
        //enable the loading texture
        instance.GetComponent<GUITexture>().enabled = true;
    }

    //function to hide the loading screen
    public static void hide()
    {
        if (!InstanceExists())
        {
            return;
        }
        instance.GetComponent<GUITexture>().enabled = false;
    }

    //function to check if the persistent instance exists
    static bool InstanceExists()
    {
        if (!instance)
        {
            return false;
        }
        return true;

    }
    public void loadLevel(string sceneName)
    {
        LoadingScreen.show();
        foreach(GameObject button in GameObject.FindGameObjectsWithTag("splash"))
        {
            button.SetActive(false);
        }
        GameObject.FindGameObjectWithTag("loading").GetComponent<Text>().enabled = true;
        Application.LoadLevelAsync(sceneName);
    }

    public void exitGame()
    {
        Debug.Log("CLOSING");
        Application.Quit();
    }
}