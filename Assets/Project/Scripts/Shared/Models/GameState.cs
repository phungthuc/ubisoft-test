using System;
using System.Collections.Generic;

namespace Project.Scripts.Shared.Models
{
    [Serializable]
    public class GameState
    {
        public List<PlayerState> players = new List<PlayerState>();

        public List<EggState> eggs = new List<EggState>();

        public float remainingTime;

        public GameState()
        {
            players = new List<PlayerState>();
            eggs = new List<EggState>();
        }
    }
}
