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
        [Tooltip("Bigger = snappier rotation")]
        [SerializeField] private float rotationSharpness = 18f;

        private RigidbodyMotor _motor;
        private Vector2 _move;
        private bool _sprintHeld;
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
            Vector3 desiredVelocity = moveDir * (speed * magnitude);

            Vector3 up = _motor.MovementPlaneNormal;
            Vector3 forward = Vector3.ProjectOnPlane(moveDir, up);

            if (forward.sqrMagnitude < 0.0001f)
                forward = Vector3.ProjectOnPlane(_motor.CurrentRotation * Vector3.forward, up);

            if (forward.sqrMagnitude > 0.0001f)
            {
                forward.Normalize();
                Quaternion target = Quaternion.LookRotation(forward, up);

                float t = 1f - Mathf.Exp(-rotationSharpness * dt);
                Quaternion blended = Quaternion.Slerp(_motor.CurrentRotation, target, t);
                _motor.SetDesiredRotation(blended);
            }

            _motor.Move(desiredVelocity, dt);
        }
    }
}