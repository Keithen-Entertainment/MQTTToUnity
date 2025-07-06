using M2MqttUnity;
using UnityEngine;
using uPLibrary.Networking.M2Mqtt.Messages;
using System;

public class MQTTToUnityController : M2MqttUnityClient
{
    public string MQTTTopic;

    // Event for received MQTT messages
    public event Action<string, string> OnMqttMessageReceived;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    protected override void Start()
    {
        base.Start();
    }

    // Update is called once per frame
    protected override void Update()
    {
        base.Update();
    }

    protected override void SubscribeTopics()
    {
        client.Subscribe(new string[] { MQTTTopic }, new byte[] { MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE });
        Debug.Log($"[MQTT] Subscribed to topic: {MQTTTopic}");
    }
    protected override void UnsubscribeTopics()
    {
        client.Unsubscribe(new string[] { MQTTTopic });
        Debug.Log($"[MQTT] Unsubscribed from topic: {MQTTTopic}");
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
        // Fire the event
        OnMqttMessageReceived?.Invoke(topic, messageString);
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
}
