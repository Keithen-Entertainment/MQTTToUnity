using M2MqttUnity;
using System;
using System.Collections.Generic;
using UnityEngine;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;

public class MQTTToUnityController : M2MqttUnityClient
{
    [Header("MQTT Configuration")]
    [Tooltip("Map of MQTT keys (prefixes) to their specific topics")]
    public List<KeyTopicsPair> MQTTKeyTopics = new List<KeyTopicsPair>();
    public bool DebuggingEnabled = false;
    // Event for received MQTT messages (all topics)
    public event Action<string, string> OnMqttMessageReceived;

    // Per-topic events (full topic as key)
    private Dictionary<string, Action<string>> topicEvents = new Dictionary<string, Action<string>>();

    [Serializable]
    public class KeyTopicsPair
    {
        public string Key;
        public string[] Topics;
    }

    // Subscribe to a specific topic event (full topic, e.g., "key/topic")
    public void SubscribeToTopicEvent(string fullTopic, Action<string> handler)
    {
        if (!topicEvents.ContainsKey(fullTopic))
        {
            topicEvents[fullTopic] = null;
        }
        topicEvents[fullTopic] += handler;
    }

    public void UnsubscribeFromTopicEvent(string fullTopic, Action<string> handler)
    {
        if (topicEvents.ContainsKey(fullTopic))
        {
            topicEvents[fullTopic] -= handler;
        }
    }

    protected override void Start()
    {
        client = new MqttClient(brokerAddress, brokerPort, isEncrypted, null, null, isEncrypted ? MqttSslProtocols.SSLv3 : MqttSslProtocols.None);
        base.Start();
    }

    protected override void Update()
    {
        base.Update();
    }

    protected override void SubscribeTopics()
    {
        var fullTopics = new List<string>();
        var qosLevels = new List<byte>();

        foreach (var pair in MQTTKeyTopics)
        {
            if (pair.Key == null || pair.Topics == null) continue;
            foreach (var topic in pair.Topics)
            {
                string fullTopic = $"{pair.Key}/{topic}";
                fullTopics.Add(fullTopic);
                qosLevels.Add(MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE);
            }
        }

        if (fullTopics.Count == 0)
        {
            if (DebuggingEnabled)
                Debug.LogWarning("[MQTT] No keys or topics to subscribe to.");
            return;
        }

        client.Subscribe(fullTopics.ToArray(), qosLevels.ToArray());
        Debug.Log($"[MQTT] Subscribed to topics: {string.Join(", ", fullTopics)}");
    }

    protected override void UnsubscribeTopics()
    {
        var fullTopics = new List<string>();

        foreach (var pair in MQTTKeyTopics)
        {
            if (pair.Key == null || pair.Topics == null) continue;
            foreach (var topic in pair.Topics)
            {
                string fullTopic = $"{pair.Key}/{topic}";
                fullTopics.Add(fullTopic);
            }
        }

        if (fullTopics.Count == 0)
        {
            if (DebuggingEnabled)
                Debug.LogWarning("[MQTT] No keys or topics to unsubscribe from.");
            return;
        }

        client.Unsubscribe(fullTopics.ToArray());
        if (DebuggingEnabled)
            Debug.Log($"[MQTT] Unsubscribed from topics: {string.Join(", ", fullTopics)}");
    }

    protected override void OnConnected()
    {
        if (DebuggingEnabled)
            Debug.Log("[MQTT] Connected to MQTT broker.");
        this.SubscribeTopics();
    }
    protected override void OnDisconnected()
    {
        if (DebuggingEnabled)
            Debug.Log("[MQTT] Disconnected from MQTT broker.");
    }
    protected override void OnConnectionFailed(string errorMessage)
    {
        if (DebuggingEnabled)
            Debug.LogError($"[MQTT] Connection failed: {errorMessage}");
    }
    protected override void DecodeMessage(string topic, byte[] message)
    {
        string messageString = System.Text.Encoding.UTF8.GetString(message);
        if (DebuggingEnabled)
            Debug.Log($"[MQTT] Received message on topic '{topic}': {messageString}");

        // Fire the global event
        OnMqttMessageReceived?.Invoke(topic, messageString);

        // Fire the per-topic event if it exists
        if (topicEvents.TryGetValue(topic, out var handler) && handler != null)
        {
            handler.Invoke(messageString);
        }
    }

    public void PublishMessage(string topic, string message, byte qosLevel = MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, bool retain = false)
    {
        StartCoroutine(PublishWithRetry(topic, message, qosLevel, retain));
    }

    private System.Collections.IEnumerator PublishWithRetry(string topic, string message, byte qosLevel, bool retain, int maxRetries = 5, float retryDelay = 1f)
    {
        int attempt = 0;
        while (attempt < maxRetries)
        {
            if (client != null && client.IsConnected)
            {
                ushort msgId = client.Publish(
                    topic,
                    System.Text.Encoding.UTF8.GetBytes(message),
                    qosLevel,
                    retain
                );
                if (DebuggingEnabled)
                    Debug.Log($"[MQTT] Published message to topic '{topic}': {message} (msgId: {msgId})");
                yield break;
            }
            else
            {
                if (DebuggingEnabled)
                    Debug.LogWarning($"[MQTT] Publish attempt {attempt + 1} failed: client not connected. Retrying in {retryDelay} seconds...");
                yield return new WaitForSeconds(retryDelay);
                attempt++;
            }
        }
        if (DebuggingEnabled)
            Debug.LogError($"[MQTT] Failed to publish message to topic '{topic}' after {maxRetries} attempts: client not connected.");
    }
}
