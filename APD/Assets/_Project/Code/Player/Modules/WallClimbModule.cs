using UnityEngine;
using _Project.Code.Core.Interfaces;
using _Project.Code.Player.Utils;

namespace _Project.Code.Player.Modules
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(RigidbodyMotor))]
    public sealed class WallClimbModule : MonoBehaviour, IPlayerModule
    {
        [Header("Climbable Surfaces")]
        [SerializeField] private LayerMask climbMask = ~0;
        [SerializeField] private float detectDistance = 0.9f;
        [SerializeField] private float detectRadius = 0.25f;

        [Tooltip("Ignore surfaces too close to floor. 0=floor, 90=perfect wall")]
        [Range(0f, 89f)]
        [SerializeField] private float minWallAngleFromUp = 35f;

        [Header("Stick + Damping")]
        [SerializeField] private float stickForce = 35f;
        [SerializeField] private float normalDamping = 8f;

        [Header("Alignment")]
        [SerializeField] private float alignSharpness = 18f;

        [Header("Wall Jump (WORLD Y)")]
        [SerializeField] private float wallJumpHeight = 1.2f;
        [SerializeField] private float detachGraceSeconds = 0.15f;

        [Header("Ledge Hop (top edge)")]
        [SerializeField] private bool enableLedgeHop = true;
        [SerializeField] private float ledgeCheckForward = 0.35f;
        [SerializeField] private float ledgeCheckDown = 0.85f;
        [SerializeField] private float ledgeHopUpSpeed = 3.5f;
        [SerializeField] private float ledgeHopForwardSpeed = 2.0f;
        [SerializeField] private float ledgeHopCooldown = 0.25f;

        [Header("Camera (recommended)")]
        [SerializeField] private Transform cameraTransform;

        [Header("Debug")]
        [SerializeField] private bool debugGizmos = true;
        [SerializeField] private bool debugLogs = false;
        [SerializeField] private float gizmoNormalLength = 0.5f;
        [SerializeField] private float gizmoVelocityScale = 0.05f;

        public bool IsClimbing => _isClimbing;

        private Rigidbody _rb;
        private RigidbodyMotor _motor;

        private bool _enabled;

        private Vector2 _move;
        private bool _jumpPressedThisFrame;

        private bool _isClimbing;
        private Vector3 _wallNormal = Vector3.up;
        private float _detachUntil;
        private float _ledgeHopUntil;

        // debug cache
        private bool _dbgAttachCastHit;
        private RaycastHit _dbgAttachHit;
        private Vector3 _dbgAttachOrigin;
        private Vector3 _dbgAttachDir;

        private bool _dbgRefreshCastHit;
        private RaycastHit _dbgRefreshHit;
        private Vector3 _dbgRefreshOrigin;
        private Vector3 _dbgRefreshDir;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _motor = GetComponent<RigidbodyMotor>();
        }

        public void SetCameraTransform(Transform cam) => cameraTransform = cam;

        public void SetInput(Vector2 move, bool sprintHeld, bool jumpPressedThisFrame)
        {
            _move = move;
            _jumpPressedThisFrame = jumpPressedThisFrame;
        }

        public void ModuleEnable()
        {
            _enabled = true;
            _isClimbing = false;
            _wallNormal = Vector3.up;
            _detachUntil = 0f;
            _ledgeHopUntil = 0f;
        }

        public void ModuleDisable()
        {
            _enabled = false;
            StopClimb();
        }

        public void Tick(float dt)
        {
            if (!_enabled) return;

            // If we recently ledge-hopped, don't reattach instantly
            if (Time.time < _ledgeHopUntil)
            {
                _jumpPressedThisFrame = false;
                return;
            }

            if (_isClimbing)
            {
                // keep contact
                if (!TryRefreshWall(out var hit))
                {
                    if (debugLogs) Debug.Log("[WallClimb] Lost wall -> StopClimb", this);
                    StopClimb();
                }
                else
                {
                    _wallNormal = hit.normal.normalized;

                    // plane follows wall
                    _motor.SetMovementPlaneNormal(_wallNormal);

                    // treat as grounded for movement feel
                    _motor.SetGroundedOverride(true, true);

                    ApplyAlignment(dt);
                    ApplyStickForces();

                    // ledge hop attempt
                    if (enableLedgeHop)
                        TryLedgeHop();

                    if (_jumpPressedThisFrame)
                        DoWallJump();
                }

                _jumpPressedThisFrame = false;
                return;
            }

            // Not climbing: respect detach grace after jump
            if (Time.time < _detachUntil)
            {
                _jumpPressedThisFrame = false;
                return;
            }

            // Try attach
            if (TryFindAttachableWall(out var wallHit))
            {
                if (debugLogs)
                {
                    float angle = Vector3.Angle(wallHit.normal, Vector3.up);
                    Debug.Log($"[WallClimb] Attach HIT={wallHit.collider.name} layer={LayerMask.LayerToName(wallHit.collider.gameObject.layer)} angle={angle:F1}", this);
                }

                StartClimb(wallHit.normal);
                ApplyAlignment(dt);
                ApplyStickForces();
            }

            _jumpPressedThisFrame = false;
        }

        // ------------------------------
        // Detection
        // ------------------------------
        private bool TryFindAttachableWall(out RaycastHit hit)
        {
            Vector3 origin = _rb.position + Vector3.up * 0.75f;

            Vector3 inputDir = _motor.GetCameraRelativeDirection(_move);
            Vector3 fwd = transform.forward;

            Vector3[] dirs =
            {
                (inputDir.sqrMagnitude > 0.0001f ? inputDir : Vector3.zero),
                fwd, -fwd,
                transform.right, -transform.right
            };

            for (int i = 0; i < dirs.Length; i++)
            {
                Vector3 dir = dirs[i];
                if (dir.sqrMagnitude < 0.0001f) continue;

                _dbgAttachOrigin = origin;
                _dbgAttachDir = dir.normalized;

                _dbgAttachCastHit = Physics.SphereCast(
                    origin,
                    detectRadius,
                    _dbgAttachDir,
                    out hit,
                    detectDistance,
                    climbMask,
                    QueryTriggerInteraction.Ignore
                );

                _dbgAttachHit = hit;

                if (!_dbgAttachCastHit) continue;

                float angle = Vector3.Angle(hit.normal, Vector3.up);
                if (angle < minWallAngleFromUp) continue;

                return true;
            }

            hit = default;
            return false;
        }

        private bool TryRefreshWall(out RaycastHit hit)
        {
            Vector3 origin = _rb.position + Vector3.up * 0.75f;
            Vector3 intoWall = -_wallNormal;

            _dbgRefreshOrigin = origin;
            _dbgRefreshDir = intoWall.normalized;

            _dbgRefreshCastHit = Physics.SphereCast(
                origin,
                detectRadius,
                _dbgRefreshDir,
                out hit,
                detectDistance,
                climbMask,
                QueryTriggerInteraction.Ignore
            );

            _dbgRefreshHit = hit;
            return _dbgRefreshCastHit;
        }

        // ------------------------------
        // State
        // ------------------------------
        private void StartClimb(Vector3 normal)
        {
            _isClimbing = true;
            _wallNormal = normal.normalized;

            _rb.useGravity = false;

            _motor.SetMovementPlaneNormal(_wallNormal);

            // ✅ freeze stable move frame (camera can move freely)
            Vector3 refForward = cameraTransform != null ? cameraTransform.forward : transform.forward;
            Vector3 forwardOnPlane = Vector3.ProjectOnPlane(refForward, _wallNormal);

            if (forwardOnPlane.sqrMagnitude < 0.0001f)
                forwardOnPlane = Vector3.ProjectOnPlane(transform.forward, _wallNormal);

            if (forwardOnPlane.sqrMagnitude < 0.0001f)
                forwardOnPlane = Vector3.Cross(_wallNormal, transform.right);

            _motor.SetStableForwardOnPlane(forwardOnPlane);
            _motor.SetMoveSpaceMode(RigidbodyMotor.MoveSpaceMode.SurfaceStable);

            // treat as grounded for movement
            _motor.SetGroundedOverride(true, true);
        }

        private void StopClimb()
        {
            if (!_isClimbing) return;

            _isClimbing = false;
            _wallNormal = Vector3.up;

            _rb.useGravity = true;

            _motor.ResetMovementPlaneNormal();
            _motor.SetMoveSpaceMode(RigidbodyMotor.MoveSpaceMode.CameraRelative);
            _motor.SetGroundedOverride(false, false);
        }

        // ------------------------------
        // Behavior
        // ------------------------------
        private void ApplyAlignment(float dt)
        {
            Vector3 up = _wallNormal;

            // ✅ Align to movement direction (stable), not to live camera forward
            Vector3 moveDir = _motor.GetCameraRelativeDirection(_move);
            Vector3 forwardOnPlane = Vector3.ProjectOnPlane(moveDir, up);

            if (forwardOnPlane.sqrMagnitude < 0.0001f)
                forwardOnPlane = Vector3.ProjectOnPlane(_motor.CurrentRotation * Vector3.forward, up);

            if (forwardOnPlane.sqrMagnitude < 0.0001f)
                return;

            forwardOnPlane.Normalize();

            Quaternion target = Quaternion.LookRotation(forwardOnPlane, up);

            float t = 1f - Mathf.Exp(-alignSharpness * dt);
            Quaternion blended = Quaternion.Slerp(_motor.CurrentRotation, target, t);
            _motor.SetDesiredRotation(blended);
        }

        private void ApplyStickForces()
        {
            // glue to wall
            _rb.AddForce(-_wallNormal * stickForce, ForceMode.Acceleration);

            // damp normal velocity
            Vector3 v = GetVelocity();
            float vn = Vector3.Dot(v, _wallNormal);
            _rb.AddForce(_wallNormal * (-vn * normalDamping), ForceMode.Acceleration);
        }

        private void DoWallJump()
        {
            StopClimb();
            _detachUntil = Time.time + detachGraceSeconds;

            float g = Mathf.Abs(Physics.gravity.y);
            float jumpSpeed = Mathf.Sqrt(2f * g * Mathf.Max(0.01f, wallJumpHeight));

            _motor.Jump(jumpSpeed); // WORLD Y
        }

        // ------------------------------
        // Ledge hop (top edge)
        // ------------------------------
        private void TryLedgeHop()
        {
            // need intent "up" on the wall
            // With SurfaceStable, move.y is "forward along stable forward", not necessarily wall-up.
            // We'll interpret ledge hop as: player pushing forward (move.y > 0) while wallUp points toward a top surface.
            if (_move.y < 0.6f) return;

            // wallUp: direction you "climb"
            Vector3 wallUp = Vector3.ProjectOnPlane(Vector3.up, _wallNormal);
            if (wallUp.sqrMagnitude < 0.0001f)
                wallUp = Vector3.ProjectOnPlane((_motor.CurrentRotation * Vector3.forward), _wallNormal);
            if (wallUp.sqrMagnitude < 0.0001f)
                return;

            wallUp.Normalize();

            // Check if wall continues slightly above
            Vector3 chest = _rb.position + Vector3.up * 1.0f;
            bool wallStillThere = Physics.SphereCast(
                chest,
                detectRadius,
                -_wallNormal,
                out _,
                detectDistance * 0.75f,
                climbMask,
                QueryTriggerInteraction.Ignore
            );

            // If wall still there, don't hop
            if (wallStillThere) return;

            // Check for ground/platform above the lip
            Vector3 probe = chest + wallUp * ledgeCheckForward;
            bool foundTop = Physics.Raycast(
                probe,
                Vector3.down,
                out RaycastHit topHit,
                ledgeCheckDown,
                ~0,
                QueryTriggerInteraction.Ignore
            );

            if (!foundTop) return;

            // If top is basically horizontal-ish
            float topAngle = Vector3.Angle(topHit.normal, Vector3.up);
            if (topAngle > 35f) return;

            if (debugLogs)
                Debug.Log($"[WallClimb] LEDGE HOP -> top={topHit.collider.name} angle={topAngle:F1}", this);

            // Do hop: exit climb, small upward + forward velocity (world)
            StopClimb();
            _ledgeHopUntil = Time.time + ledgeHopCooldown;

            Vector3 v = GetVelocity();
            v.y = Mathf.Max(v.y, ledgeHopUpSpeed);

            // push slightly onto platform direction (projected current forward onto ground plane)
            Vector3 fwd = Vector3.ProjectOnPlane(_motor.CurrentRotation * Vector3.forward, Vector3.up);
            if (fwd.sqrMagnitude < 0.0001f) fwd = Vector3.forward;
            fwd.Normalize();

            v += fwd * ledgeHopForwardSpeed;
            SetVelocity(v);
        }

        private Vector3 GetVelocity()
        {
#if UNITY_6000_0_OR_NEWER
            return _rb.linearVelocity;
#else
            return _rb.velocity;
#endif
        }

        private void SetVelocity(Vector3 v)
        {
#if UNITY_6000_0_OR_NEWER
            _rb.linearVelocity = v;
#else
            _rb.velocity = v;
#endif
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (!debugGizmos) return;
            if (_rb == null) _rb = GetComponent<Rigidbody>();

            Gizmos.color = _isClimbing ? Color.green : (_dbgAttachCastHit ? Color.yellow : Color.red);

            // attach cast
            Gizmos.DrawWireSphere(_dbgAttachOrigin, detectRadius);
            Vector3 attachEnd = _dbgAttachOrigin + (_dbgAttachDir.normalized * detectDistance);
            Gizmos.DrawWireSphere(attachEnd, detectRadius);
            Gizmos.DrawLine(_dbgAttachOrigin, attachEnd);

            if (_dbgAttachCastHit)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawSphere(_dbgAttachHit.point, 0.03f);
                Gizmos.color = Color.blue;
                Gizmos.DrawLine(_dbgAttachHit.point, _dbgAttachHit.point + _dbgAttachHit.normal * gizmoNormalLength);
            }

            // refresh cast
            if (_isClimbing)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawWireSphere(_dbgRefreshOrigin, detectRadius);
                Vector3 refreshEnd = _dbgRefreshOrigin + (_dbgRefreshDir.normalized * detectDistance);
                Gizmos.DrawWireSphere(refreshEnd, detectRadius);
                Gizmos.DrawLine(_dbgRefreshOrigin, refreshEnd);

                if (_dbgRefreshCastHit)
                {
                    Gizmos.color = Color.white;
                    Gizmos.DrawSphere(_dbgRefreshHit.point, 0.03f);

                    Gizmos.color = Color.blue;
                    Gizmos.DrawLine(_dbgRefreshHit.point, _dbgRefreshHit.point + _wallNormal * 0.6f);

                    if (Application.isPlaying)
                    {
                        Vector3 vel = GetVelocity();
                        Gizmos.color = Color.white;
                        Gizmos.DrawLine(_dbgRefreshHit.point, _dbgRefreshHit.point + vel * gizmoVelocityScale);
                    }

                    // ledge probes
                    Vector3 wallUp = Vector3.ProjectOnPlane(Vector3.up, _wallNormal);
                    if (wallUp.sqrMagnitude > 0.0001f)
                    {
                        wallUp.Normalize();
                        Vector3 chest = _rb.position + Vector3.up * 1.0f;
                        Vector3 probe = chest + wallUp * ledgeCheckForward;

                        Gizmos.color = Color.green;
                        Gizmos.DrawLine(chest, chest + wallUp * ledgeCheckForward);

                        Gizmos.color = Color.yellow;
                        Gizmos.DrawLine(probe, probe + Vector3.down * ledgeCheckDown);
                    }
                }
            }
        }
#endif
    }
}