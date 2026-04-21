using System;
using System.Collections.Generic;
using Project.Scripts.Client.Entities;
using Project.Scripts.Server.Core;
using Project.Scripts.Shared;
using Project.Scripts.Shared.Messages;
using Project.Scripts.Shared.Models;
using UnityEngine;
using UnityEngine.Pool;

namespace Project.Scripts.Client.Systems
{
    public class ClientRuntimeBridge : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private FakeNetwork fakeNetwork;
        [SerializeField] private InterpolationSystem interpolationSystem;
        [SerializeField] private InputSystem inputSystem;
        [SerializeField] private IsometricPlayerCamera playerCamera;

        [Header("View Prefabs")]
        [SerializeField] private GameObject playerPrefab;
        [SerializeField] private List<GameObject> eggPrefabs = new List<GameObject>();

        [Header("Prediction")]
        [SerializeField, Min(0.1f)] private float pendingCollectionTimeoutSeconds = 5f;
        [SerializeField, Min(0.1f)] private float predictionCollectionRadius = GameConstants.Eggs.CollectionRadius;

        [Header("Bot Render Smoothing")]
        [SerializeField, Min(0.01f)] private float botPositionSmoothTime = 0.12f;
        [SerializeField, Min(0.1f)] private float botMaxSmoothingSpeed = 30f;

        private readonly Dictionary<int, PlayerView> _playerViewsById = new Dictionary<int, PlayerView>();
        private readonly Dictionary<int, Vector3> _botSmoothingVelocityById = new Dictionary<int, Vector3>();
        private readonly Dictionary<int, Vector3> _botLastRenderPositionById = new Dictionary<int, Vector3>();
        private readonly Dictionary<int, GameObject> _eggViewsById = new Dictionary<int, GameObject>();
        private readonly List<EggState> _latestEggs = new List<EggState>();
        private readonly List<PlayerState> _latestPlayers = new List<PlayerState>();
        private readonly Dictionary<int, float> _pendingCollectedEggs = new Dictionary<int, float>();
        private readonly HashSet<int> _latestEggIdSet = new HashSet<int>();
        private bool _isMatchEnded;

        public event Action<GameOverMessage> MatchEnded;

        public event Action MatchStarted;

        private void Awake()
        {
        }

        private void OnEnable()
        {
            if (fakeNetwork != null)
            {
                fakeNetwork.OnMessageReceived += HandleMessageReceived;
            }
        }

        private void OnDisable()
        {
            if (fakeNetwork != null)
            {
                fakeNetwork.OnMessageReceived -= HandleMessageReceived;
            }
        }

        private void Update()
        {
            if (_isMatchEnded || interpolationSystem == null)
            {
                return;
            }

            float renderTime = interpolationSystem.GetRenderTime();
            foreach (KeyValuePair<int, PlayerView> pair in _playerViewsById)
            {
                if (pair.Value == null || pair.Key == GameConstants.Players.HumanPlayerId)
                {
                    continue;
                }

                Vector3 interpolatedTarget = interpolationSystem.GetInterpolatedPosition(pair.Key, renderTime);
                if (!_botSmoothingVelocityById.TryGetValue(pair.Key, out Vector3 smoothingVelocity))
                {
                    smoothingVelocity = Vector3.zero;
                }

                pair.Value.transform.position = Vector3.SmoothDamp(
                    pair.Value.transform.position,
                    interpolatedTarget,
                    ref smoothingVelocity,
                    botPositionSmoothTime,
                    botMaxSmoothingSpeed,
                    Time.deltaTime);

                _botSmoothingVelocityById[pair.Key] = smoothingVelocity;
                UpdateBotPresentation(pair.Key, pair.Value);
            }

            PredictEggCollection();
            ReconcilePendingEggs();
        }

        private void OnDestroy()
        {
            ClearAllViews();
        }

        private void PredictEggCollection()
        {
            if (_latestEggs.Count == 0 || inputSystem == null)
            {
                return;
            }

            if (!inputSystem.TryGetPredictedHumanPosition(out Vector3 humanPosition))
            {
                return;
            }

            float collectionRadiusSqr = predictionCollectionRadius * predictionCollectionRadius;
            for (int i = 0; i < _latestEggs.Count; i++)
            {
                EggState egg = _latestEggs[i];
                if (_pendingCollectedEggs.ContainsKey(egg.eggId))
                {
                    continue;
                }

                if ((egg.position - humanPosition).sqrMagnitude <= collectionRadiusSqr)
                {
                    MarkEggAsPendingCollected(egg.eggId);
                }
            }
        }

        private void ReconcilePendingEggs()
        {
            if (_pendingCollectedEggs.Count == 0)
            {
                return;
            }

            _latestEggIdSet.Clear();
            for (int i = 0; i < _latestEggs.Count; i++)
            {
                _latestEggIdSet.Add(_latestEggs[i].eggId);
            }

            List<int> resolvedEggs = ListPool<int>.Get();
            foreach (KeyValuePair<int, float> pending in _pendingCollectedEggs)
            {
                bool isServerConfirmedCollected = !_latestEggIdSet.Contains(pending.Key);
                if (isServerConfirmedCollected)
                {
                    resolvedEggs.Add(pending.Key);
                    continue;
                }

                bool isTimedOut = Time.time - pending.Value >= pendingCollectionTimeoutSeconds;
                if (isTimedOut)
                {
                    if (_eggViewsById.TryGetValue(pending.Key, out GameObject eggView) && eggView != null)
                    {
                        eggView.SetActive(true);
                    }

                    resolvedEggs.Add(pending.Key);
                }
            }

            for (int i = 0; i < resolvedEggs.Count; i++)
            {
                _pendingCollectedEggs.Remove(resolvedEggs[i]);
            }

            ListPool<int>.Release(resolvedEggs);
        }

        private void MarkEggAsPendingCollected(int eggId)
        {
            _pendingCollectedEggs[eggId] = Time.time;
            if (_eggViewsById.TryGetValue(eggId, out GameObject eggView) && eggView != null)
            {
                eggView.SetActive(false);
            }

            if (_playerViewsById.TryGetValue(GameConstants.Players.HumanPlayerId, out PlayerView humanView) && humanView != null)
            {
                humanView.PlayCollectAnimation();
            }
        }

        public void SetUserLatencyMilliseconds(float latencyMs)
        {
            if (fakeNetwork != null)
            {
                fakeNetwork.SetUserLatency(latencyMs);
            }
        }

        public IReadOnlyList<EggState> GetLatestEggs()
        {
            return _latestEggs;
        }

        public IReadOnlyList<PlayerState> GetLatestPlayers()
        {
            return _latestPlayers;
        }

        public void ResetForNewMatch()
        {
            _isMatchEnded = false;
            _latestEggs.Clear();
            _latestPlayers.Clear();
            _pendingCollectedEggs.Clear();
            ClearAllViews();
            inputSystem?.SetMatchEnded(false);
        }

        private void HandleMessageReceived(string json)
        {
            BaseMessage message = MessageSerializer.Deserialize(json);
            if (message is PlayerStateMessage snapshotMessage)
            {
                if (_isMatchEnded)
                {
                    _isMatchEnded = false;
                    inputSystem?.SetMatchEnded(false);
                    MatchStarted?.Invoke();
                }

                interpolationSystem?.OnServerSnapshotReceived(snapshotMessage.timestamp, snapshotMessage.players);
                CacheEggs(snapshotMessage.eggs);
                CachePlayers(snapshotMessage.players);
                SyncPlayerViews(snapshotMessage.players);
                SyncEggViews(snapshotMessage.eggs);

                foreach (var player in snapshotMessage.players)
                {
                    if (player.playerId == GameConstants.Players.HumanPlayerId)
                    {
                        if (inputSystem != null)
                        {
                            inputSystem.ReconcileServerPosition(player.position, snapshotMessage.lastAcknowledgedHumanInputSequence);
                        }
                        break;
                    }
                }
            }
            else if (message is GameOverMessage gameOverMessage)
            {
                HandleGameOver(gameOverMessage);
            }
        }

        private void HandleGameOver(GameOverMessage gameOverMessage)
        {
            if (_isMatchEnded)
            {
                return;
            }

            _isMatchEnded = true;
            if (gameOverMessage != null && gameOverMessage.finalPlayers != null)
            {
                CachePlayers(gameOverMessage.finalPlayers);
            }

            inputSystem?.SetMatchEnded(true);
            foreach (KeyValuePair<int, PlayerView> pair in _playerViewsById)
            {
                pair.Value?.SetMoveAnimation(false);
            }

            MatchEnded?.Invoke(gameOverMessage);
            Debug.Log("[ClientRuntimeBridge] Received GameOver. Client movement/render updates are frozen.");
        }

        private void CacheEggs(List<EggState> eggs)
        {
            _latestEggs.Clear();
            if (eggs == null)
            {
                return;
            }

            _latestEggs.AddRange(eggs);
        }

        private void CachePlayers(List<PlayerState> players)
        {
            _latestPlayers.Clear();
            if (players == null)
            {
                return;
            }

            for (int i = 0; i < players.Count; i++)
            {
                PlayerState source = players[i];
                PlayerState copy = new PlayerState(source.playerId, source.position)
                {
                    velocity = source.velocity,
                    score = source.score
                };
                _latestPlayers.Add(copy);
            }
        }

        private void SyncPlayerViews(List<PlayerState> players)
        {
            if (players == null)
            {
                return;
            }

            HashSet<int> activePlayerIds = HashSetPool<int>.Get();
            for (int i = 0; i < players.Count; i++)
            {
                PlayerState player = players[i];
                activePlayerIds.Add(player.playerId);
                EnsurePlayerView(player);
            }

            List<int> playersToRemove = ListPool<int>.Get();
            foreach (KeyValuePair<int, PlayerView> pair in _playerViewsById)
            {
                if (!activePlayerIds.Contains(pair.Key))
                {
                    if (pair.Value != null)
                    {
                        Destroy(pair.Value.gameObject);
                    }

                    playersToRemove.Add(pair.Key);
                }
            }

            for (int i = 0; i < playersToRemove.Count; i++)
            {
                int removedPlayerId = playersToRemove[i];
                _playerViewsById.Remove(removedPlayerId);
                _botSmoothingVelocityById.Remove(removedPlayerId);
                _botLastRenderPositionById.Remove(removedPlayerId);
            }

            HashSetPool<int>.Release(activePlayerIds);
            ListPool<int>.Release(playersToRemove);
        }

        private void EnsurePlayerView(PlayerState state)
        {
            if (_playerViewsById.ContainsKey(state.playerId))
            {
                return;
            }

            if (playerPrefab == null)
            {
                Debug.LogWarning("[ClientRuntimeBridge] Missing playerPrefab reference.");
                return;
            }

            GameObject playerObject = Instantiate(playerPrefab, state.position, Quaternion.identity);
            PlayerView view = playerObject.GetComponent<PlayerView>();
            if (view == null)
            {
                Debug.LogWarning("[ClientRuntimeBridge] playerPrefab has no PlayerView component.");
                Destroy(playerObject);
                return;
            }

            view.SetData(state.playerId);
            _playerViewsById[state.playerId] = view;
            _botLastRenderPositionById[state.playerId] = state.position;
            interpolationSystem?.RegisterPlayerView(view);

            if (state.playerId == GameConstants.Players.HumanPlayerId)
            {
                playerCamera?.SetTarget(view.transform);
                inputSystem?.InitializePrediction(view.transform);
            }
        }

        private void SyncEggViews(List<EggState> eggs)
        {
            if (eggs == null)
            {
                eggs = _latestEggs;
            }

            _latestEggIdSet.Clear();
            for (int i = 0; i < eggs.Count; i++)
            {
                EggState egg = eggs[i];
                _latestEggIdSet.Add(egg.eggId);
                EnsureEggView(egg);

                if (_eggViewsById.TryGetValue(egg.eggId, out GameObject view) && view != null)
                {
                    view.transform.position = egg.position;
                    if (!_pendingCollectedEggs.ContainsKey(egg.eggId))
                    {
                        view.SetActive(true);
                    }
                }
            }

            List<int> eggsToRemove = ListPool<int>.Get();
            foreach (KeyValuePair<int, GameObject> pair in _eggViewsById)
            {
                if (_latestEggIdSet.Contains(pair.Key))
                {
                    continue;
                }

                if (pair.Value != null)
                {
                    Destroy(pair.Value);
                }

                eggsToRemove.Add(pair.Key);
                _pendingCollectedEggs.Remove(pair.Key);
            }

            for (int i = 0; i < eggsToRemove.Count; i++)
            {
                _eggViewsById.Remove(eggsToRemove[i]);
            }

            ListPool<int>.Release(eggsToRemove);
        }

        private void EnsureEggView(EggState egg)
        {
            if (_eggViewsById.ContainsKey(egg.eggId))
            {
                return;
            }

            GameObject prefab = GetRandomEggPrefab();
            if (prefab == null)
            {
                return;
            }

            GameObject eggObject = Instantiate(prefab, egg.position, Quaternion.identity);
            eggObject.name = $"ClientEgg_{egg.eggId}";
            _eggViewsById[egg.eggId] = eggObject;
        }

        private GameObject GetRandomEggPrefab()
        {
            if (eggPrefabs == null || eggPrefabs.Count == 0)
            {
                Debug.LogWarning("[ClientRuntimeBridge] No egg prefabs configured.");
                return null;
            }

            int index = UnityEngine.Random.Range(0, eggPrefabs.Count);
            return eggPrefabs[index];
        }

        private void ClearAllViews()
        {
            foreach (KeyValuePair<int, GameObject> pair in _eggViewsById)
            {
                if (pair.Value != null)
                {
                    Destroy(pair.Value);
                }
            }

            foreach (KeyValuePair<int, PlayerView> pair in _playerViewsById)
            {
                if (pair.Value != null)
                {
                    Destroy(pair.Value.gameObject);
                }
            }

            _eggViewsById.Clear();
            _playerViewsById.Clear();
            _botSmoothingVelocityById.Clear();
            _botLastRenderPositionById.Clear();
        }

        private void UpdateBotPresentation(int playerId, PlayerView view)
        {
            if (view == null || playerId == GameConstants.Players.HumanPlayerId)
            {
                return;
            }

            Vector3 currentPosition = view.transform.position;
            Vector3 previousPosition = currentPosition;
            if (_botLastRenderPositionById.TryGetValue(playerId, out Vector3 cachedPosition))
            {
                previousPosition = cachedPosition;
            }

            Vector3 renderVelocity = currentPosition - previousPosition;
            bool isMoving = renderVelocity.sqrMagnitude > GameConstants.Movement.HumanInputDeadZoneSqr;
            view.SetMoveAnimation(isMoving);
            if (isMoving)
            {
                view.SetFacingDirection(renderVelocity, Time.deltaTime);
            }

            _botLastRenderPositionById[playerId] = currentPosition;
        }
    }
}
