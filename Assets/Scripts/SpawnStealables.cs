using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnStealables : MonoBehaviour
{
    public int numberOfStealables;
    public List<GameObject> selectedStealables;

    void Start(){
        selectedStealables = SelectStealables(numberOfStealables);
    }

    List<GameObject> SelectStealables(int amount){
        if(amount == 0) return new List<GameObject>();

        GameObject[] stealableSpawns = GameObject.FindGameObjectsWithTag("Stealable");
        List<GameObject> res = new List<GameObject>();
        List<int> selectedIndexes = new List<int>();
        int newIndex = -1;

        for(int i = 0; i < amount; i++){
            do{
                newIndex = Random.Range(0, stealableSpawns.Length);
            }while(selectedIndexes.Contains(newIndex));

            selectedIndexes.Add(newIndex);
        }

        for(int i = 0; i < stealableSpawns.Length; i++){
            if(!selectedIndexes.Contains(i))
                stealableSpawns[i].SetActive(false);
            else
                res.Add(stealableSpawns[i]);

        }

        return res;
    }
}
