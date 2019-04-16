﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json.Linq;
using System;
using System.Runtime.InteropServices;
using UnityEngine.XR.iOS;
using UnityEngine.Rendering;


/// <summary>
/// Class that constructs a pointcloud mesh from the map retrieved from a LibPlacenote mapping/localization session
/// </summary>
public class FeaturesVisualizer : MonoBehaviour, PlacenoteListener
{
	/// <summary>
	/// Struct that captures the status and progress of a map file transfer between client app and the Placenote Cloud
	/// </summary>
	public enum ColorMode
	{
		/// <summary>
		/// INVERSE_DEPTH configures FeatureVisualizer to color the dense
		/// pointcloud based on Jet colormapping of the inverse depth
		/// </summary>
		INVERSE_DEPTH = 0,
		/// <summary>
		/// IMAGE configures FeatureVisualizer to color the dense pointcloud with the corresponding image
		/// </summary>
		IMAGE
	}

	private static FeaturesVisualizer sInstance;
	private ColorMode mColorMode = ColorMode.IMAGE;
	private List<GameObject> mPtCloudObjs = new List<GameObject> ();
	private Dictionary<LibPlacenote.PNMeshBlockIndex, GameObject> mMeshBlocks = 
		new Dictionary<LibPlacenote.PNMeshBlockIndex, GameObject> ();
	private Dictionary<LibPlacenote.PNMeshBlockIndex, bool> mMeshBlockStatus = 
		new Dictionary<LibPlacenote.PNMeshBlockIndex, bool> ();
	private bool mEnabled = false;

	[SerializeField] Material mPtCloudMat;
	[SerializeField] Material mMeshMat;
	[SerializeField] GameObject mMap;
	[SerializeField] GameObject mPointCloud;
	[SerializeField] bool mEnableMapPoints = false;
	[SerializeField] bool mEnableMesh = true;
	[SerializeField] float mMeshVisualizationRadius = 5f;

	public Material m_ClearMaterial;
	public UnityARVideo arVideo;
	private Matrix4x4 _displayTransform;

	void Awake ()
	{
		sInstance = this;
	}

	void Start () {
		// This is required for OnPose and OnStatusChange to be triggered
		LibPlacenote.Instance.RegisterListener (this);
		UnityARSessionNativeInterface.ARFrameUpdatedEvent += UpdateFrame;
	}

	void Update ()
	{
	}

	void UpdateFrame(UnityARCamera cam)
	{
		_displayTransform = new Matrix4x4();
		_displayTransform.SetColumn(0, cam.displayTransform.column0);
		_displayTransform.SetColumn(1, cam.displayTransform.column1);
		_displayTransform.SetColumn(2, cam.displayTransform.column2);
		_displayTransform.SetColumn(3, cam.displayTransform.column3);		
	}


	void OnDestroy()
	{
		UnityARSessionNativeInterface.ARFrameUpdatedEvent -= UpdateFrame;
	}


	/// <summary>
	/// Enable rendering of pointclouds collected from LibPlacenote for every half second
	/// </summary>
	/// <remarks>
	/// NOTE: to avoid the static instance being null, please call this in Start() function in your MonoBehaviour
	/// </remarks>
	public static void EnablePointcloud ()
	{
		if (sInstance.mMap == null) {
			Debug.LogWarning (
				"Map game object reference is null, please initialize in editor.Skipping pointcloud visualization"
			);
			return;
		}

		if (sInstance.mEnableMapPoints) {
			sInstance.InvokeRepeating ("DrawMap", 0f, 0.1f);
		}
		if (LibPlacenote.Instance.Initialized() && sInstance.mEnableMesh) {
			LibPlacenote.Instance.EnableDenseMapping ();
		}
		sInstance.mEnabled = true;
	}

	/// <summary>
	/// Disable rendering of pointclouds collected from LibPlacenote
	/// </summary>
	public static void DisablePointcloud ()
	{
		sInstance.CancelInvoke ();
		ClearPointcloud ();
		LibPlacenote.Instance.DisableDenseMapping ();
		sInstance.mEnabled = false;
	}


	/// <summary>
	///  Clear currently rendering feature/landmark pointcloud
	/// </summary>
	public static void ClearPointcloud() 
	{
		Debug.Log ("Cleared pointcloud");
		MeshFilter mf = sInstance.mMap.GetComponent<MeshFilter> ();
		mf.mesh.Clear ();

		foreach (var ptCloud in sInstance.mPtCloudObjs) {
			GameObject.Destroy (ptCloud);
		}
		sInstance.mPtCloudObjs.Clear ();
	}


	public void OnInitialized (bool success, string errMsg) {
		if (!success) {
			return;
		}

		if (sInstance.mEnabled && sInstance.mEnableMesh) {
			LibPlacenote.Instance.EnableDenseMapping ();
		}
	}

	public void OnPose (Matrix4x4 outputPose, Matrix4x4 arkitPose) {
		Vector3 cameraPose = PNUtility.MatrixOps.GetPosition (outputPose);
		foreach (LibPlacenote.PNMeshBlockIndex key in mMeshBlocks.Keys) {
			Mesh mesh = mMeshBlocks [key].GetComponent<MeshFilter> ().mesh;
			if (BlockIsTooFar (key, cameraPose)) {
				mesh.Clear ();
				mMeshBlockStatus [key] = false;
			} else if (!mMeshBlockStatus [key] && BlockCloseEnoughToAdd(key, cameraPose)) {
				LibPlacenote.PNMeshBlock meshBlock = LibPlacenote.Instance.GetBlockMesh (key);
				Debug.Log ("triangle size " + meshBlock.points.Length/3);
				if (meshBlock.points == null) {
					mesh.Clear ();
				} else {
					mesh.vertices = meshBlock.points;
					mesh.colors = meshBlock.colors;
					mesh.SetIndices (meshBlock.indices, MeshTopology.Triangles, 0);
					mesh.RecalculateNormals ();
				}
				mMeshBlockStatus [key] = true;
			}
		}
	}

	public void OnStatusChange (LibPlacenote.MappingStatus prevStatus, LibPlacenote.MappingStatus currStatus) {
		if (currStatus == LibPlacenote.MappingStatus.WAITING) {
			Debug.Log ("Session stopped, resetting pointcloud mesh.");
			ClearPointcloud ();
		}
	}

	void GetColour(float v, float vmin, float vmax, ref float r, ref float g, ref float b)
	{
		// scale the gray value into the range [0, 8]
		float gray = 8*Mathf.Min(1f, Mathf.Max(0f, (v - vmin)/(vmax - vmin)));
		// s is the slope of color change
		float s = 0.5f;

		if (gray <= 1)
		{
			r = 0f;
			g = 0f;
			b = (gray+1)*s + 0.5f;
		}
		else if (gray <= 3)
		{
			r = 0f;
			g = (gray-1)*s + 0.5f;
			b = 255;
		}
		else if (gray <= 5)
		{
			r = (gray-3)*s + 0.5f;
			g = 1f;
			b = (5-gray)*s + 0.5f;
		}
		else if (gray <= 7)
		{
			r = 1f;
			g = (7-gray)*s + 0.5f;
			b = 0f;
		}
		else
		{
			r = (9-gray)*s + 0.5f;
			g = 0f;
			b = 0f;
		}
	}


	bool BlockIsTooFar(LibPlacenote.PNMeshBlockIndex blockIndex, Vector3 camPos) {
		Vector3 blockPos = new Vector3 (blockIndex.x * 0.8f, blockIndex.y * 0.8f, blockIndex.z * 0.8f);
		float dist = Vector3.Distance (camPos, blockPos);

		return dist > mMeshVisualizationRadius;
	}


	bool BlockCloseEnoughToAdd(LibPlacenote.PNMeshBlockIndex blockIndex, Vector3 camPos) {
		Vector3 blockPos = new Vector3 (blockIndex.x * 0.8f, blockIndex.y * 0.8f, blockIndex.z * 0.8f);
		float dist = Vector3.Distance (camPos, blockPos);

		return dist > (mMeshVisualizationRadius - 0.4f);
	}


	public void OnDenseMeshBlocks(Dictionary<LibPlacenote.PNMeshBlockIndex, LibPlacenote.PNMeshBlock> meshBlocks) {
		if (!LibPlacenote.Instance.Initialized()) {
			return;
		}

		if (meshBlocks == null) {
			Debug.Log ("Empty meshBlocks.");
			return;
		}

		foreach (KeyValuePair<LibPlacenote.PNMeshBlockIndex, LibPlacenote.PNMeshBlock> entry in meshBlocks) {
			// Create GameObject container with mesh components for the loaded mesh.
			GameObject meshObj = null;
			if (mMeshBlocks.ContainsKey (entry.Key)) {
				meshObj = mMeshBlocks[entry.Key];
			} else {
				meshObj = GameObject.Instantiate(mPointCloud);
				mMeshBlocks.Add (entry.Key, meshObj);
				mMeshBlockStatus.Add (entry.Key, true);
			}

			MeshFilter mf = meshObj.GetComponent<MeshFilter> ();
			if (mf == null) {
				mf = meshObj.AddComponent<MeshFilter> ();
				mf.mesh = new Mesh();
			}
			mf.mesh.Clear ();
			mf.mesh.vertices = entry.Value.points;
			mf.mesh.SetIndices (entry.Value.indices, MeshTopology.Triangles, 0);
			mf.mesh.RecalculateNormals ();

			LibPlacenote.PNMeshBlockIndex block = entry.Key;
			MeshRenderer mr = meshObj.GetComponent<MeshRenderer> ();
			if (mr == null) {
				mr = meshObj.AddComponent<MeshRenderer> ();
			}

			m_ClearMaterial.SetTexture ("_textureCbCr", arVideo._videoTextureCbCr);
			m_ClearMaterial.SetTexture ("_textureY", arVideo._videoTextureY);
			m_ClearMaterial.SetMatrix ("_DisplayTransform", _displayTransform);
			mr.material = m_ClearMaterial;
		}
	}


	public void OnDensePointcloud (LibPlacenote.PNFeaturePointUnity[] densePoints)
	{
		if (!LibPlacenote.Instance.Initialized()) {
			return;
		}

		if (densePoints == null) {
			Debug.Log ("Empty densePoints.");
			return;
		}

		Debug.Log ("Dense points size: " + densePoints.Length);
		Vector3[] points = new Vector3[densePoints.Length];
		Color[] colors = new Color[points.Length];
		for (int i = 0; i < points.Length; i++) {
			points [i].x = densePoints [i].point.x;
			points [i].y = densePoints [i].point.y;
			points [i].z = -densePoints [i].point.z;

			if (mColorMode == ColorMode.IMAGE) {
				colors [i].r = densePoints [i].color.x / 255f;
				colors [i].g = densePoints [i].color.y / 255f;
				colors [i].b = densePoints [i].color.z / 255f;
			} else {
				float distance = (float)Vector3.Distance (points[i], Camera.main.transform.position);
				float invDist = 1f/distance;
				GetColour (invDist, 0.2f, 2f, ref colors [i].r, ref colors [i].g, ref colors [i].b);
			}

			colors [i].a = 1f;
		}

		// Need to update indicies too!
		int[] indices = new int[points.Length];
		for (int i = 0; i < points.Length; ++i) {
			indices [i] = i;
		}

		// Create GameObject container with mesh components for the loaded mesh.
		GameObject pointcloudObj = GameObject.Instantiate(mPointCloud);

		MeshFilter mf = pointcloudObj.GetComponent<MeshFilter> ();
		if (mf == null) {
			mf = pointcloudObj.AddComponent<MeshFilter> ();
			mf.mesh = new Mesh ();
		} 

		mf.mesh.Clear ();
		mf.mesh.vertices = points;
		mf.mesh.colors = colors;
		mf.mesh.SetIndices (indices, MeshTopology.Points, 0);

		MeshRenderer mr = pointcloudObj.GetComponent<MeshRenderer> ();
		if (mr == null) {
			mr = pointcloudObj.AddComponent<MeshRenderer> ();
		} 

		mr.material = mPtCloudMat;

		mPtCloudObjs.Add (pointcloudObj);
	}


	public void DrawMap ()
	{
		if (LibPlacenote.Instance.GetStatus () != LibPlacenote.MappingStatus.RUNNING) {
			return;
		}

		LibPlacenote.PNFeaturePointUnity[] map = LibPlacenote.Instance.GetMap ();
		if (map == null) {
			return;
		}

		Vector3[] points = new Vector3[map.Length];
		Color[] colors = new Color[map.Length];
		for (int i = 0; i < map.Length; ++i) {

			points [i].x = map [i].point.x;
			points [i].y = map [i].point.y;
			points [i].z = -map [i].point.z;
			colors [i].r = 1 - map [i].measCount / 10f;
			colors [i].b = 0;
			colors [i].g = map [i].measCount / 10f;

			if (map [i].measCount < 4) {
				colors [i].a = 0;
			} else {
				colors [i].a = 0.2f + 0.8f * (map [i].measCount / 10f);
			}
		}

		// Need to update indicies too!
		int[] indices = new int[map.Length];
		for (int i = 0; i < map.Length; ++i) {
			indices [i] = i;
		}

		// Create GameObject container with mesh components for the loaded mesh.
		MeshFilter mf = mMap.GetComponent<MeshFilter> ();
		if (mf == null) {
			mf = mMap.AddComponent<MeshFilter> ();
			mf.mesh = new Mesh ();
		}

		mf.mesh.Clear ();
		mf.mesh.vertices = points;
		mf.mesh.colors = colors;
		mf.mesh.SetIndices (indices, MeshTopology.Points, 0);

		MeshRenderer mr = mMap.GetComponent<MeshRenderer> ();
		if (mr == null) {
			mr = mMap.AddComponent<MeshRenderer> ();
		} 

		mr.material = mPtCloudMat;
	}
}
