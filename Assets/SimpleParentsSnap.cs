using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimpleParentsSnap : MonoBehaviour {

    public GameObject objtoparent;
	// Use this for initialization
	void Start () {
		
	}
	
    void doparent()
    {
        objtoparent.transform.SetParent(transform, false);
    }
	// Update is called once per frame
	void Update () {
        if(Input.GetKeyDown(KeyCode.P))
        {
            doparent();
        }
	}
}
