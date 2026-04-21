using Project.Scripts.Shared;
using Project.Scripts.Shared.Models;
using Project.Scripts.Shared.Pathfinding;
using System.Collections.Generic;
using UnityEngine;

namespace Project.Scripts.Server.Systems
{
    public class BotController
    {
        private PlayerState _state;
        private BotAIState _currentState;
        private List<GridNode> _currentPath;
        private Vector3 _targetPosition;
        private int _targetEggId = GameConstants.Bots.InvalidEggTargetId;

        public List<GridNode> CurrentPath => _currentPath;
        public Vector3 TargetPosition => _targetPosition;

        public BotController(PlayerState state)
        {
            _state = state;
            _currentState = BotAIState.Idle;
        }

        public void Update(GameState worldState, GridSystem grid)
        {
            Vector3 startPosition = _state.position;
            switch (_currentState)
            {
                case BotAIState.Idle:
                    _currentState = BotAIState.FindNearestEgg;
                    break;

                case BotAIState.FindNearestEgg:
                    HandleFindTarget(worldState);
                    break;

                case BotAIState.Pathfinding:
                    HandlePathfinding(grid);
                    break;

                case BotAIState.Moving:
                    HandleMovement();
                    break;
            }

            float fixedDelta = Mathf.Max(Time.fixedDeltaTime, 0.0001f);
            _state.velocity = (_state.position - startPosition) * (1f / fixedDelta);
        }

        private void HandleFindTarget(GameState worldState)
        {
            if (worldState.eggs == null || worldState.eggs.Count == 0)
            {
                _currentState = BotAIState.Idle;
                return;
            }

            EggState nearestEgg = null;
            float minDistance = float.MaxValue;

            foreach (var egg in worldState.eggs)
            {
                if (egg.isCollected) continue;
                float dist = Vector3.Distance(_state.position, egg.position);
                if (dist < minDistance)
                {
                    minDistance = dist;
                    nearestEgg = egg;
                }
            }

            if (nearestEgg != null)
            {
                _targetPosition = nearestEgg.position;
                _targetEggId = nearestEgg.eggId;
                _currentState = BotAIState.Pathfinding;
            }
        }

        private void HandlePathfinding(GridSystem grid)
        {
            GridNode start = grid.NodeFromWorldPoint(_state.position);
            GridNode target = grid.NodeFromWorldPoint(_targetPosition);

            _currentPath = Pathfinding.FindPath(start, target, grid);

            if (_currentPath != null && _currentPath.Count > 0)
                _currentState = BotAIState.Moving;
            else
                _currentState = BotAIState.Idle;
        }

        private void HandleMovement()
        {
            if (_currentPath == null || _currentPath.Count == 0)
            {
                _currentState = BotAIState.Idle;
                return;
            }

            Vector3 targetWaypoint = _currentPath[0].worldPosition;
            float step = GameConstants.Movement.PlayerMoveSpeed * Time.fixedDeltaTime;

            _state.position = Vector3.MoveTowards(_state.position, targetWaypoint, step);

            if (Vector3.Distance(_state.position, targetWaypoint) < GameConstants.Bots.WaypointReachedDistance)
            {
                _currentPath.RemoveAt(0);
            }
        }
    }


}
