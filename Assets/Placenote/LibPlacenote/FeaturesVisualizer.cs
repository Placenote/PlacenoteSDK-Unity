using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;


/// <summary>
/// Class that constructs a pointcloud mesh from the map retrieved from a LibPlacenote mapping/localization session
/// </summary>
public class FeaturesVisualizer : MonoBehaviour, PlacenoteListener
{
    private static FeaturesVisualizer sInstance;
    private List<GameObject> mPtCloudObjs = new List<GameObject>();
    private Dictionary<LibPlacenote.PNMeshBlockIndex, GameObject> mMeshBlocks =
        new Dictionary<LibPlacenote.PNMeshBlockIndex, GameObject>();
    private Dictionary<LibPlacenote.PNMeshBlockIndex, bool> mMeshBlockStatus =
        new Dictionary<LibPlacenote.PNMeshBlockIndex, bool>();
    private bool mEnabled = false;

    [SerializeField] Material mPtCloudMat;
    [SerializeField] Material mMeshMat;
    [SerializeField] GameObject mMap;
    [SerializeField] GameObject mPointCloud;
    [SerializeField] bool mEnableMapPoints = false;
    [SerializeField] bool mEnableMesh = true;

    void Awake()
    {
        sInstance = this;
    }

    void Start()
    {
        // This is required for OnPose and OnStatusChange to be triggered
        LibPlacenote.Instance.RegisterListener(this);
    }


    /// <summary>
    /// Enable rendering of pointclouds collected from LibPlacenote for every half second
    /// </summary>
    /// <remarks>
    /// NOTE: to avoid the static instance being null, please call this in Start() function in your MonoBehaviour
    /// </remarks>
    public static void EnablePointcloud()
    {
        if (sInstance.mMap == null)
        {
            Debug.LogWarning(
                "Map game object reference is null, please initialize in editor.Skipping pointcloud visualization"
            );
            return;
        }

        if (sInstance.mEnableMapPoints)
        {
            sInstance.InvokeRepeating("DrawMap", 0f, 0.1f);
        }
        if (LibPlacenote.Instance.Initialized() && sInstance.mEnableMesh)
        {
            LibPlacenote.Instance.EnableDenseMapping();
        }
        sInstance.mEnabled = true;
    }

    /// <summary>
    /// Disable rendering of pointclouds collected from LibPlacenote
    /// </summary>
    public static void DisablePointcloud()
    {
        sInstance.CancelInvoke();
        ClearPointcloud();
        LibPlacenote.Instance.DisableDenseMapping();
        sInstance.mEnabled = false;
    }


    /// <summary>
    ///  Clear currently rendering feature/landmark pointcloud
    /// </summary>
    public static void ClearPointcloud()
    {
        Debug.Log("Cleared pointcloud and mesh");
        MeshFilter mf = sInstance.mMap.GetComponent<MeshFilter>();
        mf.mesh.Clear();

        foreach (var ptCloud in sInstance.mPtCloudObjs)
        {
            Destroy(ptCloud);
        }

        foreach (var block in sInstance.mMeshBlocks)
        {
            Destroy(block.Value);
        }
        sInstance.mMeshBlockStatus.Clear();
        sInstance.mPtCloudObjs.Clear();
    }


    public void OnInitialized(bool success, string errMsg)
    {
        if (!success)
        {
            return;
        }

        if (sInstance.mEnabled && sInstance.mEnableMesh)
        {
            LibPlacenote.Instance.EnableDenseMapping();
        }
    }

    public void OnPose(Matrix4x4 outputPose, Matrix4x4 arkitPose)
    {
    }

    public void OnStatusChange(LibPlacenote.MappingStatus prevStatus, LibPlacenote.MappingStatus currStatus)
    {
        if (currStatus == LibPlacenote.MappingStatus.WAITING)
        {
            Debug.Log("Session stopped, resetting pointcloud mesh.");
            ClearPointcloud();
        }
    }

    public void OnDenseMeshBlocks(Dictionary<LibPlacenote.PNMeshBlockIndex, LibPlacenote.PNMeshBlock> meshBlocks)
    {
        if (!LibPlacenote.Instance.Initialized())
        {
            return;
        }

        if (meshBlocks == null)
        {
            Debug.Log("Empty meshBlocks.");
            return;
        }

        foreach (KeyValuePair<LibPlacenote.PNMeshBlockIndex, LibPlacenote.PNMeshBlock> entry in meshBlocks)
        {
            // Create GameObject container with mesh components for the loaded mesh.
            GameObject meshObj = null;
            if (mMeshBlocks.ContainsKey(entry.Key))
            {
                meshObj = mMeshBlocks[entry.Key];
            }
            else
            {
                meshObj = Instantiate(mPointCloud);
                mMeshBlocks.Add(entry.Key, meshObj);
                mMeshBlockStatus.Add(entry.Key, true);
            }

            MeshFilter mf = meshObj.GetComponent<MeshFilter>();
            if (mf == null)
            {
                mf = meshObj.AddComponent<MeshFilter>();
                mf.mesh = new Mesh();
            }
            mf.mesh.Clear();
            mf.mesh.vertices = entry.Value.points;
            mf.mesh.colors = entry.Value.colors;
            mf.mesh.SetIndices(entry.Value.indices, MeshTopology.Triangles, 0);
            mf.mesh.RecalculateNormals();

            LibPlacenote.PNMeshBlockIndex block = entry.Key;
            MeshRenderer mr = meshObj.GetComponent<MeshRenderer>();
            if (mr == null)
            {
                mr = meshObj.AddComponent<MeshRenderer>();
            }
            mr.material = mMeshMat;
        }
    }


    public void DrawMap()
    {
        if (LibPlacenote.Instance.GetStatus() != LibPlacenote.MappingStatus.RUNNING)
        {
            return;
        }

        LibPlacenote.PNFeaturePointUnity[] map = LibPlacenote.Instance.GetMap();

        if (map == null)
        {
            return;
        }

        Vector3[] points = new Vector3[map.Length];
        Color[] colors = new Color[map.Length];
        for (int i = 0; i < map.Length; ++i)
        {

            points[i].x = map[i].point.x;
            points[i].y = map[i].point.y;
            points[i].z = -map[i].point.z;
            colors[i].r = 1 - map[i].measCount / 10f;
            colors[i].b = 0;
            colors[i].g = map[i].measCount / 10f;

            if (map[i].measCount < 4)
            {
                colors[i].a = 0;
            }
            else
            {
                colors[i].a = 0.2f + 0.8f * (map[i].measCount / 10f);
            }
        }

        // Need to update indicies too!
        int[] indices = new int[map.Length];
        for (int i = 0; i < map.Length; ++i)
        {
            indices[i] = i;
        }

        // Create GameObject container with mesh components for the loaded mesh.
        MeshFilter mf = mMap.GetComponent<MeshFilter>();
        if (mf == null)
        {
            mf = mMap.AddComponent<MeshFilter>();
            mf.mesh = new Mesh();
        }

        mf.mesh.Clear();
        mf.mesh.vertices = points;
        mf.mesh.colors = colors;
        mf.mesh.SetIndices(indices, MeshTopology.Points, 0);

        MeshRenderer mr = mMap.GetComponent<MeshRenderer>();
        if (mr == null)
        {
            mr = mMap.AddComponent<MeshRenderer>();
        }
        mr.material = mPtCloudMat;
    }
}
