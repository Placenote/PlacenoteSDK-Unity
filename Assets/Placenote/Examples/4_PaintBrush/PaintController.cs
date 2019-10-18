using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using System.Runtime.InteropServices;

public class PaintController : MonoBehaviour, PlacenoteListener {

	public GameObject drawingRootSceneObject;
	public Text textLabel;
	private bool pointCloudOn = false;

	public GameObject paintPanel;
	public GameObject startPanel;
    public GameObject loadPanel;

    [SerializeField] GameObject brushTipObject;
    [SerializeField] GameObject colorPalette;

    [SerializeField] RawImage mLocalizationThumbnail;
    [SerializeField] Image mLocalizationThumbnailContainer;

    public int drawingHistoryIndex = 0;

	// Use this for initialization
	void Start () {

        //FeaturesVisualizer.EnablePointcloud ();
        LibPlacenote.Instance.RegisterListener (this);

        mLocalizationThumbnailContainer.gameObject.SetActive(false);

        // Set up the localization thumbnail texture event.
        LocalizationThumbnailSelector.Instance.TextureEvent += (thumbnailTexture) =>
        {
            if (mLocalizationThumbnail == null)
            {
                return;
            }

            // set the width and height of the thumbnail based on the texture obtained
            RectTransform rectTransform = mLocalizationThumbnailContainer.rectTransform;
            if (thumbnailTexture.width != (int)rectTransform.rect.width)
            {
                rectTransform.SetSizeWithCurrentAnchors(
                    RectTransform.Axis.Horizontal, thumbnailTexture.width * 2);
                rectTransform.SetSizeWithCurrentAnchors(
                    RectTransform.Axis.Vertical, thumbnailTexture.height * 2);
                rectTransform.ForceUpdateRectTransforms();
            }

            // set the texture
            mLocalizationThumbnail.texture = thumbnailTexture;
        };

    }

	public void onClickEnablePointCloud()
	{
		if (pointCloudOn == false) {
            FeaturesVisualizer.EnablePointcloud(new Color(1f, 1f, 1f, 0.2f), new Color(1f, 1f, 1f, 0.8f));
			pointCloudOn = true;
			Debug.Log ("Point Cloud On");
		} else {
			FeaturesVisualizer.DisablePointcloud ();
            FeaturesVisualizer.ClearPointcloud();
            pointCloudOn = false;
			Debug.Log ("Point Cloud Off");
		}

	}

    public void OnToggleColorPaletteClick()
    {
        if (colorPalette.activeInHierarchy)
        {
            colorPalette.SetActive(false);
        }
        else
        {
            colorPalette.SetActive(true);
        }
    }


    // Update is called once per frame
    void Update () {
    

    }		

	public void onStartPaintingClick ()
	{
    
		startPanel.SetActive (false);
		paintPanel.SetActive (true);

        onClearAllClick();

        LibPlacenote.Instance.StartSession ();

        brushTipObject.SetActive(true);

        textLabel.text = "Press and hold the screen to paint";


	}



	public void OnSaveMapClick ()
	{
		

		if (!LibPlacenote.Instance.Initialized()) {
			Debug.Log ("SDK not yet initialized");
			return;
		}

		//mLabelText.text = "Saving...";
		LibPlacenote.Instance.SaveMap (
			(mapId) => {
				LibPlacenote.Instance.StopSession ();

				print("Saved Map Id:" + mapId);

				saveScene (mapId);

				textLabel.text = "Saving Your Painting: ";

				//mLabelText.text = "Saved Map ID: " + mapId;
				//mInitButtonPanel.SetActive (true);
				//mMappingButtonPanel.SetActive (false);

				//string jsonPath = Path.Combine(Application.persistentDataPath, mapId + ".json");
				//SaveShapes2JSON(jsonPath);
			},
			(completed, faulted, percentage) => {
				Debug.Log("Uploading map...");

				if(completed) {
					Debug.Log("Done Uploaded!!");

					textLabel.text = "Saved! Try Loading it!";

					startPanel.SetActive(true);
					paintPanel.SetActive(false);

                    // clearing the painting and history currently active
                    onClearAllClick();

                }
                else if(faulted)
                {
                    textLabel.text = "Map upload failed.";
                }
                else
                {
                    textLabel.text = "Saving Your Painting: " + (percentage * 100.0f).ToString("F2") + " %";
                }

            }
		);
	}
		
	public void OnLoadMapClicked ()
	{

		if (!LibPlacenote.Instance.Initialized()) {
			Debug.Log ("SDK not yet initialized");
			return;
		}

		var mSelectedMapId = GetComponent<DrawingHistoryManager> ().loadMapIDFromFile ();

		if (mSelectedMapId == null) {
			Debug.Log ("The saved map id was null!");

		} else {
			LibPlacenote.Instance.LoadMap (mSelectedMapId,
				(completed, faulted, percentage) => {
					if (completed) {

						textLabel.text = "To load your drawing, point at the area shown in the thumbnail.";

                        startPanel.SetActive(false);
                        loadPanel.SetActive(true);

                        mLocalizationThumbnailContainer.gameObject.SetActive(true);
                        LibPlacenote.Instance.StartSession ();
						//mLabelText.text = "Loaded ID: " + mSelectedMapId;
					} else if (faulted) {
						//mLabelText.text = "Failed to load ID: " + mSelectedMapId;
					}
				}
			);
		}


	}

    public void OnExitLoadedPaintingClick()
    {
        mLocalizationThumbnailContainer.gameObject.SetActive(false);

        loadPanel.SetActive(false);
        startPanel.SetActive(true);

        LibPlacenote.Instance.StopSession();
        FeaturesVisualizer.ClearPointcloud();

        onClearAllClick();
    }


    public void replayDrawing()
	{
		deleteAllObjects ();

		StartCoroutine ( GetComponent<DrawingHistoryManager> ().replayDrawing () );


	}


	public void deleteAllObjects()
	{

		int numChildren = drawingRootSceneObject.transform.childCount;

		for (int i = 0; i < numChildren; i++) {

			GameObject toDestroy = drawingRootSceneObject.transform.GetChild (i).gameObject;

			if (string.Compare (toDestroy.name, "CubeBrushTip") != 0  && string.Compare (toDestroy.name, "SphereBrushTip") != 0   ) {
				Destroy (drawingRootSceneObject.transform.GetChild (i).gameObject);
			}
		}

	}


	public void onClearAllClick()
	{
		deleteAllObjects ();
		GetComponent<DrawingHistoryManager> ().resetHistory ();
	}


	public void saveScene (string mapid)
	{
		GetComponent<DrawingHistoryManager> ().saveDrawingHistory ();

		GetComponent<DrawingHistoryManager> ().saveMapIDToFile (mapid);
	}


	public void loadSavedScene()
	{
		// delete current scene
		deleteAllObjects();

		// load saved scene
		GetComponent<DrawingHistoryManager> ().loadFromDrawingHistory ();

		// replay drawing
		replayDrawing();


	}

	public void OnPose (Matrix4x4 outputPose, Matrix4x4 arkitPose) {}



	// This function runs when LibPlacenote sends a status change message like Localized!

	public void OnStatusChange (LibPlacenote.MappingStatus prevStatus, LibPlacenote.MappingStatus currStatus)
	{
		Debug.Log ("prevStatus: " + prevStatus.ToString() + " currStatus: " + currStatus.ToString());


		if (currStatus == LibPlacenote.MappingStatus.RUNNING && prevStatus == LibPlacenote.MappingStatus.LOST) {

			Debug.Log ("Localized!");

		} else if (currStatus == LibPlacenote.MappingStatus.RUNNING && prevStatus == LibPlacenote.MappingStatus.WAITING) {
			Debug.Log ("Mapping");

		} else if (currStatus == LibPlacenote.MappingStatus.LOST) {
			Debug.Log("Searching for position lock");

		} else if (currStatus == LibPlacenote.MappingStatus.WAITING) {

		}

	}

    public void OnLocalized()
    {
        textLabel.text = "Found It!";

        loadSavedScene();

        mLocalizationThumbnailContainer.gameObject.SetActive(false);

        // To increase tracking smoothness after localization
        LibPlacenote.Instance.StopSendingFrames();
    }
}
