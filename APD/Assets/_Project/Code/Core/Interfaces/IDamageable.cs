namespace _Project.Code.Core.Interfaces
{
    public interface IDamageable
    {
        void TakeDamage(float amount, DamageInfo info);
    }

    public readonly struct DamageInfo
    {
        public readonly DamageType Type;
        public readonly UnityEngine.Vector3 Point;
        public readonly UnityEngine.Vector3 Normal;
        public readonly object Source;

        public DamageInfo(DamageType type, UnityEngine.Vector3 point, UnityEngine.Vector3 normal, object source = null)
        {
            Type = type;
            Point = point;
            Normal = normal;
            Source = source;
        }

        public static DamageInfo Simple(DamageType type, object source = null)
            => new DamageInfo(type, UnityEngine.Vector3.zero, UnityEngine.Vector3.zero, source);
    }

    public enum DamageType
    {
        Generic = 0,
        Fall = 1,
        Contact = 2,
        Projectile = 3,
    }
}