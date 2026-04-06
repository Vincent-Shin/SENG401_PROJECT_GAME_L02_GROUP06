using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

/// <summary>
/// Creates a full-screen RawImage behind the intro UI and routes the
/// VideoPlayer into a RenderTexture so WebGL can display the intro video.
/// </summary>
[DisallowMultipleComponent]
public sealed class WebIntroVideoBootstrap : MonoBehaviour
{
    [SerializeField] private string rawImageName = "IntroVideoRawImage";
    [SerializeField] private Vector2Int fallbackSize = new(1920, 1080);

    private VideoPlayer videoPlayer;
    private RawImage rawImage;
    private RenderTexture renderTexture;
    private Vector2Int currentSize;

    private void Awake()
    {
        videoPlayer = GetComponent<VideoPlayer>();
        if (videoPlayer == null)
        {
            enabled = false;
            return;
        }

        EnsureRawImage();
        EnsureRenderTexture(force: true);
    }

    private void LateUpdate()
    {
        EnsureRenderTexture(force: false);
    }

    private void OnDestroy()
    {
        if (renderTexture != null)
        {
            renderTexture.Release();
            Destroy(renderTexture);
            renderTexture = null;
        }
    }

    private void EnsureRawImage()
    {
        var existing = transform.Find(rawImageName);
        if (existing != null)
        {
            rawImage = existing.GetComponent<RawImage>();
        }

        if (rawImage != null)
        {
            return;
        }

        var rawImageObject = new GameObject(rawImageName, typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
        rawImageObject.layer = gameObject.layer;
        rawImageObject.transform.SetParent(transform, false);
        rawImageObject.transform.SetAsFirstSibling();

        var rectTransform = (RectTransform)rawImageObject.transform;
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
        rectTransform.localScale = Vector3.one;

        rawImage = rawImageObject.GetComponent<RawImage>();
        rawImage.color = Color.white;
        rawImage.raycastTarget = false;
    }

    private void EnsureRenderTexture(bool force)
    {
        var width = Mathf.Max(Screen.width, fallbackSize.x);
        var height = Mathf.Max(Screen.height, fallbackSize.y);
        var newSize = new Vector2Int(width, height);

        if (!force && newSize == currentSize && renderTexture != null)
        {
            return;
        }

        currentSize = newSize;

        if (renderTexture != null)
        {
            renderTexture.Release();
            Destroy(renderTexture);
        }

        renderTexture = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32)
        {
            name = "IntroVideoRenderTexture"
        };
        renderTexture.Create();

        rawImage.texture = renderTexture;

        videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        videoPlayer.targetTexture = renderTexture;
        videoPlayer.targetCamera = null;
        videoPlayer.aspectRatio = VideoAspectRatio.FitVertically;
    }
}
