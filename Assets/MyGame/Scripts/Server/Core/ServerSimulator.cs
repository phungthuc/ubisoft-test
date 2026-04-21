using UnityEngine;
using System.Collections.Generic;
using Project.Scripts.Shared.Models;
using Project.Scripts.Server.Systems;
using Project.Scripts.Shared;
using Project.Scripts.Shared.Config;
using Project.Scripts.Shared.Messages;
using Project.Scripts.Shared.Movement;
using Project.Scripts.Shared.Pathfinding;
using System;

namespace Project.Scripts.Server.Core
{
    public class ServerSimulator : MonoBehaviour
    {
        #region Inspector References

        [Header("References")]
        [SerializeField] private GameConfig config;
        [SerializeField] private GridSystem gridSystem;
        [SerializeField] private FakeNetwork fakeNetwork;
        private GameState _currentGameState;
        private List<BotController> _bots = new List<BotController>();

        private float _nextBroadcastTime;
        private float _nextEggSpawnTime;
        private int _nextEggId;
        private Vector3 _humanMoveIntent;

        private readonly SortedDictionary<int, (Vector3 direction, float deltaTime)> _humanInputBuffer = new SortedDictionary<int, (Vector3, float)>();
        private int _nextExpectedHumanInputSequence = 1;
        private int _lastAcknowledgedHumanInputSequence;
        private float _matchEndTime;
        private bool _isMatchEnded;
        private bool _isGameRunning;

        public event Action GameStarted;
        public event Action GameCleared;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (config == null)
            {
                Debug.LogError("[ServerSimulator] Missing GameConfig reference.");
                enabled = false;
                return;
            }

        }

        private void Start()
        {
            if (fakeNetwork == null)
            {
                Debug.LogError("[ServerSimulator] Missing FakeNetwork reference.");
                enabled = false;
                return;
            }

            StartGame();
        }

        private void OnEnable()
        {
            if (fakeNetwork != null)
            {
                fakeNetwork.OnMessageFromClient += HandleMessageFromClient;
            }
        }

        private void OnDisable()
        {
            if (fakeNetwork != null)
            {
                fakeNetwork.OnMessageFromClient -= HandleMessageFromClient;
            }
        }

        private void FixedUpdate()
        {
            if (!_isGameRunning || _isMatchEnded)
            {
                return;
            }

            if (Time.time >= _matchEndTime)
            {
                EndMatch();
                return;
            }

            ApplyHumanPlayerMovement();

            foreach (var bot in _bots)
            {
                bot.Update(_currentGameState, gridSystem);
            }

            UpdateCollisions();

            UpdateEggSpawning();

            HandleBroadcasting();
        }

        #endregion

        #region Client Messages

        private void HandleMessageFromClient(string json)
        {
            if (!_isGameRunning || _isMatchEnded)
            {
                return;
            }

            BaseMessage message = MessageSerializer.Deserialize(json);
            if (message is PlayerMoveInputMessage move && move.playerId == GameConstants.Players.HumanPlayerId)
            {
                if (move.inputSequence < _nextExpectedHumanInputSequence)
                {
                    return;
                }

                float dt = Mathf.Clamp(move.clientDeltaTime, 0.0001f, GameConstants.Movement.MaxInputDeltaTimeSeconds);
                Vector3 direction = new Vector3(move.moveX, 0f, move.moveZ);
                _humanInputBuffer[move.inputSequence] = (direction, dt);
            }
        }

        #endregion

        #region Players

        private void InitializePlayers()
        {
            float xStart = -gridSystem.gridWorldSize.x / 2;
            float xSpace = gridSystem.gridWorldSize.x / config.maxPlayers;
            for (int i = 0; i < config.maxPlayers; i++)
            {
                var playerState = new PlayerState(i, new Vector3(xStart + i * xSpace, 0, 0));
                _currentGameState.players.Add(playerState);

                if (i != GameConstants.Players.HumanPlayerId)
                {
                    _bots.Add(new BotController(playerState));
                }
            }
        }

        private void ApplyHumanPlayerMovement()
        {
            if (_currentGameState == null || _currentGameState.players.Count == 0)
            {
                return;
            }

            PlayerState human = null;
            for (int i = 0; i < _currentGameState.players.Count; i++)
            {
                if (_currentGameState.players[i].playerId == GameConstants.Players.HumanPlayerId)
                {
                    human = _currentGameState.players[i];
                    break;
                }
            }

            if (human == null)
            {
                return;
            }

            Vector3 humanPositionStart = human.position;
            int consumedInputs = 0;

            while (consumedInputs < GameConstants.Movement.MaxHumanInputsPerFixedStep &&
                   _humanInputBuffer.TryGetValue(_nextExpectedHumanInputSequence, out (Vector3 direction, float deltaTime) buffered))
            {
                _humanInputBuffer.Remove(_nextExpectedHumanInputSequence);

                if (buffered.direction.sqrMagnitude >= GameConstants.Movement.HumanInputDeadZoneSqr)
                {
                    _humanMoveIntent = buffered.direction;
                }
                else
                {
                    _humanMoveIntent = Vector3.zero;
                }

                HumanGridMovement.TryMoveWithSlide(
                    ref human.position,
                    gridSystem,
                    buffered.direction,
                    buffered.deltaTime,
                    GameConstants.Movement.PlayerMoveSpeed);

                _lastAcknowledgedHumanInputSequence = _nextExpectedHumanInputSequence;
                _nextExpectedHumanInputSequence++;
                consumedInputs++;
            }

            if (consumedInputs == 0 && _humanMoveIntent.sqrMagnitude >= GameConstants.Movement.HumanInputDeadZoneSqr)
            {
                HumanGridMovement.TryMoveWithSlide(
                    ref human.position,
                    gridSystem,
                    _humanMoveIntent,
                    Time.fixedDeltaTime,
                    GameConstants.Movement.PlayerMoveSpeed);
            }

            human.velocity = (human.position - humanPositionStart) * (1f / Time.fixedDeltaTime);
        }

        #endregion

        #region Gameplay

        private void UpdateCollisions()
        {
            float collectionRadius = GameConstants.Eggs.CollectionRadius;

            foreach (var player in _currentGameState.players)
            {
                for (int i = _currentGameState.eggs.Count - 1; i >= 0; i--)
                {
                    var egg = _currentGameState.eggs[i];

                    if (egg.isCollected) continue;

                    Vector3 collisionSample = player.position;
                    if (player.velocity.sqrMagnitude > 1e-6f)
                    {
                        collisionSample += player.velocity * GameConstants.Movement.CollisionExtrapolationSeconds;
                    }

                    if (Vector3.Distance(collisionSample, egg.position) < collectionRadius)
                    {
                        egg.isCollected = true;
                        player.score++;
                        _currentGameState.eggs.RemoveAt(i);

                        Debug.Log($"Player {player.playerId} collected Egg {egg.eggId}. Score: {player.score}");
                    }
                }
            }
        }

        private void UpdateEggSpawning()
        {
            if (Time.time < _nextEggSpawnTime)
            {
                return;
            }

            if (_currentGameState.eggs.Count < config.maxEggsOnScreen)
            {
                SpawnRandomEgg();
                _nextEggSpawnTime = Time.time + Mathf.Max(GameConstants.Eggs.MinSpawnIntervalFloor, config.eggSpawnRate);
            }
        }

        private void SpawnRandomEgg()
        {
            GridNode randomNode = gridSystem.GetRandomWalkableNode();
            if (randomNode != null)
            {
                Vector3 spawnPosition = randomNode.worldPosition;
                spawnPosition.y = 0.5f;

                EggState newEgg = new EggState(_nextEggId++, spawnPosition, Color.white);
                _currentGameState.eggs.Add(newEgg);
            }
        }

        #endregion

        #region Networking

        private void HandleBroadcasting()
        {
            if (Time.time >= _nextBroadcastTime)
            {
                PlayerStateMessage msg = new PlayerStateMessage(CreatePlayersSnapshot(), CreateEggsSnapshot());
                msg.timestamp = Time.time;
                msg.lastAcknowledgedHumanInputSequence = _lastAcknowledgedHumanInputSequence;
                string json = MessageSerializer.Serialize(msg);

                fakeNetwork.Send(json);
                Debug.Log($"[ServerSimulator] Snapshot published at {msg.timestamp:F2}s");

                ScheduleNextBroadcast();
            }
        }

        private void ScheduleNextBroadcast()
        {
            float interval = UnityEngine.Random.Range(config.minUpdateInterval, config.maxUpdateInterval);
            _nextBroadcastTime = Time.time + interval;
            Debug.Log($"[ServerSimulator] Next snapshot in {interval:F2}s (window {config.minUpdateInterval:F1}-{config.maxUpdateInterval:F1})");
        }

        private List<PlayerState> CreatePlayersSnapshot()
        {
            List<PlayerState> snapshot = new List<PlayerState>(_currentGameState.players.Count);
            foreach (PlayerState player in _currentGameState.players)
            {
                PlayerState clone = new PlayerState(player.playerId, player.position)
                {
                    velocity = player.velocity,
                    score = player.score
                };
                snapshot.Add(clone);
            }

            return snapshot;
        }

        private void EndMatch()
        {
            if (_isMatchEnded)
            {
                return;
            }

            _isMatchEnded = true;
            _isGameRunning = false;
            _humanMoveIntent = Vector3.zero;
            _humanInputBuffer.Clear();

            foreach (PlayerState player in _currentGameState.players)
            {
                player.velocity = Vector3.zero;
            }

            PlayerStateMessage finalSnapshot = new PlayerStateMessage(CreatePlayersSnapshot(), CreateEggsSnapshot())
            {
                timestamp = Time.time,
                lastAcknowledgedHumanInputSequence = _lastAcknowledgedHumanInputSequence
            };
            fakeNetwork?.Send(MessageSerializer.Serialize(finalSnapshot));

            GameOverMessage gameOver = new GameOverMessage(CreatePlayersSnapshot())
            {
                timestamp = Time.time
            };
            fakeNetwork?.Send(MessageSerializer.Serialize(gameOver));

            Debug.Log("[ServerSimulator] Match ended. Server simulation is now frozen.");
            Clear();
        }

        private List<EggState> CreateEggsSnapshot()
        {
            List<EggState> snapshot = new List<EggState>(_currentGameState.eggs.Count);
            foreach (EggState egg in _currentGameState.eggs)
            {
                EggState clone = new EggState(egg.eggId, egg.position, egg.eggColor)
                {
                    isCollected = egg.isCollected
                };
                snapshot.Add(clone);
            }

            return snapshot;
        }

        #endregion

        #region Public API

        public void StartGame()
        {
            _currentGameState = new GameState
            {
                remainingTime = Mathf.Max(0f, config != null ? config.gameDuration : 0f)
            };

            _bots.Clear();
            _humanInputBuffer.Clear();
            _humanMoveIntent = Vector3.zero;
            _nextExpectedHumanInputSequence = 1;
            _lastAcknowledgedHumanInputSequence = 0;
            _nextEggId = 0;
            InitializePlayers();

            for (int i = 0; i < config.totalEggsWhenStarting; i++)
            {
                SpawnRandomEgg();
            }

            _nextEggSpawnTime = Time.time;
            ScheduleNextBroadcast();
            _matchEndTime = Time.time + Mathf.Max(0f, config.gameDuration);
            _isMatchEnded = false;
            _isGameRunning = true;

            GameStarted?.Invoke();
        }

        public void Clear()
        {
            _isGameRunning = false;
            _isMatchEnded = true;

            _humanMoveIntent = Vector3.zero;
            _humanInputBuffer.Clear();
            _nextExpectedHumanInputSequence = 1;
            _lastAcknowledgedHumanInputSequence = 0;

            _bots.Clear();

            if (_currentGameState != null)
            {
                _currentGameState.players.Clear();
                _currentGameState.eggs.Clear();
                _currentGameState.remainingTime = 0f;
            }

            GameCleared?.Invoke();
        }

        public List<BotController> GetBots() => _bots;

        #endregion
    }
}
