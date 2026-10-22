using UnityEngine;

public enum DamageKind
{
    Normal, // balas, explosiones
    Fire    // quemadura (BurnShot) y fuego en el piso (GroundFire)
}

// Todo lo que el damage number necesita saber de un golpe.
public struct DamageInfo
{
    public EnemyHealth target;
    public Vector3 position;
    public float damage;
    public bool isCrit;
    public DamageKind kind;
    public bool killed;
}

// Contexto del golpe que se está aplicando. Quien hace daño lo setea justo antes
// de llamar a TakeDamage y lo limpia después; así no hay que cambiar IDamageable
// ni todos los TakeDamage(float) del proyecto.
//   DamageContext.Set(isCrit, DamageKind.Normal);
//   target.TakeDamage(dmg);
//   DamageContext.Clear();
public static class DamageContext
{
    public static bool IsCrit { get; private set; }
    public static DamageKind Kind { get; private set; } = DamageKind.Normal;

    public static void Set(bool isCrit, DamageKind kind)
    {
        IsCrit = isCrit;
        Kind = kind;
    }

    public static void Clear()
    {
        IsCrit = false;
        Kind = DamageKind.Normal;
    }
}
