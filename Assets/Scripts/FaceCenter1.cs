using System;
using System.Collections;
using Alteruna.Trinity;
using Mediapipe.Tasks.Vision.FaceLandmarker;
using UnityEngine;

namespace Mediapipe.Unity.Sample.FaceLandmarkDetection
{
    public class SinglePlayerNoseTipDetector : VisionTaskApiRunner<FaceLandmarker>
    {
        [SerializeField] private PlayerController playerController;
        [SerializeField] private Transform cameraDisplayTransform;

        private Experimental.TextureFramePool _textureFramePool;
        public readonly FaceLandmarkDetectionConfig config = new FaceLandmarkDetectionConfig();

        private Vector2? _pendingScreenNoseTip;

        private int textureWidth;
        private int textureHeight;

        private float displayX, displayY, displayWidth, displayHeight;
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

            CalculateDisplayRect();

            var transformationOptions = imageSource.GetTransformationOptions();

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
            if (cameraDisplayTransform != null)
            {
                var spriteRenderer = cameraDisplayTransform.GetComponent<SpriteRenderer>();
                if (spriteRenderer != null && spriteRenderer.sprite != null)
                {
                    var bounds = spriteRenderer.bounds;

                    Vector3 bottomLeft = Camera.main.WorldToScreenPoint(new Vector3(bounds.min.x, bounds.min.y, bounds.center.z));
                    Vector3 topRight = Camera.main.WorldToScreenPoint(new Vector3(bounds.max.x, bounds.max.y, bounds.center.z));

                    displayX = bottomLeft.x;
                    displayY = bottomLeft.y;
                    displayWidth = topRight.x - bottomLeft.x;
                    displayHeight = topRight.y - bottomLeft.y;
                }
                else
                {
                    Debug.LogWarning("SpriteRenderer not found on cameraDisplayTransform, using full screen");
                    SetFullScreenDisplay();
                }
            }
            else
            {
                SetFullScreenDisplay();
            }
        }

        private void SetFullScreenDisplay()
        {
            displayX = 0;
            displayY = 0;
            displayWidth = UnityEngine.Screen.width;
            displayHeight = UnityEngine.Screen.height;
        }

        private Vector2 MapNormalizedToScreen(float normalizedX, float normalizedY)
        {
            if (isMirrorMode)
            {
                normalizedX = 1f - normalizedX;
            }

            float screenX = displayX + (normalizedX * displayWidth);
            float screenY = displayY + ((1f - normalizedY) * displayHeight);

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

                    float normalizedX = noseTip.x;
                    float normalizedY = noseTip.y;
                    Vector2 screenPos = MapNormalizedToScreen(normalizedX, normalizedY);

                    _pendingScreenNoseTip = screenPos;
                }
            }
        }
    }
}
