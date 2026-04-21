using UnityEngine;

namespace Project.Scripts.Shared.Config
{
    [CreateAssetMenu(fileName = "GameConfig", menuName = "UbisoftTest/GameConfig")]
    public class GameConfig : ScriptableObject
    {
        [Header("General Settings")]
        [Tooltip("Total number of players (including local player and bots)")]
        [Range(2, 10)]
        public int maxPlayers = 5;

        [Tooltip("Game duration (seconds)")]
        public float gameDuration = 60f;

        [Header("Server Simulator Settings")]
        [Tooltip("Minimum random interval between snapshot sends")]
        public float minUpdateInterval = 1f;

        [Tooltip("Maximum random interval between snapshot sends")]
        public float maxUpdateInterval = 5f;

        [Header("Egg Settings")]
        public int maxEggsOnScreen = 10;
        public float eggSpawnRate = 2f;
        public float totalEggsWhenStarting = 5;

        [Header("Network Simulation")]
        public float baseNetworkLatency = 0f;
        [Tooltip("Optional random jitter added on top of network latency (seconds)")]
        public float networkJitter = 0f;
    }
}
