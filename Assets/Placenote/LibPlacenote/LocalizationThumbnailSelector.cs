using System;
using UnityEngine;
using UnityEngine.XR.ARFoundation;


/// <summary>
/// Class 
/// </summary>
public class LocalizationThumbnailSelector : MonoBehaviour, PlacenoteListener
{
    private static LocalizationThumbnailSelector sInstance;
    private int mMaxLmSize = -1;
    private RenderTexture mBestRenderTexture;
    private Texture2D mThumbnailTexture;
    private int mThumbnailScale = 6;
    private Action<Texture2D> textureEvent = (texture) =>
     {
         Debug.Log("Got new thumbnail texture");
     };

    [SerializeField] ARCameraBackground mArBackground;
    public static LocalizationThumbnailSelector Instance => sInstance;

    public Action<Texture2D> TextureEvent { get => textureEvent; set => textureEvent = value; }

    void Awake()
    {
        sInstance = this;
    }


    void Update()
    {
        Graphics.Blit(null, mBestRenderTexture, mArBackground.material);
    }

    void Start()
    {
        // This is required for OnPose and OnStatusChange to be triggered
        LibPlacenote.Instance.RegisterListener(this);
        mBestRenderTexture = new RenderTexture(Screen.width / mThumbnailScale,
            Screen.height / mThumbnailScale, 16, RenderTextureFormat.ARGB32);
        mBestRenderTexture.Create();
        mThumbnailTexture = new Texture2D(Screen.width / mThumbnailScale,
            Screen.height / mThumbnailScale, TextureFormat.ARGB32, false);
    }

    public void OnPose(Matrix4x4 outputPose, Matrix4x4 arkitPose)
    {
        if (LibPlacenote.Instance.GetMode() != LibPlacenote.MappingMode.MAPPING)
        {
            return;
        }

        LibPlacenote.PNFeaturePointUnity[] trackedLandmarks = LibPlacenote.Instance.GetTrackedFeatures();
        if (trackedLandmarks == null)
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
    }

    private void SetCurrentImageAsThumbnail()
    {
        if (Screen.width / mThumbnailScale != (int)mThumbnailTexture.width)
        {
            mThumbnailTexture.Resize(Screen.width / mThumbnailScale,
                Screen.height / mThumbnailScale);

            mBestRenderTexture.Release();
            mBestRenderTexture = new RenderTexture(Screen.width / mThumbnailScale,
                Screen.height / mThumbnailScale, 16, RenderTextureFormat.ARGB32);
            mBestRenderTexture.Create();
        }

        RenderTexture.active = mBestRenderTexture;
        mThumbnailTexture.ReadPixels(new Rect(0, 0, mBestRenderTexture.width, mBestRenderTexture.height), 0, 0);
        mThumbnailTexture.Apply();
        LibPlacenote.Instance.SetLocalizationThumbnail(mThumbnailTexture);
        TextureEvent(mThumbnailTexture);
    }

    public void OnStatusChange(LibPlacenote.MappingStatus prevStatus, LibPlacenote.MappingStatus currStatus)
    {
        if (prevStatus != LibPlacenote.MappingStatus.WAITING && currStatus == LibPlacenote.MappingStatus.WAITING)
        {
            mMaxLmSize = -1;
        }
        else if (prevStatus == LibPlacenote.MappingStatus.WAITING)
        {
            if (LibPlacenote.Instance.GetMode() == LibPlacenote.MappingMode.LOCALIZING)
            {
                LibPlacenote.Instance.GetLocalizationThumbnail((thumbnailTex) => {
                    mThumbnailTexture = thumbnailTex;
                    TextureEvent(mThumbnailTexture);
                });
            }
        }
    }

    public void OnLocalized()
    {
    }
}
