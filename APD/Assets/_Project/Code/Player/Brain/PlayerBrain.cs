using UnityEngine;
using UnityEngine.InputSystem;
using _Project.Code.Player.Modules;
using _Project.Code.Player.Utils;

namespace _Project.Code.Player.Brain
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerInput))]
    [RequireComponent(typeof(CharacterMotor))]
    [RequireComponent(typeof(MovementModule))]
    [RequireComponent(typeof(JumpModule))]
    public sealed class PlayerBrain : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerInput playerInput;
        [SerializeField] private CharacterMotor motor;
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
            motor = GetComponent<CharacterMotor>();
            movementModule = GetComponent<MovementModule>();
            jumpModule = GetComponent<JumpModule>();
        }

        private void Awake()
        {
            if (playerInput == null) playerInput = GetComponent<PlayerInput>();
            if (motor == null) motor = GetComponent<CharacterMotor>();
            if (movementModule == null) movementModule = GetComponent<MovementModule>();
            if (jumpModule == null) jumpModule = GetComponent<JumpModule>();

            // Asegura mapa correcto
            if (!string.IsNullOrEmpty(playerInput.defaultActionMap))
                playerInput.SwitchCurrentActionMap(playerInput.defaultActionMap);
            else
                playerInput.SwitchCurrentActionMap("Player");

            // Asegura actions habilitadas
            playerInput.actions?.Enable();

            // Cache actions
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
            if (GetComponent<CharacterController>() == null) gameObject.AddComponent<CharacterController>();
            if (GetComponent<CharacterMotor>() == null) gameObject.AddComponent<CharacterMotor>();
            if (GetComponent<MovementModule>() == null) gameObject.AddComponent<MovementModule>();
            if (GetComponent<JumpModule>() == null) gameObject.AddComponent<JumpModule>();
        }
#endif

        private void OnEnable()
        {
            // Cámara fallback
            if (cameraTransform == null && Camera.main != null)
                cameraTransform = Camera.main.transform;

            if (cameraTransform != null)
                motor.SetCamera(cameraTransform);

            // Habilita módulos
            jumpModule.ModuleEnable();
            movementModule.ModuleEnable();

            // Suscripción directa a acciones (funciona aunque PlayerInput esté en Send Messages)
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
            // Empuja input a módulos
            jumpModule.SetJumpPressedThisFrame(_jumpPressedThisFrame);
            movementModule.SetInput(_move, _sprintHeld);

            float dt = Time.deltaTime;

            // Orden: primero vertical, luego Move()
            jumpModule.Tick(dt);
            movementModule.Tick(dt);

            _jumpPressedThisFrame = false;
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