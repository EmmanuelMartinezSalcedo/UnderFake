using System.Collections;
using Mediapipe.Tasks.Vision.HandLandmarker;
using UnityEngine;

namespace Mediapipe.Unity.Sample.HandLandmarkDetection
{
    public class HandLandmarkerController : VisionTaskApiRunner<HandLandmarker>
    {
        [SerializeField] private MultiplayerPlayerController leftHandController;
        [SerializeField] private MultiplayerPlayerController rightHandController;

        private Experimental.TextureFramePool _textureFramePool;
        public readonly HandLandmarkDetectionConfig config = new HandLandmarkDetectionConfig
        {
            NumHands = 2
        };

        private Vector2? _pendingLeftHand;
        private Vector2? _pendingRightHand;

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

                taskApi.DetectAsync(image, GetCurrentTimestampMillisec(), imageProcessingOptions);
            }
        }

        private void OnHandLandmarkDetectionOutput(HandLandmarkerResult result, Image image, long timestamp)
        {
            _pendingLeftHand = null;
            _pendingRightHand = null;

            for (int i = 0; i < result.handLandmarks.Count; i++)
            {
                var landmarks = result.handLandmarks[i];
                var wrist = landmarks.landmarks[0]; // wrist (landmark 0)

                float x = wrist.x;
                float y = wrist.y;

                x = 0.5f + (x - 0.5f) * flipX;
                y = 0.5f + (y - 0.5f) * flipY;

                int pixelX = (int)(x * textureWidth);
                int pixelY = (int)(y * textureHeight);

                // Asegúrate de que handedness esté presente
                if (i < result.handedness.Count && result.handedness[i].categories.Count > 0)
                {
                    var handType = result.handedness[i].categories[0].categoryName;

                    if (handType == "Left")
                        _pendingLeftHand = new Vector2(pixelX, pixelY);
                    else if (handType == "Right")
                        _pendingRightHand = new Vector2(pixelX, pixelY);
                }
            }
        }
    }
}
