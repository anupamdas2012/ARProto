
namespace Firebase.Sample.Database
{
    using Firebase;
    using Firebase.Database;
    using Firebase.Unity.Editor;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;

    public class C_FirebaseDBManager : MonoBehaviour
    {
        private DatabaseReference db;
        private long topScore;
        public FilePanelController panelController;
        //dependency status
        private DependencyStatus dependencyStatus = DependencyStatus.UnavailableOther;



        private void Start()
        {


            /// Check firebase dependencies are all in order
            FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task => {
                dependencyStatus = task.Result;
                if (dependencyStatus == DependencyStatus.Available)
                {
                    //init firebase
                    InitializeFirebase(); 
                }
                else
                {
                    Debug.LogError(
                      "Could not resolve all Firebase dependencies: " + dependencyStatus);
                }
            });

            /*
            FirebaseApp.DefaultInstance.SetEditorDatabaseUrl("https://fir-iosstoragetutorial.firebaseio.com/");
            //db  = FirebaseDatabase.DefaultInstance.GetReference("scores");
            db = FirebaseDatabase.DefaultInstance.GetReference("ARdata");

            db.ValueChanged += HandleValueChanged;
            */
            //GetTopScore();

        }

        /// <summary>
        /// initiliaze firebase database
        /// </summary>
        private void InitializeFirebase()
        {
            
            FirebaseApp.DefaultInstance.SetEditorDatabaseUrl("https://fir-iosstoragetutorial.firebaseio.com/");
            //db  = FirebaseDatabase.DefaultInstance.GetReference("scores");
            db = FirebaseDatabase.DefaultInstance.GetReference("ARdata");

            db.ValueChanged += HandleValueChanged;
        }


        //unsubscribe on distroy
		private void OnDestroy()
		{
            db.ValueChanged -= HandleValueChanged;
		}


		void HandleValueChanged(object sender, ValueChangedEventArgs args)
        {

            panelController.clearPanel();

            Debug.Log("database value changed");
            Dictionary<string, object> results = (Dictionary<string, object>)args.Snapshot.Value;
            foreach (var item in results)
            {
                Debug.Log("key: " + item.Key);
                Dictionary<string, object> tableResults = (Dictionary<string, object>)item.Value;
                Debug.Log("filename: " + tableResults["filename"]);
                Debug.Log("metadata: " + tableResults["metadata"]);
                panelController.addButton(tableResults["filename"] as string);
                //Debug.Log("value: " + item.Value);
            }
            //GetTopScore();
        }

        public void WriteToDb()
        {
            db.RunTransaction(UpdateTopScore);
        }

        private TransactionResult UpdateTopScore(MutableData md)
        {
            int newTopScore = 10;
            md.Value = new Dictionary<string, object>() { { "topscore", newTopScore } };
            return TransactionResult.Success(md);
        }

        private void GetTopScore()
        {
            //clear all existing buttons
           // panelController.clearPanel();

            db.GetValueAsync().ContinueWith(task => {
                if (task.IsFaulted)
                {
                    Debug.Log("ERROR WITH GET TOP SCORE DATA");
                    // ERROR HANDLER
                }
                else if (task.IsCompleted)
                {
                    Dictionary<string, object> results = (Dictionary<string, object>)task.Result.Value;
                    foreach (var item in results)
                    {
                        Debug.Log("key: " + item.Key);
                        Dictionary<string, object> tableResults = (Dictionary<string, object>)item.Value;
                        Debug.Log("filename: " + tableResults["filename"]);
                        Debug.Log("metadata: " + tableResults["metadata"]);
                        panelController.addButton(tableResults["filename"] as string);
                        //Debug.Log("value: " + item.Value);
                    }
                    //DataSnapshot dsnapshot = task.Result.va;

                    //topScore = results["topscore"];
                    //Debug.Log("topscore is: " + dsnapshot.ToString());
                }
            });
        }



        void writeDBButton()
        {
            Debug.Log("updating top score");
            WriteToDb();
        }
        // Update is called once per frame
        void Update()
        {
            if(Input.GetKeyDown(KeyCode.U))
            {
                Debug.Log("updating top score");
                WriteToDb();
            }
        }
    }
}