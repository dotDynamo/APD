using UnityEngine;
using _Project.Code.Core.Interfaces;
using _Project.Code.Player.Utils;

namespace _Project.Code.Player.Modules
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterMotor))]
    public sealed class JumpModule : MonoBehaviour, IPlayerModule
    {
        [Header("Jump & Gravity")]
        [SerializeField] private float gravity = -20f;
        [SerializeField] private float jumpHeight = 1.2f;
        [SerializeField] private float groundedStickForce = -2.0f;

        [Header("Feel")]
        [SerializeField] private float coyoteTime = 0.10f;
        [SerializeField] private float jumpBuffer = 0.10f;

        private CharacterMotor _motor;

        private bool _jumpPressedThisFrame;
        private float _lastGroundedTime = -999f;
        private float _lastJumpPressedTime = -999f;

        private float _verticalVelocity;
        private bool _enabled;

        public void SetJumpPressedThisFrame(bool pressed)
        {
            _jumpPressedThisFrame = pressed;
            if (pressed) _lastJumpPressedTime = Time.time;
        }

        public void ModuleEnable() => _enabled = true;
        public void ModuleDisable() => _enabled = false;

        private void Awake()
        {
            _motor = GetComponent<CharacterMotor>();
            if (_motor == null) _motor = gameObject.AddComponent<CharacterMotor>();
        }

        public void Tick(float dt)
        {
            if (!_enabled) return;

            HandleGrounding();
            HandleJumpAndGravity(dt);

            _motor.SetVerticalVelocity(_verticalVelocity);
            _jumpPressedThisFrame = false;
        }

        private void HandleGrounding()
        {
            if (_motor.IsGrounded)
            {
                _lastGroundedTime = Time.time;

                if (_verticalVelocity < 0f)
                    _verticalVelocity = groundedStickForce;
            }
        }

        private void HandleJumpAndGravity(float dt)
        {
            bool canCoyote = (Time.time - _lastGroundedTime) <= coyoteTime;
            bool buffered = (Time.time - _lastJumpPressedTime) <= jumpBuffer;

            if (buffered && (_motor.IsGrounded || canCoyote))
            {
                _verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
                _lastJumpPressedTime = -999f;
                _lastGroundedTime = -999f;
            }

            _verticalVelocity += gravity * dt;
        }
    }
}