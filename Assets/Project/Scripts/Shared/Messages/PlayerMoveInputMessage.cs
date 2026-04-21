using System;

namespace Project.Scripts.Shared.Messages
{
    [Serializable]
    public class PlayerMoveInputMessage : BaseMessage
    {
        public int playerId;

        public int inputSequence;

        public float clientDeltaTime;

        public float moveX;
        public float moveZ;

        public PlayerMoveInputMessage() : base(MessageType.PlayerMoveInput)
        {
        }

        public PlayerMoveInputMessage(int playerId, int inputSequence, float clientDeltaTime, float moveX, float moveZ) : base(MessageType.PlayerMoveInput)
        {
            this.playerId = playerId;
            this.inputSequence = inputSequence;
            this.clientDeltaTime = clientDeltaTime;
            this.moveX = moveX;
            this.moveZ = moveZ;
        }
    }
}
