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
        [SerializeField] private float groundedStickVelocity = -2.0f;
        [SerializeField] private float groundStickMaxAngleFromUp = 25f;

        private RigidbodyMotor _motor;

        private float _lastGroundedTime = -999f;
        private float _lastJumpPressedTime = -999f;

        private bool _enabled;

        public void SetJumpPressedThisFrame(bool pressed)
        {
            if (!pressed) return;
            _lastJumpPressedTime = Time.time;
        }

        public void ModuleEnable() => _enabled = true;
        public void ModuleDisable() => _enabled = false;

        private void Awake()
        {
            _motor = GetComponent<RigidbodyMotor>();
        }

        public void Tick(float dt)
        {
            if (!_enabled) return;

            if (_motor.IsGrounded)
                _lastGroundedTime = Time.time;

            bool canCoyote = (Time.time - _lastGroundedTime) <= coyoteTime;
            bool buffered = (Time.time - _lastJumpPressedTime) <= jumpBuffer;

            float g = Mathf.Abs(Physics.gravity.y);
            float jumpSpeed = Mathf.Sqrt(2f * g * Mathf.Max(0.01f, jumpHeight));

            if (buffered && (_motor.IsGrounded || canCoyote))
            {
                _motor.Jump(jumpSpeed);
                _lastJumpPressedTime = -999f;
                _lastGroundedTime = -999f;
                return;
            }

            // ground stick only on real ground plane
            if (_motor.IsGrounded)
            {
                float angle = Vector3.Angle(_motor.MovementPlaneNormal, Vector3.up);
                if (angle <= groundStickMaxAngleFromUp)
                {
                    float vy = _motor.GetVerticalVelocity();
                    if (vy <= 0.05f)
                        _motor.Jump(groundedStickVelocity);
                }
            }
        }
    }
}