using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverScreen : MonoBehaviour
{
    public GameObject finalAnimPlaceholder;
    public GameObject robberAnimPrefab;
    public GameObject guardAnimPrefab;
    private void Start()
    {
        bool isGuard = Globals.winner%2==0;
        if (!isGuard)
        {
            Instantiate(robberAnimPrefab, finalAnimPlaceholder.transform);
        }
        else
        {
            Instantiate(guardAnimPrefab, finalAnimPlaceholder.transform);
        }
        Globals.audioManagers[1].PlayClip((Globals.winner%2==0) ? "Audio/winGuard" : "Audio/winRobber");
    }

    void Update()
    {
        if(Input.GetKeyDown(KeyCode.R)){
            SceneManager.LoadScene("startOnline");
        }
    }
}
