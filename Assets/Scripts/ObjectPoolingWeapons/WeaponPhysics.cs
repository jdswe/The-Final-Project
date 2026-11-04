using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Utilidades de "física alterada y absurda" (GDD) compartidas por las armas:
/// explosiones desmesuradas, mandar a volar enemigos y glitches de teletransporte.
/// </summary>
public static class WeaponPhysics
{
    /// <summary>Busca la HealthManager de un enemigo aunque el collider esté en un hijo.</summary>
    public static HealthManager GetEnemyHealth(Collider col)
    {
        if (col == null) return null;
        HealthManager h = col.GetComponentInParent<HealthManager>();
        if (h == null) return null;
        if (!h.CompareTag("Enemy") && !col.CompareTag("Enemy")) return null;
        return h;
    }

    /// <summary>
    /// Manda a volar a un enemigo. Usa EnemyBase.Launch (nivel 1) si existe; para cualquier
    /// otro enemigo (p. ej. la mamá del nivel 2) apaga su IA un momento y usa su Rigidbody.
    /// </summary>
    public static void Launch(GameObject target, Vector3 impulse, float stunTime)
    {
        if (target == null) return;

        EnemyBase enemy = target.GetComponentInParent<EnemyBase>();
        if (enemy != null)
        {
            enemy.Launch(impulse, stunTime);
            return;
        }

        Rigidbody rb = target.GetComponentInParent<Rigidbody>();
        if (rb == null) return;

        LaunchRecovery recovery = rb.GetComponent<LaunchRecovery>();
        if (recovery == null) recovery = rb.gameObject.AddComponent<LaunchRecovery>();
        recovery.Launch(rb, impulse, stunTime);
    }

    /// <summary>
    /// Explosión desmesurada: daño en área a enemigos, los manda a volar y empuja cualquier
    /// objeto físico cercano (muebles, cajas...). No daña al jugador.
    /// </summary>
    public static void Explode(Vector3 center, float radius, float damage, float launchForce, float upwardsLift,
                               float stunTime, GameObject directHit = null, float directDamage = 0f)
    {
        HashSet<HealthManager> damaged = new HashSet<HealthManager>();
        HashSet<Rigidbody> pushed = new HashSet<Rigidbody>();

        foreach (Collider col in Physics.OverlapSphere(center, radius, ~0, QueryTriggerInteraction.Ignore))
        {
            HealthManager health = GetEnemyHealth(col);
            if (health != null)
            {
                if (!damaged.Add(health)) continue;

                bool isDirect = directHit != null && (col.gameObject == directHit || col.transform.IsChildOf(health.transform) && directHit.transform.IsChildOf(health.transform));
                health.takeDamage(isDirect && directDamage > 0f ? directDamage : damage);

                Vector3 away = health.transform.position - center;
                away.y = 0f;
                if (away.sqrMagnitude < 0.01f) away = Random.insideUnitSphere;
                away.y = 0f;
                Vector3 impulse = (away.normalized + Vector3.up * upwardsLift).normalized * launchForce;
                Launch(health.gameObject, impulse, stunTime);
                continue;
            }

            // Objetos sueltos (no jugador): también salen volando
            Rigidbody rb = col.attachedRigidbody;
            if (rb != null && !rb.isKinematic && pushed.Add(rb) && !rb.CompareTag("Player")
                && rb.GetComponentInParent<HealthManager>() == null)
            {
                rb.AddExplosionForce(launchForce, center, radius, upwardsLift, ForceMode.VelocityChange);
            }
        }
    }

    /// <summary>Glitch: teletransporta un objeto a un punto libre cercano.</summary>
    public static bool GlitchTeleport(Transform target, float minDistance, float maxDistance)
    {
        if (target == null) return false;

        for (int i = 0; i < 8; i++)
        {
            Vector2 r = Random.insideUnitCircle.normalized * Random.Range(minDistance, maxDistance);
            Vector3 candidate = target.position + new Vector3(r.x, 0f, r.y);

            // Debe haber suelo debajo y espacio libre
            if (!Physics.Raycast(candidate + Vector3.up * 3f, Vector3.down, out RaycastHit ground, 8f, ~0, QueryTriggerInteraction.Ignore))
                continue;
            Vector3 point = ground.point + Vector3.up * 0.1f;
            if (Physics.CheckCapsule(point + Vector3.up * 0.5f, point + Vector3.up * 1.6f, 0.4f, ~0, QueryTriggerInteraction.Ignore))
                continue;

            Rigidbody rb = target.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.position = point;
                if (!rb.isKinematic) rb.linearVelocity = Vector3.zero;
            }
            target.position = point;
            return true;
        }
        return false;
    }
}
