using Project.Scripts.Shared;
using UnityEngine;
using TMPro;

namespace Project.Scripts.Client.Entities
{
    public class PlayerView : MonoBehaviour
    {
        [Header("Identity")]
        [SerializeField] private SkinnedMeshRenderer meshRenderer;
        [Header("Animation")]
        [SerializeField] private Animator animator;
        [SerializeField] private string movingParameterName = "IsMoving";
        [SerializeField] private string collectTriggerName = "Collect";
        [Header("Rotation")]
        [SerializeField] private float rotationLerpSpeed = 14f;

        public int PlayerId { get; private set; }
        private int _movingParameterHash;
        private int _collectTriggerHash;
        private bool _isAnimationHashesInitialized;

        private void Awake()
        {
            InitializeAnimationHashes();
        }

        public void SetData(int id)
        {
            PlayerId = id;

            if (id == GameConstants.Players.HumanPlayerId)
                meshRenderer.material.color = Color.yellow;
            else
                meshRenderer.material.color = Color.gray;
        }

        public void SetMoveAnimation(bool isMoving)
        {
            if (animator == null)
            {
                return;
            }

            EnsureAnimationHashes();
            if (_movingParameterHash != 0)
            {
                animator.SetBool(_movingParameterHash, isMoving);
            }
        }

        public void PlayCollectAnimation()
        {
            if (animator == null)
            {
                return;
            }

            EnsureAnimationHashes();
            if (_collectTriggerHash != 0)
            {
                animator.SetTrigger(_collectTriggerHash);
            }
        }

        public void SetFacingDirection(Vector3 worldDirection, float deltaTime)
        {
            Vector3 planarDirection = new Vector3(worldDirection.x, 0f, worldDirection.z);
            if (planarDirection.sqrMagnitude <= GameConstants.Movement.HumanInputDeadZoneSqr)
            {
                return;
            }

            Quaternion targetRotation = Quaternion.LookRotation(planarDirection.normalized, Vector3.up);
            float rotationT = Mathf.Clamp01(rotationLerpSpeed * Mathf.Max(deltaTime, 0f));
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationT);
        }

        private void EnsureAnimationHashes()
        {
            if (_isAnimationHashesInitialized)
            {
                return;
            }

            InitializeAnimationHashes();
        }

        private void InitializeAnimationHashes()
        {
            _movingParameterHash = string.IsNullOrWhiteSpace(movingParameterName) ? 0 : Animator.StringToHash(movingParameterName);
            _collectTriggerHash = string.IsNullOrWhiteSpace(collectTriggerName) ? 0 : Animator.StringToHash(collectTriggerName);
            _isAnimationHashesInitialized = true;
        }
    }
}
