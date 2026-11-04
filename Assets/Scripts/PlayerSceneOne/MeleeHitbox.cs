using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Golpe de la "Chancla del Destino". Cada golpe daña una sola vez a cada enemigo y lo
/// manda a volar lejos (física absurda del GDD).
/// </summary>
public class MeleeHitbox : MonoBehaviour
{
    public int meleeDamage = 200;
    [SerializeField] ParticleSystem MeleeVfx;

    [Header("Chancla del Destino: ¡manda a volar!")]
    public float launchForce = 25f;
    public float upwardsLift = 0.9f;
    public float launchStunTime = 2f;

    private readonly HashSet<HealthManager> hitThisSwing = new HashSet<HealthManager>();

    private void OnEnable()
    {
        // Nuevo golpe: se puede volver a dañar a todos
        hitThisSwing.Clear();
    }

    private void OnTriggerEnter(Collider other)
    {
        Strike(WeaponPhysics.GetEnemyHealth(other));
    }

    /// <summary>Aplica el golpe (lo usa también el auto-apuntado para garantizar el impacto).</summary>
    public bool Strike(HealthManager health)
    {
        if (health == null || health.IsDead || !hitThisSwing.Add(health)) return false;

        if (MeleeVfx != null) MeleeVfx.Play();
        health.takeDamage(meleeDamage);

        // Mandar a volar en la dirección del golpe
        Transform origin = transform.root;
        Vector3 away = health.transform.position - origin.position;
        away.y = 0f;
        if (away.sqrMagnitude < 0.01f) away = origin.forward;
        WeaponPhysics.Launch(health.gameObject, (away.normalized + Vector3.up * upwardsLift).normalized * launchForce, launchStunTime);

        // 🎵 Sonido de golpe
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySfx("slap");
        return true;
    }
}
