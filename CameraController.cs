using UnityEngine;
using System.Collections;
using System.Text;
using System;
using UnityEngine.Networking;

public class CameraController : MonoBehaviour
{
    public Camera surveillanceCamera;
    public string cameraId;
    public string serverUrl = "http://localhost:5001/detect";
    public float captureInterval = 5.0f;
    public int captureWidth = 224;
    public int captureHeight = 224;
    public static bool thiefDetected = false;
    private Texture2D texture2D;
    private RenderTexture renderTexture;
    private int totalImagesSent = 0;
    private int successfulDetections = 0;
    private float totalLatency = 0f;
    private int latencySamples = 0;

    void Start()
    {
        renderTexture = new RenderTexture(captureWidth, captureHeight, 24);
        texture2D = new Texture2D(captureWidth, captureHeight, TextureFormat.RGB24, false);
        StartCoroutine(SendLogMessage($"Initialized"));
        StartCoroutine(SendAgentInfoToSimulationServer());
        StartCoroutine(CaptureRoutine());
    }

    IEnumerator CaptureRoutine()
    {
        yield return new WaitForSeconds(25.0f);
        while (!thiefDetected)
        {
            yield return new WaitForSeconds(captureInterval);
            yield return StartCoroutine(CaptureAndSendImage());
        }
    }

    IEnumerator CaptureAndSendImage()
    {
        if (thiefDetected)
        {
            yield break;
        }
        surveillanceCamera.targetTexture = renderTexture;
        RenderTexture.active = renderTexture;
        surveillanceCamera.Render();
        texture2D.ReadPixels(new Rect(0, 0, captureWidth, captureHeight), 0, 0);
        texture2D.Apply();
        surveillanceCamera.targetTexture = null;
        RenderTexture.active = null;
        byte[] imageBytes = texture2D.EncodeToJPG();
        string imageBase64 = Convert.ToBase64String(imageBytes);
        DetectionRequest requestData = new DetectionRequest
        {
            camera_id = cameraId,
            image_base64 = imageBase64
        };
        string jsonData = JsonUtility.ToJson(requestData);
        totalImagesSent++;
        float startTime = Time.time;
        using (UnityWebRequest www = new UnityWebRequest(serverUrl, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");
            yield return www.SendWebRequest();
            float latency = Time.time - startTime;
            totalLatency += latency;
            latencySamples++;
            if (www.result == UnityWebRequest.Result.Success)
            {
                string responseText = www.downloadHandler.text;
                DetectionResponse response = JsonUtility.FromJson<DetectionResponse>(responseText);
                bool localThiefDetected = false;
                if (response.detected_objects != null && response.detected_objects.Length > 0)
                {
                    foreach (var detectedObject in response.detected_objects)
                    {
                        string className = detectedObject.class_name ?? "Unknown";
                        if (className == "person")
                        {
                            localThiefDetected = true;
                            successfulDetections++;
                            float cameraDetectionTime = Time.time;
                            DroneController.Instance.detectionStartTime = cameraDetectionTime;
                            DroneController.Instance.cameraDetectionTime = cameraDetectionTime;
                        }
                    }
                }
                if (localThiefDetected)
                {
                    StartCoroutine(SendLogMessage($"CFP sent to drone"));
                    StartCoroutine(SendLogMessage($"Thief detected"));
                    DroneController.Instance.ReceiveAlert(transform.position);
                    thiefDetected = true;
                    StartCoroutine(SendKQMLMessage("inform", $"Thief detected"));
                }
                else
                {
                    StartCoroutine(SendLogMessage($"Thief not detected"));
                }
            }
            else
            {
                StartCoroutine(SendLogMessage($"Error sending image: {www.error}", "ERROR"));
            }
        }
    }

    IEnumerator SendAgentInfoToSimulationServer()
    {
        string simulationServerUrl = "http://localhost:5002/agent_action";
        AgentActionData data = new AgentActionData
        {
            agent_id = cameraId,
            action = "Initialized"
        };
        string jsonData = JsonUtility.ToJson(data);
        using (UnityWebRequest www = new UnityWebRequest(simulationServerUrl, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");
            yield return www.SendWebRequest();
            if (www.result == UnityWebRequest.Result.Success)
            {
                StartCoroutine(SendLogMessage($"Successful connection to server"));
            }
            else
            {
                StartCoroutine(SendLogMessage($"Server connection error: {www.error}", "ERROR"));
            }
        }
    }

    private IEnumerator SendLogMessage(string message, string logLevel = "INFO")
    {
        string simulationServerUrl = "http://localhost:5002/log_message";
        LogMessageData data = new LogMessageData
        {
            agent_id = cameraId,
            message = message,
            log_level = logLevel
        };
        string jsonData = JsonUtility.ToJson(data);
        using (UnityWebRequest www = new UnityWebRequest(simulationServerUrl, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");
            yield return www.SendWebRequest();
        }
    }

    private IEnumerator SendKQMLMessage(string performative, string content)
    {
        string simulationServerUrl = "http://localhost:5002/kqml_message";
        KQMLMessageData data = new KQMLMessageData
        {
            sender = cameraId,
            receiver = "SimulationServer",
            performative = performative,
            content = content
        };
        string jsonData = JsonUtility.ToJson(data);
        using (UnityWebRequest www = new UnityWebRequest(simulationServerUrl, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");
            yield return www.SendWebRequest();
        }
    }

    public (float TotalLatency, int LatencySamples, int SuccessfulDetections, int TotalImagesSent) GetMetricsData()
    {
        return (totalLatency, latencySamples, successfulDetections, totalImagesSent);
    }

    [Serializable]
    public class AgentActionData
    {
        public string agent_id;
        public string action;
    }

    [Serializable]
    public class LogMessageData
    {
        public string agent_id;
        public string message;
        public string log_level;
    }

    [Serializable]
    public class DetectionRequest
    {
        public string camera_id;
        public string image_base64;
    }

    [Serializable]
    public class DetectionResponse
    {
        public string camera_id;
        public DetectedObject[] detected_objects;
    }

    [Serializable]
    public class DetectedObject
    {
        public string class_name;
        public float confidence;
        public float[] coordinates;
    }

    [Serializable]
    public class KQMLMessageData
    {
        public string sender;
        public string receiver;
        public string performative;
        public string content;
    }
}