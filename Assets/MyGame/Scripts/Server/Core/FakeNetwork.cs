using Project.Scripts.Shared;
using Project.Scripts.Shared.Config;
using System.Collections.Generic;
using UnityEngine;

namespace Project.Scripts.Server.Core
{
    [DefaultExecutionOrder(-100)]
    public class FakeNetwork : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField] private GameConfig config;

        private readonly Queue<DelayedMessage> _toClientQueue = new Queue<DelayedMessage>();

        private readonly Queue<DelayedMessage> _toServerQueue = new Queue<DelayedMessage>();

        public System.Action<string> OnMessageReceived;

        public System.Action<string> OnMessageFromClient;

        private float _userLatency;

        public void SetUserLatency(float latencyMs) => _userLatency = latencyMs * GameConstants.Time.MillisecondsToSeconds;

        public void Send(string jsonData)
        {
            float jitter = config.networkJitter > 0f ? Random.Range(0f, config.networkJitter) : 0f;
            float totalDelay = config.baseNetworkLatency + _userLatency + jitter;
            float deliveryTime = Time.time + totalDelay;

            _toClientQueue.Enqueue(new DelayedMessage(jsonData, deliveryTime));
            Debug.Log($"[FakeNetwork] Enqueue to client. delay={totalDelay:F3}s (base={config.baseNetworkLatency:F3}, user={_userLatency:F3}, jitter={jitter:F3})");
        }

        public void SendFromClient(string jsonData)
        {
            float jitter = config.networkJitter > 0f ? Random.Range(0f, config.networkJitter) : 0f;
            float totalDelay = config.baseNetworkLatency + _userLatency + jitter;
            float deliveryTime = Time.time + totalDelay;

            Debug.Log($"[FakeNetwork] Enqueue from client. delay={totalDelay:F3}s (base={config.baseNetworkLatency:F3}, user={_userLatency:F3}, jitter={jitter:F3})");
            _toServerQueue.Enqueue(new DelayedMessage(jsonData, deliveryTime));
        }

        private void Update()
        {
            while (_toClientQueue.Count > 0 && Time.time >= _toClientQueue.Peek().deliveryTime)
            {
                DelayedMessage msg = _toClientQueue.Dequeue();

                OnMessageReceived?.Invoke(msg.jsonData);
            }

            while (_toServerQueue.Count > 0 && Time.time >= _toServerQueue.Peek().deliveryTime)
            {
                DelayedMessage msg = _toServerQueue.Dequeue();

                OnMessageFromClient?.Invoke(msg.jsonData);
            }
        }
    }

    class DelayedMessage
    {
        public string jsonData;

        public float deliveryTime;

        public DelayedMessage(string data, float time)
        {
            jsonData = data;
            deliveryTime = time;
        }
    }
}
