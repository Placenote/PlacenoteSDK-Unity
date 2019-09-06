using System;
using System.Collections.Generic;
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
    private bool mThumbnailSelected = false;

    [SerializeField] int mThumbnailScale = 6;
    [SerializeField] RawImage mImage;
    [SerializeField] Text mLabelText;
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
        mBestRenderTexture = new RenderTexture(Screen.width / mThumbnailScale,
            Screen.height / mThumbnailScale, 16, RenderTextureFormat.ARGB32);
        mBestRenderTexture.Create();
        mThumbnailTexture = new Texture2D(Screen.width / mThumbnailScale,
            Screen.height / mThumbnailScale, TextureFormat.ARGB32, false);
        if (mImage != null)
        {
            mImage.texture = mBestRenderTexture;
        }
    }

    public void OnPose(Matrix4x4 outputPose, Matrix4x4 arkitPose)
    {
        // if it's been manually selected, don't keep on auto-selecting
        if (mThumbnailSelected)
        {
            return;
        }

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
            if (trackedLandmarks[i].measCount > 4)
            {
                lmSize++;
            }
        }

        if (lmSize > mMaxLmSize)
        {
            mMaxLmSize = lmSize;
            Debug.Log("Updated thumbnail with " + mMaxLmSize + " tracked landmarks");
            SetCurrentImageAsThumbnail();
        }
    }

    private void SetCurrentImageAsThumbnail()
    {
        if (mImage != null && Screen.width / mThumbnailScale != (int)mImage.rectTransform.rect.width)
        {
            mImage.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,
                Screen.width / mThumbnailScale);
            mImage.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,
                Screen.height / mThumbnailScale);
            mImage.rectTransform.ForceUpdateRectTransforms();
        }
        Graphics.Blit(null, mBestRenderTexture, mARBackground.material);
        RenderTexture.active = mBestRenderTexture;
        mThumbnailTexture.ReadPixels(new Rect(0, 0, mBestRenderTexture.width, mBestRenderTexture.height), 0, 0);
        mThumbnailTexture.Apply();
    }

    public void OnSelectThumbnailClick()
    {
        if (LibPlacenote.Instance.GetMode() != LibPlacenote.MappingMode.MAPPING)
        {
            // Prompt that it's not in mapping mode
            return;
        }

        LibPlacenote.PNFeaturePointUnity[] trackedLandmarks = LibPlacenote.Instance.GetTrackedFeatures();
        if (trackedLandmarks == null)
        {
            // Prompt that there's not enough features
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

        if (lmSize < 20)
        {
            // Prompt that there's not enough features
            mLabelText.text = "Can't select thumbnail with " + lmSize + " < 20 landmarks";
            return;
        }

        mThumbnailSelected = true;
        SetCurrentImageAsThumbnail();
        mLabelText.text = "Selected new thumbnail!";
    }

    public void OnStatusChange(LibPlacenote.MappingStatus prevStatus, LibPlacenote.MappingStatus currStatus)
    {
        if (prevStatus != LibPlacenote.MappingStatus.WAITING && currStatus == LibPlacenote.MappingStatus.WAITING)
        {
            mMaxLmSize = -1;
            mThumbnailSelected = false;
            mImage.gameObject.SetActive(false);
        }
        else if (prevStatus == LibPlacenote.MappingStatus.WAITING)
        {
            mImage.gameObject.SetActive(true);
        }
    }

    public void DownloadThumbnail(string mapId)
    {
        string thumbnailPath = Path.Combine(Application.persistentDataPath, mapId + ".png");

        // Save Render Texture into a jpg
        LibPlacenote.Instance.SyncLocalizationThumbnail(mapId, thumbnailPath,
            (completed, faulted, progress) =>
            {
                if (!completed || faulted)
                {
                    return;
                }

                Debug.Log("Downloaded localization thumbnail");
                RectTransform rectTransform = mImage.rectTransform;
                byte[] fileData = File.ReadAllBytes(thumbnailPath);
                mThumbnailTexture = new Texture2D(2, 2);
                mThumbnailTexture.LoadImage(fileData);

                if (mThumbnailTexture.width != (int)rectTransform.rect.width)
                {
                    rectTransform.SetSizeWithCurrentAnchors(
                        RectTransform.Axis.Horizontal, mThumbnailTexture.width);
                    rectTransform.SetSizeWithCurrentAnchors(
                        RectTransform.Axis.Vertical, mThumbnailTexture.height);
                    rectTransform.ForceUpdateRectTransforms();
                }
                mImage.texture = mThumbnailTexture;
            }
        );
    }

    public void UploadThumbnail(string mapId)
    {
        string thumbnailPath = Path.Combine(Application.persistentDataPath, mapId + ".png");
        byte[] imgBuffer = mThumbnailTexture.EncodeToPNG();
        System.IO.File.WriteAllBytes(thumbnailPath, imgBuffer);

        // Save Render Texture into a jpg
        LibPlacenote.Instance.SyncLocalizationThumbnail(mapId, thumbnailPath,
            (completed, faulted, progress) =>
            {
                if (!completed || faulted)
                {
                    return;
                }

                Debug.Log("Uploaded localization thumbnail");
                File.Delete(thumbnailPath);
            }
        );
    }

    public void OnInitialized(bool success, string errMsg)
    {
    }

    public void OnDenseMeshBlocks(Dictionary<LibPlacenote.PNMeshBlockIndex, LibPlacenote.PNMeshBlock> meshBlocks)
    {
    }
}
