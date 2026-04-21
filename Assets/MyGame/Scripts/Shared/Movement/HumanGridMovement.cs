using Project.Scripts.Server.Systems;
using UnityEngine;

namespace Project.Scripts.Shared.Movement
{
    public static class HumanGridMovement
    {
        public static void TryMoveWithSlide(
            ref Vector3 position,
            GridSystem gridSystem,
            Vector3 direction,
            float deltaTime,
            float speed)
        {
            if (deltaTime <= 0f || direction.sqrMagnitude < GameConstants.Movement.HumanInputDeadZoneSqr)
            {
                return;
            }

            Vector3 delta = direction.normalized * (speed * deltaTime);
            if (gridSystem == null)
            {
                position += delta;
                return;
            }

            Vector3 from = position;
            Vector3 target = from + delta;

            if (gridSystem.IsWalkableAtWorldPoint(target))
            {
                position = target;
                return;
            }

            Vector3 slideX = from + new Vector3(delta.x, 0f, 0f);
            if (Mathf.Abs(delta.x) > GameConstants.Movement.AxisEpsilon && gridSystem.IsWalkableAtWorldPoint(slideX))
            {
                position = slideX;
                return;
            }

            Vector3 slideZ = from + new Vector3(0f, 0f, delta.z);
            if (Mathf.Abs(delta.z) > GameConstants.Movement.AxisEpsilon && gridSystem.IsWalkableAtWorldPoint(slideZ))
            {
                position = slideZ;
            }
        }
    }
}
