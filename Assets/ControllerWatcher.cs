using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ControllerWatcher : MonoBehaviour
{
    private bool pressed;
    private Letter currentLetter;

    private Letter lastGrabbed;
    
    public Transform grabPoint;

    public Animator handAnim;
    private Vector3 lastPos;
    private static readonly int Closed_hash = Animator.StringToHash("closed");

    public float throwForce = 100;
    public float throwThresh = 10;

    public float breakDistance = 0.1f;

    private static Letter[] Letters;

    // Start is called before the first frame update
    void Start()
    {
        Letters = FindObjectsOfType<Letter>();
    }

    // Update is called once per frame
    void Update()
    {
        if (pressed && currentLetter != null)
        {
            // if (lastGrabbed && lastGrabbed != currentLetter)
            // {
            //     lastGrabbed.lastTouched = false;
            //     
            // }
            
            lastGrabbed = currentLetter;
            
            currentLetter.StartWordMove(grabPoint.transform.position);

            if (currentLetter.left || currentLetter.right) //chained - don't move immediately
            {
                if (Vector3.Distance(grabPoint.transform.position, currentLetter.transform.position) >= breakDistance)
                {
                    currentLetter.ToggleGrabbed(true);
                }
            }
            else
            {
                currentLetter.transform.position = grabPoint.transform.position;
                currentLetter.ToggleGrabbed(true);
            }
        }
    }

    private void FixedUpdate()
    {
        lastPos = transform.position;
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.tag.Equals("SnapZone")) return;
        
        var letter = other.GetComponent<Letter>();
        if(letter) letter.beingTouched = true;
        if (!pressed && letter)
        {
            currentLetter = letter;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.tag.Equals("SnapZone")) return;
        
        if (!pressed && currentLetter)
        {
            // lastOther.ToggleGrabbed(false);
            currentLetter = null;
        }
    }

    //called with event
    public void OnButtonPress(bool notUsed)
    {
        this.pressed = !this.pressed;
        
        handAnim.SetBool(Closed_hash, pressed);

        if (!pressed && currentLetter)
        {
            // print((transform.position - lastPos).sqrMagnitude);
            // if ((transform.position - lastPos).sqrMagnitude > throwThresh)
            // {
            //     Debug.DrawLine(transform.position,
            //         transform.position + (transform.position - lastPos) * throwForce * 1000, Color.blue, 999999);
            //     currentLetter.GetComponent<Rigidbody>().AddForce((transform.position - lastPos) * throwForce);
            // }

            currentLetter.ToggleGrabbed(false);
            currentLetter = null;
        }
    } 
}
