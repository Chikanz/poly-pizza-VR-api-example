using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LookAtPlayer : MonoBehaviour
{

    private static Camera cam; 
    
    // Start is called before the first frame update
    void Start()
    {
        if(!cam) cam = Camera.main;
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 look = transform.position - cam.transform.position;
        Debug.DrawLine(transform.position, transform.position + look, Color.blue);
        transform.rotation = Quaternion.LookRotation(look);
    }
}
