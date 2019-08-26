using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
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
    [SerializeField] RawImage mImage;
    [SerializeField] ARCameraBackground mARBackground;

    public static LocalizationThumbnailSelector Instance
    {
        get
        {
            return sInstance;
        }
    }

    void Awake()
    {
        sInstance = this;
    }

    void Start()
    {
        // This is required for OnPose and OnStatusChange to be triggered
        LibPlacenote.Instance.RegisterListener(this);
        mBestRenderTexture = new RenderTexture(Screen.width, Screen.height, 16, RenderTextureFormat.ARGB32);
        mBestRenderTexture.Create();
        mThumbnailTexture = new Texture2D(Screen.width, Screen.height, TextureFormat.ARGB32, false);
        if (mImage != null)
        {
            mImage.texture = mBestRenderTexture;
        }
    }

    public void OnPose(Matrix4x4 outputPose, Matrix4x4 arkitPose)
    {
        LibPlacenote.PNFeaturePointUnity[] trackedLandmarks = LibPlacenote.Instance.GetTrackedFeatures();
        if (trackedLandmarks == null)
        {
            return;
        }

        int lmSize = 0;
        for (int i = 0; i < trackedLandmarks.Length; i++)
        {
            if (trackedLandmarks[i].measCount > 4)
            {
                lmSize++;
            }
        }

        if (lmSize > mMaxLmSize)
        {
            mMaxLmSize = lmSize;
            if (mImage != null && Screen.width != (int)mImage.rectTransform.rect.width)
            {
                Debug.Log(String.Format("Screen {0} {1} uvRect {2} {3}", Screen.width, Screen.height,
                    mImage.rectTransform.rect.width, mImage.rectTransform.rect.height));
                mImage.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Screen.width);
                mImage.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Screen.height);
                mImage.rectTransform.ForceUpdateRectTransforms();
            }
            Graphics.Blit(null, mBestRenderTexture, mARBackground.material);
            Graphics.CopyTexture(mBestRenderTexture, 0, 0, 0, 0,
                mBestRenderTexture.width, mBestRenderTexture.height, mThumbnailTexture, 0, 0, 0, 0);
        }
    }

    public void OnStatusChange(LibPlacenote.MappingStatus prevStatus, LibPlacenote.MappingStatus currStatus)
    {
        if (prevStatus != LibPlacenote.MappingStatus.WAITING && currStatus == LibPlacenote.MappingStatus.WAITING)
        {
            mMaxLmSize = -1;
        }
    }

    public Texture2D GetThumbnailCandidate()
    {
        if (mMaxLmSize < 0)
        {
            return null;
        }

        return mThumbnailTexture;
    }

    public void SyncThumbnail(string mapId)
    {
        string thumbnailPath = Path.Combine(Application.persistentDataPath, "thumbnail.png");
        byte[] imgBuffer = mThumbnailTexture.EncodeToPNG();
        System.IO.File.WriteAllBytes(thumbnailPath, imgBuffer);

        // Save Render Texture into a jpg
        LibPlacenote.Instance.SyncLocalizationThumbnail(mapId, thumbnailPath,
            (completed, faulted, progress) =>
            {
                if (completed && !faulted)
                {
                    Debug.Log("Uploaded localization thumbnail");
                    File.Delete(thumbnailPath);
                }
            }
        );
    }
}
