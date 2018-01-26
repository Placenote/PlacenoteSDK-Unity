using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.UI;
using UnityEngine.XR.iOS;
using System.Runtime.InteropServices;
using System.IO;



[System.Serializable]
public class ShapeInfo
{
	public float px;
	public float py;
	public float pz;
	public float qx;
	public float qy;
	public float qz;
	public float qw;
	public int shapeType;
}


[System.Serializable]
public class ShapeList
{
	public ShapeInfo[] shapes;
}


public class PlacenoteSampleView : MonoBehaviour, PlacenoteListener
{
	[SerializeField] GameObject mMapSelectedPanel;
	[SerializeField] GameObject mInitButtonPanel;
	[SerializeField] GameObject mMappingButtonPanel;
	[SerializeField] GameObject mMapListPanel;
	[SerializeField] GameObject mExitButton;
	[SerializeField] GameObject mListElement;
	[SerializeField] RectTransform mListContentParent;
	[SerializeField] ToggleGroup mToggleGroup;
	[SerializeField] Text mLabelText;
	[SerializeField] Material mShapeMaterial;

	private UnityARSessionNativeInterface mSession;
	private bool mFrameUpdated = false;
	private UnityARImageFrameData mImage = null;
	private UnityARCamera mARCamera;
	private bool mARKitInit = false;
	private List<ShapeInfo> shapeInfoList = new List<ShapeInfo> ();
	private List<GameObject> shapeObjList = new List<GameObject> ();
	private string mSelectedMapId;

	private BoxCollider mBoxColliderDummy;
	private SphereCollider mSphereColliderDummy;
	private CapsuleCollider mCapColliderDummy;


	// Use this for initialization
	void Start ()
	{
		mMapListPanel.SetActive (false);

		mSession = UnityARSessionNativeInterface.GetARSessionNativeInterface ();
		UnityARSessionNativeInterface.ARFrameUpdatedEvent += ARFrameUpdated;
		StartARKit ();
		FeaturesVisualizer.EnablePointcloud ();
		LibPlacenote.Instance.RegisterListener (this);
	}


	private void ARFrameUpdated (UnityARCamera camera)
	{
		mFrameUpdated = true;
		mARCamera = camera;
	}


	private void InitARFrameBuffer ()
	{
		mImage = new UnityARImageFrameData ();

		int yBufSize = mARCamera.videoParams.yStride * mARCamera.videoParams.yHeight;
		mImage.y.data = Marshal.AllocHGlobal (yBufSize);
		mImage.y.width = (ulong)mARCamera.videoParams.yWidth;
		mImage.y.height = (ulong)mARCamera.videoParams.yHeight;
		mImage.y.stride = (ulong)mARCamera.videoParams.yStride;

		int vuBufSize = mARCamera.videoParams.vuStride * mARCamera.videoParams.vuHeight;
		mImage.vu.data = Marshal.AllocHGlobal (vuBufSize);
		mImage.vu.width = (ulong)mARCamera.videoParams.vuWidth;
		mImage.vu.height = (ulong)mARCamera.videoParams.vuHeight;
		mImage.vu.stride = (ulong)mARCamera.videoParams.vuStride;

		mSession.SetCapturePixelData (true, mImage.y.data, mImage.vu.data);
	}

	
	// Update is called once per frame
	void Update ()
	{
		if (mFrameUpdated) {
			mFrameUpdated = false;
			if (mImage == null) {
				InitARFrameBuffer ();
			}

			if (mARCamera.trackingState == ARTrackingState.ARTrackingStateNotAvailable) {
				// ARKit pose is not yet initialized
				return;
			} else if (!mARKitInit) {
				mARKitInit = true;
				mLabelText.text = "ARKit Initialized";
			}

			Matrix4x4 matrix = mSession.GetARKitPoseMatrix4x4 ();
			Vector3 arkitPosition = PNUtility.MatrixOps.GetPosition (matrix);
			Quaternion arkitQuat = PNUtility.MatrixOps.GetRotation (matrix);

			LibPlacenote.Instance.SendARFrame (mImage, arkitPosition, arkitQuat);
		}
	}


	public void OnListMapClick ()
	{
		if (!LibPlacenote.Instance.Initialized()) {
			Debug.Log ("SDK not yet initialized");
			ToastManager.ShowToast ("SDK not yet initialized", 2f);
			return;
		}

		foreach (Transform t in mListContentParent.transform) {
			Destroy (t.gameObject);
		}

		mMapListPanel.SetActive (true);
		mInitButtonPanel.SetActive (false);
		LibPlacenote.Instance.ListMaps ((mapList) => {
			// render the map list!
			foreach (LibPlacenote.MapInfo mapId in mapList) {
				AddMapToList (mapId);
			}
		});
	}


	public void OnCancelClick ()
	{
		mMapSelectedPanel.SetActive (false);
		mMapListPanel.SetActive (false);
		mInitButtonPanel.SetActive (true);
	}


	public void OnExitClick ()
	{
		mInitButtonPanel.SetActive (true);
		mExitButton.SetActive (false);
		LibPlacenote.Instance.StopSession ();
	}


	void AddMapToList (LibPlacenote.MapInfo mapInfo)
	{
		GameObject newElement = Instantiate (mListElement) as GameObject;
		MapInfoElement listElement = newElement.GetComponent<MapInfoElement> ();
		listElement.Initialize (mapInfo, mToggleGroup, mListContentParent, (value) => {
			OnMapSelected (mapInfo.placeId);
		});
	}


	void OnMapSelected (string selectedMapId)
	{
		mSelectedMapId = selectedMapId;
		mMapSelectedPanel.SetActive (true);
	}


	public void OnLoadMapClicked ()
	{
		if (!LibPlacenote.Instance.Initialized()) {
			Debug.Log ("SDK not yet initialized");
			ToastManager.ShowToast ("SDK not yet initialized", 2f);
			return;
		}

		mLabelText.text = "Loading Map ID: " + mSelectedMapId;
		LibPlacenote.Instance.LoadMap (mSelectedMapId,
			(completed, faulted, percentage) => {
				if (completed) {
					mMapSelectedPanel.SetActive (false);
					mMapListPanel.SetActive (false);
					mInitButtonPanel.SetActive (false);
					mExitButton.SetActive (true);

					LibPlacenote.Instance.StartSession ();
					mLabelText.text = "Loaded ID: " + mSelectedMapId;
				} else if (faulted) {
					mLabelText.text = "Failed to load ID: " + mSelectedMapId;
				}
			}
		);
	}

	public void OnDeleteMapClicked ()
	{
		if (!LibPlacenote.Instance.Initialized()) {
			Debug.Log ("SDK not yet initialized");
			ToastManager.ShowToast ("SDK not yet initialized", 2f);
			return;
		}

		mLabelText.text = "Deleting Map ID: " + mSelectedMapId;
		LibPlacenote.Instance.DeleteMap (mSelectedMapId, (deleted, errMsg) => {
			if (deleted) {
				mMapSelectedPanel.SetActive (false);
				mLabelText.text = "Deleted ID: " + mSelectedMapId;
				OnListMapClick();
			} else {
				mLabelText.text = "Failed to delete ID: " + mSelectedMapId;
			}
		});
	}


	public void OnNewMapClick ()
	{
		if (!LibPlacenote.Instance.Initialized()) {
			Debug.Log ("SDK not yet initialized");
			return;
		}

		mInitButtonPanel.SetActive (false);
		mMappingButtonPanel.SetActive (true);

		LibPlacenote.Instance.StartSession ();
	}


	private void StartARKit ()
	{
		mLabelText.text = "Initializing ARKit";
		Application.targetFrameRate = 60;
		ARKitWorldTackingSessionConfiguration config = new ARKitWorldTackingSessionConfiguration ();
		config.planeDetection = UnityARPlaneDetection.Horizontal;
		config.alignment = UnityARAlignment.UnityARAlignmentGravity;
		config.getPointCloudData = true;
		config.enableLightEstimation = true;
		mSession.RunWithConfig (config);
	}


	public void OnSaveMapClick ()
	{
		if (!LibPlacenote.Instance.Initialized()) {
			Debug.Log ("SDK not yet initialized");
			ToastManager.ShowToast ("SDK not yet initialized", 2f);
			return;
		}

		mLabelText.text = "Saving...";
		LibPlacenote.Instance.SaveMap (
			(mapId) => {
				LibPlacenote.Instance.StopSession ();
				mLabelText.text = "Saved Map ID: " + mapId;
				mInitButtonPanel.SetActive (true);
				mMappingButtonPanel.SetActive (false);

				string jsonPath = Path.Combine(Application.persistentDataPath, mapId + ".json");
				SaveShapes2JSON(jsonPath);
			},
			(completed, faulted, percentage) => {}
		);
	}
		

	public void OnDropShapeClick ()
	{
		Vector3 shapePosition = Camera.main.transform.position + Camera.main.transform.forward * 0.3f;
		Quaternion shapeRotation = Camera.main.transform.rotation;

		System.Random rnd = new System.Random ();
		PrimitiveType type = (PrimitiveType) rnd.Next(0, 3);

		ShapeInfo shapeInfo = new ShapeInfo ();
		shapeInfo.px = shapePosition.x;
		shapeInfo.py = shapePosition.y;
		shapeInfo.pz = shapePosition.z;
		shapeInfo.qx = shapeRotation.x;
		shapeInfo.qy = shapeRotation.y;
		shapeInfo.qz = shapeRotation.z;
		shapeInfo.qw = shapeRotation.w;
		shapeInfo.shapeType = type.GetHashCode ();
		shapeInfoList.Add(shapeInfo);

		GameObject shape = ShapeFromInfo(shapeInfo);
		shapeObjList.Add(shape);
	}


	private GameObject ShapeFromInfo(ShapeInfo info)
	{
		GameObject shape = GameObject.CreatePrimitive ((PrimitiveType)info.shapeType);
		shape.transform.position = new Vector3(info.px, info.py, info.pz);
		shape.transform.rotation = new Quaternion(info.qx, info.qy, info.qz, info.qw);
		shape.transform.localScale = new Vector3 (0.05f, 0.05f, 0.05f);
		shape.GetComponent<MeshRenderer> ().material = mShapeMaterial;

		return shape;
	}


	private void ClearShapes () {
		foreach (var obj in shapeObjList) {
			Destroy (obj);
		}
		shapeObjList.Clear ();
		shapeInfoList.Clear ();
	}

	private void SaveShapes2JSON (String filePath)
	{
		Debug.Log ("Saving to " + filePath);
		ShapeList shapeList = new ShapeList ();
		shapeList.shapes = new ShapeInfo[shapeInfoList.Count];
		for (int i = 0; i < shapeInfoList.Count; i++) {
			shapeList.shapes [i] = shapeInfoList [i];
		}

		String shapeListJson = JsonUtility.ToJson (shapeList);
		Debug.Log ("Shape JSON:\n" + shapeListJson);

		using (StreamWriter outputFile = new StreamWriter (filePath)) {
			outputFile.Write (shapeListJson);
			ClearShapes ();
		}
	}


	private void LoadShapesJSON (string filePath)
	{
		Debug.Log ("Loading from " + filePath);
		ClearShapes ();

		using (StreamReader inputFile = new StreamReader (filePath)) {
			string shapeListJson = inputFile.ReadToEnd ();
			Debug.Log ("Shape JSON:\n" + shapeListJson);

			ShapeList shapeList = JsonUtility.FromJson<ShapeList> (shapeListJson);
			if (shapeList.shapes == null) {
				Debug.Log ("no shapes dropped");
				return;
			}

			foreach (var shapeInfo in shapeList.shapes) {
				shapeInfoList.Add (shapeInfo);
				GameObject shape = ShapeFromInfo(shapeInfo);
				shapeObjList.Add(shape);
			}
		}
	}


	public void OnPose (Matrix4x4 outputPose, Matrix4x4 arkitPose) {}


	public void OnStatusChange (LibPlacenote.MappingStatus prevStatus, LibPlacenote.MappingStatus currStatus)
	{
		Debug.Log ("prevStatus: " + prevStatus.ToString() + " currStatus: " + currStatus.ToString());
		if (currStatus == LibPlacenote.MappingStatus.RUNNING && prevStatus == LibPlacenote.MappingStatus.LOST) {
			mLabelText.text = "Localized";
			string jsonPath = Path.Combine (Application.persistentDataPath, mSelectedMapId + ".json");

			if (File.Exists (jsonPath) && shapeObjList.Count == 0) {
				LoadShapesJSON (jsonPath);
			}
		} else if (currStatus == LibPlacenote.MappingStatus.RUNNING && prevStatus == LibPlacenote.MappingStatus.WAITING) {
			mLabelText.text = "Mapping";
		} else if (currStatus == LibPlacenote.MappingStatus.LOST) {
			mLabelText.text = "Searching for position lock";
		} else if (currStatus == LibPlacenote.MappingStatus.WAITING) {
			if (shapeObjList.Count != 0) {
				ClearShapes ();
			}
		}
	}
}
