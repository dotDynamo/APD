using UnityEngine;

namespace _Project.Code.Player.Utils
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class RigidbodyMotor : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Rigidbody rb;
        [SerializeField] private Transform cameraTransform;

        [Header("Grounding")]
        [SerializeField] private LayerMask groundMask = ~0;
        [SerializeField] private float groundCheckDistance = 0.25f;
        [SerializeField] private float groundCheckRadius = 0.25f;
        [SerializeField] private float groundCheckOffset = 0.10f;

        [Header("Movement Acceleration")]
        [SerializeField] private float maxAccelGround = 65f;
        [SerializeField] private float maxAccelAir = 22f;
        [SerializeField] private float brakeGround = 95f;
        [SerializeField] private float brakeAir = 12f;

        [Header("Rotation")]
        [Tooltip("Bigger = snappier rotation.")]
        [SerializeField] private float rotationSharpness = 18f;

        [Header("Physics Quality")]
        [SerializeField] private float maxDepenetrationVelocity = 12f;

        [Header("Debug")]
        [SerializeField] private bool debugGrounding = true;

        private Vector3 _targetHorizontalVelocity; // world XZ
        private bool _isGrounded;
        public bool IsGrounded => _isGrounded;

        private Collider _selfCollider;

        // Rotation target
        private bool _hasDesiredRotation;
        private Quaternion _desiredRotation;

        // Debug state for grounding
        private bool _groundHitMask;
        private bool _groundHitFallback;
        private RaycastHit _groundHitInfo;

        /// <summary>Use RB rotation (prevents jitter vs transform.eulerAngles).</summary>
        public Quaternion CurrentRotation => rb != null ? rb.rotation : transform.rotation;

        private void Reset()
        {
            rb = GetComponent<Rigidbody>();
        }

        private void Awake()
        {
            if (rb == null) rb = GetComponent<Rigidbody>();
            _selfCollider = GetComponent<Collider>();

            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            // prevent physics from tipping us
            rb.constraints = RigidbodyConstraints.FreezeRotation;

            // Unity 6 uses linearDamping / angularDamping
            rb.linearDamping = 0f;
            rb.angularDamping = 0f;
            rb.sleepThreshold = 0f;
            rb.maxDepenetrationVelocity = maxDepenetrationVelocity;

            rb.useGravity = true;
            rb.isKinematic = false;
        }

        public void SetCamera(Transform cam) => cameraTransform = cam;

        public void SetDesiredRotation(Quaternion rotation)
        {
            _desiredRotation = rotation;
            _hasDesiredRotation = true;
        }

        private void FixedUpdate()
        {
            UpdateGrounded();

            ApplyRotation(Time.fixedDeltaTime);
            ApplyHorizontalVelocity(Time.fixedDeltaTime);
        }

        private void ApplyRotation(float fixedDt)
        {
            if (!_hasDesiredRotation) return;

            // Smooth toward desired rotation
            float t = 1f - Mathf.Exp(-rotationSharpness * fixedDt);
            Quaternion newRot = Quaternion.Slerp(rb.rotation, _desiredRotation, t);
            rb.MoveRotation(newRot);
        }

        private void UpdateGrounded()
        {
            Vector3 origin = rb.position + Vector3.up * groundCheckOffset;

            _groundHitMask = Physics.SphereCast(
                origin,
                groundCheckRadius,
                Vector3.down,
                out RaycastHit rhMask,
                groundCheckDistance,
                groundMask,
                QueryTriggerInteraction.Ignore
            );

            _groundHitFallback = false;
            RaycastHit rhFinal = rhMask;

            // If mask misses, try Everything (fallback)
            if (!_groundHitMask)
            {
                _groundHitFallback = Physics.SphereCast(
                    origin,
                    groundCheckRadius,
                    Vector3.down,
                    out RaycastHit rhAll,
                    groundCheckDistance,
                    ~0,
                    QueryTriggerInteraction.Ignore
                );

                if (_groundHitFallback)
                    rhFinal = rhAll;
            }

            bool hit = _groundHitMask || _groundHitFallback;

            // Ignore our own collider if it ever gets hit
            if (hit && _selfCollider != null && rhFinal.collider == _selfCollider)
                hit = false;

            _isGrounded = hit;
            _groundHitInfo = rhFinal;

            // Optional console warning: only when mask misses but fallback hits
            if (debugGrounding && Application.isPlaying && !_groundHitMask && _groundHitFallback)
            {
                Debug.LogWarning(
                    $"[RigidbodyMotor][Grounding] groundMask MISS, fallback HIT -> " +
                    $"Hit '{_groundHitInfo.collider.name}' on Layer '{LayerMask.LayerToName(_groundHitInfo.collider.gameObject.layer)}'. " +
                    $"Fix: put ground on the Ground layer OR include its layer in groundMask.",
                    this
                );
            }
        }

        // ======================================================
        // API expected by your Modules
        // ======================================================

        public Vector3 GetCameraRelativeDirection(Vector2 move)
        {
            if (move.sqrMagnitude < 0.0001f) return Vector3.zero;

            Vector3 forward = Vector3.forward;
            Vector3 right = Vector3.right;

            if (cameraTransform != null)
            {
                forward = cameraTransform.forward;
                right = cameraTransform.right;

                forward.y = 0f;
                right.y = 0f;

                if (forward.sqrMagnitude > 0.0001f) forward.Normalize();
                if (right.sqrMagnitude > 0.0001f) right.Normalize();
            }

            Vector3 world = (right * move.x) + (forward * move.y);
            return world.sqrMagnitude > 1f ? world.normalized : world;
        }

        public void Move(Vector3 horizontalVelocity, float dt)
        {
            _targetHorizontalVelocity = new Vector3(horizontalVelocity.x, 0f, horizontalVelocity.z);
        }

        public float GetVerticalVelocity()
        {
            return GetVelocity().y;
        }

        public void Jump(float jumpSpeed)
        {
            Vector3 v = GetVelocity();
            if (v.y < 0f) v.y = 0f;
            v.y = jumpSpeed;
            SetVelocity(v);
        }

        // ======================================================
        // Movement core
        // ======================================================

        private void ApplyHorizontalVelocity(float fixedDt)
        {
            Vector3 v = GetVelocity();
            Vector3 planar = new Vector3(v.x, 0f, v.z);

            Vector3 target = new Vector3(_targetHorizontalVelocity.x, 0f, _targetHorizontalVelocity.z);
            bool hasInput = target.sqrMagnitude > 0.0001f;

            float accel = _isGrounded ? maxAccelGround : maxAccelAir;
            float brake = _isGrounded ? brakeGround : brakeAir;

            Vector3 newPlanar = hasInput
                ? Vector3.MoveTowards(planar, target, accel * fixedDt)
                : Vector3.MoveTowards(planar, Vector3.zero, brake * fixedDt);

            SetVelocity(new Vector3(newPlanar.x, v.y, newPlanar.z));
        }

        private Vector3 GetVelocity()
        {
#if UNITY_6000_0_OR_NEWER
            return rb.linearVelocity;
#else
            return rb.velocity;
#endif
        }

        private void SetVelocity(Vector3 v)
        {
#if UNITY_6000_0_OR_NEWER
            rb.linearVelocity = v;
#else
            rb.velocity = v;
#endif
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (!debugGrounding) return;
            if (rb == null) rb = GetComponent<Rigidbody>();

            Vector3 origin = (rb != null ? rb.position : transform.position) + Vector3.up * groundCheckOffset;
            Vector3 end = origin + Vector3.down * groundCheckDistance;

            // Green = mask hit, Yellow = fallback hit, Red = no hit
            if (_groundHitMask) Gizmos.color = Color.green;
            else if (_groundHitFallback) Gizmos.color = Color.yellow;
            else Gizmos.color = Color.red;

            // Draw start/end spheres (cast volume) + line
            Gizmos.DrawWireSphere(origin, groundCheckRadius);
            Gizmos.DrawWireSphere(end, groundCheckRadius);
            Gizmos.DrawLine(origin, end);

            // Hit point + normal
            if (_isGrounded)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawSphere(_groundHitInfo.point, 0.03f);

                Gizmos.color = Color.blue;
                Gizmos.DrawLine(_groundHitInfo.point, _groundHitInfo.point + _groundHitInfo.normal * 0.35f);
            }
        }
#endif
    }
}