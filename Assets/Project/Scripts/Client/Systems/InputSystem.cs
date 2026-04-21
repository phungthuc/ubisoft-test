using System.Collections.Generic;
using Project.Scripts.Server.Core;
using Project.Scripts.Server.Systems;
using Project.Scripts.Shared;
using Project.Scripts.Client.Entities;
using Project.Scripts.Shared.Messages;
using Project.Scripts.Shared.Movement;

using UnityEngine;

namespace Project.Scripts.Client.Systems
{
    [DefaultExecutionOrder(-40)]
    public class InputSystem : MonoBehaviour
    {
        [SerializeField] private FakeNetwork fakeNetwork;
        [SerializeField] private GridSystem gridSystem;

        private Transform _humanView;
        private PlayerView _humanPlayerView;

        private int _nextInputSequence = 1;
        private readonly List<InputRecord> _inputHistory = new List<InputRecord>();
        private Vector3 _pendingReconcileCorrection;
        private bool _isMatchEnded;

        private struct InputRecord
        {
            public int sequence;
            public Vector3 direction;
            public float deltaTime;
        }

        public void InitializePrediction(Transform view)
        {
            _humanView = view;
            _humanPlayerView = view != null ? view.GetComponent<PlayerView>() : null;
            _inputHistory.Clear();
            _nextInputSequence = 1;
            _pendingReconcileCorrection = Vector3.zero;
            _isMatchEnded = false;
        }

        public bool TryGetPredictedHumanPosition(out Vector3 position)
        {
            if (_humanView == null)
            {
                position = Vector3.zero;
                return false;
            }

            position = _humanView.position;
            return true;
        }

        public void ResetForNewMatch()
        {
            _nextInputSequence = 1;
            _inputHistory.Clear();
            _pendingReconcileCorrection = Vector3.zero;
            _isMatchEnded = false;
            _humanPlayerView?.SetMoveAnimation(false);
        }

        public void SetMatchEnded(bool isMatchEnded)
        {
            _isMatchEnded = isMatchEnded;
            if (_isMatchEnded)
            {
                _pendingReconcileCorrection = Vector3.zero;
                _inputHistory.Clear();
                _humanPlayerView?.SetMoveAnimation(false);
            }
        }

        public void ReconcileServerPosition(Vector3 serverPosition, int acknowledgedInputSequence)
        {
            if (_humanView == null || _isMatchEnded)
            {
                return;
            }

            TrimInputHistory(acknowledgedInputSequence);
            Vector3 replayEnd = ReplayFromServer(serverPosition);

            float drift = Vector3.Distance(_humanView.position, replayEnd);
            if (drift <= GameConstants.Movement.ReconcileMinErrorDistance)
            {
                _pendingReconcileCorrection = Vector3.zero;
                return;
            }

            if (drift > GameConstants.Movement.MaxDriftThreshold)
            {
                _humanView.position = replayEnd;
                _pendingReconcileCorrection = Vector3.zero;
                return;
            }

            _pendingReconcileCorrection = replayEnd - _humanView.position;
        }

        private void LateUpdate()
        {
            if (fakeNetwork == null || _isMatchEnded)
            {
                return;
            }

            float horizontal = Input.GetAxisRaw("Horizontal");
            float vertical = Input.GetAxisRaw("Vertical");

            Vector3 rawInput = new Vector3(horizontal, 0, vertical).normalized;
            Vector3 rotatedDirection = Quaternion.Euler(0, 45, 0) * rawInput;

            int sequence = _nextInputSequence++;
            float clientDelta = Time.deltaTime;
            var message = new PlayerMoveInputMessage(
                GameConstants.Players.HumanPlayerId,
                sequence,
                clientDelta,
                rotatedDirection.x,
                rotatedDirection.z);

            RecordInput(sequence, rotatedDirection, clientDelta);
            fakeNetwork.SendFromClient(MessageSerializer.Serialize(message));

            bool isMoving = rotatedDirection.sqrMagnitude >= GameConstants.Movement.HumanInputDeadZoneSqr;
            _humanPlayerView?.SetMoveAnimation(isMoving);
            if (isMoving)
            {
                _humanPlayerView?.SetFacingDirection(rotatedDirection, Time.deltaTime);
            }
            PredictLocalMovement(rotatedDirection);
            ApplyPendingReconcileCorrection();
        }

        private void RecordInput(int sequence, Vector3 direction, float deltaTime)
        {
            _inputHistory.Add(new InputRecord
            {
                sequence = sequence,
                direction = direction,
                deltaTime = deltaTime
            });

            while (_inputHistory.Count > GameConstants.Movement.MaxInputHistory)
            {
                _inputHistory.RemoveAt(0);
            }
        }

        private static void TrimInputHistoryList(List<InputRecord> history, int acknowledgedInputSequence)
        {
            int firstKeepIndex = history.FindIndex(r => r.sequence > acknowledgedInputSequence);
            if (firstKeepIndex < 0)
            {
                history.Clear();
                return;
            }

            if (firstKeepIndex > 0)
            {
                history.RemoveRange(0, firstKeepIndex);
            }
        }

        private void TrimInputHistory(int acknowledgedInputSequence)
        {
            TrimInputHistoryList(_inputHistory, acknowledgedInputSequence);
        }

        private Vector3 ReplayFromServer(Vector3 serverPosition)
        {
            if (gridSystem == null)
            {
                return _humanView != null ? _humanView.position : serverPosition;
            }

            Vector3 position = serverPosition;
            for (int i = 0; i < _inputHistory.Count; i++)
            {
                InputRecord r = _inputHistory[i];
                HumanGridMovement.TryMoveWithSlide(
                    ref position,
                gridSystem,
                    r.direction,
                    r.deltaTime,
                    GameConstants.Movement.PlayerMoveSpeed);
            }

            return position;
        }

        private void ApplyPendingReconcileCorrection()
        {
            if (_humanView == null || _pendingReconcileCorrection.sqrMagnitude < 1e-10f)
            {
                return;
            }

            float maxStep = GameConstants.Movement.ReconcileCorrectionMaxSpeed * Time.deltaTime;
            Vector3 step = Vector3.ClampMagnitude(_pendingReconcileCorrection, maxStep);
            _humanView.position += step;
            _pendingReconcileCorrection -= step;
        }

        private void PredictLocalMovement(Vector3 direction)
        {
            if (_humanView == null || gridSystem == null)
            {
                return;
            }

            Vector3 pos = _humanView.position;
            HumanGridMovement.TryMoveWithSlide(
                ref pos,
                gridSystem,
                direction,
                Time.deltaTime,
                GameConstants.Movement.PlayerMoveSpeed);
            _humanView.position = pos;
        }
    }
}
