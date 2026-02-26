using UnityEngine;
using _Project.Code.Core.Interfaces;
using _Project.Code.Player.Utils;

namespace _Project.Code.Player.Modules
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RigidbodyMotor))]
    public sealed class MovementModule : MonoBehaviour, IPlayerModule
    {
        [Header("Movement")]
        [SerializeField] private float walkSpeed = 10f;
        [SerializeField] private float sprintSpeed = 33f;

        [Header("Turning")]
        [Tooltip("Smaller = snappier rotation")]
        [SerializeField] private float rotationSmoothTime = 0.08f;

        private RigidbodyMotor _motor;

        private Vector2 _move;
        private bool _sprintHeld;

        private float _turnSmoothVelocity;
        private bool _enabled;

        public void SetInput(Vector2 move, bool sprintHeld)
        {
            _move = move;
            _sprintHeld = sprintHeld;
        }

        public void ModuleEnable() => _enabled = true;
        public void ModuleDisable() => _enabled = false;

        private void Awake()
        {
            _motor = GetComponent<RigidbodyMotor>();
        }

        /// <summary>Call from FixedUpdate.</summary>
        public void Tick(float dt)
        {
            if (!_enabled) return;

            Vector3 moveDir = _motor.GetCameraRelativeDirection(_move);
            float magnitude = Mathf.Clamp01(_move.magnitude);

            if (moveDir.sqrMagnitude < 0.0001f || magnitude < 0.01f)
            {
                _motor.Move(Vector3.zero, dt);
                return;
            }

            float speed = _sprintHeld ? sprintSpeed : walkSpeed;
            Vector3 planarVelocity = moveDir * (speed * magnitude);

            // Use Rigidbody rotation as the "current yaw" to prevent oscillation/shake
            float currentYaw = _motor.CurrentRotation.eulerAngles.y;
            float targetYaw = Mathf.Atan2(moveDir.x, moveDir.z) * Mathf.Rad2Deg;

            float smoothYaw = Mathf.SmoothDampAngle(
                currentYaw,
                targetYaw,
                ref _turnSmoothVelocity,
                rotationSmoothTime,
                Mathf.Infinity,
                dt
            );

            _motor.SetDesiredRotation(Quaternion.Euler(0f, smoothYaw, 0f));
            _motor.Move(planarVelocity, dt);
        }
    }
}