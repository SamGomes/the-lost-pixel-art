using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;


public class StartSceneFunctionalities : MonoBehaviour
{
    public Button serverSelector;
    public Button joinSelector;
    public Text robberConnectionIp;
    // Start is called before the first frame update
    void Start()
    {
        
        string iServerBuildPath = Application.streamingAssetsPath + "/isServer.cfg";
        bool isServerBuild = false;

        
        try
        {
            isServerBuild = (File.ReadAllText(iServerBuildPath)=="true");
        }
        catch (FileNotFoundException e)
        {
            iServerBuildPath = "";
            Debug.Log("Caught FileNotFoundException exception: " + e.Message);
        }

        InitGlobals();
        if (isServerBuild)
        {
            Globals.currOnlineOption = "SERVER";
            SceneManager.LoadScene("mainScene");
        }
        else
        {
            Globals.audioManagers[0].PlayInfiniteClip("Audio/background","Audio/background");
            serverSelector.onClick.AddListener(delegate()
            {
                Globals.currOnlineOption = "HOST";
                Globals.audioManagers[1].PlayClip("Audio/picking");
                SceneManager.LoadScene("mainScene");
            });
            joinSelector.onClick.AddListener(delegate()
            {
                Globals.currOnlineOption = "CLIENT";
                Globals.serverCode = robberConnectionIp.text;
                Globals.audioManagers[1].PlayClip("Audio/picking");
                SceneManager.LoadScene("mainScene");
            });
        }
        
        
    }

    public void InitGlobals()
    {
        Globals.savedGameObjects = new List<GameObject>();
        Globals.audioManagers = new List<AudioManager>();
        Globals.audioManagers.Add(new AudioManager(true)); //background
        Globals.audioManagers.Add(new AudioManager(false)); //walking fx
        Globals.audioManagers.Add(new AudioManager(false)); //picking fx
        Globals.audioManagers.Add(new AudioManager(false)); //xRay fx
        Globals.currOnlineOption = "";
        Globals.serverCode = "";
        Globals.winner = -1;
    }
}
