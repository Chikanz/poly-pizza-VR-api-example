using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChangeRenderOrder : MonoBehaviour
{
    public int queue;
    
    // Start is called before the first frame update
    void Start()
    {
       GetComponent<Renderer>().material.renderQueue = queue; 
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
