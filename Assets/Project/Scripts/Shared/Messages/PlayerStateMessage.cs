using Project.Scripts.Shared.Models;
using System;
using System.Collections.Generic;

namespace Project.Scripts.Shared.Messages
{
    [Serializable]
    public class PlayerStateMessage : BaseMessage
    {
        public List<PlayerState> players;
        public List<EggState> eggs;

        public int lastAcknowledgedHumanInputSequence;

        public PlayerStateMessage(List<PlayerState> players, List<EggState> eggs) : base(MessageType.PlayerStateUpdate)
        {
            this.players = players;
            this.eggs = eggs;
        }
    }

    [Serializable]
    public class GameOverMessage : BaseMessage
    {
        public List<PlayerState> finalPlayers;

        public GameOverMessage() : base(MessageType.GameOver)
        {
        }

        public GameOverMessage(List<PlayerState> finalPlayers) : base(MessageType.GameOver)
        {
            this.finalPlayers = finalPlayers;
        }
    }
}
