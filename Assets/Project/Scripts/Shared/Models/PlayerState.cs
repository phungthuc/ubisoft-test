using UnityEngine;
using System;

namespace Project.Scripts.Shared.Models
{
    [Serializable]
    public class PlayerState
    {
        public int playerId;
        public Vector3 position;
        public Vector3 velocity;
        public int score;

        public PlayerState(int id, Vector3 pos)
        {
            this.playerId = id;
            this.position = pos;
            this.velocity = Vector3.zero;
            this.score = 0;
        }
    }
}
