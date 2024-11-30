using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Text;
using UnityEngine.Networking;

public class DroneController : MonoBehaviour
{
    public Vector3 landingStationPosition = new Vector3(0.0f, 0.6f, 0.0f);
    public float cameraDetectionTime = 0f;
    public bool falseAlarm = false;
    public float detectionStartTime = 0f;
    public static DroneController Instance { get; private set; }
    [SerializeField] private Camera droneCamera;
    private float takeOffHeight = 10.0f;
    private float takeOffSpeed = 2.0f;
    private float moveSpeed = 2.0f;
    private float windStrength = 1.0f;
    private float batteryCapacity = 100.0f;
    private float batteryConsumptionRate = 1.0f;
    private float lowBatteryThreshold = 10.0f;
    private string serverUrl = "http://localhost:5001/detect";
    private int captureWidth = 224;
    private int captureHeight = 224;
    private bool isTakeOffMode = false;
    private bool isPatrolMode = false;
    private bool isReturnMode = false;
    private bool isLandingMode = false;
    private bool isThiefMode = false;
    private bool hasLandedMode = false;
    private float currentBatteryLevel;
    private Texture2D texture2D;
    private RenderTexture renderTexture;
    private List<Vector3> patrolPoints = new List<Vector3>();
    private int currentPatrolIndex = 0;
    private Vector3 targetPosition;
    private Vector3 windEffect = Vector3.zero;
    private bool thiefDetected = false;
    private float batteryUsedPerCycle = 0f;
    private float patrolCycleTime = 0f;
    private int completedPatrolCycles = 0;
    private float patrolStartTime = 0f;
    private string finalReport = "";
    private List<float> detectionConfirmationTimes = new List<float>();
    private float droneTotalLatency = 0f;
    private int droneLatencySamples = 0;
    private int droneSuccessfulDetections = 0;
    private int droneImagesSent = 0;
    private bool hasConfirmedThief = false;
    private bool cnetInProgress = false;
    private bool cnetAccepted = false;
    private float cnetBid = 0f;
    private float lastLoggedBatteryLevel;
    private float totalDistanceTraveled = 0f;
    private Vector3 lastPosition;
    private List<float> timeData = new List<float>();
    private List<float> batteryData = new List<float>();
    private List<float> distanceData = new List<float>();
    private float simulationStartTime;


    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        currentBatteryLevel = batteryCapacity;
        lastLoggedBatteryLevel = currentBatteryLevel;
        lastPosition = transform.position;
        simulationStartTime = Time.time;
        renderTexture = new RenderTexture(captureWidth, captureHeight, 24);
        texture2D = new Texture2D(captureWidth, captureHeight, TextureFormat.RGB24, false);
        patrolPoints.Add(new Vector3(5.0f, takeOffHeight, 5.0f));
        patrolPoints.Add(new Vector3(5.0f, takeOffHeight, -5.0f));
        patrolPoints.Add(new Vector3(-5.0f, takeOffHeight, -5.0f));
        patrolPoints.Add(new Vector3(-5.0f, takeOffHeight, 5.0f));
        StartCoroutine(SendLogMessage($"Initialized (Battery: {currentBatteryLevel}%)"));
        StartCoroutine(SendAgentInfoToSimulationServer());
        StartCoroutine(TakeOffMode());
    }

    void Update()
    {
        Battery();
        float distanceThisFrame = Vector3.Distance(lastPosition, transform.position);
        totalDistanceTraveled += distanceThisFrame;
        lastPosition = transform.position;
        float elapsedTime = Time.time - simulationStartTime;
        timeData.Add(elapsedTime);
        batteryData.Add(currentBatteryLevel);
        distanceData.Add(totalDistanceTraveled);
    }

    IEnumerator TakeOffMode()
    {
        isPatrolMode = false;
        isReturnMode = false;
        isLandingMode = false;
        isThiefMode = false;
        hasLandedMode = false;
        isTakeOffMode = true;
        targetPosition = new Vector3(transform.position.x, takeOffHeight, transform.position.z);
        StartCoroutine(SendLogMessage($"Taking off"));
        while (Vector3.Distance(transform.position, targetPosition) > 0.1f)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, takeOffSpeed * Time.deltaTime);
            yield return null;
        }
        StartCoroutine(SendLogMessage($"Patrolling"));
        isTakeOffMode = false;
        StartCoroutine(PatrolMode());
    }

    IEnumerator PatrolMode()
    {
        isTakeOffMode = false;
        isReturnMode = false;
        isLandingMode = false;
        isThiefMode = false;
        hasLandedMode = false;
        isPatrolMode = true;
        if (currentPatrolIndex == 0 && patrolStartTime == 0)
        {
            patrolStartTime = Time.time;
        }
        currentPatrolIndex = 0;
        targetPosition = patrolPoints[currentPatrolIndex];
        float photoTimer = 0f;
        float windTimer = 0f;
        while (isPatrolMode)
        {
            Vector3 desiredDirection = (targetPosition - transform.position).normalized;
            Vector3 movement = desiredDirection * moveSpeed * Time.deltaTime + windEffect * Time.deltaTime;
            transform.position += movement;
            if (desiredDirection != Vector3.zero)
            {
                Quaternion toRotation = Quaternion.LookRotation(desiredDirection);
                transform.rotation = Quaternion.Lerp(transform.rotation, toRotation, Time.deltaTime * 2.0f);
            }
            photoTimer += Time.deltaTime;
            windTimer += Time.deltaTime;
            if (photoTimer >= 5f)
            {
                photoTimer = 0f;
                StartCoroutine(Photo());
            }
            if (windTimer >= 3f)
            {
                windTimer = 0f;
                Wind();
            }
            if (!isPatrolMode) yield break;
            if (Vector3.Distance(transform.position, targetPosition) < 0.5f)
            {
                if (currentPatrolIndex == 0 && patrolStartTime > 0)
                {
                    float cycleTime = Time.time - patrolStartTime;
                    patrolCycleTime += cycleTime;
                    batteryUsedPerCycle += (batteryCapacity - currentBatteryLevel);
                    completedPatrolCycles++;
                    patrolStartTime = Time.time;
                }
                currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Count;
                targetPosition = patrolPoints[currentPatrolIndex];
                StartCoroutine(SendLogMessage($"Changing patrol point"));
            }
            yield return null;
        }
        if (isThiefMode)
        {
            StartCoroutine(ThiefMode());
        }
        else if (isReturnMode)
        {
            StartCoroutine(ReturnMode());
        }
    }

    IEnumerator ThiefMode()
    {
        isTakeOffMode = false;
        isPatrolMode = false;
        isReturnMode = false;
        isLandingMode = false;
        hasLandedMode = false;
        isThiefMode = true;
        Vector3 thiefPosition = Find();
        if (thiefPosition == Vector3.zero)
        {
            isThiefMode = false;
            StartCoroutine(ReturnMode());
            yield break;
        }
        if (targetPosition != thiefPosition)
        {
            targetPosition = thiefPosition;
            StartCoroutine(SendLogMessage($"Thief located, moving"));
        }
        if (!cnetInProgress)
        {
            cnetInProgress = true;
            StartCoroutine(StartContractNetProtocol());
        }
        while (isThiefMode)
        {
            if (!cnetAccepted)
            {
                yield return null;
                continue;
            }
            Vector3 desiredDirection = (targetPosition - transform.position).normalized;
            Vector3 movement = desiredDirection * moveSpeed * Time.deltaTime;
            transform.position += movement;
            if (desiredDirection != Vector3.zero)
            {
                Quaternion toRotation = Quaternion.LookRotation(desiredDirection);
                transform.rotation = Quaternion.Lerp(transform.rotation, toRotation, Time.deltaTime * 2.0f);
            }
            if (Vector3.Distance(transform.position, targetPosition) < 0.5f)
            {
                yield return StartCoroutine(Photo());
                float confirmationTime = Time.time - cameraDetectionTime;
                detectionConfirmationTimes.Clear();
                detectionConfirmationTimes.Add(confirmationTime);
                isThiefMode = false;
                StartCoroutine(ReturnMode());
                yield break;
            }
            yield return null;
        }
    }

    public IEnumerator ReturnMode()
    {
        if (isReturnMode)
        {
            yield break;
        }
        isTakeOffMode = false;
        isPatrolMode = false;
        isLandingMode = false;
        isThiefMode = false;
        hasLandedMode = false;
        isReturnMode = true;
        targetPosition = new Vector3(landingStationPosition.x, takeOffHeight, landingStationPosition.z);
        StartCoroutine(SendLogMessage($"Returning to base"));
        while (isReturnMode)
        {
            Vector3 desiredDirection = (targetPosition - transform.position).normalized;
            Vector3 movement = desiredDirection * moveSpeed * Time.deltaTime;
            transform.position += movement;
            if (desiredDirection != Vector3.zero)
            {
                Quaternion toRotation = Quaternion.LookRotation(desiredDirection);
                transform.rotation = Quaternion.Lerp(transform.rotation, toRotation, Time.deltaTime * 2.0f);
            }
            if (Vector3.Distance(transform.position, targetPosition) < 0.5f)
            {
                isReturnMode = false;
                StartCoroutine(SendLogMessage($"Descending"));
                StartCoroutine(LandingMode());
                yield break;
            }
            yield return null;
        }
    }

    IEnumerator LandingMode()
    {
        if (isLandingMode)
        {
            yield break;
        }
        isTakeOffMode = false;
        isPatrolMode = false;
        isReturnMode = false;
        isThiefMode = false;
        hasLandedMode = false;
        isLandingMode = true;
        targetPosition = landingStationPosition;
        StartCoroutine(SendLogMessage($"Landing"));
        while (isLandingMode)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, takeOffSpeed * Time.deltaTime);
            if (Vector3.Distance(transform.position, targetPosition) < 0.05f)
            {
                isLandingMode = false;
                StartCoroutine(SendLogMessage($"Landing completed"));
                StartCoroutine(HasLandedMode());
                yield break;
            }
            yield return null;
        }
    }

    IEnumerator HasLandedMode()
    {
        if (hasLandedMode)
        {
            yield break;
        }
        isTakeOffMode = false;
        isPatrolMode = false;
        isReturnMode = false;
        isThiefMode = false;
        isLandingMode = false;
        hasLandedMode = true;
        if (completedPatrolCycles > 0)
        {
            float averagePatrolTime = patrolCycleTime / completedPatrolCycles;
            float averageBatteryConsumption = batteryUsedPerCycle / completedPatrolCycles;
            finalReport += $"Average patrol time per cycle: {averagePatrolTime:F2} seconds.\n";
            finalReport += $"Average battery consumption per cycle: {averageBatteryConsumption:F2}%.\n";
        }
        finalReport += $"Total distance traveled by the drone: {totalDistanceTraveled:F2} meters.\n";
        if (currentBatteryLevel > lowBatteryThreshold && thiefDetected && !falseAlarm)
        {
            finalReport += "Successful simulation: Thief detected and sufficient battery to return to station.\n";
        }
        else
        {
            finalReport += "Failed simulation: Success conditions not met.\n";
        }
        float totalLatency = 0f;
        int totalLatencySamples = 0;
        int totalSuccessfulDetections = 0;
        int totalImagesSent = 0;
        foreach (CameraController camera in FindObjectsByType<CameraController>(FindObjectsSortMode.None))
        {
            var (cameraLatency, cameraSamples, cameraDetections, cameraImagesSent) = camera.GetMetricsData();
            totalLatency += cameraLatency;
            totalLatencySamples += cameraSamples;
            totalSuccessfulDetections += cameraDetections;
            totalImagesSent += cameraImagesSent;
        }
        var (droneLatency, droneSamples, droneDetections, droneImagesSent) = GetDroneMetrics();
        totalLatency += droneLatency;
        totalLatencySamples += droneSamples;
        totalSuccessfulDetections += droneDetections;
        totalImagesSent += droneImagesSent;
        float averageLatency = totalLatencySamples > 0 ? totalLatency / totalLatencySamples : 0f;
        float detectionRate = totalImagesSent > 0 ? (totalSuccessfulDetections / (float)totalImagesSent) * 100 : 0f;
        finalReport += $"Average server latency: {averageLatency:F2} seconds.\n";
        finalReport += $"Successful detection rate: {detectionRate:F2}%.\n";
        if (detectionConfirmationTimes.Count > 0)
        {
            float confirmationTime = detectionConfirmationTimes[0];
            finalReport += $"Detection-confirmation time: {confirmationTime:F2} seconds.\n";
        }
        StartCoroutine(SendFinalReport());
        StartCoroutine(SendLogMessage("Final Report:\n" + finalReport));
        yield return new WaitForSeconds(5f);
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    void Battery()
    {
        if (currentBatteryLevel > 0)
        {
            currentBatteryLevel -= batteryConsumptionRate * Time.deltaTime;
            currentBatteryLevel = Mathf.Max(currentBatteryLevel, 0f);
            float percentageDecrement = batteryCapacity * 0.25f;
            if (currentBatteryLevel <= lastLoggedBatteryLevel - percentageDecrement)
            {
                lastLoggedBatteryLevel -= percentageDecrement;
                StartCoroutine(SendLogMessage($"Battery: {currentBatteryLevel:F2}%"));
            if (currentBatteryLevel <= lowBatteryThreshold && !isLandingMode && !hasLandedMode)
            {
                StartCoroutine(SendLogMessage($"Low battery, returning"));
                StartCoroutine(ReturnMode());
            }
            }
        }
    }

    public void ReceiveAlert(Vector3 alertPosition)
    {
        isPatrolMode = false;
        isThiefMode = true;
        targetPosition = alertPosition;
        StartCoroutine(SendLogMessage($"Alert received"));
        StartCoroutine(ThiefMode());
    }

    IEnumerator Photo()
    {
        if (hasConfirmedThief && !isThiefMode)
        {
            yield break;
        }
        droneCamera.targetTexture = renderTexture;
        RenderTexture.active = renderTexture;
        droneCamera.Render();
        texture2D.ReadPixels(new Rect(0, 0, captureWidth, captureHeight), 0, 0);
        texture2D.Apply();
        droneCamera.targetTexture = null;
        RenderTexture.active = null;
        byte[] imageBytes = texture2D.EncodeToJPG();
        string imageBase64 = Convert.ToBase64String(imageBytes);
        DetectionRequest requestData = new DetectionRequest
        {
            camera_id = "Drone1",
            image_base64 = imageBase64
        };
        string jsonData = JsonUtility.ToJson(requestData);
        droneImagesSent++;
        float startTime = Time.time;
        using (UnityWebRequest www = new UnityWebRequest(serverUrl, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");
            yield return www.SendWebRequest();
            float latency = Time.time - startTime;
            droneTotalLatency += latency;
            droneLatencySamples++;
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
                            droneSuccessfulDetections++;
                            if (!thiefDetected)
                            {
                                thiefDetected = true;
                                StartCoroutine(SendLogMessage($"Thief detected"));
                                if (isThiefMode)
                                {
                                    Notification();
                                    hasConfirmedThief = true;
                                }
                            }
                        }
                    }
                }
                if (!localThiefDetected)
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

    void Notification()
    {
        GameObject guard = GameObject.Find("Security1");
        if (guard != null)
        {
            StartCoroutine(SendLogMessage("Notifying guard"));
            guard.GetComponent<SecurityGuardController>()?.TakeControlOfDrone(this);
        }
        else
        {
            StartCoroutine(SendLogMessage("Guard not found", "ERROR"));
        }
    }

    Vector3 Find()
    {
        GameObject robber = GameObject.Find("Robber1");
        if (robber != null)
        {
            Vector3 robberPosition = robber.transform.position;
            robberPosition.y += 3;
            robberPosition.z += 3;
            return robberPosition;
        }
        else
        {
            StartCoroutine(SendLogMessage("Thief not found", "ERROR"));
            return Vector3.zero;
        }
    }

    void Wind()
    {
        if (isPatrolMode && !isReturnMode && !isLandingMode && !isThiefMode && !hasLandedMode && !isTakeOffMode)
        {
            float windX = UnityEngine.Random.Range(-windStrength, windStrength);
            float windZ = UnityEngine.Random.Range(-windStrength, windStrength);
            windEffect = new Vector3(windX, 0.0f, windZ);
            StartCoroutine(SendLogMessage($"Wind applied"));
        }
    }

    IEnumerator SendAgentInfoToSimulationServer()
    {
        string simulationServerUrl = "http://localhost:5002/agent_action";
        AgentActionData data = new AgentActionData
        {
            agent_id = "Drone1",
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

    IEnumerator StartContractNetProtocol()
    {
        yield return StartCoroutine(SendKQMLMessage("call_for_proposal", "Search and confirm thief"));
        StartCoroutine(SendLogMessage($"CFP sent to drone"));
        yield return new WaitForSeconds(1.0f);
        cnetBid = UnityEngine.Random.Range(5.0f, 10.0f);
        yield return StartCoroutine(SendKQMLMessage("propose", $"Cost={cnetBid}"));
        StartCoroutine(SendLogMessage($"Proposal sent with bid: {cnetBid}"));
        yield return new WaitForSeconds(1.0f);
        cnetAccepted = true;
        yield return StartCoroutine(SendKQMLMessage("accept_proposal", "Proposal accepted"));
        StartCoroutine(SendLogMessage("Proposal accepted, searching for thief"));
        yield return new WaitForSeconds(2.0f);
        StartCoroutine(SendLogMessage("Task completed"));
    }

    private IEnumerator SendLogMessage(string message, string logLevel = "INFO")
    {
        string simulationServerUrl = "http://localhost:5002/log_message";
        LogMessageData data = new LogMessageData
        {
            agent_id = "Drone1",
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
            sender = "Drone1",
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

    private IEnumerator SendFinalReport()
    {
        string url = "http://localhost:5002/final_report";
        FinalReportData reportData = new FinalReportData
        {
            time = timeData.ToArray(),
            battery = batteryData.ToArray(),
            distance = distanceData.ToArray()
        };
        string jsonData = JsonUtility.ToJson(reportData);
        using (UnityWebRequest www = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");
            yield return www.SendWebRequest();
        }
    }

    public void SetThiefMode(bool state)
    {
        isThiefMode = state;
    }

    private (float TotalLatency, int LatencySamples, int SuccessfulDetections, int TotalImagesSent) GetDroneMetrics()
    {
        return (droneTotalLatency, droneLatencySamples, droneSuccessfulDetections, droneImagesSent);
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

    [Serializable]
    public class FinalReportData
    {
        public float[] time;
        public float[] battery;
        public float[] distance;
    }
}