using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;
using System.IO;
using UnityEngine.UI;

public class SaveAndLoadAMap : MonoBehaviour, PlacenoteListener
{
    [SerializeField] GameObject mInitPanel;
    [SerializeField] GameObject mMappingPanel;
    [SerializeField] GameObject mLoadingPanel;
    [SerializeField] GameObject mCubePrefab;

    [SerializeField] FileReadWrite mFileHandler;

    public Text statusLabel;

    private string savedMapID;  // to hold the last saved MapID
    private Vector3 cubePosition;  // to hold the last saved cube position

    void Start()
    {
        LibPlacenote.Instance.RegisterListener(this); // Register listener for onStatusChange and OnPose
        statusLabel.text = "Click New Map to start scanning";
    }


    // Start a mapping session

    public void OnNewMapClick()
    {
        // Start Placenote mapping
        LibPlacenote.Instance.StartSession();
        FeaturesVisualizer.EnablePointcloud(); // optional: enable point cloud visualization

        // place a cube in front of the camera just when you start mapping
        cubePosition = Camera.main.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, 0.5f)); // get position 0.5m in front of camera
        GameObject cube = Instantiate(mCubePrefab, cubePosition, Quaternion.identity); // instantiate the cube

        // UI navigation and label updates
        statusLabel.text = "Scan the area around the cube. Hit Save when the button turns blue";
        mInitPanel.SetActive(false);
        mMappingPanel.SetActive(true);
        mLoadingPanel.SetActive(false);
    }


    // Runs when a new pose is received from Placenote.
    // In this example we're useing this function to monitor mapping progress.

    public void OnPose(Matrix4x4 outputPose, Matrix4x4 arkitPose)
    {
        /*
        // we only care about the mapping mode here
        if (LibPlacenote.Instance.GetMode() != LibPlacenote.MappingMode.MAPPING)
        {
            return;
        }

        // get current point cloud and the full pull point built so far
        LibPlacenote.PNFeaturePointUnity[] currentTrackedFeatures = LibPlacenote.Instance.GetTrackedFeatures();
        LibPlacenote.PNFeaturePointUnity[] fullPointCloudMap = LibPlacenote.Instance.GetMap();

        // Check if either are null
        if (currentTrackedFeatures == null || fullPointCloudMap == null)
        {
            return;
        }

        int lmSize = 0;
        for (int i = 0; i < trackedLandmarks.Length; i++)
        {
            if (trackedLandmarks[i].measCount > 2)
            {
                lmSize++;
            }
        }

        if (lmSize > mMaxLmSize)
        {
            mMaxLmSize = lmSize;
            SetCurrentImageAsThumbnail();
        }
        */

    }

    // Save a map and upload it to Placenote cloud
    public void OnSaveMapClick()
    {
        mMappingPanel.SetActive(false);
        mInitPanel.SetActive(true);


        if (!LibPlacenote.Instance.Initialized())
        {
            statusLabel.text = "SDK not yet initialized";
            return;
        }

        //mLabelText.text = "Saving...";
        LibPlacenote.Instance.SaveMap(
        (mapId) =>
        {
            savedMapID = mapId;
            LibPlacenote.Instance.StopSession();
            mFileHandler.WriteMapIDToFile(mapId);
        },
        (completed, faulted, percentage) =>
        {
            if (completed) {
                statusLabel.text = "Upload Complete:" + savedMapID;
            }
            else if (faulted) {
                statusLabel.text = "Upload of Map: " + savedMapID + " failed";
            }
            else {
                statusLabel.text = "Upload Progress: " + percentage.ToString("F2") + "/1.0)";
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
        savedMapID = mFileHandler.ReadMapIDFromFile();

        if (savedMapID == null)
        {
            statusLabel.text = "You haven't saved a map yet";
            return;
        }

        mInitPanel.SetActive(false);
        mLoadingPanel.SetActive(true);

        LibPlacenote.Instance.LoadMap(savedMapID, 
        (completed, faulted, percentage) =>    
        {
            if (completed) {

                LibPlacenote.Instance.StartSession();
                statusLabel.text = "Trying to Localize Map: " + savedMapID;
            }
            else if (faulted) {
                statusLabel.text = "Failed to load ID: " + savedMapID;
            }
            else {
                statusLabel.text = "Download Progress: " + percentage.ToString("F2") + "/1.0)";
            }
        }
                                      
        );
    }



    // Runs when LibPlacenote sends a status change message like Localized!
    public void OnStatusChange(LibPlacenote.MappingStatus prevStatus, LibPlacenote.MappingStatus currStatus) { }

    // Runs when LibPlacenote send a "Localized" message
    public void OnLocalized()
    {
        statusLabel.text = "Localized!";
    }



}

