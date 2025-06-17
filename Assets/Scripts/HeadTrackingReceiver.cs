using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using Newtonsoft.Json;
using System.Diagnostics;
using System.IO;



public class HeadTrackingReceiver : MonoBehaviour
{
    [Header("Configuración de Red")]
    public string serverIP = "127.0.0.1";
    public int serverPort = 12345;

    [Header("Elementos de escena")]
    public SpriteRenderer backgroundSpriteRenderer;
    public PlayerController head;
    public Camera mainCamera;

    [Header("Configuración")]
    public float smoothingFactor = 0.8f;

    private TcpClient tcpClient;
    private NetworkStream stream;
    private Thread receiveThread;
    private bool isConnected = false;
    private bool shouldStop = false;

    private Texture2D webcamTexture;
    private HeadData latestHeadData;
    private bool hasNewData = false;
    private Vector2 smoothedPosition;
    private Vector2 targetPosition;

    private StringBuilder messageBuffer = new StringBuilder();

    private Process pythonProcess;

    void Start()
    {
        Thread.Sleep(2000);
        StartPythonConnection();
        Thread.Sleep(2000);
        webcamTexture = new Texture2D(2, 2);
        ConnectToServer();
    }

    void StartPythonConnection()
    {
        string relativePath = "Scripts/Python/HeadTracking/HeadTracker.exe";
        string exePath = Path.Combine(Application.dataPath, relativePath);

        if (File.Exists(exePath))
        {
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = exePath;
            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;
            startInfo.CreateNoWindow = true;

            string currentPath = System.Environment.GetEnvironmentVariable("PATH") ?? "";
            string exeDir = Path.GetDirectoryName(exePath);
            startInfo.EnvironmentVariables["PATH"] = exeDir + ";" + currentPath;

            pythonProcess = new Process();
            pythonProcess.StartInfo = startInfo;

            pythonProcess.OutputDataReceived += (sender, args) =>
            {
                if (!string.IsNullOrEmpty(args.Data))
                    UnityEngine.Debug.Log("[PYTHON] " + args.Data);
            };

            UnityEngine.Debug.Log("Ignore the next Debug");
            pythonProcess.ErrorDataReceived += (sender, args) =>
            {
                if (!string.IsNullOrEmpty(args.Data))
                    UnityEngine.Debug.Log("[PYTHON] " + args.Data);
            };

            pythonProcess.Start();
            pythonProcess.BeginOutputReadLine();
            pythonProcess.BeginErrorReadLine();
        }
        else
        {
            UnityEngine.Debug.LogError("HeadTracker.exe not found at: " + exePath);
        }
    }


    void ConnectToServer()
    {
        try
        {
            tcpClient = new TcpClient(serverIP, serverPort);
            stream = tcpClient.GetStream();
            isConnected = true;

            receiveThread = new Thread(ReceiveData);
            receiveThread.Start();
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogError($"Error conectando al servidor: {e.Message}");
        }
    }

    void ReceiveData()
    {
        byte[] buffer = new byte[1024 * 1024];

        while (isConnected && !shouldStop)
        {
            try
            {
                if (stream.DataAvailable)
                {
                    int bytesRead = stream.Read(buffer, 0, buffer.Length);
                    if (bytesRead > 0)
                    {
                        string receivedData = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        ProcessReceivedData(receivedData);
                    }
                }
                Thread.Sleep(1);
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError($"Error recibiendo datos: {e.Message}");
                break;
            }
        }
    }

    void ProcessReceivedData(string data)
    {
        messageBuffer.Append(data);
        string bufferContent = messageBuffer.ToString();

        while (true)
        {
            int colonIndex = bufferContent.IndexOf(':');
            if (colonIndex == -1) break;

            string lengthStr = bufferContent.Substring(0, colonIndex);
            if (!int.TryParse(lengthStr, out int messageLength)) break;

            int totalMessageLength = colonIndex + 1 + messageLength;
            if (bufferContent.Length < totalMessageLength) break;

            string jsonMessage = bufferContent.Substring(colonIndex + 1, messageLength);
            bufferContent = bufferContent.Substring(totalMessageLength);

            try
            {
                HeadData headData = JsonConvert.DeserializeObject<HeadData>(jsonMessage);
                latestHeadData = headData;
                hasNewData = true;
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError($"Error parseando JSON: {e.Message}");
            }
        }

        messageBuffer.Clear();
        messageBuffer.Append(bufferContent);
    }

    void Update()
    {
        if (hasNewData && latestHeadData != null)
        {
            ProcessHeadData(latestHeadData);
            hasNewData = false;
        }

        if (head != null && backgroundSpriteRenderer != null && backgroundSpriteRenderer.sprite != null)
        {
            smoothedPosition = targetPosition;

            Vector3 spriteScale = backgroundSpriteRenderer.transform.localScale;
            float spriteWidth = backgroundSpriteRenderer.sprite.bounds.size.x * spriteScale.x;
            float spriteHeight = backgroundSpriteRenderer.sprite.bounds.size.y * spriteScale.y;

            float clampedX = Mathf.Clamp(smoothedPosition.x, 0f, 1f);
            float clampedY = Mathf.Clamp(1f - smoothedPosition.y, 0f, 1f);

            float worldX = backgroundSpriteRenderer.transform.position.x - spriteWidth / 2f + clampedX * spriteWidth;
            float worldY = backgroundSpriteRenderer.transform.position.y - spriteHeight / 2f + clampedY * spriteHeight;

            Vector3 worldPos = new Vector3(worldX, worldY, 0f);
            head.SetTargetPosition(worldPos);
        }
    }

    void ProcessHeadData(HeadData data)
    {
        if (!string.IsNullOrEmpty(data.frame_data) && backgroundSpriteRenderer != null)
        {
            try
            {
                byte[] imageBytes = Convert.FromBase64String(data.frame_data);
                webcamTexture.LoadImage(imageBytes);

                Sprite newSprite = Sprite.Create(webcamTexture,
                    new Rect(0, 0, webcamTexture.width, webcamTexture.height),
                    new Vector2(0.5f, 0.5f));
                backgroundSpriteRenderer.sprite = newSprite;
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError($"Error cargando imagen: {e.Message}");
            }
        }

        if (data.head_position != null)
        {
            targetPosition = new Vector2(
                data.head_position.normalized_x,
                data.head_position.normalized_y
            );
        }
    }

    void OnApplicationQuit() => Disconnect();

    void OnDestroy()
    {
        Disconnect();
        if (pythonProcess != null && !pythonProcess.HasExited)
        {
            pythonProcess.Kill();
            pythonProcess.Dispose();
            pythonProcess = null;
        }
    }
    void Disconnect()
    {
        shouldStop = true;
        isConnected = false;

        if (receiveThread != null && receiveThread.IsAlive)
        {
            receiveThread.Join(1000);
            if (receiveThread.IsAlive)
                receiveThread.Abort();
        }

        stream?.Close();
        tcpClient?.Close();

        if (webcamTexture != null)
        {
            DestroyImmediate(webcamTexture);
        }
    }
}