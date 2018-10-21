using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class C_DebugText : MonoBehaviour {


    public Text debugtext;
	// Use this for initialization
	void Start () {
		
	}

    public void debugLog(string thetext)
    {
        debugtext.text = thetext;
    }
	
	// Update is called once per frame
	void Update () {
		
	}
}
