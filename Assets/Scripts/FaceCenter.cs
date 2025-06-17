using System.Collections;
using Mediapipe.Tasks.Vision.FaceLandmarker;
using UnityEngine;

namespace Mediapipe.Unity.Sample.FaceLandmarkDetection
{
    public class FaceLandmarkerNoseTipDetector : VisionTaskApiRunner<FaceLandmarker>
    {
        [SerializeField] private MultiplayerPlayerController playerController;

        private Experimental.TextureFramePool _textureFramePool;
        public readonly FaceLandmarkDetectionConfig config = new FaceLandmarkDetectionConfig();

        private Vector2? _pendingScreenNoseTip;

        private float flipX = 1f;
        private float flipY = 1f;
        private int textureWidth;
        private int textureHeight;

        public override void Stop()
        {
            base.Stop();
            _textureFramePool?.Dispose();
            _textureFramePool = null;
        }

        void Update()
        {
            if (_pendingScreenNoseTip.HasValue)
            {
                var screenPos = new Vector3(_pendingScreenNoseTip.Value.x, _pendingScreenNoseTip.Value.y, 10f); // z > 0 necesario
                var worldPos = Camera.main.ScreenToWorldPoint(screenPos);
                worldPos.z = 0;

                playerController?.SetTargetPosition(worldPos);
                _pendingScreenNoseTip = null;
            }
        }

        protected override IEnumerator Run()
        {
            yield return AssetLoader.PrepareAssetAsync(config.ModelPath);

            var options = config.GetFaceLandmarkerOptions(
                config.RunningMode == Tasks.Vision.Core.RunningMode.LIVE_STREAM ? OnFaceLandmarkDetectionOutput : null
            );
            taskApi = FaceLandmarker.CreateFromOptions(options, GpuManager.GpuResources);

            var imageSource = ImageSourceProvider.ImageSource;
            yield return imageSource.Play();

            if (!imageSource.isPrepared)
            {
                Debug.LogError("Failed to start ImageSource, exiting...");
                yield break;
            }

            textureWidth = imageSource.textureWidth;
            textureHeight = imageSource.textureHeight;

            _textureFramePool = new Experimental.TextureFramePool(textureWidth, textureHeight, TextureFormat.RGBA32, 10);
            screen2D.Initialize(imageSource);

            var transformationOptions = imageSource.GetTransformationOptions();
            flipX = transformationOptions.flipHorizontally ? -1f : 1f;
            flipY = transformationOptions.flipVertically ? -1f : 1f;

            var imageProcessingOptions = new Tasks.Vision.Core.ImageProcessingOptions(
                rotationDegrees: (int)transformationOptions.rotationAngle
            );

            var waitForEndOfFrame = new WaitForEndOfFrame();

            while (true)
            {
                if (isPaused)
                    yield return new WaitWhile(() => isPaused);

                if (!_textureFramePool.TryGetTextureFrame(out var textureFrame))
                {
                    yield return null;
                    continue;
                }

                yield return waitForEndOfFrame;
                textureFrame.ReadTextureOnCPU(imageSource.GetCurrentTexture(), transformationOptions.flipHorizontally, transformationOptions.flipVertically);
                var image = textureFrame.BuildCPUImage();
                textureFrame.Release();

                if (config.RunningMode == Tasks.Vision.Core.RunningMode.LIVE_STREAM)
                {
                    taskApi.DetectAsync(image, GetCurrentTimestampMillisec(), imageProcessingOptions);
                }
                else
                {
                    var result = FaceLandmarkerResult.Alloc(options.numFaces);
                    if (taskApi.TryDetect(image, imageProcessingOptions, ref result))
                    {
                        StoreNoseTip(result);
                    }
                }
            }
        }

        private void OnFaceLandmarkDetectionOutput(FaceLandmarkerResult result, Image image, long timestamp)
        {
            StoreNoseTip(result);
        }

        private void StoreNoseTip(FaceLandmarkerResult result)
        {
            if (result.faceLandmarks.Count > 0)
            {
                var landmarks = result.faceLandmarks[0];
                if (landmarks.landmarks.Count > 1)
                {
                    var noseTip = landmarks.landmarks[1];
                    float x = noseTip.x;
                    float y = noseTip.y;

                    // Aplicar inversión si es necesario
                    x = 0.5f + (x - 0.5f) * flipX;
                    y = 0.5f + (y - 0.5f) * flipY;

                    // Convertir a coordenadas de píxeles
                    int pixelX = (int)(x * textureWidth);
                    int pixelY = (int)(y * textureHeight);

                    // Desplazamiento relativo a la pantalla
                    int offsetX = (int)(UnityEngine.Screen.width * 0.05f);  // 5% del ancho
                    int offsetY = (int)(UnityEngine.Screen.height * 0.15f); // 15% del alto

                    // Aplicar desplazamiento
                    pixelX -= offsetX;
                    pixelY -= offsetY;

                    _pendingScreenNoseTip = new Vector2(pixelX, pixelY);
                }
            }
        }

    }
}
