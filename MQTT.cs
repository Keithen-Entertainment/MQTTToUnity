using UnityEngine;
using UnityEngine.Events;
using System;
using System.Collections.Generic;
using M2MqttUnity;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;

[Serializable]
public class TopicEventPair
{
    public string topic;
    public UnityEvent<string> onMessageReceived;
}

public class MQTT : M2MqttUnityClient
{
    [Header("MQTT Topic Events")]
    public List<TopicEventPair> topicEvents = new List<TopicEventPair>();

    protected override void Start()
    {
        client = new MqttClient(brokerAddress, brokerPort, isEncrypted, null, null, isEncrypted ? MqttSslProtocols.SSLv3 : MqttSslProtocols.None);
        base.Start();
        SubscribeToTopics();
    }

    private void SubscribeToTopics()
    {
        foreach (var pair in topicEvents)
        {
            if (!string.IsNullOrEmpty(pair.topic))
            {
                client.Subscribe(new string[] { pair.topic }, new byte[] { MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE });
            }
        }
    }

    protected override void DecodeMessage(string topic, byte[] message)
    {
        string msg = System.Text.Encoding.UTF8.GetString(message);
        foreach (var pair in topicEvents)
        {
            if (pair.topic == topic && pair.onMessageReceived != null)
            {
                pair.onMessageReceived.Invoke(msg);
            }
        }
    }

    public void PublishMessage(string topic, string message)
    {
        if (client != null && client.IsConnected)
        {
            client.Publish(topic, System.Text.Encoding.UTF8.GetBytes(message), MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, false);
        }
        else
        {
            Debug.LogWarning("MQTT client is not connected. Cannot publish message.");
        }
    }
}
