using System;
using UnityEngine;
using _Project.Code.Core.Interfaces;

namespace _Project.Code.Player.Modules
{
    public sealed class HealthModule : MonoBehaviour, IDamageable
    {
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float iFrames = 0f;

        public float CurrentHealth { get; private set; }
        public bool IsDead => CurrentHealth <= 0f;

        public event Action<float> Damaged;
        public event Action Died;

        private float _invulUntil;

        private void Awake()
        {
            CurrentHealth = maxHealth;
        }

        public void TakeDamage(float amount, DamageInfo info)
        {
            if (IsDead || amount <= 0f) return;
            if (Time.time < _invulUntil) return;

            CurrentHealth = Mathf.Max(0f, CurrentHealth - amount);
            _invulUntil = Time.time + iFrames;

            Damaged?.Invoke(amount);

            if (IsDead)
                Died?.Invoke();
        }
    }
}