using System;

namespace Project.Scripts.Shared.Messages
{
    [Serializable]
    public class CollectEggMessage : BaseMessage
    {
        public int playerId;
        public int eggId;

        public CollectEggMessage(int playerId, int eggId) : base(MessageType.EggCollected)
        {
            this.playerId = playerId;
            this.eggId = eggId;
        }
    }
}
