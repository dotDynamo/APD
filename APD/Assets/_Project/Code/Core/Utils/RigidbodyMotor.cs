using UnityEngine;

namespace _Project.Code.Player.Utils
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class RigidbodyMotor : MonoBehaviour
    {
        public enum MoveSpaceMode { CameraRelative, SurfaceStable }

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

        [Header("Move Space")]
        [SerializeField] private MoveSpaceMode moveSpaceMode = MoveSpaceMode.CameraRelative;

        [Header("Debug")]
        [SerializeField] private bool debugGrounding = true;
        [SerializeField] private bool debugMovementPlane = true;

        private Vector3 _targetPlanarVelocity;
        private bool _isGrounded;
        public bool IsGrounded => _isGrounded;

        private bool _groundedOverrideActive;
        private bool _groundedOverrideValue;

        private Collider _selfCollider;

        private bool _hasDesiredRotation;
        private Quaternion _desiredRotation;

        private bool _groundHitMask;
        private bool _groundHitFallback;
        private RaycastHit _groundHitInfo;

        // ✅ Plane-based movement (Mario Galaxy)
        private Vector3 _movementPlaneNormal = Vector3.up;
        public Vector3 MovementPlaneNormal => _movementPlaneNormal;

        // ✅ Stable frame when wall-walking
        private Vector3 _stableForwardOnPlane = Vector3.forward;

        public Quaternion CurrentRotation => rb != null ? rb.rotation : transform.rotation;
        public Transform CameraTransform => cameraTransform;

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

            rb.constraints = RigidbodyConstraints.FreezeRotation;

            rb.linearDamping = 0f;
            rb.angularDamping = 0f;
            rb.sleepThreshold = 0f;
            rb.maxDepenetrationVelocity = maxDepenetrationVelocity;

            rb.useGravity = true;
            rb.isKinematic = false;

            _movementPlaneNormal = Vector3.up;
            _targetPlanarVelocity = Vector3.zero;
        }

        public void SetCamera(Transform cam) => cameraTransform = cam;

        public void SetDesiredRotation(Quaternion rotation)
        {
            _desiredRotation = rotation;
            _hasDesiredRotation = true;
        }

        // ======================================================
        // ✅ Plane API
        // ======================================================
        public void SetMovementPlaneNormal(Vector3 normal)
        {
            if (normal.sqrMagnitude < 0.0001f) return;
            _movementPlaneNormal = normal.normalized;
        }

        public void ResetMovementPlaneNormal()
        {
            _movementPlaneNormal = Vector3.up;
        }

        // ======================================================
        // ✅ Grounded Override (for wall-walk feel)
        // ======================================================
        public void SetGroundedOverride(bool active, bool value)
        {
            _groundedOverrideActive = active;
            _groundedOverrideValue = value;
        }

        private bool EffectiveGrounded => _groundedOverrideActive ? _groundedOverrideValue : _isGrounded;

        // ======================================================
        // ✅ Move Space (camera-relative vs stable on surface)
        // ======================================================
        public void SetMoveSpaceMode(MoveSpaceMode mode) => moveSpaceMode = mode;

        public void SetStableForwardOnPlane(Vector3 forwardOnPlane)
        {
            forwardOnPlane = Vector3.ProjectOnPlane(forwardOnPlane, _movementPlaneNormal);
            if (forwardOnPlane.sqrMagnitude < 0.0001f) return;
            _stableForwardOnPlane = forwardOnPlane.normalized;
        }

        private void FixedUpdate()
        {
            UpdateGrounded();

            ApplyRotation(Time.fixedDeltaTime);
            ApplyPlanarVelocity(Time.fixedDeltaTime);
        }

        private void ApplyRotation(float fixedDt)
        {
            if (!_hasDesiredRotation) return;

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

            if (hit && _selfCollider != null && rhFinal.collider == _selfCollider)
                hit = false;

            _isGrounded = hit;
            _groundHitInfo = rhFinal;

            if (debugGrounding && Application.isPlaying && !_groundHitMask && _groundHitFallback)
            {
                Debug.LogWarning(
                    $"[RigidbodyMotor][Grounding] groundMask MISS, fallback HIT -> " +
                    $"Hit '{_groundHitInfo.collider.name}' on Layer '{LayerMask.LayerToName(_groundHitInfo.collider.gameObject.layer)}'.",
                    this
                );
            }
        }

        // ======================================================
        // API expected by your Modules
        // ======================================================

        /// <summary>
        /// Camera-relative direction projected onto CURRENT movement plane,
        /// OR stable surface frame when wall-walking.
        /// </summary>
        public Vector3 GetCameraRelativeDirection(Vector2 move)
        {
            if (move.sqrMagnitude < 0.0001f) return Vector3.zero;

            Vector3 forward, right;

            if (moveSpaceMode == MoveSpaceMode.SurfaceStable)
            {
                forward = _stableForwardOnPlane;
                right = Vector3.Cross(_movementPlaneNormal, forward);
                if (right.sqrMagnitude < 0.0001f) right = Vector3.right;
                right.Normalize();
            }
            else
            {
                forward = cameraTransform != null ? cameraTransform.forward : transform.forward;
                right = cameraTransform != null ? cameraTransform.right : transform.right;

                forward = Vector3.ProjectOnPlane(forward, _movementPlaneNormal);
                right = Vector3.ProjectOnPlane(right, _movementPlaneNormal);

                if (forward.sqrMagnitude > 0.0001f) forward.Normalize();
                if (right.sqrMagnitude > 0.0001f) right.Normalize();
            }

            Vector3 world = (right * move.x) + (forward * move.y);
            return world.sqrMagnitude > 1f ? world.normalized : world;
        }

        public void Move(Vector3 desiredVelocity, float dt)
        {
            _targetPlanarVelocity = Vector3.ProjectOnPlane(desiredVelocity, _movementPlaneNormal);
        }

        public float GetVerticalVelocity() => GetVelocity().y;

        /// <summary>WORLD Y jump.</summary>
        public void Jump(float jumpSpeed)
        {
            Vector3 v = GetVelocity();
            if (v.y < 0f) v.y = 0f;
            v.y = jumpSpeed;
            SetVelocity(v);
        }

        // ======================================================
        // Movement core (plane-based)
        // ======================================================
        private void ApplyPlanarVelocity(float fixedDt)
        {
            Vector3 v = GetVelocity();
            Vector3 n = _movementPlaneNormal;

            Vector3 vN = n * Vector3.Dot(v, n);
            Vector3 vP = v - vN;

            Vector3 targetP = _targetPlanarVelocity;
            bool hasInput = targetP.sqrMagnitude > 0.0001f;

            float accel = EffectiveGrounded ? maxAccelGround : maxAccelAir;
            float brake = EffectiveGrounded ? brakeGround : brakeAir;

            Vector3 newP = hasInput
                ? Vector3.MoveTowards(vP, targetP, accel * fixedDt)
                : Vector3.MoveTowards(vP, Vector3.zero, brake * fixedDt);

            SetVelocity(newP + vN);
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
            if (rb == null) rb = GetComponent<Rigidbody>();

            if (debugGrounding)
            {
                Vector3 origin = (rb != null ? rb.position : transform.position) + Vector3.up * groundCheckOffset;
                Vector3 end = origin + Vector3.down * groundCheckDistance;

                if (_groundHitMask) Gizmos.color = Color.green;
                else if (_groundHitFallback) Gizmos.color = Color.yellow;
                else Gizmos.color = Color.red;

                Gizmos.DrawWireSphere(origin, groundCheckRadius);
                Gizmos.DrawWireSphere(end, groundCheckRadius);
                Gizmos.DrawLine(origin, end);

                if (_isGrounded)
                {
                    Gizmos.color = Color.cyan;
                    Gizmos.DrawSphere(_groundHitInfo.point, 0.03f);
                    Gizmos.color = Color.blue;
                    Gizmos.DrawLine(_groundHitInfo.point, _groundHitInfo.point + _groundHitInfo.normal * 0.35f);
                }
            }

            if (debugMovementPlane && rb != null)
            {
                Gizmos.color = Color.magenta;
                Vector3 p = rb.position + Vector3.up * 0.2f;
                Gizmos.DrawLine(p, p + _movementPlaneNormal * 0.7f);

                Gizmos.color = Color.white;
                Gizmos.DrawLine(p, p + _stableForwardOnPlane * 0.7f);
            }
        }
#endif
    }
}