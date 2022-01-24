using System;
using System.Collections;
using System.Collections.Generic;
using PolyPizza;
using TMPro;
using UnityEngine;

public class Letter : MonoBehaviour
{
    public string letter;

    public Letter left;
    public Letter right;
    private SphereCollider _sphereCollider;

    public Transform leftHitbox;
    public Transform rightHitbox;

    public bool grabbed;

    public bool beingTouched;

    public static Letter lastTouched;

    public Color TouchedCol;

    private static GameObject lastModel;

    private Letter wordStart = null;

    public bool movingWord;

    public float movebackSpeed = 0.1f;

    //when letters get seperated by word move spring them back, don't snap while doing this 
    private bool springingBack = false;
    private bool _isEndLetter;

    void Start()
    {
        _sphereCollider = GetComponent<SphereCollider>();
        GetComponentInChildren<TextMeshProUGUI>().text = letter;
    }

    private async void OnTriggerStay(Collider other)
    {
        if (other.tag.Equals("SnapZone") && !grabbed && !springingBack)
        {
            if (other.GetComponentInParent<Letter>() == lastTouched) return; //dont snap last touched 

            transform.position = other.transform.position;
            if (other.name.Contains("Left"))
            {
                other.GetComponentInParent<Letter>().left = this;
                right = other.GetComponentInParent<Letter>();
            }
            else
            {
                other.GetComponentInParent<Letter>().right = this;
                left = other.GetComponentInParent<Letter>();
            }


            other.enabled = false;
            other.GetComponent<MeshRenderer>().enabled = false;

            if (!left) leftHitbox.GetComponent<SphereCollider>().enabled = true;
            if (!right) rightHitbox.GetComponent<SphereCollider>().enabled = true;

            //Debug
            if (!left) leftHitbox.GetComponent<MeshRenderer>().enabled = true;
            if (!right) rightHitbox.GetComponent<MeshRenderer>().enabled = true;

            if (lastModel) lastModel.SetActive(false);
        }
    }

    public void StartWordMove(Vector3 grabPoint)
    {
        if (!left && !right) return; //only care about words
        grabbed = true;

        wordStart = GetStartLetter();
        
        if (movingWord && (this == wordStart || _isEndLetter)) //Move grabbed letters normally
        {
            transform.position = grabPoint;
        }
        
        if (wordStart.grabbed && _isEndLetter) //start moving word if we're the end word
        {
            movingWord = true;
            wordStart.movingWord = true;
            var letters = GetLetterString();

            Vector3 line =  wordStart.transform.position - transform.position; //from word end to start
            Debug.DrawLine(transform.position, transform.position + line, Color.blue);
            
            for (int i = 0; i < letters.Count; i++)
            {
                if(i == 0 || i == letters.Count - 1) continue; //only move middle letter
                float pos = (float)i / (letters.Count - 1);
                letters[i].transform.position = wordStart.transform.position - (line * pos);
            }
        }
    }


    //turn off my sphere colliders
    public void ToggleGrabbed(bool grabbed)
    {
        if (grabbed && !movingWord)
        {
            lastTouched = this;

            //Remove all ties to last connected letters
            if (left)
            {
                var other = left.rightHitbox;
                if (other)
                {
                    if (other.TryGetComponent(out SphereCollider sphereCollider)) sphereCollider.enabled = true;
                    if (other.TryGetComponent(out MeshRenderer MR)) MR.enabled = true;
                }

                left.right = null;
            }

            if (right)
            {
                var other = right.leftHitbox;
                if (other)
                {
                    if (other.TryGetComponent(out SphereCollider sphereCollider)) sphereCollider.enabled = true;
                    if (other.TryGetComponent(out MeshRenderer MR)) MR.enabled = true;
                }

                right.left = null;
            }

            left = null;
            right = null;
        }
        else
        {
            movingWord = false;
        }

        this.grabbed = grabbed;
    }

    // Update is called once per frame
    void Update()
    {
        _isEndLetter = left && !right;
        
        //Show availible slots when left alone
        if (!grabbed)
        {
            leftHitbox.GetComponent<SphereCollider>().enabled = !left;
            rightHitbox.GetComponent<SphereCollider>().enabled = !right;

            //Debug
            leftHitbox.GetComponent<MeshRenderer>().enabled = !left;
            rightHitbox.GetComponent<MeshRenderer>().enabled = !right;
        }
        else //disable when grabbed
        {
            leftHitbox.GetComponent<SphereCollider>().enabled = false;
            rightHitbox.GetComponent<SphereCollider>().enabled = false;

            //Debug
            leftHitbox.GetComponent<MeshRenderer>().enabled = false;
            rightHitbox.GetComponent<MeshRenderer>().enabled = false;
        }
        
        //Move back together after being grabbed
        springingBack = false;
        if (!movingWord)
        {
            if (left && !left.grabbed && Vector3.Distance(left.rightHitbox.transform.position, transform.position) > 0.005f)
            {
                transform.position = Vector3.Lerp(transform.position, left.rightHitbox.transform.position, movebackSpeed);
                springingBack = true;
            }
            
            if (right && !right.grabbed && Vector3.Distance(right.leftHitbox.transform.position, transform.position) > 0.005f)
            {
                transform.position = Vector3.Lerp(transform.position, right.leftHitbox.transform.position, movebackSpeed);
                springingBack = true;
            }
        }
        
        //Make the model when start and end are close
        if (movingWord && canMakeModel && _isEndLetter && Vector3.Distance(wordStart.transform.position, transform.position) < 0.05f)
        {
            makeModel();
        }
    }

    private bool canMakeModel = true;
    async void makeModel()
    {
        if (!canMakeModel) return;
        
        canMakeModel = false;
        var word = GetWord();
        var model = await APIManager.instance.GetExactModel(word);
        if (model != null)
        {
            var modelObj = await APIManager.instance.MakeModel(model, 0.25f);
            modelObj.transform.position = transform.position + Vector3.up * 0.25f;
            modelObj.transform.rotation = Quaternion.Euler(-90 + Mathf.Rad2Deg * model.Orbit.phi,
                -(Mathf.Rad2Deg * model.Orbit.theta), 0f);
            modelObj.transform.Rotate(Vector3.up, 180); //facing wrong way
            lastModel = modelObj;
        }
    }

    private void FixedUpdate()
    {
        GetComponent<MeshRenderer>().material.color = Color.Lerp(GetComponent<MeshRenderer>().material.color,
            beingTouched ? TouchedCol : new Color(0, 0, 0, 0), 0.2f);
        beingTouched = false;
    }

    Letter GetStartLetter()
    {
        var currentLetter = this;
        int count = 0;
        while (currentLetter.left)
        {
            currentLetter = currentLetter.left;
            count++;

            if (count > 100)
            {
                Debug.LogError("Letter search too many iterations");
                break;
            }
        }

        return currentLetter;
    }
    
    List<Letter> GetLetterString()
    {
        //Go all the way to the first letter
        var currentLetter = GetStartLetter();
        wordStart = currentLetter;

        //Then traverse right and concat to get word
        List<Letter> word = new List<Letter>();
        int count = 0;
        while (currentLetter)
        {
            word.Add(currentLetter);
            currentLetter = currentLetter.right;
            
            count++;

            if (count > 100)
            {
                Debug.LogError("Get letter too many iterations");
                break;
            }
        }

        return word;
    }
    
    string GetWord()
    {
        //Go all the way to the first letter
        var currentLetter = GetStartLetter();
        wordStart = currentLetter;

        //Then traverse right and concat to get word
        string word = "";
        int count = 0;
        while (currentLetter)
        {
            word += currentLetter.letter; //TODO replace with SB
            currentLetter = currentLetter.right;
            
            count++;
            if (count > 100)
            {
                Debug.LogError("Get letter too many iterations");
                break;
            }
        }

        return word;
    }
}