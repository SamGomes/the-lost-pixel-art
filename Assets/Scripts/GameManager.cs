using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using UnityEngine;
using UnityEngine.SceneManagement;

using Mirror;
using Random = UnityEngine.Random;
using Vector3 = UnityEngine.Vector3;

class PlayerServerState
{
    public int orderNum;
    public Vector3 position;
    public Vector3 rotation;
    public int money;
    
    public Vector3 mousePos;

    public int carriedObjIndex;

}

//mainly implements the server
public class GameManager : NetworkManager
{
    public List<Vector3> dropoutPoints;

    public ClientMainGameElements cmge;
    public Transform popupPositioner;
    public Camera worldCam;
    //public ClientMainGameElements cmge;
    
    private bool isGameReady;
    private bool isGameOver = false;
    private List<PlayerClientI> playerClients; //player client interface
    private List<PlayerServerState> playerServerStates;
    [Header("Spawning")]
    public List<Transform> possibleSpawnPositions;
    public List<int> selectedSpawnPositions;
    
    [Header("Stealables")]
    public int numberOfLevelStealables;
    public List<int> selectedStealablesIndexes;
    public List<GameObject> selectedStealables;
    public List<int> selectedStealableValues;
    private int totalGalleryValue;
    private int totalStolen = 0;

    [Header("Player Movement")]
    public float colliderRadius = 0.3f;
    public float guardMovementSpeed = 0.1f;
    public float robberMovementSpeed = 0.1f;
    public float backwardsMovePenalty = 0f;

    [Header("Time remaining")]
    public int secondsRemaining;
    public int maxLevelTime;


    private string GetMyIpAdress()
    {
        foreach(NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
        {
            if(ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 || ni.NetworkInterfaceType == NetworkInterfaceType.Ethernet)
            {
                foreach (UnicastIPAddressInformation ip in ni.GetIPProperties().UnicastAddresses)
                {
                    if (ip.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    {
                        string ipAddress = ip.Address.ToString();
                        return ipAddress;
                    }
                }
            }  
        }

        return "-.-.-.-";
    }

    //run by both host and client
    public override void Start()
    {
       
        //check connection type
        if (!NetworkClient.active)
        {
            Popup popup = new Popup(false, worldCam, popupPositioner);
            if (Globals.activeInfoPopups)
            {
                popup.SetMessage("Welcome to the wait lobby." );
                popup.DisplayPopup();
                // popup.AddOnHide(delegate
                // {
                //     Popup popup2 = new Popup(false, worldCam, popupPositioner);
                //     popup2.SetMessage("When you are ready to begin," +
                //                      " simply click on the \"Ready to Start!\" button");
                //     popup2.DisplayPopup();
                //     return 0;
                // });
            }

            if (Globals.serverCode == "")
            {
                networkAddress = "localhost";
            }
            else
            {
                networkAddress = Globals.serverCode;
            }
            
            if (Globals.currOnlineOption == "SERVER")
            {
                StartServer();
            }
            else if (Globals.currOnlineOption == "HOST")
            {
                Debug.Log("ip: "+networkAddress);
                StartHost();
            }
            else if (Globals.currOnlineOption == "CLIENT")
            {
                Debug.Log("ip: "+networkAddress);
                StartClient();
            }
        }
        else
        {
            Debug.Log("Connecting to " + this.networkAddress + "...");
        }
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        playerClients = new List<PlayerClientI>();
        playerServerStates = new List<PlayerServerState>();

        possibleSpawnPositions = new List<Transform>();
        GameObject[] spawns = GameObject.FindGameObjectsWithTag("Spawn");
        foreach(GameObject s in spawns){
            possibleSpawnPositions.Add(s.transform);
        }

        selectedSpawnPositions = new List<int>();
        for(int i =0; i < 2; i++){
            int newIndex = -1;
            do{
                newIndex = Random.Range(0, possibleSpawnPositions.Count);
            }while(selectedSpawnPositions.Contains(newIndex));
            selectedSpawnPositions.Add(newIndex);
        }

        cmge.StopCurrentAudioClip(0);
        cmge.PlayInfiniteAudioClip(0, "Audio/backgroundClock","Audio/backgroundClock");
        selectedStealablesIndexes = new List<int>();
        selectedStealablesIndexes = SelectStealables(numberOfLevelStealables);
        InvokeRepeating(nameof(PlayerUpdates),0.0f,0.01f);
    }
    private void PlayerUpdates()
    {
        PlayerMovementChanges();
    }

    List<int> SelectStealables(int amount){
        if(amount == 0) return new List<int>();

        totalGalleryValue = 0;
        GameObject[] stealableSpawns = GameObject.FindGameObjectsWithTag("Stealable");
        List<int> selectedIndexes = new List<int>();
        int newIndex = -1;

        for(int i = 0; i < amount; i++){
            do{
                newIndex = Random.Range(0, stealableSpawns.Length);
            }while(selectedIndexes.Contains(newIndex));

            selectedIndexes.Add(newIndex);
            selectedStealables.Add(stealableSpawns[newIndex]);
            int randomVal = Random.Range(0, 1000000);
            selectedStealableValues.Add(randomVal);
            totalGalleryValue += randomVal;
        }
        
        return selectedIndexes;
    }
    

    public override void OnClientDisconnect(NetworkConnection conn)
    {
        base.OnClientDisconnect(conn);
        StopClient();
    }

    public override void OnServerConnect(NetworkConnection conn)
    {
        base.OnServerConnect(conn);
        Debug.Log("Connected client with ip: "+conn.address +" and id: "+ conn.connectionId+" and isReady: "+ conn.isReady);
    }

    public override void OnServerAddPlayer(NetworkConnection conn)
    {
        //check if room is full
        if (numPlayers == 2)
        {
            conn.Disconnect();
            return;
        }
        
        CreatePlayer(conn);
        
        // init all players
        for(int i=0; i< playerClients.Count; i++)
        {
            InitPlayer(conn, i);
            playerClients[i].UpdateMoney(playerServerStates[i].money);
        }
        
        //create lobby on first player entry
        // if (numPlayers == 1)
        // {
        //     
        // }
        
        if (numPlayers == 2)
        {
            isGameReady = true;
            secondsRemaining = maxLevelTime;
            cmge.PopulateStealables(selectedStealablesIndexes);
            StartCoroutine("Countdown");
        }
    }

    IEnumerator Countdown(){
        while(!isGameOver){
            yield return new WaitForSeconds(1f);
            secondsRemaining--;

            CheckTimerEnded();
            foreach (PlayerClientI p in playerClients)
            {
                p.UpdateCoutdown(secondsRemaining);
            }
        }
    }

    void InitPlayer(NetworkConnection conn, int orderNum)
    {
        PlayerClientI playerClientI = playerClients[orderNum];
        
        //only create a server state for the last entered player
        if (orderNum == playerServerStates.Count)
        {
            //only add one server state per player
            PlayerServerState newPlayerState = new PlayerServerState();

            newPlayerState.position = possibleSpawnPositions[selectedSpawnPositions[orderNum]].position;
            newPlayerState.orderNum = orderNum;
            if (!IsGuard(newPlayerState))//if robber create a dropout point in the position
            {
                dropoutPoints.Add(newPlayerState.position);
                cmge.InstantiateDropoutObject(newPlayerState.position);
                newPlayerState.money = 0;
            }
            else
            {
                newPlayerState.money = totalGalleryValue;
            }
            Vector3 startingRotation = new Vector3(0, possibleSpawnPositions[selectedSpawnPositions[orderNum]].eulerAngles.y, 0);
            newPlayerState.rotation = startingRotation;
            
            newPlayerState.carriedObjIndex = -1;
            playerServerStates.Add(newPlayerState);
            playerClientI.UpdatePlayerState(newPlayerState.position, newPlayerState.rotation);
            playerClientI.UpdatePlayerState(newPlayerState.position, newPlayerState.rotation);
        }

        playerClientI.moveUp = false;
        playerClientI.moveLeft = false;
        playerClientI.moveRight = false;
        //all these methods are broadcasted to each client
        playerClientI.Init(orderNum);
    }

   

    void CreatePlayer(NetworkConnection conn)
    {
        GameObject playerGameObject = Instantiate(playerPrefab);
        //instantiates playerGameObject in all clients automatically
        NetworkServer.AddPlayerForConnection(conn, playerGameObject);
        PlayerClientI playerClientI = playerGameObject.GetComponent<PlayerClientI>();
        playerClients.Add(playerClientI);
    }

    [Server]
    private bool IsGuard(PlayerServerState pss)
    {
        return (pss.orderNum % 2 == 0);
    }
    //--------------------- player movement ------------------------
    [Server]
    private void PlayerMovementChanges()
    {
        if(!isGameReady) return;

        foreach (PlayerServerState pss in playerServerStates)
        {
            PlayerClientI playerClientI = playerClients[pss.orderNum];
            if ((playerClientI.moveUp || playerClientI.moveDown) && !(playerClientI.moveUp && playerClientI.moveDown))
            {
                var movement = playerClientI.IsGuard()? guardMovementSpeed: robberMovementSpeed;

                if(playerClientI.moveDown) movement *= -(1-backwardsMovePenalty);

                float xPos = Mathf.Sin(pss.rotation.y * Mathf.Deg2Rad) * Mathf.Cos(pss.rotation.x * Mathf.Deg2Rad);
                float yPos = Mathf.Sin(-pss.rotation.x * Mathf.Deg2Rad);
                float zPos = Mathf.Cos(pss.rotation.x * Mathf.Deg2Rad) * Mathf.Cos(pss.rotation.y * Mathf.Deg2Rad);

                float newPosY = yPos + (float) Math.Sin(Time.time * 20) * 0.1f;
                Vector3 nextPosition = pss.position + new Vector3(xPos, newPosY, zPos) * movement;
                nextPosition.y = nextPosition.y < 0.0f ? 0.0f : nextPosition.y;

                nextPosition = CollisionHandling(nextPosition, colliderRadius);
                // transform.position += transform.forward * movement;
                pss.position = nextPosition;
            }
            playerClientI.UpdatePlayerState(pss.position, pss.rotation);

            if (playerClientI.moveRight)
            {
                pss.rotation += new Vector3(0.0f, 2.5f, 0.0f);
                playerClientI.UpdatePlayerState(pss.position, pss.rotation);
            }

            if (playerClientI.moveLeft)
            {
                pss.rotation -= new Vector3(0.0f, 2.5f, 0.0f);
                playerClientI.UpdatePlayerState(pss.position, pss.rotation);
            }

            if (playerClientI.stealPress)
            {
                if (pss.carriedObjIndex == -1)
                {
                    for (int i = 0; i < selectedStealablesIndexes.Count; i++)
                    {
                        GameObject stealable = selectedStealables[i];
                        if ((stealable.transform.position - pss.position).sqrMagnitude < 20.0f)
                        {
                            pss.carriedObjIndex = i;
                            if (!IsGuard(pss))
                            {
                                pss.money += selectedStealableValues[pss.carriedObjIndex];
                                playerClients[pss.orderNum].UpdateMoney(pss.money);
                                foreach (PlayerServerState innerPss in playerServerStates)
                                {
                                    if (IsGuard(innerPss))
                                    {
                                        innerPss.money -= selectedStealableValues[pss.carriedObjIndex];
                                    }
                                    playerClients[innerPss.orderNum].UpdateMoney(innerPss.money);
                                }
                            }
                            
                        }
                    }
                }
                else
                {
                    if (!IsGuard(pss))
                    {
                        bool isReadyToDropout = false;
                        foreach (Vector3 drpPoint in dropoutPoints)
                        {
                            if ((drpPoint - pss.position).sqrMagnitude < 50.0f)
                            {
                                isReadyToDropout = true;
                            }
                        }
                        if (isReadyToDropout)
                        {
                            selectedStealables[pss.carriedObjIndex].transform.position = new Vector3(0.0f,-100.0f,0.0f); //remove it from reach
                            cmge.DespawnStealable(pss.carriedObjIndex);
                            totalStolen++;
                            CheckIfStolenEverything();
                        }
                        else
                        {
                            pss.money -= selectedStealableValues[pss.carriedObjIndex];
                            playerClients[pss.orderNum].UpdateMoney(pss.money);
                            foreach (PlayerServerState innerPss in playerServerStates)
                            {
                                if (IsGuard(innerPss))
                                {
                                    innerPss.money += selectedStealableValues[pss.carriedObjIndex];
                                }
                                playerClients[innerPss.orderNum].UpdateMoney(innerPss.money);
                            }
                        }
                    }
                    pss.carriedObjIndex = -1;
                }

                playerClientI.stealPress = false;
            }

            if (pss.carriedObjIndex != -1)
            {
                float xPos = Mathf.Sin(pss.rotation.y * Mathf.Deg2Rad) * Mathf.Cos(pss.rotation.x * Mathf.Deg2Rad);
                float zPos = Mathf.Cos(pss.rotation.x * Mathf.Deg2Rad) * Mathf.Cos(pss.rotation.y * Mathf.Deg2Rad);

                Vector3 newStealablePos = pss.position;
                newStealablePos.x += xPos * 1.5f;
                newStealablePos.y = selectedStealables[pss.carriedObjIndex].transform.position.y;
                newStealablePos.z += zPos * 1.5f;
                selectedStealables[pss.carriedObjIndex].transform.position = newStealablePos;

                Vector3 newStealableRot = pss.rotation;
                Vector3 currRot = selectedStealables[pss.carriedObjIndex].transform.rotation.eulerAngles;
                selectedStealables[pss.carriedObjIndex].transform.rotation = Quaternion.Euler(currRot + newStealableRot);
                cmge.UpdateStealableTransform(pss.carriedObjIndex, newStealablePos, newStealableRot);
            }
            // if (playerClientI.mouseMoved)
            // {
            //         
            // }
        }

        CheckGuardCaughtRobber(playerServerStates[0], playerServerStates[1]);
    }

    Vector3 CollisionHandling(Vector3 newPos, float characterRadius){
        Collider[] sceneColliders = FindObjectsOfType<Collider>();
        foreach(Collider col in sceneColliders){
            Vector3 nearestPoint = col.ClosestPoint(newPos);
            if(Vector3.Distance(nearestPoint, newPos) <= characterRadius){
                Vector3 adjustment = (newPos-nearestPoint).normalized*(characterRadius - Vector3.Distance(nearestPoint, newPos));
                newPos += new Vector3(adjustment.x, 0, adjustment.z);
            }
        }
        return newPos;
    }

    //--------------------- win condition -------------------------


    void CheckGuardCaughtRobber(PlayerServerState guardState, PlayerServerState robberState){
        if(isGameOver) return;

        Vector3 guardForward = new Vector3(Mathf.Cos(Mathf.Deg2Rad * (guardState.rotation.y-90)), 0, -Mathf.Sin(Mathf.Deg2Rad * (guardState.rotation.y-90)));

        Vector3 guardToRobber = robberState.position - guardState.position;

        float visionAngle = Mathf.Abs(Vector3.Angle(guardForward, guardToRobber));
        if(visionAngle > 90) return;
        //visionAngle = 180 - visionAngle;

        float distance = Vector3.Distance(guardState.position, robberState.position);

        if(distance < 20f && visionAngle < 20f){
            RaycastHit hit;
            if(Physics.Raycast(guardState.position + new Vector3(0, 0.3f, 0), guardToRobber, out hit, 100f)){
                if(hit.distance < distance) return;
            }

            Debug.Log(visionAngle);
            
            isGameOver = true;
            cmge.EndGameInAllClients(0);
        }
    }

    void CheckTimerEnded(){
        if(isGameOver) return;

        if(secondsRemaining == 0){
            isGameOver = true;
            cmge.EndGameInAllClients(0);
            return;
        }

    }

    void CheckIfStolenEverything(){
        if(isGameOver) return;

        if(totalStolen == selectedStealables.Count){
            isGameOver = true;
            cmge.EndGameInAllClients(1);
            return;
        }
    }
    
}
