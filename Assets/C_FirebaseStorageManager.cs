    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;

    using Firebase;
    using Firebase.Storage;
    using Firebase.Database;
    using System;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Text;


    public class C_FirebaseStorageManager : MonoBehaviour
    {
        //storage bucket
        protected string MyStorageBucket = "gs://fir-iosstoragetutorial.appspot.com/";
        //protected string MyStorageBucket;

        // Hold a reference to the FirebaseStorage object so that we're not reinitializing the API on
        // each transfer.
        protected FirebaseStorage storage;

        //dependency status
        private DependencyStatus dependencyStatus = DependencyStatus.UnavailableOther;

        // Currently enabled logging verbosity.
        protected Firebase.LogLevel logLevel = Firebase.LogLevel.Info;

        //cached data path for files
        protected string persistentDataPath;

        //uri file
        protected static string UriFileScheme = Uri.UriSchemeFile + "://";


        // Cloud Storage location to download from / upload to.
        protected string storageLocation;

        // Metadata to change when uploading a file.
        protected string fileMetadataChangeString = "";
 

        // Cancellation token source for the current operation.
        protected CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
   
        // Previously completed task.
        protected Task previousTask;

        // Whether an operation is in progress.
        protected bool operationInProgress;

        public C_DebugText debugTextController;


        protected string arDataDir = "/ARData";


        // When the app starts, check to make sure that we have
        // the required dependencies to use Firebase, and if not,
        // add them if possible.
        protected virtual void Start()
        {

            debugTextController = GetComponent<C_DebugText>();


            persistentDataPath = Application.persistentDataPath;


            DebugLog("persistent data path is: " + persistentDataPath);

            FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task => {
                dependencyStatus = task.Result;
                if (dependencyStatus == DependencyStatus.Available)
                {
                    InitializeFirebase();
                }
                else
                {
                    DebugLog("!!!!!!!!!!Could not resolve all Firebase dependencies: " + dependencyStatus);
                    Debug.LogError(
                      "!!!!!!!!!!Could not resolve all Firebase dependencies: " + dependencyStatus);
                }
            });
        }

		private void OnDestroy()
		{
			
		}

		protected void InitializeFirebase()
        {

            Debug.Log("Initilizing firebase");
            /*
            //--------------------------------------
            auth = Firebase.Auth.FirebaseAuth.DefaultInstance;
            auth.SignInWithEmailAndPasswordAsync("mail@mail.com", "strongpassword").ContinueWith(task => {
                if (task.IsCanceled)
                {
                    Debug.LogError("SignInWithEmailAndPasswordAsync was canceled.");
                    return;
                }
                if (task.IsFaulted)
                {
                    Debug.LogError("SignInWithEmailAndPasswordAsync encountered an error: " + task.Exception);
                    return;
                }
                Firebase.Auth.FirebaseUser newUser = task.Result;
                Debug.LogFormat("User signed in successfully: {0} ({1})",
                    newUser.DisplayName, newUser.UserId);
            });
            */

            // get storage bucket
            var appBucket = FirebaseApp.DefaultInstance.Options.StorageBucket;
            storage = FirebaseStorage.DefaultInstance;
            if (!String.IsNullOrEmpty(appBucket))
            {
                MyStorageBucket = String.Format("gs://{0}/", appBucket);
            }
            else
            {   
                DebugLog("failed to get storage bucket");
            }

            storage.LogLevel = logLevel;
           
        }


        // Get the local filename as a URI relative to the persistent data path if the path isn't
        // already a file URI.
        protected virtual string PathToPersistentDataPathUriString(string filename)
        {
            if (filename.StartsWith(UriFileScheme))
            {
                return filename;
            }
            return String.Format("{0}{1}/{2}", UriFileScheme, persistentDataPath,
                                 filename);
        }

        // Convert a string in the form:
        //   key1=value1
        //   ...
        //   keyN=valueN
        //
        // to a MetadataChange object.
        //
        // If an empty string is provided this method returns null.
        MetadataChange StringToMetadataChange(string metadataString)
        {
            var metadataChange = new MetadataChange();
            var customMetadata = new Dictionary<string, string>();
            bool hasMetadata = false;
            foreach (var metadataStringLine in metadataString.Split(new char[] { '\n' }))
            {
                if (metadataStringLine.Trim() == "")
                    continue;
                var keyValue = metadataStringLine.Split(new char[] { '=' });
                if (keyValue.Length != 2)
                {
                    DebugLog(String.Format("Ignoring malformed metadata line '{0}' tokens={2}",
                                           metadataStringLine, keyValue.Length));
                    continue;
                }
                hasMetadata = true;
                var key = keyValue[0];
                var value = keyValue[1];
                if (key == "CacheControl")
                {
                    metadataChange.CacheControl = value;
                }
                else if (key == "ContentDisposition")
                {
                    metadataChange.ContentDisposition = value;
                }
                else if (key == "ContentEncoding")
                {
                    metadataChange.ContentEncoding = value;
                    Debug.Log("!!!!!ENCODING: " + value.ToString());
                }
                else if (key == "ContentLanguage")
                {
                    metadataChange.ContentLanguage = value;
                }
                else if (key == "ContentType")
                {
                    metadataChange.ContentType = value;
                }
                else
                {
                    customMetadata[key] = value;
                }
            }
            if (customMetadata.Count > 0)
                metadataChange.CustomMetadata = customMetadata;
            return hasMetadata ? metadataChange : null;
        }


        // Retrieve a storage reference from the user specified path.
        protected StorageReference GetStorageReference()
        {
            // If this is an absolute path including a bucket create a storage instance.
            if (storageLocation.StartsWith("gs://") ||
                storageLocation.StartsWith("http://") ||
                storageLocation.StartsWith("https://"))
            {
                var storageUri = new Uri(storageLocation);
                var firebaseStorage = FirebaseStorage.GetInstance(
                  String.Format("{0}://{1}", storageUri.Scheme, storageUri.Host));


                return firebaseStorage.GetReferenceFromUrl(storageLocation);
            }
            else
            {
                DebugLog("failed to get storage reference");
            }
            // When using relative paths use the default storage instance which uses the bucket supplied
            // on creation of FirebaseApp.
            return FirebaseStorage.DefaultInstance.GetReference(storageLocation);
        }


        // Wait for task completion, throwing an exception if the task fails.
        // This could be typically implemented using
        // yield return new WaitUntil(() => task.IsCompleted);
        // however, since many procedures in this sample nest coroutines and we want any task exceptions
        // to be thrown from the top level coroutine (e.g UploadBytes) we provide this
        // CustomYieldInstruction implementation wait for a task in the context of the coroutine using
        // common setup and tear down code.
        class WaitForTaskCompletion : CustomYieldInstruction
        {
            Task task;
            C_FirebaseStorageManager storageTestClass = new C_FirebaseStorageManager();

            // Create an enumerator that waits for the specified task to complete.
            public WaitForTaskCompletion(C_FirebaseStorageManager firebaseStorageTest, Task task)
            {
                storageTestClass.previousTask = task;
                storageTestClass.operationInProgress = true;
                this.storageTestClass = firebaseStorageTest;
                this.task = task;
            }

            // Wait for the task to complete.
            public override bool keepWaiting
            {
                get
                {
                    if (task.IsCompleted)
                    {
                        storageTestClass.operationInProgress = false;
                        storageTestClass.cancellationTokenSource = new CancellationTokenSource();
                        if (task.IsFaulted)
                        {
                            storageTestClass.DisplayStorageException(task.Exception);
                        }
                        return false;
                    }
                    return true;
                }
            }
        }

        // Output text to the debug log text field, as well as the console.
        public void DebugLog(string s)
        {
            Debug.Log(s);
            debugTextController.debugLog(s);
            /*
            logText += s + "\n";

            while (logText.Length > kMaxLogSize)
            {
                int index = logText.IndexOf("\n");
                logText = logText.Substring(index + 1);
            }

            scrollViewVector.y = int.MaxValue;
            */
        }   

        // Display a storage exception.
        protected void DisplayStorageException(Exception exception)
        {
            var storageException = exception as StorageException;
            if (storageException != null)
            {
                DebugLog(String.Format("Error Code: {0}", storageException.ErrorCode));
                DebugLog(String.Format("HTTP Result Code: {0}", storageException.HttpResultCode));
                DebugLog(String.Format("Recoverable: {0}", storageException.IsRecoverableException));
                DebugLog(storageException.ToString());
            }
            else
            {
                DebugLog(exception.ToString());
            }
        }

        // Write upload state to the log.
        protected virtual void DisplayUploadState(UploadState uploadState)
        {
            if (operationInProgress)
            {
                
                DebugLog(String.Format("Uploading {0}: {1} out of {2}", uploadState.Reference.Name,
                                       uploadState.BytesTransferred, uploadState.TotalByteCount));
                
            }
        }


        // Convert a Metadata object to a string.
        protected string MetadataToString(StorageMetadata metadata, bool onlyMutableFields)
        {
            var fieldsAndValues = new Dictionary<string, object> {
        {"ContentType", metadata.ContentType},
        {"CacheControl", metadata.CacheControl},
        {"ContentDisposition", metadata.ContentDisposition},
        {"ContentEncoding", metadata.ContentEncoding},
        {"ContentLanguage", metadata.ContentLanguage}
      };
            if (!onlyMutableFields)
            {
                foreach (var kv in new Dictionary<string, object> {
                            {"Reference", metadata.Reference != null ?
                                              metadata.Reference.Path : null},
                            {"Path", metadata.Path},
                            {"Name", metadata.Name},
                            {"Bucket", metadata.Bucket},
                            {"Generation", metadata.Generation},
                            {"MetadataGeneration", metadata.MetadataGeneration},
                            {"CreationTimeMillis", metadata.CreationTimeMillis},
                            {"UpdatedTimeMillis", metadata.UpdatedTimeMillis},
                            {"SizeBytes", metadata.SizeBytes},
                            {"Md5Hash", metadata.Md5Hash}
                         })
                {
                    fieldsAndValues[kv.Key] = kv.Value;
                }
            }
            foreach (var key in metadata.CustomMetadataKeys)
            {
                fieldsAndValues[key] = metadata.GetCustomMetadata(key);
            }
            var fieldAndValueStrings = new List<string>();
            foreach (var kv in fieldsAndValues)
            {
                fieldAndValueStrings.Add(String.Format("{0}={1}", kv.Key, kv.Value));
            }
            return String.Join("\n", fieldAndValueStrings.ToArray());
        }

    // Write download state to the log.
        protected virtual void DisplayDownloadState(DownloadState downloadState)
        {
            if (operationInProgress)
            {
                DebugLog(String.Format("Downloading {0}: {1} out of {2}", downloadState.Reference.Name,
                                       downloadState.BytesTransferred, downloadState.TotalByteCount));
            }
        }

        // Get a local filesystem path from a file:// URI.
        protected string FileUriStringToPath(string fileUriString)
        {
            return Uri.UnescapeDataString((new Uri(fileUriString)).PathAndQuery);
        }


        // Download from Cloud Storage to a local file.
        protected IEnumerator DownloadToFile(string localFilename)
        {
            var storageReference = GetStorageReference();

            DebugLog("DownloadToFile got storage reference:" + storageReference);
            var localFilenameUriString = PathToPersistentDataPathUriString(localFilename);


                
            DebugLog(String.Format("Downloading {0} to {1}...", storageReference.Path,
                                   localFilenameUriString));


            var task = storageReference.GetFileAsync(   localFilenameUriString,
                                                        new StorageProgress<DownloadState>(DisplayDownloadState),
                                                        cancellationTokenSource.Token);


            //yield return new WaitUntil(() => task.IsCompleted);
            yield return new WaitForTaskCompletion(this, task);

            if (!(task.IsFaulted || task.IsCanceled))
            {
                var filename = FileUriStringToPath(localFilenameUriString);
                DebugLog(String.Format("Finished downloading file {0} ({1})", localFilenameUriString,
                                       filename));

                //load the object
                GetComponent<C_LoadObjAsync>().loadMeshAsync(localFilename);
                //DebugLog(String.Format("File Size {0} bytes\n", (new FileInfo(filename)).Length));
                //fileContents = File.ReadAllText(filename);
            }
            else
            {
                //task.Exception.Message;
                DebugLog("!!!!!!SOMEHTHING WENT WRONG WITH THE DOWNLOAD: " + task.Exception.Message);

            }
        }

    
        // Display the result of an upload operation.
        protected void DisplayUploadComplete(Task<StorageMetadata> task)
        {
            if (!(task.IsFaulted || task.IsCanceled))
            {
                //fileContents = "";
                fileMetadataChangeString = "";
                DebugLog("Finished uploading");
                DebugLog(MetadataToString(task.Result, false));
                DebugLog("Press the Download button to download text from Cloud Storage\n");

            }
        }

        // Upload a file from the local filesystem to Cloud Storage.
        protected IEnumerator UploadFromFile(string localFilename)
        {
            string localFilenameUriString = PathToPersistentDataPathUriString(localFilename);
            StorageReference storageReference = GetStorageReference();
           
            DebugLog(String.Format("Uploading '{0}' to '{1}'...", localFilenameUriString,
                                   storageReference.Path));
            
            var task = storageReference.PutFileAsync(
            localFilenameUriString, StringToMetadataChange(fileMetadataChangeString),
            new StorageProgress<UploadState>(DisplayUploadState),
            cancellationTokenSource.Token, null);
            yield return new WaitUntil(() => task.IsCompleted);
            //yield return new WaitForTaskCompletion(this, task);
            DisplayUploadComplete(task);
        }


        public void Upload(string localFilename)
        {
            if (String.IsNullOrEmpty(storageLocation))
            {
                storageLocation = MyStorageBucket + localFilename;
            }
            Debug.Log("uploading");
            StartCoroutine(UploadFromFile(localFilename));

        }

        public void Download(string localFilename,bool overwriteExisting)
        {
            //if (String.IsNullOrEmpty(storageLocation))
            //{
            storageLocation = MyStorageBucket  + localFilename;

            Debug.Log("storage location on firebase: " + storageLocation);

            //string localFilenameUriString = PathToPersistentDataPathUriString(localFilename);
            string localFile = persistentDataPath + "/" + localFilename;
            DebugLog("localfilenameuristring: " + localFile);
            //Debug.Log("localfilenameuristring: " + localFile);
            if (File.Exists(localFile) && !overwriteExisting)
            {
                //file already exists, do not overwrite
                Debug.Log("file exists! we wont overwite it");
            }
            else
            {
                DebugLog("!!!!!!!!!!downloading file from firebase storage");
               // Debug.Log("downloading");
                StartCoroutine(DownloadToFile(localFilename));
            }
        }

        /*
        // Exit if escape (or back, on mobile) is pressed.
        protected virtual void Update()
        {
            
            if (Input.GetKeyDown(KeyCode.U))
            {
                if (String.IsNullOrEmpty(storageLocation))
                {
                    storageLocation = MyStorageBucket + localFilename;
                }
                Debug.Log("uploading");
                StartCoroutine(UploadFromFile());
            }


            if (Input.GetKeyDown(KeyCode.D))
            {

                if (String.IsNullOrEmpty(storageLocation))
                {
                    storageLocation = MyStorageBucket + "testmesh2.obj";
                }
                //Debug.Log("downloading: " + storageLocation);
                Download("testmesh2.obj", true);
                //StartCoroutine(DownloadToFile("testmesh2.obj"));
            }


        }*/

    }

