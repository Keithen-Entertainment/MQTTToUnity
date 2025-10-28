using UnityEngine;
using UnityEngine.Events;
using System;
using System.Collections.Generic;
using M2MqttUnity;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;


[Serializable]
public class TopicMessageEventPair
{
    public string topic;
    [Tooltip("Leave blank to match any message on this topic. If set, UnityEvent will only fire when the message matches exactly.")]
    public string message;
    public UnityEvent<string> onMessageReceived;
}

public class MQTT : M2MqttUnityClient
{
    public static MQTT Instance { get; private set; }
    [Header("Options")]
    public bool ShowConnectionMessages = true;
    public bool ShowTopicSubscriptionsMessages = true;
    public bool ShowMessageReceivedMessages = true;
    public bool ShowMessagePublishedMessages = true;

    [Header("KeepAlive Settings")]
    public bool keepAliveEnabled = false;
    public string keepAliveTopic = "";
    public float keepAliveInterval = 30f;
    private float keepAliveTimer = 0f;
    
    [Header("MQTT Topic/Message/Events")]
    [Tooltip("Add a topic and UnityEvent. Optionally set a message to only fire the event when the message matches exactly; leave blank to fire for any message on the topic.")]
    public List<TopicMessageEventPair> topicMessageEvents = new List<TopicMessageEventPair>();


    protected override void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(this.gameObject);
    }

    protected override void Start()
    {
        client = new MqttClient(brokerAddress, brokerPort, isEncrypted, null, null, isEncrypted ? MqttSslProtocols.SSLv3 : MqttSslProtocols.None);
        base.Start();
    }

    private void SubscribeToTopics()
    {
        foreach (var pair in topicMessageEvents)
        {
            if (!string.IsNullOrEmpty(pair.topic))
            {
                client.Subscribe(new string[] { pair.topic }, new byte[] { MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE });
                if (ShowTopicSubscriptionsMessages)
                {
                    if (!string.IsNullOrEmpty(pair.message))
                    {
                        Debug.Log($"Subscribed to MQTT topic: {pair.topic} with message filter: {pair.message}");
                    }
                    else
                    {
                        Debug.Log($"Subscribed to MQTT topic: {pair.topic}");
                    }
                }
            }
        }
    }

    protected override void DecodeMessage(string topic, byte[] message)
    {
        string msg = System.Text.Encoding.UTF8.GetString(message);
        foreach (var pair in topicMessageEvents)
        {
            if (pair.topic == topic && pair.onMessageReceived != null)
            {
                if (string.IsNullOrEmpty(pair.message) || pair.message == msg)
                {
                    if (ShowMessageReceivedMessages)
                    {
                        Debug.Log($"MQTT Received message on topic '{topic}': {msg}");
                    }
                    pair.onMessageReceived.Invoke(msg);
                }
            }
        }
    }

    public void PublishMessage(string topic, string message)
    {
        if (client != null && client.IsConnected)
        {
            client.Publish(topic, System.Text.Encoding.UTF8.GetBytes(message), MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, false);
            if (ShowMessagePublishedMessages)
            {
                Debug.Log($"MQTT Published message on topic '{topic}': {message}");
            }
        }
        else
        {
            Debug.LogWarning("MQTT client is not connected. Cannot publish message.");
        }
    }
    protected override void OnConnecting()
    {
        if (ShowConnectionMessages)
        {
            Debug.Log("MQTT Connecting to broker...");
        }
        base.OnConnecting();
    }
    protected override void OnConnected()
    {
        if (ShowConnectionMessages)
        {
            Debug.Log("MQTT Connected to broker.");
        }
        base.OnConnected();
        SubscribeToTopics();
    }
    protected override void OnDisconnected()
    {
        if (ShowConnectionMessages)
        {
            Debug.Log("MQTT Disconnected from broker.");
        }
        base.OnDisconnected();
    }
    protected override void Update()
    {
        base.Update();

        if (keepAliveEnabled && client != null && client.IsConnected)
        {
            keepAliveTimer += Time.deltaTime;
            if (keepAliveTimer >= keepAliveInterval)
            {
                PublishMessage(keepAliveTopic, "keep-alive");
                keepAliveTimer = 0f;
            }
        }
    }
}
