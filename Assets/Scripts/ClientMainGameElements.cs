using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

//wrapper class for broadcasted GameManager calls
public class ClientMainGameElements : NetworkBehaviour
{
    public GameObject dropoutObject;

    public GameObject[] allStealables;
    public List<int> selectedIndexes;
    
    public IEnumerator PopulateStealablesWhenReady(List<int> selectedIndexes)
    {
        while (allStealables == null)
        {
            yield return null;
        }

        this.selectedIndexes = selectedIndexes;
        for(int i = 0; i < allStealables.Length; i++){
            if(!selectedIndexes.Contains(i))
                allStealables[i].SetActive(false);
        }
    }
    
    [ClientRpc]
    public void PopulateStealables(List<int> selectedIndexes)
    {
        Debug.Log("allStealables: "+allStealables.Length);
        allStealables = GameObject.FindGameObjectsWithTag("Stealable");
        StartCoroutine(PopulateStealablesWhenReady(selectedIndexes));
    }
    [ClientRpc]
    public void InstantiateDropoutObject(Vector3 position)
    {
        GameObject obj = Instantiate(dropoutObject);
        obj.transform.position = position;
    }
    
    [ClientRpc]
    public void UpdateStealableTransform(int carriedObjIndex, Vector3 position, Vector3 rotation)
    {
        allStealables[selectedIndexes[carriedObjIndex]].transform.position = position;
        allStealables[selectedIndexes[carriedObjIndex]].transform.rotation = Quaternion.Euler(rotation);
    }
    [ClientRpc]
    public void DespawnStealable(int carriedObjIndex)
    {
        allStealables[selectedIndexes[carriedObjIndex]].SetActive(false);
    }
    
    [ClientRpc]
    public void PlayAudioClip(int managerIndex, string clipPath)
    {
        Globals.audioManagers[managerIndex].PlayClip(clipPath);
    }
    
    [ClientRpc]
    public void StopCurrentAudioClip(int managerIndex)
    {
        Globals.audioManagers[managerIndex].StopCurrentClip();
    }
    
    [ClientRpc]
    public void PlayInfiniteAudioClip(int managerIndex, string introClipPath, string loopClipPath)
    {
        Globals.audioManagers[managerIndex].PlayInfiniteClip(introClipPath, loopClipPath);
    }

    [ClientRpc]
    public void EndGameInAllClients(int winner)
    {
        PlayerClientI[] players = FindObjectsOfType<PlayerClientI>();
        int thisPlayer = -1;

        foreach(PlayerClientI p in players){
            if(p.isLocalPlayer)
                thisPlayer = p.GetOrder();
        }

        GameManager.singleton.StopClient();
        // GameManager.singleton.StopServer();
        Globals.winner = winner;

        Globals.audioManagers[0].StopCurrentClip();
        Globals.audioManagers[1].StopCurrentClip();
        Globals.audioManagers[2].StopCurrentClip();
        if(winner == thisPlayer)
            SceneManager.LoadScene("VictoryScreen");
        else
            SceneManager.LoadScene("LossScreen");

    }
    
}