using UnityEngine;

namespace Project.Scripts.Client.Systems
{
    public class ClientPlayerBootstrap : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private InputSystem inputSystem;
        [SerializeField] private ClientRuntimeBridge runtimeBridge;
        [SerializeField] private MatchHudController matchHudController;

        private void Start()
        {
            runtimeBridge?.ResetForNewMatch();
            inputSystem?.ResetForNewMatch();
            matchHudController?.ResetForNewMatch();
        }

        private void OnEnable()
        {
            if (runtimeBridge == null)
            {
                return;
            }

            runtimeBridge.MatchStarted += HandleMatchStarted;
            runtimeBridge.MatchEnded += HandleMatchEnded;
        }

        private void OnDisable()
        {
            if (runtimeBridge == null)
            {
                return;
            }

            runtimeBridge.MatchStarted -= HandleMatchStarted;
            runtimeBridge.MatchEnded -= HandleMatchEnded;
        }

        private void HandleMatchStarted()
        {
            inputSystem?.ResetForNewMatch();
            matchHudController?.ResetForNewMatch();
        }

        private void HandleMatchEnded(Project.Scripts.Shared.Messages.GameOverMessage _)
        {
            inputSystem?.SetMatchEnded(true);
        }
    }
}
