using System.Collections;
using UnityEngine;
using _Project.Code.Core.Interfaces;
using _Project.Code.Player.Utils;

namespace _Project.Code.Player.Modules
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RigidbodyMotor))]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class FallDamageModule : MonoBehaviour, IPlayerModule
    {
        [Header("Fall Damage")]
        [SerializeField] private float fallThresholdVelocity = 15f; // same meaning as your old script
        [SerializeField] private float damageMultiplier = 1f;

        [Header("Feedback")]
        [SerializeField] private Renderer targetRenderer;
        [SerializeField] private float flashSeconds = 2f;

        [Header("Damage Target (optional)")]
        [Tooltip("If set, will damage this object. If null, will try GetComponent<IDamageable>() then HealthModule.")]
        [SerializeField] private MonoBehaviour damageReceiverOverride;

        private RigidbodyMotor _motor;
        private Rigidbody _rb;

        private bool _enabled;
        private bool _prevGrounded;
        private float _prevYVel;

        private void Reset()
        {
            targetRenderer = GetComponent<Renderer>();
        }

        private void Awake()
        {
            _motor = GetComponent<RigidbodyMotor>();
            _rb = GetComponent<Rigidbody>();

            if (targetRenderer == null)
                targetRenderer = GetComponentInChildren<Renderer>();
        }

        public void ModuleEnable()
        {
            _enabled = true;
            _prevGrounded = _motor.IsGrounded;
            _prevYVel = GetVelocity().y;
        }

        public void ModuleDisable()
        {
            _enabled = false;
        }

        public void Tick(float dt)
        {
            if (!_enabled) return;

            bool groundedNow = _motor.IsGrounded;

            // Landing edge: was airborne, now grounded
            if (!_prevGrounded && groundedNow)
            {
                // Use previous frame Y velocity for a stable "impact" measure
                if (_prevYVel < -fallThresholdVelocity)
                {
                    float damage = Mathf.Abs(_prevYVel + fallThresholdVelocity) * damageMultiplier;

                    DealDamage(damage);
                    FlashRed();
                }
            }

            _prevGrounded = groundedNow;
            _prevYVel = GetVelocity().y;
        }

        private void DealDamage(float amount)
        {
            if (amount <= 0f) return;

            // Priority: explicit override
            if (damageReceiverOverride is _Project.Code.Core.Interfaces.IDamageable dmgA)
            {
                dmgA.TakeDamage(amount, default);
                return;
            }

            // Try IDamageable on same GO
            var dmg = GetComponent<_Project.Code.Core.Interfaces.IDamageable>();
            if (dmg != null)
            {
                dmg.TakeDamage(amount, default);
                return;
            }

            // Try HealthModule (if you use it)
            var health = GetComponent<HealthModule>();
            if (health != null)
            {
                health.TakeDamage(amount, default);
                return;
            }

            // If none exists, just log (non-fatal)
            Debug.Log($"[FallDamageModule] Damage: {amount:0.##} (No receiver found)", this);
        }

        private void FlashRed()
        {
            if (targetRenderer == null) return;
            StopAllCoroutines();
            StartCoroutine(DamageRed());
        }

        private IEnumerator DamageRed()
        {
            Color original = targetRenderer.material.color;
            targetRenderer.material.color = Color.red;
            yield return new WaitForSeconds(flashSeconds);
            targetRenderer.material.color = original;
        }

        private Vector3 GetVelocity()
        {
#if UNITY_6000_0_OR_NEWER
            return _rb.linearVelocity;
#else
            return _rb.velocity;
#endif
        }
    }
}