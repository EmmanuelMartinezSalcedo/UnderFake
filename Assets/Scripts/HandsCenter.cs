using System.Collections;
using Mediapipe.Tasks.Vision.HandLandmarker;
using UnityEngine;

namespace Mediapipe.Unity.Sample.HandLandmarkDetection
{
    public class HandLandmarkerController : VisionTaskApiRunner<HandLandmarker>
    {
        [SerializeField] private MultiplayerPlayerController leftHandController;
        [SerializeField] private MultiplayerPlayerController rightHandController;
        [SerializeField] private Transform cameraDisplayTransform;

        private Experimental.TextureFramePool _textureFramePool;
        public readonly HandLandmarkDetectionConfig config = new HandLandmarkDetectionConfig
        {
            NumHands = 2
        };

        private Vector2? _pendingLeftHand;
        private Vector2? _pendingRightHand;

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
            if (_pendingLeftHand.HasValue)
            {
                var screenPos = new Vector3(_pendingLeftHand.Value.x, _pendingLeftHand.Value.y, 10f);
                var worldPos = Camera.main.ScreenToWorldPoint(screenPos);
                worldPos.z = 0;
                leftHandController?.SetTargetPosition(worldPos);
                _pendingLeftHand = null;
            }

            if (_pendingRightHand.HasValue)
            {
                var screenPos = new Vector3(_pendingRightHand.Value.x, _pendingRightHand.Value.y, 10f);
                var worldPos = Camera.main.ScreenToWorldPoint(screenPos);
                worldPos.z = 0;
                rightHandController?.SetTargetPosition(worldPos);
                _pendingRightHand = null;
            }
        }

        protected override IEnumerator Run()
        {
            yield return AssetLoader.PrepareAssetAsync(config.ModelPath);

            var options = config.GetHandLandmarkerOptions(
                config.RunningMode == Tasks.Vision.Core.RunningMode.LIVE_STREAM ? OnHandLandmarkDetectionOutput : null
            );
            taskApi = HandLandmarker.CreateFromOptions(options, GpuManager.GpuResources);

            var imageSource = ImageSourceProvider.ImageSource;
            yield return imageSource.Play();

            if (!imageSource.isPrepared)
            {
                Debug.LogError("Failed to start ImageSource");
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

                taskApi.DetectAsync(image, GetCurrentTimestampMillisec(), imageProcessingOptions);
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

        private void OnHandLandmarkDetectionOutput(HandLandmarkerResult result, Image image, long timestamp)
        {
            _pendingLeftHand = null;
            _pendingRightHand = null;

            for (int i = 0; i < result.handLandmarks.Count; i++)
            {
                var landmarks = result.handLandmarks[i];
                var wrist = landmarks.landmarks[0];

                float normalizedX = wrist.x;
                float normalizedY = wrist.y;

                Vector2 screenPos = MapNormalizedToScreen(normalizedX, normalizedY);

                if (i < result.handedness.Count && result.handedness[i].categories.Count > 0)
                {
                    var handType = result.handedness[i].categories[0].categoryName;

                    if (handType == "Left")
                    {
                        _pendingLeftHand = screenPos;
                    }
                    else if (handType == "Right")
                    {
                        _pendingRightHand = screenPos;
                    }
                }
            }
        }
    }
}