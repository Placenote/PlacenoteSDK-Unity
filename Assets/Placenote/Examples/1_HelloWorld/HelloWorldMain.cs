using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;
using System.IO;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;

namespace HelloWorld
{
    public class HelloWorldMain : MonoBehaviour, PlacenoteListener
    {
        [SerializeField] GameObject mInitPanel;
        [SerializeField] GameObject mMappingPanel;
        [SerializeField] GameObject mLoadingPanel;
        [SerializeField] GameObject mModelWaving;
        [SerializeField] GameObject mModelDancing;
        [SerializeField] Image mMappingProgressBar;

        public Text statusLabel;

        private string savedMapID;  // to hold the last saved MapID
        private Vector3 savedModelPosition;  // to hold the last saved model position
        private Quaternion savedModelRotation;

        private float minMapSize = 250;

        void Start()
        {
            // wait for ar session to start tracking and for placenote to initialize
            StartCoroutine(WaitForARSessionThenStart());
        }

        IEnumerator WaitForARSessionThenStart()
        {
            statusLabel.text = "Starting Placenote AR Session";

            while (ARSession.state != ARSessionState.SessionTracking || !LibPlacenote.Instance.Initialized())
            {
                yield return null;
            }

            // Activating placenote
            //------------------
            LibPlacenote.Instance.RegisterListener(this); // Register listener for onStatusChange and OnPose
            FeaturesVisualizer.EnablePointcloud(); // Optional - to see the point features

            // ---------------

            // AR Session has started tracking here. Now start the session
            statusLabel.text = "Click New Map to start scanning, or load an previous map.";
            mInitPanel.SetActive(true);
        }

        // Start a new session
        public void OnNewMapClick()
        {

            // UI navigation and label updates to signal entry into mapping mdoe
            statusLabel.text = "Point at any flat surface, like a table, then hit the + button to place the model";
            mInitPanel.SetActive(false);
            GetComponent<ReticleController>().StartReticle();
        }


        public void AddModelAtReticle(GameObject reticle)
        {
            Debug.Log("button clicked");

            // place a model in front of the camera just when you start mapping
            mModelWaving.transform.position = reticle.transform.position;
            mModelWaving.transform.LookAt(new Vector3(Camera.main.transform.position.x, mModelWaving.transform.position.y, Camera.main.transform.position.z));

            mModelWaving.SetActive(true);

            GetComponent<ReticleController>().StopReticle();

            StartMapping();
        }

        public void StartMapping()
        {
            // check that placenote is initialized
            if (!LibPlacenote.Instance.Initialized())
            {
                statusLabel.text = "SDK not yet initialized";
                return;
            }

            mMappingPanel.SetActive(true);

            statusLabel.text = "Ok! Now scan the area near and around this model. The progress bar shows minimum map size.";

            // reset the save button to uninteractable and inactive
            mMappingPanel.transform.GetChild(0).gameObject.GetComponent<Button>().interactable = false;
            mMappingPanel.transform.GetChild(0).gameObject.SetActive(false);
            mMappingProgressBar.fillAmount = 0;

            // Start Placenote mapping
            LibPlacenote.Instance.StartSession();
        }

        // In this example we're useing this callback to monitor mapping progress.
        public void OnPose(Matrix4x4 outputPose, Matrix4x4 arkitPose)
        {

            // we only care about the mapping mode here
            if (LibPlacenote.Instance.GetMode() != LibPlacenote.MappingMode.MAPPING)
            {
                return;
            }

            // get the full point built so far
            List<Vector3> fullPointCloudMap = FeaturesVisualizer.GetPointCloud();

            // Check if either are null
            if (fullPointCloudMap == null)
            {
                return;
            }

            mMappingProgressBar.fillAmount = fullPointCloudMap.Count * 1.0f / minMapSize;

            if (fullPointCloudMap.Count >= minMapSize)
            {

                mMappingPanel.transform.GetChild(0).gameObject.SetActive(true);

                if (mMappingPanel.transform.GetChild(0).gameObject.GetComponent<Button>().interactable == true)
                {
                    statusLabel.text = "Minimum map size reached. Hit save!";
                }
                else
                {
                    statusLabel.text = "Minimum map size reached but you cannot save yet. Scan one area with many feature points until the button turns blue!";
                }

            }


            // Check the map quality to confirm whether you can save
            if (LibPlacenote.Instance.GetMappingQuality() == LibPlacenote.MappingQuality.GOOD)
            {
                // Turn the save button blue
                // The save button is the only child object of the mapping panel in the scene
                // If you change the scene remember to modify this.
                mMappingPanel.transform.GetChild(0).gameObject.GetComponent<Button>().interactable = true;
            }
        }


        public void OnResetClick()
        {

            mLoadingPanel.SetActive(false);
            mInitPanel.SetActive(true);

            LibPlacenote.Instance.StopSession();
            FeaturesVisualizer.ClearPointcloud();

            mModelWaving.SetActive(false);
            mModelDancing.SetActive(false);

            statusLabel.text = "Session was reset. Create a new map or re-load an existing map";

        }

        // Save a map and upload it to Placenote cloud
        public void OnSaveMapClick()
        {
            // close the mapping panel
            mMappingPanel.SetActive(false);

            mModelWaving.SetActive(false);
            FeaturesVisualizer.ClearPointcloud();

            statusLabel.text = "Saving your scene.";

            LibPlacenote.Instance.SaveMap(
            (mapId) =>
            {
                savedMapID = mapId;
                LibPlacenote.Instance.StopSession();
                GetComponent<FileReadWrite>().WriteMapIDToFile(mapId);
                GetComponent<FileReadWrite>().WriteModelTransformToFile(mModelWaving.transform.position, mModelWaving.transform.rotation);

                statusLabel.text = "Uploading map with ID: " + savedMapID;
            },
            (completed, faulted, percentage) =>
            {
                if (completed)
                {
                    statusLabel.text = "Upload Completed! You can now click Load Map.";

                    // enable the panel to let users load map
                    mInitPanel.SetActive(true);
                }
                else if (faulted)
                {
                    statusLabel.text = "Upload of Map: " + savedMapID + " failed";
                }
                else
                {
                    statusLabel.text = "Upload Progress: " + (percentage * 100).ToString("F2") + "%";
                }
            }
            );
        }

        // Load map and relocalize. Check OnStatusChange function for behaviour upon relocalization
        public void OnLoadMapClicked()
        {
            if (!LibPlacenote.Instance.Initialized())
            {
                statusLabel.text = "SDK not yet initialized";
                return;
            }

            // Reading the last saved MapID from file
            savedMapID = GetComponent<FileReadWrite>().ReadMapIDFromFile();


            if (savedMapID == null)
            {
                statusLabel.text = "You haven't created a map yet. Click new map first";
                return;
            }

            // read the saved model position
            bool dataLoadSuccess = GetComponent<FileReadWrite>().ReadModelTransformFromFile(ref savedModelPosition, ref savedModelRotation);

            if (!dataLoadSuccess)
            {
                statusLabel.text = "There was a problem with loading the saved model data";
                return;
            }

            mInitPanel.SetActive(false);
            mLoadingPanel.SetActive(true);

            LibPlacenote.Instance.LoadMap(savedMapID,
            (completed, faulted, percentage) =>
            {
                if (completed)
                {

                    LibPlacenote.Instance.StartSession();

                    statusLabel.text = "Trying to Localize Map: " + savedMapID;
                }
                else if (faulted)
                {
                    statusLabel.text = "Failed to load ID: " + savedMapID;
                }
                else
                {
                    statusLabel.text = "Download Progress: " + percentage.ToString("F2") + "/1.0)";
                }
            }

            );
        }


        // Runs when LibPlacenote sends a status change message like Localized!
        public void OnStatusChange(LibPlacenote.MappingStatus prevStatus, LibPlacenote.MappingStatus currStatus)
        {
            if (prevStatus == LibPlacenote.MappingStatus.LOST && currStatus == LibPlacenote.MappingStatus.RUNNING)
            {
                Debug.Log("On status says localized the scene");
            }
        }

        // Runs when LibPlacenote send a "Localized" message
        public void OnLocalized()
        {
            Debug.Log("Localized the scene");

            statusLabel.text = "Localized!";

            // load your model back in the same position
            mModelDancing.transform.position = savedModelPosition;
            mModelDancing.transform.rotation = savedModelRotation;
            mModelDancing.SetActive(true);
        }

    }

}