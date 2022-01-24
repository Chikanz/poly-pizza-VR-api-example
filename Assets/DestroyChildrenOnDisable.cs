using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DestroyChildrenOnDisable : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnDisable()
    {
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }

        transform.localScale = Vector3.one;
    }
}
