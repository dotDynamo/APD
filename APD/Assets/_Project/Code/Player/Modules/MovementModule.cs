using UnityEngine;
using _Project.Code.Core.Interfaces;
using _Project.Code.Player.Utils;

namespace _Project.Code.Player.Modules
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterMotor))]
    public sealed class MovementModule : MonoBehaviour, IPlayerModule
    {
        [Header("Movement")]
        [SerializeField] private float walkSpeed = 4.5f;
        [SerializeField] private float sprintSpeed = 7.0f;
        [SerializeField] private float rotationSmoothTime = 0.08f;

        private CharacterMotor _motor;

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
            _motor = GetComponent<CharacterMotor>();
            if (_motor == null) _motor = gameObject.AddComponent<CharacterMotor>();
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

            float targetAngle = Mathf.Atan2(moveDir.x, moveDir.z) * Mathf.Rad2Deg;
            float angle = Mathf.SmoothDampAngle(
                transform.eulerAngles.y,
                targetAngle,
                ref _turnSmoothVelocity,
                rotationSmoothTime
            );
            transform.rotation = Quaternion.Euler(0f, angle, 0f);

            float speed = _sprintHeld ? sprintSpeed : walkSpeed;
            Vector3 horizontalVelocity = moveDir * (speed * magnitude);

            _motor.Move(horizontalVelocity, dt);
        }
    }
}