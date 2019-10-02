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
    private int mThumbnailScale = 6;

    [SerializeField] RawImage mImage;
    [SerializeField] ARCameraBackground mArBackground;

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

        gameObject.SetActive(false);
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
        if (mImage != null && Screen.width / mThumbnailScale != (int)mImage.rectTransform.rect.width)
        {
            mImage.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,
                Screen.width / mThumbnailScale);
            mImage.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,
                Screen.height / mThumbnailScale);
            mImage.rectTransform.ForceUpdateRectTransforms();

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

        if (mImage != null)
        {
            mImage.texture = mThumbnailTexture;
        }

        LibPlacenote.Instance.SetLocalizationThumbnail(mThumbnailTexture);
    }

    public void OnStatusChange(LibPlacenote.MappingStatus prevStatus, LibPlacenote.MappingStatus currStatus)
    {
        Debug.Log("prev status " + prevStatus + " curr status " + currStatus);
        if (prevStatus != LibPlacenote.MappingStatus.WAITING && currStatus == LibPlacenote.MappingStatus.WAITING)
        {
            mMaxLmSize = -1;
            mImage.gameObject.SetActive(false);
        }
        else if (prevStatus == LibPlacenote.MappingStatus.WAITING)
        {
            mImage.gameObject.SetActive(true);
            if (LibPlacenote.Instance.GetMode() == LibPlacenote.MappingMode.LOCALIZING)
            {
                LibPlacenote.Instance.GetLocalizationThumbnail((thumbnailTex) => {
                    mThumbnailTexture = thumbnailTex;
                    RectTransform rectTransform = mImage.rectTransform;

                    if (mThumbnailTexture.width != (int)rectTransform.rect.width)
                    {
                        rectTransform.SetSizeWithCurrentAnchors(
                            RectTransform.Axis.Horizontal, mThumbnailTexture.width);
                        rectTransform.SetSizeWithCurrentAnchors(
                            RectTransform.Axis.Vertical, mThumbnailTexture.height);
                        rectTransform.ForceUpdateRectTransforms();
                    }

                    if (mImage != null)
                    {
                        mImage.texture = mThumbnailTexture;
                    }
                });
            }
        }
    }

    public void OnLocalized()
    {
    }
}
