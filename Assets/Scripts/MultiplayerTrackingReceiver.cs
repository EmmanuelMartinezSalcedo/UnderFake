using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using Newtonsoft.Json;
using System.Diagnostics;
using System.IO;

public interface ITrackingData
{
    string frame_data { get; set; }
}

[System.Serializable]
public class HeadData : ITrackingData
{
    public Position head_position;
    public string frame_data { get; set; }
}

[System.Serializable]
public class HandsData : ITrackingData
{
    public HandCoordinates hand_positions;
    public string frame_data { get; set; }
}

[System.Serializable]
public class HandCoordinates
{
    public Position left;
    public Position right;
}

[System.Serializable]
public class Position
{
    public float normalized_x;
    public float normalized_y;
}


public class MultiplayerTrackingReceiver : MonoBehaviour
{
    [Header("Configuración de Red")]
    public string serverIP = "127.0.0.1";
    public int serverPort = 12345;

    [Header("Elementos de escena")]
    public SpriteRenderer backgroundSpriteRenderer;
    public MultiplayerPlayerController head;
    public MultiplayerPlayerController barrierLeft;
    public MultiplayerPlayerController barrierRight;
    public Camera mainCamera;

    [Header("Configuración")]
    public float smoothingFactor = 0.8f;

    [Header("Status")]
    public bool isConnected = false;
    public bool isReceivingData = false;

    [Header("Player")]
    public bool isPlayer2 = true;

    [Header("PlayerData")]
    public GameObject Heart;
    public GameObject BarrierLeft;
    public GameObject BarrierRight;

    private TcpClient tcpClient;
    private NetworkStream stream;
    private Thread receiveThread;

    private Texture2D webcamTexture;
    private ITrackingData latestData;
    private bool hasNewData = false;

    private Vector2 headSmoothedPositon;
    private Vector2 headTargetPosition;

    private Vector2 leftHandSmoothedPositon;
    private Vector2 leftHandTargetPosition;
    private Vector2 rightHandSmoothedPositon;
    private Vector2 rightHandTargetPosition;

    private StringBuilder messageBuffer = new StringBuilder();

    private Process pythonProcess;

    void Start()
    {
        PreparePlayerData();
        StartPythonConnection();
        webcamTexture = new Texture2D(2, 2);
        Thread conn = new Thread(ConnectToServer);
        conn.Start();
    }

    public void StartPythonConnection()
    {
        string relativePath;
        if (isPlayer2)
        {
            relativePath = "Scripts/Python/HandsTracking/HandsTracker.exe";
        }
        else
        {
            relativePath = "Scripts/Python/HeadTracking/HeadTracker.exe";
        }

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
            pythonProcess.ErrorDataReceived += (sender, args) =>
            {
                if (!string.IsNullOrEmpty(args.Data))
                    UnityEngine.Debug.LogError("[PYTHON ERROR] " + args.Data);
            };

            pythonProcess.Start();
            pythonProcess.BeginOutputReadLine();
            pythonProcess.BeginErrorReadLine();

            UnityEngine.Debug.Log("Python process started");
        }
        else
        {
            UnityEngine.Debug.LogError("HeadTracker.exe not found at: " + exePath);
        }
    }

    void ConnectToServer()
    {
        int attempt = 1;

        while (true)
        {
            try
            {
                UnityEngine.Debug.Log($"Connection attempt {attempt}");
                tcpClient = new TcpClient(serverIP, serverPort);
                UnityEngine.Debug.Log("Connected!");

                stream = tcpClient.GetStream();
                isConnected = true;

                UnityEngine.Debug.Log("Connected to Python server");

                if (WaitForMessage("PYTHON_READY", 30f))
                {
                    UnityEngine.Debug.Log("Received PYTHON_READY");

                    SendCustomMessage("UNITY_READY\n");
                    UnityEngine.Debug.Log("Sent UNITY_READY");

                    isReceivingData = true;

                    receiveThread = new Thread(ReceiveData);
                    receiveThread.Start();
                    return;
                }
                else
                {
                    UnityEngine.Debug.LogError("Failed to receive PYTHON_READY");
                }

                tcpClient.Close();
                isConnected = false;
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogWarning($"Connection attempt {attempt} failed: {e.Message}");
            }

            UnityEngine.Debug.Log("Retrying in 2 seconds...");
            Thread.Sleep(2000);
            attempt++;
        }
    }

    private bool WaitForMessage(string expectedMessage, float timeoutSeconds)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        byte[] buffer = new byte[1024];

        while (stopwatch.Elapsed.TotalSeconds < timeoutSeconds)
        {
            if (stream.DataAvailable)
            {
                try
                {
                    int bytesRead = stream.Read(buffer, 0, buffer.Length);
                    string receivedMessage = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();

                    UnityEngine.Debug.Log($"Received: '{receivedMessage}', Expected: '{expectedMessage}'");

                    if (receivedMessage.StartsWith(expectedMessage))
                    {
                        return true;
                    }
                    else
                    {
                        UnityEngine.Debug.LogWarning($"Unexpected message: {receivedMessage}");
                    }
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogError($"Error reading message: {e.Message}");
                    return false;
                }
            }
            Thread.Sleep(100);
        }

        UnityEngine.Debug.LogError($"Timeout waiting for '{expectedMessage}'");
        return false;
    }

    private void SendCustomMessage(string message)
    {
        try
        {
            string fullMessage = message + "\n";
            byte[] data = Encoding.UTF8.GetBytes(fullMessage);
            stream.Write(data, 0, data.Length);
            stream.Flush();
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogError($"Error sending message: {e.Message}");
            throw;
        }
    }

    void ReceiveData()
    {
        byte[] buffer = new byte[1024 * 1024];
        string incompleteData = "";

        UnityEngine.Debug.Log("Data reception thread started");

        while (isReceivingData && isConnected)
        {
            try
            {
                if (stream.DataAvailable)
                {
                    int bytesRead = stream.Read(buffer, 0, buffer.Length);
                    if (bytesRead > 0)
                    {
                        string newData = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        incompleteData += newData;

                        while (true)
                        {
                            int colonIndex = incompleteData.IndexOf(':');
                            if (colonIndex == -1) break;

                            string lengthStr = incompleteData.Substring(0, colonIndex);
                            if (int.TryParse(lengthStr, out int expectedLength))
                            {
                                int totalMessageLength = colonIndex + 1 + expectedLength;
                                if (incompleteData.Length >= totalMessageLength)
                                {
                                    string jsonData = incompleteData.Substring(colonIndex + 1, expectedLength);

                                    ProcessReceivedData($"{lengthStr}:{jsonData}");

                                    incompleteData = incompleteData.Substring(totalMessageLength);
                                }
                                else
                                {
                                    break;
                                }
                            }
                            else
                            {
                                UnityEngine.Debug.LogError("Invalid message format - could not parse length");

                                incompleteData = "";
                                break;
                            }
                        }
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

        UnityEngine.Debug.Log("Data reception thread ended");
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
                if (!isPlayer2)
                {
                    HeadData headData = JsonConvert.DeserializeObject<HeadData>(jsonMessage);
                    latestData = headData;
                }
                else
                {
                    HandsData handsData = JsonConvert.DeserializeObject<HandsData>(jsonMessage);
                    latestData = handsData;
                }

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

    void PreparePlayerData()
    {
        if (!isPlayer2)
        {
            BarrierLeft.GetComponent<PolygonCollider2D>().enabled = false;
            BarrierLeft.GetComponent<SpriteRenderer>().enabled = false;

            BarrierRight.GetComponent<PolygonCollider2D>().enabled = false;
            BarrierRight.GetComponent<SpriteRenderer>().enabled = false;
        }
        else
        {
            Heart.GetComponent<PolygonCollider2D>().enabled = false;
            Heart.GetComponent<SpriteRenderer>().enabled = false;
        }
    }


    void Update()
    {
        if (hasNewData && latestData != null)
        {
            if (!isPlayer2 && latestData is HeadData head)
            {
                ProcessHeadData(head);
            }
            else if (latestData is HandsData hands)
            {
                ProcessHandsData(hands);
            }
            hasNewData = false;
        }

        if (!isPlayer2)
        {
            if (head != null && backgroundSpriteRenderer != null && backgroundSpriteRenderer.sprite != null)
            {
                headSmoothedPositon = headTargetPosition;

                Vector3 spriteScale = backgroundSpriteRenderer.transform.localScale;
                float spriteWidth = backgroundSpriteRenderer.sprite.bounds.size.x * spriteScale.x;
                float spriteHeight = backgroundSpriteRenderer.sprite.bounds.size.y * spriteScale.y;

                float clampedX = Mathf.Clamp(headSmoothedPositon.x, 0f, 1f);
                float clampedY = Mathf.Clamp(1f - headSmoothedPositon.y, 0f, 1f);

                float worldX = backgroundSpriteRenderer.transform.position.x - spriteWidth / 2f + clampedX * spriteWidth;
                float worldY = backgroundSpriteRenderer.transform.position.y - spriteHeight / 2f + clampedY * spriteHeight;

                Vector3 worldPos = new Vector3(worldX, worldY, 0f);

                if (head == null) UnityEngine.Debug.LogError("Head controller is NULL");
                head.SetTargetPosition(worldPos);
            }
        }
        else
        {
            if (barrierLeft != null && backgroundSpriteRenderer != null && backgroundSpriteRenderer.sprite != null)
            {
                leftHandSmoothedPositon = leftHandTargetPosition;

                Vector3 spriteScale = backgroundSpriteRenderer.transform.localScale;
                float spriteWidth = backgroundSpriteRenderer.sprite.bounds.size.x * spriteScale.x;
                float spriteHeight = backgroundSpriteRenderer.sprite.bounds.size.y * spriteScale.y;

                float clampedX = Mathf.Clamp(leftHandSmoothedPositon.x, 0f, 1f);
                float clampedY = Mathf.Clamp(1f - leftHandSmoothedPositon.y, 0f, 1f);

                float worldX = backgroundSpriteRenderer.transform.position.x - spriteWidth / 2f + clampedX * spriteWidth;
                float worldY = backgroundSpriteRenderer.transform.position.y - spriteHeight / 2f + clampedY * spriteHeight;

                Vector3 worldPos = new Vector3(worldX, worldY, 0f);

                if (barrierLeft == null) UnityEngine.Debug.LogError("barrierLeft controller is NULL");
                barrierLeft.SetTargetPosition(worldPos);
            }
            if (barrierRight != null && backgroundSpriteRenderer != null && backgroundSpriteRenderer.sprite != null)
            {
                rightHandSmoothedPositon = rightHandTargetPosition;

                Vector3 spriteScale = backgroundSpriteRenderer.transform.localScale;
                float spriteWidth = backgroundSpriteRenderer.sprite.bounds.size.x * spriteScale.x;
                float spriteHeight = backgroundSpriteRenderer.sprite.bounds.size.y * spriteScale.y;

                float clampedX = Mathf.Clamp(rightHandSmoothedPositon.x, 0f, 1f);
                float clampedY = Mathf.Clamp(1f - rightHandSmoothedPositon.y, 0f, 1f);

                float worldX = backgroundSpriteRenderer.transform.position.x - spriteWidth / 2f + clampedX * spriteWidth;
                float worldY = backgroundSpriteRenderer.transform.position.y - spriteHeight / 2f + clampedY * spriteHeight;

                Vector3 worldPos = new Vector3(worldX, worldY, 0f);

                if (barrierRight == null) UnityEngine.Debug.LogError("barrierRight controller is NULL");
                barrierRight.SetTargetPosition(worldPos);
            }
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
            headTargetPosition = new Vector2(
                data.head_position.normalized_x,
                data.head_position.normalized_y
            );
        }
    }

    void ProcessHandsData(HandsData data)
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

        if (data.hand_positions?.left != null)
        {
            leftHandTargetPosition = new Vector2(
                data.hand_positions.left.normalized_x,
                data.hand_positions.left.normalized_y
            );
        }

        if (data.hand_positions?.right != null)
        {
            rightHandTargetPosition = new Vector2(
                data.hand_positions.right.normalized_x,
                data.hand_positions.right.normalized_y
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
        isReceivingData = false;
        isConnected = false;

        if (receiveThread != null && receiveThread.IsAlive)
        {
            receiveThread.Join(1000);
            if (receiveThread.IsAlive)
                receiveThread.Abort();
        }

        stream?.Close();
        tcpClient?.Close();

        if (pythonProcess != null && !pythonProcess.HasExited)
        {
            pythonProcess.Kill();
            pythonProcess.Dispose();
        }

        if (webcamTexture != null)
        {
            DestroyImmediate(webcamTexture);
        }
    }
}