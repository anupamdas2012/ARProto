using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FileButton : MonoBehaviour {

    public Text buttonText;
    public FilePanelController panelController;
	// Use this for initialization
	void Start () {
		
	}
	
	public void onButtonClick()
    {
        panelController.loadButtonClicked(buttonText.text);
    }

	// Update is called once per frame
	void Update () {
		
	}
}
