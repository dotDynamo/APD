using UnityEngine;

namespace _Project.Code.Player.Utils
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterController))]
    public sealed class CharacterMotor : MonoBehaviour
    {
        [SerializeField] private Transform cameraTransform;

        private CharacterController _cc;

        public float VerticalVelocity { get; private set; }
        public bool IsGrounded => _cc != null && _cc.isGrounded;

        public Transform CameraTransform => cameraTransform;

        private void Awake()
        {
            _cc = GetComponent<CharacterController>();
            if (_cc == null) _cc = gameObject.AddComponent<CharacterController>();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (GetComponent<CharacterController>() == null)
                gameObject.AddComponent<CharacterController>();
        }
#endif

        public void SetCamera(Transform cam) => cameraTransform = cam;
        public void SetVerticalVelocity(float value) => VerticalVelocity = value;

        /// <summary>
        /// ÚNICO punto donde se llama CharacterController.Move() por frame.
        /// </summary>
        public void Move(Vector3 horizontalVelocity, float dt)
        {
            Vector3 velocity = horizontalVelocity + Vector3.up * VerticalVelocity;
            _cc.Move(velocity * dt);
        }

        public Vector3 GetCameraRelativeDirection(Vector2 moveInput)
        {
            if (cameraTransform == null) return Vector3.zero;

            Vector3 inputDir = new Vector3(moveInput.x, 0f, moveInput.y);
            if (inputDir.sqrMagnitude < 0.0001f) return Vector3.zero;

            Vector3 camForward = cameraTransform.forward;
            Vector3 camRight = cameraTransform.right;

            camForward.y = 0f;
            camRight.y = 0f;
            camForward.Normalize();
            camRight.Normalize();

            Vector3 dir = (camForward * inputDir.z + camRight * inputDir.x);
            return dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector3.zero;
        }
    }
}