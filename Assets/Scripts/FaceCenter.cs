using System.Collections;
using Mediapipe.Tasks.Components.Containers;
using Mediapipe.Tasks.Vision.FaceLandmarker;
using UnityEngine;

namespace Mediapipe.Unity.Sample.FaceLandmarkDetection
{
    public class FaceLandmarkerNoseTipDetector : VisionTaskApiRunner<FaceLandmarker>
    {
        [SerializeField] private MultiplayerPlayerController playerController;
        [SerializeField] private RectTransform cameraDisplayRect;

        private Experimental.TextureFramePool _textureFramePool;
        public readonly FaceLandmarkDetectionConfig config = new FaceLandmarkDetectionConfig();

        private Vector2? _pendingScreenNoseTip;

        private float flipX = 1f;
        private float flipY = 1f;
        private int textureWidth;
        private int textureHeight;

        // Variables para mapeo de coordenadas
        private UnityEngine.Rect displayRect;
        private bool isMirrorMode = false;

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
                var screenPos = new Vector3(_pendingScreenNoseTip.Value.x, _pendingScreenNoseTip.Value.y, 10f);
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

            // Calcular el �rea de display de la c�mara
            CalculateDisplayRect();

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

        private void CalculateDisplayRect()
        {
            if (cameraDisplayRect != null)
            {
                // Usar el RectTransform espec�fico donde se muestra la c�mara
                Vector3[] corners = new Vector3[4];
                cameraDisplayRect.GetWorldCorners(corners);

                Vector2 screenBottomLeft = RectTransformUtility.WorldToScreenPoint(Camera.main, corners[0]);
                Vector2 screenTopRight = RectTransformUtility.WorldToScreenPoint(Camera.main, corners[2]);

                displayRect = new UnityEngine.Rect(screenBottomLeft.x, screenBottomLeft.y,
                                     screenTopRight.x - screenBottomLeft.x,
                                     screenTopRight.y - screenBottomLeft.y);
                
            }
            else
            {
                // Fallback: usar toda la pantalla
                displayRect = new UnityEngine.Rect(0, 0, UnityEngine.Screen.width, UnityEngine.Screen.height);
            }
        }

        private Vector2 MapNormalizedToScreen(float normalizedX, float normalizedY)
        {
            // Si est� en modo espejo, invertir X
            if (isMirrorMode)
            {
                normalizedX = 1f - normalizedX;
            }

            // Mapear coordenadas normalizadas (0-1) al �rea de display
            float screenX = displayRect.x + (normalizedX * displayRect.width);
            float screenY = displayRect.y + ((1f - normalizedY) * displayRect.height); // Invertir Y porque MediaPipe usa origen arriba-izquierda

            return new Vector2(screenX, screenY);
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

                    // Las coordenadas de MediaPipe ya est�n normalizadas (0-1)
                    float normalizedX = noseTip.x;
                    float normalizedY = noseTip.y;

                    // Mapear a coordenadas de pantalla
                    Vector2 screenPos = MapNormalizedToScreen(normalizedX, normalizedY);

                    _pendingScreenNoseTip = screenPos;
                }
            }
        }
    }
}