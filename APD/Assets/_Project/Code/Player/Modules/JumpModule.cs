using UnityEngine;
using _Project.Code.Core.Interfaces;
using _Project.Code.Player.Utils;

namespace _Project.Code.Player.Modules
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RigidbodyMotor))]
    public sealed class JumpModule : MonoBehaviour, IPlayerModule
    {
        [Header("Jump")]
        [SerializeField] private float jumpHeight = 1.2f;

        [Header("Feel")]
        [SerializeField] private float coyoteTime = 0.10f;
        [SerializeField] private float jumpBuffer = 0.10f;

        [Header("Ground stick")]
        [Tooltip("Keeps the RB gently pinned to ground when grounded (prevents micro-bounce).")]
        [SerializeField] private float groundedStickVelocity = -2.0f;

        private RigidbodyMotor _motor;

        private float _lastGroundedTime = -999f;
        private float _lastJumpPressedTime = -999f;

        private bool _enabled;

        public void SetJumpPressedThisFrame(bool pressed)
        {
            if (!pressed) return;

            Debug.Log("JumpModule received jump input");
            _lastJumpPressedTime = Time.time;
        }

        public void ModuleEnable() => _enabled = true;
        public void ModuleDisable() => _enabled = false;

        private void Awake()
        {
            _motor = GetComponent<RigidbodyMotor>();
        }

        /// <summary>
        /// Call from FixedUpdate.
        /// </summary>
        public void Tick(float dt)
        {
            if (!_enabled) return;

            Debug.Log("Grounded: " + _motor.IsGrounded);

            if (_motor.IsGrounded)
                _lastGroundedTime = Time.time;

            bool canCoyote = (Time.time - _lastGroundedTime) <= coyoteTime;
            bool buffered = (Time.time - _lastJumpPressedTime) <= jumpBuffer;

            // Compute jump speed from Physics.gravity (Unity gravity)
            float g = Mathf.Abs(Physics.gravity.y);
            float jumpSpeed = Mathf.Sqrt(2f * g * Mathf.Max(0.01f, jumpHeight));

            if (buffered && (_motor.IsGrounded || canCoyote))
            {
                _motor.Jump(jumpSpeed);

                // consume
                _lastJumpPressedTime = -999f;
                _lastGroundedTime = -999f;
                return;
            }

            // Ground stick: only if grounded and not moving up
            if (_motor.IsGrounded)
            {
                float vy = _motor.GetVerticalVelocity();
                if (vy <= 0.05f)
                    _motor.Jump(groundedStickVelocity);
            }
        }
    }
}