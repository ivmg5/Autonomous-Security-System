using UnityEngine;
using System.Collections;
using System.Text;
using System;
using UnityEngine.Networking;

public class SecurityGuardController : MonoBehaviour
{
    public void TakeControlOfDrone(DroneController drone)
    {
        StartCoroutine(SendLogMessage("Guard took control of the drone"));
        AnalyzeSituation(drone);
    }

    void AnalyzeSituation(DroneController drone)
    {
        StartCoroutine(SendLogMessage("Analyzing with drone camera"));
        StartCoroutine(StartVotingProcess(drone));
    }

    IEnumerator StartVotingProcess(DroneController drone)
    {
        yield return StartCoroutine(SendKQMLMessage("call_for_vote", "Is it a threat?"));
        yield return new WaitForSeconds(1.0f);
        int votesInFavor = UnityEngine.Random.Range(0, 5);
        int votesAgainst = 5 - votesInFavor;
        string vote = votesInFavor > votesAgainst ? "yes" : "no";
        StartCoroutine(SendLogMessage($"Vote sent to drone: {vote}"));
        if (votesInFavor > votesAgainst)
        {
            StartCoroutine(SendLogMessage("Alarm activated"));
            drone.falseAlarm = false;
        }
        else
        {
            StartCoroutine(SendLogMessage("False alarm"));
            drone.falseAlarm = true;
        }
        StartCoroutine(SendLogMessage("Analysis completed"));
        drone.SetThiefMode(false);
        drone.StartCoroutine(drone.ReturnMode());
    }

    private IEnumerator SendLogMessage(string message, string logLevel = "INFO")
    {
        string simulationServerUrl = "http://localhost:5002/log_message";
        LogMessageData data = new LogMessageData
        {
            agent_id = "Security1",
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
            sender = "Security1",
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

    [Serializable]
    public class LogMessageData
    {
        public string agent_id;
        public string message;
        public string log_level;
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