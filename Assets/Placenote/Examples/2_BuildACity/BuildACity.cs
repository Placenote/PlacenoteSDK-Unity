using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;
using System.IO;
using UnityEngine.UI;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace BuildACity
{
    public class BuildACity : MonoBehaviour, PlacenoteListener
    {
        // UI game object references
        [SerializeField] GameObject initPanel;
        [SerializeField] GameObject mappingPanel;
        [SerializeField] GameObject localizedPanel;
        [SerializeField] Texture2D starTexture;

        [SerializeField] Button saveMapButton;
        [SerializeField] Image saveButtonProgressBar;

        [SerializeField] Text notifications;

        // to hold the last saved MapID
        private string savedMapID;
        private float minMapSize = 200;

        // to store the meta data we save
        private LibPlacenote.MapMetadata downloadedMetaData;

        private bool mapQualityThresholdCrossed = false;


        void Start()
        {

            // customize the feature point visualization.
            //FeaturesVisualizer.EnablePointcloud(new Color(1f,1f,1f,0.2f), new Color(0f, 1f, 1f, 0.6f)); // Optional - to see the point features
            //FeaturesVisualizer.SetPointCloudTexture(starTexture);

            LibPlacenote.Instance.RegisterListener(this); // Register listener for onStatusChange and OnPose
            notifications.text = "Click New Map to start";
        }

        // Add shape when button is clicked.
        public void OnNewMapClick()
        {
            // check that placenote is initialized
            if (!LibPlacenote.Instance.Initialized())
            {
                notifications.text = "SDK not yet initialized";
                return;
            }

            saveButtonProgressBar.gameObject.GetComponent<Image>().fillAmount = 0;
            mapQualityThresholdCrossed = false;

            notifications.text = "Press down on a model, position it and then release to place it.";

            initPanel.SetActive(false);
            mappingPanel.SetActive(true);

            GetComponent<PlacementReticleController>().StartReticle();

            LibPlacenote.Instance.StartSession();


        }

        // Save a map and upload it to Placenote cloud
        public void OnSaveMapClick()
        {

            if (!mapQualityThresholdCrossed)
            {
                notifications.text = "Map quality is not good enough to save. Scan a small area with many features and try again.";
                return;
            }

            mappingPanel.SetActive(false);

            GetComponent<PlacementReticleController>().StopReticle();

            // save and upload the map
            LibPlacenote.Instance.SaveMap(
            (mapId) =>
            {
                savedMapID = mapId;
                LibPlacenote.Instance.StopSession();

                // we save the map id we get from Placenote into a local file
                // This can also be saved on any web backend you build for your application
                WriteMapIDToFile(mapId);

            },
            (completed, faulted, percentage) =>
            {
                if (completed) {
                    notifications.text = "Map Uploaded:" + savedMapID;

                    // create serialized meta data object and upload it

                    LibPlacenote.MapMetadataSettable metadata = CreateMetaDataObject();

                    LibPlacenote.Instance.SetMetadata(savedMapID, metadata, (success) => {
                        if (success)
                        {
                            notifications.text = "Meta data successfully saved";

                            initPanel.SetActive(true);

                            //FeaturesVisualizer.ClearPointcloud();
                            GetComponent<ModelManager>().ClearModels();

                        }
                        else
                        {
                            notifications.text = "Meta data failed to save";
                        }
                    });

                }
                else if (faulted) {
                    notifications.text = "Upload of Map: " + savedMapID + " failed";
                }
                else {
                    notifications.text = "Upload Progress: " + percentage.ToString("F2") + "/1.0)";
                }
            }
            );
        }


        public LibPlacenote.MapMetadataSettable CreateMetaDataObject()
        {
            LibPlacenote.MapMetadataSettable metadata = new LibPlacenote.MapMetadataSettable();

            metadata.name = "Astronaut Paradise";

            // get GPS location of device to save with map
            bool useLocation = Input.location.status == LocationServiceStatus.Running;
            LocationInfo locationInfo = Input.location.lastData;
            if (useLocation)
            {
                metadata.location = new LibPlacenote.MapLocation();
                metadata.location.latitude = locationInfo.latitude;
                metadata.location.longitude = locationInfo.longitude;
                metadata.location.altitude = locationInfo.altitude;
            }

            JObject userdata = new JObject();
            JObject modelList = GetComponent<ModelManager>().Models2JSON();
            userdata["modelList"] = modelList;

            metadata.userdata = userdata;
            return metadata;
        }
         

        // Load map and relocalize. Check OnStatusChange function for behaviour upon relocalization
        public void OnLoadMapClicked()
        {
            if (!LibPlacenote.Instance.Initialized())
            {
                notifications.text = "SDK not yet initialized";
                return;
            }

            // Reading the last saved MapID from file
            savedMapID = ReadMapIDFromFile();

            if (savedMapID == null)
            {
                notifications.text = "You haven't saved a map yet";
                return;
            }

            initPanel.SetActive(false);
            localizedPanel.SetActive(true);


            LibPlacenote.Instance.LoadMap(savedMapID,
            (completed, faulted, percentage) =>
            {
                if (completed)
                {
                    // Get the meta data as soon as the map is downloaded
                    LibPlacenote.Instance.GetMetadata(savedMapID,(LibPlacenote.MapMetadata obj) => 
                    {
                        if (obj!=null) {
                            downloadedMetaData = obj;

                            // Now try to localize the map
                            LibPlacenote.Instance.StartSession();
                            notifications.text = "Trying to Localize Map: " + savedMapID;
                        }
                        else {
                            notifications.text = "Failed to download meta data";
                            return;
                        }
                    });

                }
                else if (faulted)
                {
                    notifications.text = "Failed to load ID: " + savedMapID;
                }
                else
                {
                    notifications.text = "Download Progress: " + percentage.ToString("F2") + "/1.0)";
                }
            }

            );
        }

        public void OnExitClicked()
        {
            LibPlacenote.Instance.StopSession();
            //FeaturesVisualizer.ClearPointcloud();
            GetComponent<ModelManager>().ClearModels();

            initPanel.SetActive(true);
            localizedPanel.SetActive(false);

            notifications.text = "Session was reset. Click new map or load map to start again";

        }

        // Runs when a new pose is received from Placenote.    

       
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
                Debug.Log("Point cloud is null");
                return;
            }

            Debug.Log("Point cloud size = " + fullPointCloudMap.Count);

            saveButtonProgressBar.gameObject.GetComponent<Image>().fillAmount = fullPointCloudMap.Count / minMapSize;

            if (fullPointCloudMap.Count >= minMapSize)
            {

                notifications.text = "Minimum map created. Keep adding models or save the map anytime.";

                // Check the map quality to confirm whether you can save
                if (LibPlacenote.Instance.GetMappingQuality() == LibPlacenote.MappingQuality.GOOD)
                {
                    mapQualityThresholdCrossed = true;
                }

            }

        }



        // Runs when LibPlacenote sends a status change message like Localized!
        public void OnStatusChange(LibPlacenote.MappingStatus prevStatus, LibPlacenote.MappingStatus currStatus)
        {

        }

        public void OnLocalized()
        {
            notifications.text = "Localized!";
            JToken modelData = downloadedMetaData.userdata;
            GetComponent<ModelManager>().LoadModelsFromJSON(modelData);

        }

        private void WriteMapIDToFile(string mapID)
        {
            string path = Application.persistentDataPath + "/mapID.txt";
            Debug.Log(path);
            StreamWriter writer = new StreamWriter(path, false);
            writer.WriteLine(mapID);
            writer.Close();
        }

        private string ReadMapIDFromFile()
        {
            string path = Application.persistentDataPath + "/mapID.txt";
            Debug.Log(path);

            if (System.IO.File.Exists(path))
            {
                StreamReader reader = new StreamReader(path);
                string returnValue = reader.ReadLine();

                Debug.Log(returnValue);
                reader.Close();

                return returnValue;
            }
            else
            {
                return null;
            }


        }

    }
}
