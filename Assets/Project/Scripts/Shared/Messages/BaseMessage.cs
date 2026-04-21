using System;

namespace Project.Scripts.Shared.Messages
{
    [Serializable]
    public abstract class BaseMessage
    {
        public MessageType type;

        public float timestamp;

        protected BaseMessage(MessageType type)
        {
            this.type = type;
            this.timestamp = 0f;
        }
    }
}
