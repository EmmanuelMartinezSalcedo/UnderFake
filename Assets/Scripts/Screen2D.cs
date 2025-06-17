using Mediapipe.Unity;
using UnityEngine;

public class Screen2D : MonoBehaviour
{
    [SerializeField] private SpriteRenderer _renderer;
    private ImageSource _imageSource;
    private Texture2D _texture2D;
    private WebCamTexture _webcamTex;

    public void Initialize(ImageSource imageSource)
    {
        _imageSource = imageSource;

        var sourceTexture = imageSource.GetCurrentTexture();
        if (sourceTexture == null)
        {
            Debug.LogError("ImageSource returned null texture");
            return;
        }

        if (sourceTexture is WebCamTexture webcamTex)
        {
            _webcamTex = webcamTex;
            _texture2D = new Texture2D(webcamTex.width, webcamTex.height, TextureFormat.RGBA32, false);
            ApplyTextureToSprite(_texture2D);
        }
        else if (sourceTexture is RenderTexture renderTexture)
        {
            RenderTexture.active = renderTexture;
            _texture2D = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGBA32, false);
            _texture2D.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
            _texture2D.Apply();
            RenderTexture.active = null;
            ApplyTextureToSprite(_texture2D);
        }
        else if (sourceTexture is Texture2D tex2D)
        {
            _texture2D = tex2D;
            ApplyTextureToSprite(_texture2D);
        }
        else
        {
            Debug.LogError("Unsupported texture type: " + sourceTexture.GetType());
        }
    }

    private void ApplyTextureToSprite(Texture2D texture)
    {
        var sprite = Sprite.Create(
            texture,
            new Rect(0, 0, texture.width, texture.height),
            new Vector2(0.5f, 0.5f),
            100f
        );
        _renderer.sprite = sprite;
        Vector3 scale = transform.localScale;
        scale.x = -2f;
        transform.localScale = scale;

    }

    void Update()
    {
        if (_webcamTex != null && _webcamTex.didUpdateThisFrame)
        {
            _texture2D.SetPixels32(_webcamTex.GetPixels32());
            _texture2D.Apply();
        }
    }
}
