using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using TMPro;
using UnityEngine;

public class PlayerClientI: NetworkBehaviour
{
    //client stuff
    private bool isGuard;
    private TextMeshProUGUI moneyText;
    private TextMeshProUGUI countdownText;
    private Vector3 mousePos;
    private bool inited;
    
    //server managed code
    public bool moveLeft;
    public bool moveRight;
    public bool moveUp;
    public bool moveDown;
    public bool stealPress;
    private int orderNum;
    
    
    [ClientRpc]
    public void Init(int orderNum)
    {
        if (inited)
        {
            return;
        }

        this.orderNum = orderNum;
        
        isGuard = orderNum % 2 == 0;
        transform.Find("Guard").gameObject.SetActive(isGuard);
        transform.Find("Robber").gameObject.SetActive(!isGuard);

        if (!isLocalPlayer)
        {
            Canvas canvas = GetComponentInChildren<Canvas>();
            canvas.gameObject.SetActive(false);
            
            MeshRenderer mr = GetComponentInChildren<MeshRenderer>();
            if (mr != null)
            {
                mr.gameObject.SetActive(isGuard);
            }
            GetComponentInChildren<Light>().gameObject.SetActive(isGuard); //guard does not see the light of the robber
            transform.Find((isGuard) ? "Guard/Sprite" : "Guard/SpriteNightVS").gameObject.SetActive(false);
        }
        else
        {
            moneyText = transform.Find((isGuard) ? "Guard/Canvas/MoneyText" : "Robber/Canvas/MoneyText").GetComponent<TextMeshProUGUI>();
            countdownText = transform.Find((isGuard) ? "Guard/Canvas/CountdownText" : "Robber/Canvas/CountdownText").GetComponent<TextMeshProUGUI>();
            if (!isGuard)
            {
                Globals.audioManagers[2].PlayInfiniteClip("Audio/xRay", "Audio/xRay");
            }
        }
        inited = true;
    }

    void Start()
    {
        inited = false;
        if (isLocalPlayer)
        {
            Instantiate(Resources.Load<GameObject>((isGuard)? "Prefabs/FollowCameraGuard" : "Prefabs/FollowCameraRobber" ), transform);
        }
    }

    
    [Command]
    void MoveLeftKeyDown()
    {
        moveLeft = true;
    }
    [Command]
    void MoveLeftKeyUp()
    {
        moveLeft = false;
    }
    
    
    [Command]
    void MoveRightKeyDown()
    {
        moveRight = true;
    }
    [Command]
    void MoveRightKeyUp()
    {
        moveRight = false;
    }
    
    [Command]
    void MoveUpKeyDown()
    {
        moveUp = true;
    }
    [Command]
    void MoveUpKeyUp()
    {
        moveUp = false;
    }

    [Command]
    void MoveDownKeyDown()
    {
        moveDown = true;
    }
    [Command]
    void MoveDownKeyUp()
    {
        moveDown = false;
    }
    
    [Command]
    void StealPress()
    {
        stealPress = true;
    }
    
    [ClientRpc]
    public void UpdateCoutdown(int timeLeft)
    {
        if (isLocalPlayer)
        {
            countdownText.text = "Time Left: " + timeLeft;
        }
    }
    
    [ClientRpc]
    public void UpdateMoney(int money)
    {
        if (isLocalPlayer)
        {
            moneyText.text = "$ " + money.ToString();
        }
    }

    [ClientRpc]
    public void UpdatePlayerState(Vector3 position, Vector3 rotation)
    {
        float movementDelta = 0.005f;
        if (isLocalPlayer)
        {
            if ((transform.position - position).sqrMagnitude >= movementDelta)
            {
                if (!Globals.audioManagers[1].GetSource().isPlaying)
                {
                    Globals.audioManagers[1].PlayInfiniteClip((isGuard) ? "Audio/walkingGuard" : "Audio/walkingRobber", (isGuard) ? "Audio/walkingGuard" : "Audio/walkingRobber");
                }
            }else
            {
                Globals.audioManagers[1].StopCurrentClip();
            }
        }
        
        if ((transform.rotation.eulerAngles - rotation).sqrMagnitude >= movementDelta)
        {
            transform.rotation = Quaternion.Euler(rotation);
        }
        if ((transform.position - position).sqrMagnitude >= movementDelta)
        {
            transform.position = position;
        }
    }
    
    void Update()
    {
        if (!isLocalPlayer)
        {
            return;
        }
        
        if (Input.GetKeyDown(KeyCode.A))
        {
            MoveLeftKeyDown();
        }
        if (Input.GetKeyUp(KeyCode.A))
        {
            MoveLeftKeyUp();
        }
        if (Input.GetKeyDown(KeyCode.D))
        {
            MoveRightKeyDown();
        }
        if (Input.GetKeyUp(KeyCode.D))
        {            
            MoveRightKeyUp();
        }
        if (Input.GetKeyDown(KeyCode.W))
        {
            MoveUpKeyDown();
        }
        if (Input.GetKeyUp(KeyCode.W))
        {
            MoveUpKeyUp();
        }
        if (Input.GetKeyDown(KeyCode.S))
        {
            MoveDownKeyDown();
        }
        if (Input.GetKeyUp(KeyCode.S))
        {
            MoveDownKeyUp();
        }
        
        if (Input.GetKeyDown(KeyCode.E))
        {
            Globals.audioManagers[2].PlayClip("Audio/picking");
            StealPress();
        }

    }

    public int GetOrder(){
        return orderNum;
    }

    public bool IsGuard(){
        return isGuard;
    }
    
    
}
