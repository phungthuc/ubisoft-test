using UnityEngine;
using System;

namespace Project.Scripts.Shared.Models
{
    [Serializable]
    public class EggState
    {
        public int eggId;
        public Vector3 position;
        public bool isCollected;
        public Color eggColor;

        public EggState(int id, Vector3 pos, Color color)
        {
            this.eggId = id;
            this.position = pos;
            this.eggColor = color;
            this.isCollected = false;
        }
    }
}