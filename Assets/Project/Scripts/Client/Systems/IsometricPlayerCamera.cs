using Project.Scripts.Shared;
using UnityEngine;

namespace Project.Scripts.Client.Systems
{
    [RequireComponent(typeof(Camera))]
    public class IsometricPlayerCamera : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private Transform target;

        [Header("Isometric Settings")]
        [SerializeField] private float distance = 10f;
        [SerializeField] private float height = 10f;
        [SerializeField] private float yaw = 45f;
        [SerializeField] private float pitch = 35f;

        [Header("Offset")]
        [SerializeField] private Vector3 targetOffset = new Vector3(0f, 1f, 0f);

        [Header("Smoothing")]
        [SerializeField] private float positionSmoothTime = 0.12f;
        [SerializeField] private float rotationSmoothSpeed = 10f;

        private Vector3 _velocity;
        private Camera _cam;

        private void Awake()
        {
            _cam = GetComponent<Camera>();

            _cam.orthographic = true;
            _cam.orthographicSize = GameConstants.Camera.DefaultOrthographicSize;
        }

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
        }

        private void LateUpdate()
        {
            if (target == null) return;

            Quaternion targetRotation = Quaternion.Euler(pitch, yaw, 0f);

            Vector3 offset = targetRotation * new Vector3(0f, 0f, -distance);
            offset.y += height;

            Vector3 desiredPosition = target.position + targetOffset + offset;

            transform.position = Vector3.SmoothDamp(
                transform.position,
                desiredPosition,
                ref _velocity,
                positionSmoothTime
            );

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                Time.deltaTime * rotationSmoothSpeed
            );
        }
    }
}
