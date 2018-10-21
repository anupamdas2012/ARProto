using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TriLib;
using System;


public class C_LoadObjAsync : MonoBehaviour {


    public GameObject currentLoadedARObject = null;

    public void loadMeshAsync(string filename)
    {

        if (currentLoadedARObject!=null)
            unloadARObject(currentLoadedARObject);
        
        Debug.Log("!!! ATTEMPTING TO LOAD MESH");
        using (var assetLoader = new AssetLoaderAsync())
        {
            try
            {
                var assetLoaderOptions = AssetLoaderOptions.CreateInstance();
                //                assetLoaderOptions.RotationAngles = new Vector3(90f, 180f, 0f);
                //              assetLoaderOptions.AutoPlayAnimations = true;

                assetLoaderOptions.Use32BitsIndexFormat = true;
                string filetoload = Application.persistentDataPath + "/" + filename;

                assetLoader.LoadFromFile(filetoload, assetLoaderOptions, null, delegate (GameObject loadedGameObject)
                {
                    
                    //recalculateNormals(loadedGameObject);
                    loadedGameObject.transform.position = Vector3.zero;
                    loadedGameObject.transform.rotation = Quaternion.identity;
                    currentLoadedARObject = loadedGameObject;

                    Debug.Log("!!!!!!!!!**** game object loaded");
                });
            }
            catch (Exception e)
            {
                Debug.LogError(e.ToString());
            }
        }
    }

    private void unloadARObject(GameObject currentLoadedARObject)
    {
        Destroy(currentLoadedARObject);
    }

    void recalculateNormals(GameObject themesh)
    {
        MeshFilter[] meshFilters = themesh.GetComponentsInChildren<MeshFilter>();
        foreach(MeshFilter meshfilter in meshFilters)
        {
            meshfilter.mesh.RecalculateNormals();
        }
    }
	// Update is called once per frame
	void Update () {
		
	}
}
