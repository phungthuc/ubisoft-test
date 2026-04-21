using Project.Scripts.Shared.Models;
using System;

namespace Project.Scripts.Shared.Messages
{
    [Serializable]
    public class EggSpawnMessage : BaseMessage
    {
        public EggState egg;

        public EggSpawnMessage(EggState egg) : base(MessageType.EggSpawn)
        {
            this.egg = egg;
        }
    }
}
