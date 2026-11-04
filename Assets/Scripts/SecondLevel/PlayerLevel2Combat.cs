using UnityEngine;

/// <summary>
/// Combate del jugador en el nivel 2 (la casa).
/// - Clic izquierdo (Fire1): lanza patos de goma (usa el BulletPool3D asignado).
/// - Clic derecho (Fire2): golpe con la "Chancla del Destino": daña a la mamá si está cerca
///   y devuelve las chanclas que vengan hacia el jugador (estilo "parry").
/// </summary>
[RequireComponent(typeof(HealthManager))]
public class PlayerLevel2Combat : MonoBehaviour
{
    [Header("Patos de goma")]
    public BulletPool3D duckPool;
    public float fireCooldown = 0.35f;
    public float spawnForwardOffset = 0.6f;
    public float spawnHeight = 1.0f;

    [Header("Chancla (cuerpo a cuerpo / parry)")]
    public float meleeCooldown = 0.8f;
    public float meleeRange = 2.2f;
    public float meleeDamage = 80f;
    public float parryWindow = 0.35f;
    [Tooltip("Física absurda del GDD: el chanclazo manda a volar a la mamá")]
    public float meleeLaunchForce = 22f;
    public float meleeLaunchStun = 2f;

    [Header("Efectos (packs VFX)")]
    public GameObject slapVfx;
    public GameObject parryVfx;
    public float parryRadius = 2.0f;

    private float lastFireTime = -10f;
    private float lastMeleeTime = -10f;
    private float parryUntil = -1f;
    private Collider[] ownColliders;
    private Transform cam;
    private Animator animator;
    private HealthManager health;

    public bool IsParrying => Time.time <= parryUntil;

    void Start()
    {
        ownColliders = GetComponentsInChildren<Collider>();
        animator = GetComponent<Animator>();
        health = GetComponent<HealthManager>();
        if (Camera.main != null) cam = Camera.main.transform;
        if (duckPool == null) duckPool = GetComponentInChildren<BulletPool3D>();
    }

    void Update()
    {
        if (Time.timeScale == 0f || (health != null && health.IsDead)) return;

        if (Input.GetButtonDown("Fire1") && Time.time >= lastFireTime + fireCooldown)
        {
            lastFireTime = Time.time;
            ThrowDuck();
        }
        else if (Input.GetButtonDown("Fire2") && Time.time >= lastMeleeTime + meleeCooldown)
        {
            lastMeleeTime = Time.time;
            SandalSlap();
        }

        if (IsParrying) TryParry();
    }

    Vector3 AimForward()
    {
        Vector3 f = cam != null ? cam.forward : transform.forward;
        f.y = 0f;
        if (f.sqrMagnitude < 0.001f) f = transform.forward;
        return f.normalized;
    }

    void ThrowDuck()
    {
        if (duckPool == null)
        {
            Debug.LogWarning("⚠️ PlayerLevel2Combat: no hay BulletPool3D de patos asignado.");
            return;
        }

        Vector3 forward = AimForward();
        // Girar al jugador hacia donde dispara
        transform.rotation = Quaternion.LookRotation(forward);

        Vector3 spawnPos = transform.position + Vector3.up * spawnHeight + forward * spawnForwardOffset;
        GameObject duck = duckPool.GetBullet(spawnPos, Quaternion.LookRotation(forward));
        if (duck == null) return;

        foreach (Collider dc in duck.GetComponentsInChildren<Collider>())
            foreach (Collider oc in ownColliders)
                if (dc != null && oc != null) Physics.IgnoreCollision(dc, oc);

        if (AudioManager.Instance != null) AudioManager.Instance.PlaySfx("shot pistol");
    }

    void SandalSlap()
    {
        parryUntil = Time.time + parryWindow;
        if (animator != null && HasParameter(animator, "isAttacking")) animator.SetTrigger("isAttacking");
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySfx("slap");
        VfxUtil.Spawn(slapVfx, transform.position + Vector3.up + transform.forward * 1.2f, transform.rotation, 1.5f);

        // Golpe cuerpo a cuerpo
        Vector3 center = transform.position + Vector3.up * 1f + transform.forward * (meleeRange * 0.5f);
        foreach (Collider c in Physics.OverlapSphere(center, meleeRange * 0.6f, ~0, QueryTriggerInteraction.Ignore))
        {
            if (!c.CompareTag("Enemy")) continue;
            HealthManager enemyHealth = c.GetComponentInParent<HealthManager>();
            if (enemyHealth != null && enemyHealth != health)
            {
                enemyHealth.takeDamage(meleeDamage);
                if (!enemyHealth.IsDead)
                {
                    Vector3 away = enemyHealth.transform.position - transform.position;
                    away.y = 0f;
                    Vector3 impulse = away.normalized * meleeLaunchForce + Vector3.up * meleeLaunchForce * 0.6f;
                    WeaponPhysics.Launch(enemyHealth.gameObject, impulse, meleeLaunchStun);
                }
                break;
            }
        }
    }

    void TryParry()
    {
        foreach (Collider c in Physics.OverlapSphere(transform.position + Vector3.up, parryRadius, ~0, QueryTriggerInteraction.Collide))
        {
            Projectile p = c.GetComponentInParent<Projectile>();
            if (p != null && !p.IsReflected)
            {
                Transform target = p.owner != null ? p.owner.transform : null;
                p.Reflect(target);
                VfxUtil.Spawn(parryVfx, p.transform.position, Quaternion.identity, 1.5f);
            }
        }
    }

    static bool HasParameter(Animator a, string name)
    {
        if (a.runtimeAnimatorController == null) return false;
        foreach (AnimatorControllerParameter p in a.parameters)
            if (p.name == name && p.type == AnimatorControllerParameterType.Trigger) return true;
        return false;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position + Vector3.up, parryRadius);
    }
}
