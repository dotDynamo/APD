using UnityEngine;
using UnityEngine.InputSystem;
using _Project.Code.Player.Modules;
using _Project.Code.Player.Utils;

namespace _Project.Code.Player.Brain
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerInput))]
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(RigidbodyMotor))]
    [RequireComponent(typeof(MovementModule))]
    [RequireComponent(typeof(JumpModule))]
    public sealed class PlayerBrain : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerInput playerInput;
        [SerializeField] private RigidbodyMotor motor;
        [SerializeField] private MovementModule movementModule;
        [SerializeField] private JumpModule jumpModule;

        [Header("Camera")]
        [Tooltip("Si no asignas nada, usará Camera.main automáticamente.")]
        [SerializeField] private Transform cameraTransform;

        // input cached
        private Vector2 _move;
        private bool _sprintHeld;
        private bool _jumpPressedThisFrame;

        // Actions
        private InputAction _moveAction;
        private InputAction _sprintAction;
        private InputAction _jumpAction;

        private void Reset()
        {
            playerInput = GetComponent<PlayerInput>();
            motor = GetComponent<RigidbodyMotor>();
            movementModule = GetComponent<MovementModule>();
            jumpModule = GetComponent<JumpModule>();
        }

        private void Awake()
        {
            if (playerInput == null) playerInput = GetComponent<PlayerInput>();
            if (motor == null) motor = GetComponent<RigidbodyMotor>();
            if (movementModule == null) movementModule = GetComponent<MovementModule>();
            if (jumpModule == null) jumpModule = GetComponent<JumpModule>();

            if (!string.IsNullOrEmpty(playerInput.defaultActionMap))
                playerInput.SwitchCurrentActionMap(playerInput.defaultActionMap);
            else
                playerInput.SwitchCurrentActionMap("Player");

            playerInput.actions?.Enable();

            _moveAction = playerInput.actions.FindAction("Move", throwIfNotFound: false);
            _sprintAction = playerInput.actions.FindAction("Sprint", throwIfNotFound: false);
            _jumpAction = playerInput.actions.FindAction("Jump", throwIfNotFound: false);

            if (_moveAction == null) Debug.LogError("PlayerBrain: Action 'Move' no encontrada.", this);
            if (_sprintAction == null) Debug.LogError("PlayerBrain: Action 'Sprint' no encontrada.", this);
            if (_jumpAction == null) Debug.LogError("PlayerBrain: Action 'Jump' no encontrada.", this);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (GetComponent<PlayerInput>() == null) gameObject.AddComponent<PlayerInput>();
            if (GetComponent<Rigidbody>() == null) gameObject.AddComponent<Rigidbody>();
            if (GetComponent<RigidbodyMotor>() == null) gameObject.AddComponent<RigidbodyMotor>();
            if (GetComponent<MovementModule>() == null) gameObject.AddComponent<MovementModule>();
            if (GetComponent<JumpModule>() == null) gameObject.AddComponent<JumpModule>();
        }
#endif

        private void OnEnable()
        {
            if (cameraTransform == null && Camera.main != null)
                cameraTransform = Camera.main.transform;

            if (cameraTransform != null)
                motor.SetCamera(cameraTransform);

            jumpModule.ModuleEnable();
            movementModule.ModuleEnable();

            if (_moveAction != null)
            {
                _moveAction.performed += OnMove;
                _moveAction.canceled += OnMoveCanceled;
            }

            if (_sprintAction != null)
            {
                _sprintAction.performed += OnSprintPerformed;
                _sprintAction.canceled += OnSprintCanceled;
            }

            if (_jumpAction != null)
                _jumpAction.performed += OnJumpPerformed;
        }

        private void OnDisable()
        {
            if (_moveAction != null)
            {
                _moveAction.performed -= OnMove;
                _moveAction.canceled -= OnMoveCanceled;
            }

            if (_sprintAction != null)
            {
                _sprintAction.performed -= OnSprintPerformed;
                _sprintAction.canceled -= OnSprintCanceled;
            }

            if (_jumpAction != null)
                _jumpAction.performed -= OnJumpPerformed;

            movementModule.ModuleDisable();
            jumpModule.ModuleDisable();
        }

        private void Update()
        {
            // Only cache input + edge triggers in Update
            movementModule.SetInput(_move, _sprintHeld);
            jumpModule.SetJumpPressedThisFrame(_jumpPressedThisFrame);

            // consume edge
            _jumpPressedThisFrame = false;
        }

        private void FixedUpdate()
        {
            // Apply movement/jump in FixedUpdate (Rigidbody timebase)
            float dt = Time.fixedDeltaTime;

            // Order: vertical first, then planar (consistent)
            jumpModule.Tick(dt);
            movementModule.Tick(dt);
        }

        private void OnMove(InputAction.CallbackContext ctx) => _move = ctx.ReadValue<Vector2>();
        private void OnMoveCanceled(InputAction.CallbackContext ctx) => _move = Vector2.zero;

        private void OnSprintPerformed(InputAction.CallbackContext ctx) => _sprintHeld = true;
        private void OnSprintCanceled(InputAction.CallbackContext ctx) => _sprintHeld = false;

        private void OnJumpPerformed(InputAction.CallbackContext ctx) => _jumpPressedThisFrame = true;

        public void SetCameraTransform(Transform cam)
        {
            cameraTransform = cam;
            motor.SetCamera(cam);
        }
    }
}