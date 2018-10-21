using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Firebase.Sample.Storage;

public class FilePanelController : MonoBehaviour {

    public  C_FirebaseStorageManager storageManager;
    public GameObject fileButton;
    private List<GameObject> buttonlist = new List<GameObject>();
	// Use this for initialization
	void Start () {
		
	}


	// Update is called once per frame
	void Update () {
		
	}

    public void addButton(string buttonString)
    {
        GameObject button = Instantiate(fileButton);
        button.transform.parent = gameObject.transform;
        button.GetComponent<FileButton>().buttonText.text = buttonString;
        button.SetActive(true);
        buttonlist.Add(button);
    }

    public void loadButtonClicked(string filename)
    {
        storageManager.Download(filename, true);        

    }

    public void clearPanel()
    {
        if (buttonlist != null)
        {
            foreach(GameObject button in buttonlist)
            {
                Destroy(button);
            }
            buttonlist.Clear();

        }
    }
}
