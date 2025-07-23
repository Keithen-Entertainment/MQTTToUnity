using M2MqttUnity;
using System;
using System.Collections.Generic;
using UnityEngine;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;

public class MQTTToUnityController : M2MqttUnityClient
{
    [Header("MQTT Configuration")]
    [Tooltip("The main topic(key) to listen for")]
    public string MQTTKey;
    public string[] MQTTTopics;

    // Event for received MQTT messages (all topics)
    public event Action<string, string> OnMqttMessageReceived;

    // Per-topic events
    private Dictionary<string, Action<string>> topicEvents = new Dictionary<string, Action<string>>();

    // Subscribe to a specific topic event
    public void SubscribeToTopicEvent(string topic, Action<string> handler)
    {
        string fullTopic = $"{MQTTKey}/{topic}";
        if (!topicEvents.ContainsKey(fullTopic))
        {
            topicEvents[fullTopic] = null;
        }
        topicEvents[fullTopic] += handler;
    }

    // Unsubscribe from a specific topic event
    public void UnsubscribeFromTopicEvent(string topic, Action<string> handler)
    {
        string fullTopic = $"{MQTTKey}/{topic}";
        if (topicEvents.ContainsKey(fullTopic))
        {
            topicEvents[fullTopic] -= handler;
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    protected override void Start()
    {
        client = new MqttClient(brokerAddress, brokerPort, isEncrypted, null, null, isEncrypted ? MqttSslProtocols.SSLv3 : MqttSslProtocols.None);
        base.Start();
    }

    // Update is called once per frame
    protected override void Update()
    {
        base.Update();
    }

    protected override void SubscribeTopics()
    {
        if (MQTTTopics == null || MQTTTopics.Length == 0)
        {
            Debug.LogWarning("[MQTT] No topics to subscribe to.");
            return;
        }

        // Add MQTTKey as prefix to each topic
        string[] prefixedTopics = new string[MQTTTopics.Length];
        byte[] qosLevels = new byte[MQTTTopics.Length];
        for (int i = 0; i < MQTTTopics.Length; i++)
        {
            prefixedTopics[i] = $"{MQTTKey}/{MQTTTopics[i]}";
            qosLevels[i] = MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE; // or your desired QoS
        }

        client.Subscribe(prefixedTopics, qosLevels);
        Debug.Log($"[MQTT] Subscribed to topics: {string.Join(", ", prefixedTopics)}");
    }

    protected override void UnsubscribeTopics()
    {
        if (MQTTTopics == null || MQTTTopics.Length == 0)
        {
            Debug.LogWarning("[MQTT] No topics to unsubscribe from.");
            return;
        }

        // Add MQTTKey as prefix to each topic
        string[] prefixedTopics = new string[MQTTTopics.Length];
        for (int i = 0; i < MQTTTopics.Length; i++)
        {
            prefixedTopics[i] = $"{MQTTKey}/{MQTTTopics[i]}";
        }

        client.Unsubscribe(prefixedTopics);
        Debug.Log($"[MQTT] Unsubscribed from topics: {string.Join(", ", prefixedTopics)}");
    }

    protected override void OnConnected()
    {
        Debug.Log("[MQTT] Connected to MQTT broker.");
        this.SubscribeTopics();
    }
    protected override void OnDisconnected()
    {
        Debug.Log("[MQTT] Disconnected from MQTT broker.");
    }
    protected override void OnConnectionFailed(string errorMessage)
    {
        Debug.LogError($"[MQTT] Connection failed: {errorMessage}");
    }
    protected override void DecodeMessage(string topic, byte[] message)
    {
        string messageString = System.Text.Encoding.UTF8.GetString(message);
        Debug.Log($"[MQTT] Received message on topic '{topic}': {messageString}");

        // Fire the global event
        OnMqttMessageReceived?.Invoke(topic, messageString);

        // Fire the per-topic event if it exists
        if (topicEvents.TryGetValue(topic, out var handler) && handler != null)
        {
            handler.Invoke(messageString);
        }
    }

    // --- Add this method to publish messages ---
    public void PublishMessage(string topic, string message, byte qosLevel = MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, bool retain = false)
    {
        if (client != null && client.IsConnected)
        {
            ushort msgId = client.Publish(
                topic,
                System.Text.Encoding.UTF8.GetBytes(message),
                qosLevel,
                retain
            );
            Debug.Log($"[MQTT] Published message to topic '{topic}': {message} (msgId: {msgId})");
        }
        else
        {
            Debug.LogWarning("[MQTT] Cannot publish: client not connected.");
        }
    }
    public void SendPuzzelFinished()
    {
        Debug.Log("[MQTT] Send finished message to MQTT broker");
        this.PublishMessage(MQTTKey + "/finished", "finished");
    }
}
