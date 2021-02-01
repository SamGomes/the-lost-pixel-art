using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotateSprite : MonoBehaviour
{
    public Animator animator;
    void Start(){
        animator = GetComponentInChildren<Animator>();
    }
    void Update(){
        if (Camera.main == null) //for server
        {
            return;
        }

        Transform cameraPosition = Camera.main.transform;
        if (animator != null)
        {
            //update front/back sprite status
            if (Vector3.Dot(transform.parent.forward, cameraPosition.forward) > 0)
            {
                animator.SetBool("back", true);
            }
            else
            {
                animator.SetBool("back", false);
            }
        }

        //billboard
        Vector3 prevRotation = transform.rotation.eulerAngles;
        transform.LookAt(cameraPosition);
        transform.eulerAngles = new Vector3(prevRotation.x, transform.rotation.eulerAngles.y+180, prevRotation.z);


    }   
}
