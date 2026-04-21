using Project.Scripts.Client.Entities;
using Project.Scripts.Shared;
using Project.Scripts.Shared.Models;
using System.Collections.Generic;
using UnityEngine;

namespace Project.Scripts.Client.Systems
{
    public class InterpolationSystem : MonoBehaviour
    {
        private const float c_MaxExtrapolationSeconds = 1.0f;

        private List<Snapshot> _snapshotBuffer = new List<Snapshot>();
        private List<PlayerView> playerViews = new List<PlayerView>();
        private readonly Dictionary<int, Vector3> _lastKnownPositions = new Dictionary<int, Vector3>();

        [Tooltip("Initial interpolation delay before the first snapshot measurement arrives.")]
        [SerializeField] private float interpolationDelay = 0.12f;

        [SerializeField] private int maxSnapshotBufferSize = 30;

        private float _measuredDeliveryDelay;
        private bool _hasSmoothedDelaySample;

        public void OnServerSnapshotReceived(float serverTime, List<PlayerState> players)
        {
            if (players == null || players.Count == 0)
            {
                return;
            }

            float deliveryDelay = Mathf.Max(0f, Time.time - serverTime);
            _measuredDeliveryDelay = Mathf.Lerp(
                _measuredDeliveryDelay,
                deliveryDelay,
                GameConstants.Interpolation.DelaySmoothing);

            float targetDelay = Mathf.Clamp(
                _measuredDeliveryDelay + GameConstants.Interpolation.DelayExtraMargin,
                GameConstants.Interpolation.DelayMinSeconds,
                GameConstants.Interpolation.DelayMaxSeconds);

            if (!_hasSmoothedDelaySample)
            {
                interpolationDelay = targetDelay;
                _hasSmoothedDelaySample = true;
            }
            else
            {
                interpolationDelay = Mathf.Lerp(interpolationDelay, targetDelay, 0.18f);
            }

            Snapshot snapshot = new Snapshot(serverTime, players);
            int insertIndex = _snapshotBuffer.FindIndex(s => s.timestamp > serverTime);
            if (insertIndex >= 0)
            {
                _snapshotBuffer.Insert(insertIndex, snapshot);
            }
            else
            {
                _snapshotBuffer.Add(snapshot);
            }

            if (_snapshotBuffer.Count > maxSnapshotBufferSize)
            {
                _snapshotBuffer.RemoveAt(0);
            }

            foreach (PlayerState player in players)
            {
                _lastKnownPositions[player.playerId] = player.position;
            }
        }

        public Vector3 GetInterpolatedPosition(int playerId, float renderTime)
        {
            if (_snapshotBuffer.Count == 0)
            {
                if (_lastKnownPositions.TryGetValue(playerId, out Vector3 knownPosition))
                {
                    return knownPosition;
                }

                return Vector3.zero;
            }

            if (_snapshotBuffer.Count == 1)
            {
                if (TryGetPlayerState(_snapshotBuffer[0], playerId, out PlayerState onlyState))
                {
                    float extrapolationTime = Mathf.Clamp(renderTime - _snapshotBuffer[0].timestamp, 0f, c_MaxExtrapolationSeconds);
                    Vector3 extrapolated = onlyState.position + onlyState.velocity * extrapolationTime;
                    _lastKnownPositions[playerId] = extrapolated;
                    return extrapolated;
                }

                return GetLastKnownPositionOrDefault(playerId);
            }

            Snapshot sA = null;
            Snapshot sB = null;

            for (int i = 0; i < _snapshotBuffer.Count - 1; i++)
            {
                if (renderTime >= _snapshotBuffer[i].timestamp && renderTime <= _snapshotBuffer[i + 1].timestamp)
                {
                    sA = _snapshotBuffer[i];
                    sB = _snapshotBuffer[i + 1];
                    break;
                }
            }

            if (sA != null && sB != null)
            {
                float timeDelta = sB.timestamp - sA.timestamp;
                if (timeDelta <= Mathf.Epsilon)
                {
                    return TryGetPlayerPosition(sB, playerId, out Vector3 duplicateTimePosition)
                        ? duplicateTimePosition
                        : GetLastKnownPositionOrDefault(playerId);
                }

                if (!TryGetPlayerPosition(sA, playerId, out Vector3 posA) ||
                    !TryGetPlayerPosition(sB, playerId, out Vector3 posB))
                {
                    return GetLastKnownPositionOrDefault(playerId);
                }

                float t = Mathf.Clamp01((renderTime - sA.timestamp) / timeDelta);
                Vector3 interpolated = Vector3.Lerp(posA, posB, t);
                _lastKnownPositions[playerId] = interpolated;
                return interpolated;
            }

            Snapshot earliestSnapshot = _snapshotBuffer[0];
            if (renderTime < earliestSnapshot.timestamp &&
                TryGetPlayerPosition(earliestSnapshot, playerId, out Vector3 earliestPosition))
            {
                _lastKnownPositions[playerId] = earliestPosition;
                return earliestPosition;
            }

            Snapshot latestSnapshot = _snapshotBuffer[_snapshotBuffer.Count - 1];
            if (TryGetPlayerState(latestSnapshot, playerId, out PlayerState latestState))
            {
                float extrapolationTime = Mathf.Clamp(renderTime - latestSnapshot.timestamp, 0f, c_MaxExtrapolationSeconds);
                Vector3 extrapolated = latestState.position + latestState.velocity * extrapolationTime;
                _lastKnownPositions[playerId] = extrapolated;
                return extrapolated;
            }

            return GetLastKnownPositionOrDefault(playerId);
        }

        public float GetRenderTime()
        {
            return Time.time - interpolationDelay;
        }

        private static bool TryGetPlayerPosition(Snapshot snapshot, int playerId, out Vector3 position)
        {
            if (TryGetPlayerState(snapshot, playerId, out PlayerState state))
            {
                position = state.position;
                return true;
            }

            position = Vector3.zero;
            return false;
        }

        private static bool TryGetPlayerState(Snapshot snapshot, int playerId, out PlayerState state)
        {
            for (int i = 0; i < snapshot.players.Count; i++)
            {
                if (snapshot.players[i].playerId == playerId)
                {
                    state = snapshot.players[i];
                    return true;
                }
            }

            state = null;
            return false;
        }

        private Vector3 GetLastKnownPositionOrDefault(int playerId)
        {
            return _lastKnownPositions.TryGetValue(playerId, out Vector3 lastPosition)
                ? lastPosition
                : Vector3.zero;
        }

        public void RegisterPlayerView(PlayerView view)
        {
            if (!playerViews.Contains(view))
            {
                playerViews.Add(view);
            }
        }
    }

    public class Snapshot
    {
        public float timestamp;
        public List<PlayerState> players;

        public Snapshot(float time, List<PlayerState> players)
        {
            timestamp = time;
            this.players = new List<PlayerState>(players.Count);
            foreach (PlayerState player in players)
            {
                PlayerState clone = new PlayerState(player.playerId, player.position)
                {
                    velocity = player.velocity,
                    score = player.score
                };
                this.players.Add(clone);
            }
        }
    }
}
