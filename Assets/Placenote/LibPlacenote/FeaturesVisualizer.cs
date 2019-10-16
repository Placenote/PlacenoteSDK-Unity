using System;
using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(ParticleSystem))]

/// <summary>
/// Class that constructs a pointcloud mesh from the map retrieved from a LibPlacenote mapping/localization session
/// </summary>
public class FeaturesVisualizer : MonoBehaviour, PlacenoteListener
{
    private static FeaturesVisualizer sInstance;

    [SerializeField] GameObject mPointCloud; // the particle system
    ParticleSystem m_ParticleSystem;
    ParticleSystem.Particle[] m_Particles;
    int m_NumParticles;

    private Gradient gradient;
    private GradientColorKey[] colorKey;
    private GradientAlphaKey[] alphaKey;

    void Awake()
    {
        sInstance = this;

    }

    void Start()
    {
        // This is required for OnPose and OnStatusChange to be triggered
        LibPlacenote.Instance.RegisterListener(this);
        m_ParticleSystem = mPointCloud.GetComponent<ParticleSystem>();

    }

    void Update()
    {
    }

    /// <summary>
    /// Enable rendering of pointclouds collected from LibPlacenote for every half second
    /// </summary>
    /// <remarks>
    /// NOTE: to avoid the static instance being null, please call this in Start() function in your MonoBehaviour
    /// </remarks>
    public static void EnablePointcloud(Color? weak = null, Color? strong = null)
    {
        // Set colors of point cloud

        sInstance.gradient = new Gradient();

        // Populate the color keys at the relative time 0 and 1 (0 and 100%)
        sInstance.colorKey = new GradientColorKey[2];
        sInstance.colorKey[0].color = weak ?? Color.red;
        sInstance.colorKey[0].time = 0.0f;
        sInstance.colorKey[1].color = strong ?? Color.green;
        sInstance.colorKey[1].time = 1.0f;

        // Populate the alpha  keys at relative time 0 and 1  (0 and 100%)
        sInstance.alphaKey = new GradientAlphaKey[2];

        if (weak != null)
        {
            sInstance.alphaKey[0].alpha = weak.Value.a;
        }
        else
        {
            sInstance.alphaKey[0].alpha = 0.2f;
        }

        if (strong != null)
        {
            sInstance.alphaKey[1].alpha = strong.Value.a;
        }
        else
        {
            sInstance.alphaKey[1].alpha = 1.0f;
        }

        sInstance.alphaKey[0].time = 0.0f;
        sInstance.alphaKey[1].time = 1.0f;

        sInstance.gradient.SetKeys(sInstance.colorKey, sInstance.alphaKey);

        sInstance.mPointCloud.SetActive(true);
        SetVisible(true);
        sInstance.InvokeRepeating("DrawPointCloud", 0f, 0.1f);

    }



    /// <summary>
    /// Disable rendering of pointclouds collected from LibPlacenote
    /// </summary>
    public static void DisablePointcloud()
    {
        sInstance.CancelInvoke();

        ClearPointcloud();
        SetVisible(false);

    }


    /// <summary>
    ///  Clear currently rendering feature/landmark pointcloud
    /// </summary>
    public static void ClearPointcloud()
    {

        sInstance.m_ParticleSystem.Clear();

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

    /// <summary>
    /// Sets the point cloud texture.
    /// </summary>
    /// <param name="ptTexture">Point texture.</param>
    public static void SetPointCloudTexture(Texture2D ptTexture)
    {
        if (ptTexture == null)
            return;

        sInstance.mPointCloud.GetComponent<ParticleSystem>().GetComponent<ParticleSystemRenderer>().sharedMaterial.mainTexture = ptTexture;
    }

    /// <summary>
    /// Draws the point cloud.
    /// </summary>
    public void DrawPointCloud()
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

            colors[i] = sInstance.gradient.Evaluate(map[i].measCount / 10f);

            // hide points with very low measure count (number of observations)
            colors[i].a = 0.2f + 1.6f * (map[i].measCount / 10f);
        }

        // start creating the particle system points
        int numParticles = points.Length;
        if (m_Particles == null || m_Particles.Length < numParticles)
            m_Particles = new ParticleSystem.Particle[numParticles];

        //var color = m_ParticleSystem.main.startColor.color;
        var size = m_ParticleSystem.main.startSize.constant;

        for (int i = 0; i < numParticles; ++i)
        {
            m_Particles[i].startColor = colors[i];
            m_Particles[i].startSize = size;
            m_Particles[i].position = points[i];
            m_Particles[i].remainingLifetime = 1f;
        }

        // Remove any existing particles by setting remainingLifetime
        // to a negative value.
        for (int i = numParticles; i < m_NumParticles; ++i)
        {
            m_Particles[i].remainingLifetime = -1f;
        }

        m_ParticleSystem.SetParticles(m_Particles, Math.Max(numParticles, m_NumParticles));
        m_NumParticles = numParticles;

    }

    /// <summary>
    /// Gets the point cloud.
    /// </summary>
    /// <returns>The point cloud.</returns>
    public static List<Vector3> GetPointCloud()
    {

        if (LibPlacenote.Instance.GetStatus() != LibPlacenote.MappingStatus.RUNNING)
        {
            return null;
        }

        LibPlacenote.PNFeaturePointUnity[] map = LibPlacenote.Instance.GetMap();

        if (map == null)
        {
            return null;
        }

        List<Vector3> pointCloud = new List<Vector3>();
        for (int i = 0; i < map.Length; ++i)
        {
            pointCloud.Add(new Vector3(map[i].point.x, map[i].point.y, -map[i].point.z));
        }

        return pointCloud;
    }


    static void SetVisible(bool visible)
    {
        if (sInstance.m_ParticleSystem == null)
            return;

        var pRenderer = sInstance.m_ParticleSystem.GetComponent<Renderer>();
        if (pRenderer != null)
            pRenderer.enabled = visible;
    }

    public void OnLocalized()
    {
    }
}
