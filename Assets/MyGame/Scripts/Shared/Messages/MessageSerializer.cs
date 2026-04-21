using UnityEngine;

namespace Project.Scripts.Shared.Messages
{
    public static class MessageSerializer
    {
        [System.Serializable]
        private class MessageEnvelope
        {
            public MessageType type;
        }

        public static string Serialize<T>(T message) where T : BaseMessage
        {
            return JsonUtility.ToJson(message);
        }

        public static BaseMessage Deserialize(string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                return null;
            }

            var dummy = JsonUtility.FromJson<MessageEnvelope>(json);
            if (dummy == null)
            {
                Debug.LogWarning("Cannot deserialize message envelope.");
                return null;
            }

            switch (dummy.type)
            {
            case MessageType.PlayerStateUpdate:
                return JsonUtility.FromJson<PlayerStateMessage>(json);
            case MessageType.EggSpawn:
                return JsonUtility.FromJson<EggSpawnMessage>(json);
            case MessageType.EggCollected:
                return JsonUtility.FromJson<CollectEggMessage>(json);
            case MessageType.PlayerMoveInput:
                return JsonUtility.FromJson<PlayerMoveInputMessage>(json);
            case MessageType.GameOver:
                return JsonUtility.FromJson<GameOverMessage>(json);
            default:
                Debug.LogWarning($"Unknown message type: {dummy.type}");
                return null;
            }
        }
    }
}
