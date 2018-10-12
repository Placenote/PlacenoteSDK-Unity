using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.UI;
using UnityEngine.XR.iOS;
using System.Runtime.InteropServices;
using System.IO;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;




public class PlacenoteSpatialCaptureSample : MonoBehaviour, PlacenoteListener {

	[SerializeField] Text mLabelText;
	[SerializeField] String mMapID;
	[SerializeField] GameObject mModelParent;

	private UnityARSessionNativeInterface mSession;
	private bool mFrameUpdated = false;
	private UnityARCamera mARCamera;
	private UnityARImageFrameData mImage = null;
	private bool mARKitInit = false;
	private bool mPlacenoteInit = false;

	// Use this for initialization
	void Start ()
	{
		Input.location.Start ();
		mSession = UnityARSessionNativeInterface.GetARSessionNativeInterface ();
		UnityARSessionNativeInterface.ARFrameUpdatedEvent += ARFrameUpdated;
		StartARKit ();
		LibPlacenote.Instance.RegisterListener (this);
	}

	private void StartARKit ()
	{
		mLabelText.text = "Initializing ARKit";
		Application.targetFrameRate = 60;
		ARKitWorldTrackingSessionConfiguration config = new ARKitWorldTrackingSessionConfiguration ();
		config.planeDetection = UnityARPlaneDetection.Horizontal;
		config.alignment = UnityARAlignment.UnityARAlignmentGravity;
		config.getPointCloudData = true;
		config.enableLightEstimation = true;
		mSession.RunWithConfig (config);
	}

	private void ARFrameUpdated (UnityARCamera camera)
	{
		mFrameUpdated = true;
		mARCamera = camera;
	}

	private void InitARFrameBuffer ()
	{
		mImage = new UnityARImageFrameData ();

		int yBufSize = mARCamera.videoParams.yWidth * mARCamera.videoParams.yHeight;
		mImage.y.data = Marshal.AllocHGlobal (yBufSize);
		mImage.y.width = (ulong)mARCamera.videoParams.yWidth;
		mImage.y.height = (ulong)mARCamera.videoParams.yHeight;
		mImage.y.stride = (ulong)mARCamera.videoParams.yWidth;

		// This does assume the YUV_NV21 format
		int vuBufSize = mARCamera.videoParams.yWidth * mARCamera.videoParams.yWidth/2;
		mImage.vu.data = Marshal.AllocHGlobal (vuBufSize);
		mImage.vu.width = (ulong)mARCamera.videoParams.yWidth/2;
		mImage.vu.height = (ulong)mARCamera.videoParams.yHeight/2;
		mImage.vu.stride = (ulong)mARCamera.videoParams.yWidth;

		mSession.SetCapturePixelData (true, mImage.y.data, mImage.vu.data);
	}
		
	public void LoadMap() {

		if (!mARKitInit) {
			return;
		}

		if (mMapID != null) {
			mLabelText.text = "Loading Map " + mMapID;
			LibPlacenote.Instance.LoadMap (mMapID,
				(completed, faulted, percentage) => {
					if (completed) {
						LibPlacenote.Instance.StartSession ();
						mLabelText.text = "Loaded ID: " + mMapID;

					} else if (faulted) {
						mLabelText.text = "Failed to load ID: " + mMapID;
					} else {
						mLabelText.text = "Downloading " + mMapID + "(" + (percentage * 100).ToString ("F2") + ")";
					}
				}
			);
		}
	}
	
	// Update is called once per frame
	void Update () {
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
				mLabelText.text = "ARKit is ready!";
			}

			if (!LibPlacenote.Instance.Initialized ()) {
				//LibPlacenote is not ready
				return;
			} else if (!mPlacenoteInit) {
				mPlacenoteInit = true;
				mLabelText.text = "Placenote is ready!";
				LoadMap ();

			}

			Matrix4x4 matrix = mSession.GetCameraPose ();

			Vector3 arkitPosition = PNUtility.MatrixOps.GetPosition (matrix);
			Quaternion arkitQuat = PNUtility.MatrixOps.GetRotation (matrix);
			if (mARKitInit && mPlacenoteInit) {
				LibPlacenote.Instance.SendARFrame (mImage, arkitPosition, arkitQuat, mARCamera.videoParams.screenOrientation);
			}
		}
	}


	public void OnPose (Matrix4x4 outputPose, Matrix4x4 arkitPose) {}


	public void OnStatusChange (LibPlacenote.MappingStatus prevStatus, LibPlacenote.MappingStatus currStatus)
	{
		Debug.Log ("prevStatus: " + prevStatus.ToString () + " currStatus: " + currStatus.ToString ());
		if (currStatus == LibPlacenote.MappingStatus.RUNNING && prevStatus == LibPlacenote.MappingStatus.LOST) {
			mLabelText.text = "Localized";
			mModelParent.SetActive (true);
		} else if (currStatus == LibPlacenote.MappingStatus.RUNNING && prevStatus == LibPlacenote.MappingStatus.WAITING) {
			mLabelText.text = "Mapping";
		} else if (currStatus == LibPlacenote.MappingStatus.LOST) {
			mLabelText.text = "Searching for position lock";
		} else if (currStatus == LibPlacenote.MappingStatus.WAITING) {
			mLabelText.text = "Waiting!";
		}
	}






}
