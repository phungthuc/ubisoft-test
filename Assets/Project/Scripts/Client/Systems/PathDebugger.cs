using Project.Scripts.Shared;
using Project.Scripts.Server.Systems;
using System.Collections.Generic;
using UnityEngine;

namespace Project.Scripts.Client.Systems
{
    public class PathDebugger : MonoBehaviour
    {
        public List<BotController> botsToDebug = new List<BotController>();

        private void OnDrawGizmos()
        {
            if (botsToDebug == null) return;

            foreach (var bot in botsToDebug)
            {
                if (bot == null || bot.CurrentPath == null) continue;

                Gizmos.color = Color.green;
                for (int i = 0; i < bot.CurrentPath.Count - 1; i++)
                {
                    Gizmos.DrawLine(bot.CurrentPath[i].worldPosition, bot.CurrentPath[i + 1].worldPosition);
                }

                foreach (var node in bot.CurrentPath)
                {
                    Gizmos.DrawWireCube(node.worldPosition, Vector3.one * GameConstants.DebugGizmos.PathNodeWireCubeSize);
                }

                Gizmos.color = Color.yellow;
                Gizmos.DrawSphere(bot.TargetPosition, GameConstants.DebugGizmos.BotTargetSphereRadius);
            }
        }
    }
}
