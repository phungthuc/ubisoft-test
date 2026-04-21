using System.Collections.Generic;
using System.Text;
using Project.Scripts.Server.Core;
using Project.Scripts.Shared.Config;
using Project.Scripts.Shared.Messages;
using Project.Scripts.Shared.Models;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace Project.Scripts.Client.Systems
{
    public class MatchHudController : MonoBehaviour
    {
        #region Inspector References

        [Header("References")]
        [SerializeField] private ClientRuntimeBridge runtimeBridge;
        [SerializeField] private ServerSimulator serverSimulator;
        [SerializeField] private GameConfig gameConfig;
        [SerializeField] private TextMeshProUGUI timerText;
        [SerializeField] private TextMeshProUGUI leaderboardText;

        [SerializeField] private TextMeshProUGUI leaderboardTextEndScreen;

        public UnityEvent OnMatchEnded;

        #endregion

        #region Runtime Fields

        private readonly List<PlayerState> _sortedPlayers = new List<PlayerState>();
        private readonly StringBuilder _leaderboardBuilder = new StringBuilder(256);
        private float _remainingTimeSeconds;
        private bool _isMatchEnded;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (gameConfig != null)
            {
                _remainingTimeSeconds = Mathf.Max(0f, gameConfig.gameDuration);
            }
        }

        private void OnEnable()
        {
            if (runtimeBridge != null)
            {
                runtimeBridge.MatchEnded += HandleMatchEnded;
                runtimeBridge.MatchStarted += HandleMatchStarted;
            }

            if (serverSimulator != null)
            {
                serverSimulator.GameStarted += HandleServerGameStarted;
            }
        }

        private void OnDisable()
        {
            if (runtimeBridge != null)
            {
                runtimeBridge.MatchEnded -= HandleMatchEnded;
                runtimeBridge.MatchStarted -= HandleMatchStarted;
            }

            if (serverSimulator != null)
            {
                serverSimulator.GameStarted -= HandleServerGameStarted;
            }
        }

        private void Update()
        {
            UpdateCountdown();
            UpdateLeaderboard();
        }

        #endregion

        #region UI Updates

        private void UpdateCountdown()
        {
            if (timerText == null)
            {
                return;
            }

            if (!_isMatchEnded)
            {
                _remainingTimeSeconds = Mathf.Max(0f, _remainingTimeSeconds - Time.deltaTime);
            }
            int totalSeconds = Mathf.CeilToInt(_remainingTimeSeconds);
            int minutes = totalSeconds / 60;
            int seconds = totalSeconds % 60;
            timerText.text = $"{minutes:00}.{seconds:00}";
        }

        private void UpdateLeaderboard()
        {
            if (leaderboardText == null)
            {
                return;
            }

            IReadOnlyList<PlayerState> players = runtimeBridge != null ? runtimeBridge.GetLatestPlayers() : null;
            if (players == null || players.Count == 0)
            {
                leaderboardText.text = "No players";
                return;
            }

            _sortedPlayers.Clear();
            for (int i = 0; i < players.Count; i++)
            {
                _sortedPlayers.Add(players[i]);
            }

            _sortedPlayers.Sort(ComparePlayerScoreDesc);

            _leaderboardBuilder.Clear();
            _leaderboardBuilder.AppendLine("Leaderboard");
            for (int i = 0; i < _sortedPlayers.Count; i++)
            {
                PlayerState player = _sortedPlayers[i];
                _leaderboardBuilder.Append(i + 1)
                    .Append(". P")
                    .Append(player.playerId)
                    .Append(" - ")
                    .Append(player.score)
                    .AppendLine(" eggs");
            }

            leaderboardText.text = _leaderboardBuilder.ToString();
        }

        #endregion

        #region Helpers

        private static int ComparePlayerScoreDesc(PlayerState left, PlayerState right)
        {
            int scoreCompare = right.score.CompareTo(left.score);
            if (scoreCompare != 0)
            {
                return scoreCompare;
            }

            return left.playerId.CompareTo(right.playerId);
        }

        private void HandleMatchEnded(GameOverMessage gameOverMessage)
        {
            leaderboardTextEndScreen.text = _leaderboardBuilder.ToString();
            _isMatchEnded = true;
            _remainingTimeSeconds = 0f;
            OnMatchEnded?.Invoke();
            _leaderboardBuilder.Clear();
        }

        public void ResetForNewMatch()
        {
            runtimeBridge?.ResetForNewMatch();

            _remainingTimeSeconds = Mathf.Max(0f, gameConfig != null ? gameConfig.gameDuration : 0f);
            _leaderboardBuilder.Clear();
            if (leaderboardText != null)
            {
                leaderboardText.text = "No players";
            }
            if (timerText != null)
            {
                int totalSeconds = Mathf.CeilToInt(_remainingTimeSeconds);
                int minutes = totalSeconds / 60;
                int seconds = totalSeconds % 60;
                timerText.text = $"{minutes:00}.{seconds:00}";
            }
            if (leaderboardTextEndScreen != null)
            {
                leaderboardTextEndScreen.text = string.Empty;
            }
            _isMatchEnded = false;

        }

        private void HandleMatchStarted()
        {
            ResetForNewMatch();
        }

        private void HandleServerGameStarted()
        {
            ResetForNewMatch();
        }

        #endregion
    }
}
