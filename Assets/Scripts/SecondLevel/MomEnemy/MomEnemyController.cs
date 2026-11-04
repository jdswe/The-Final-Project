using UnityEngine;
using System.Collections;

/// <summary>
/// Jefa del nivel 2 (GDD: "la madre lo espera ansiosa y de mal humor").
/// Camina hacia el jugador y lanza la "chancla" constantemente.
/// Con menos del 50% de vida entra en "modo furia": se mueve más rápido,
/// ataca más seguido y lanza varias chanclas en abanico.
/// </summary>
public class MomEnemyController : MonoBehaviour
{
    [Header("Movimiento")]
    public Transform player;
    public float moveSpeed = 2f;
    public float delayBeforeWalking = 2f;
    public float obstacleDetectionDistance = 1.5f;
    public float turnSpeed = 120f;
    public float walkDuration = 5f;
    [Tooltip("Distancia a la que deja de acercarse al jugador")]
    public float stopDistance = 2.5f;

    [Header("Ataque")]
    public GameObject attackPrefab;
    public Transform firePoint;
    public float attackDuration = 5f;
    public float projectileDamage = 50f;

    [Header("Modo furia (vida < 50%)")]
    [Range(0f, 1f)] public float rageHealthPercent = 0.5f;
    public float rageSpeedMultiplier = 1.6f;
    public float rageAttackDurationMultiplier = 0.5f;
    public int rageProjectiles = 3;
    public float rageSpreadAngle = 20f;

    [Header("Muerte")]
    public float delayBeforeWin = 2.5f;

    [Header("Efectos (packs VFX)")]
    public GameObject rageVfx;   // aura mientras está furiosa
    public GameObject deathVfx;  // explosión al ser derrotada

    private GameObject rageInstance;

    private float timer = 0f;
    private float walkTimer = 0f;
    private bool isWalking = false;
    private bool isAttacking = false;
    private bool isDead = false;
    private bool isEnraged = false;

    private Animator animator;
    private HealthManager health;
    private Collider[] ownColliders;

    void Start()
    {
        animator = GetComponent<Animator>();
        health = GetComponent<HealthManager>();
        ownColliders = GetComponentsInChildren<Collider>();

        if (player == null)
        {
            GameObject playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null) player = playerObj.transform;
        }

        SetWalkingAnim(false);
    }

    void Update()
    {
        if (isDead || player == null || !player.gameObject.activeInHierarchy) return;

        CheckRage();

        timer += Time.deltaTime;

        if (!isWalking && !isAttacking && timer >= delayBeforeWalking)
        {
            StartWalking();
        }

        if (isWalking && !isAttacking)
        {
            walkTimer += Time.deltaTime;

            MoveTowardsPlayer();

            if (walkTimer >= CurrentWalkDuration)
            {
                StopWalking();
                StartCoroutine(PerformAttack());
            }
        }
    }

    float CurrentSpeed => isEnraged ? moveSpeed * rageSpeedMultiplier : moveSpeed;
    float CurrentWalkDuration => isEnraged ? walkDuration * rageAttackDurationMultiplier : walkDuration;
    float CurrentAttackDuration => isEnraged ? attackDuration * rageAttackDurationMultiplier : attackDuration;

    void CheckRage()
    {
        if (isEnraged || health == null) return;

        if (health.HealthPercent <= rageHealthPercent)
        {
            isEnraged = true;
            if (animator != null) animator.speed = 1.3f;
            rageInstance = VfxUtil.Spawn(rageVfx, transform.position, transform.rotation, 0f, transform);
            Debug.Log("😡 ¡La mamá entró en modo furia!");
        }
    }

    void StartWalking()
    {
        isWalking = true;
        walkTimer = 0f;
        SetWalkingAnim(true);
    }

    void StopWalking()
    {
        isWalking = false;
        SetWalkingAnim(false);
    }

    void SetWalkingAnim(bool value)
    {
        if (animator != null) animator.SetBool("isWalking", value);
    }

    void MoveTowardsPlayer()
    {
        Vector3 directionToPlayer = player.position - transform.position;
        directionToPlayer.y = 0;
        float distance = directionToPlayer.magnitude;
        if (distance < 0.01f) return;
        directionToPlayer /= distance;

        if (IsObstacleAhead())
        {
            transform.Rotate(Vector3.up, turnSpeed * Time.deltaTime);
        }
        else
        {
            Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
        }

        if (distance > stopDistance)
        {
            transform.position += transform.forward * CurrentSpeed * Time.deltaTime;
        }
    }

    bool IsObstacleAhead()
    {
        Ray ray = new Ray(transform.position + Vector3.up * 0.5f, transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, obstacleDetectionDistance, ~0, QueryTriggerInteraction.Ignore))
        {
            // El jugador no es un obstáculo: es el objetivo
            return !hit.transform.IsChildOf(player) && hit.transform != player;
        }
        return false;
    }

    IEnumerator PerformAttack()
    {
        isAttacking = true;

        // Mirar al jugador antes de lanzar
        Vector3 look = player.position - transform.position;
        look.y = 0;
        if (look.sqrMagnitude > 0.001f) transform.rotation = Quaternion.LookRotation(look);

        if (AudioManager.Instance != null) AudioManager.Instance.PlaySfx("slap");
        if (animator != null) animator.SetTrigger("attack");

        yield return new WaitForSeconds(0.3f);

        if (!isDead)
        {
            int count = isEnraged ? Mathf.Max(1, rageProjectiles) : 1;
            for (int i = 0; i < count; i++)
            {
                float angle = count == 1 ? 0f : Mathf.Lerp(-rageSpreadAngle, rageSpreadAngle, i / (float)(count - 1));
                ThrowSandal(angle);
            }
        }

        yield return new WaitForSeconds(Mathf.Max(0.2f, CurrentAttackDuration - 0.3f));

        isAttacking = false;
        timer = 0f;
        if (!isDead) StartWalking();
    }

    void ThrowSandal(float yawOffset)
    {
        if (attackPrefab == null)
        {
            Debug.LogWarning("⚠️ MomEnemyController: no hay 'attackPrefab' (chancla) asignado.");
            return;
        }

        Vector3 origin = firePoint != null ? firePoint.position : transform.position + Vector3.up * 1.5f;
        Vector3 target = player.position + Vector3.up * 0.8f; // apuntar al torso
        Vector3 direction = Quaternion.Euler(0f, yawOffset, 0f) * (target - origin).normalized;

        GameObject projectile = Instantiate(attackPrefab, origin, Quaternion.LookRotation(direction));

        // Que la chancla no choque con la propia mamá
        Collider[] projectileColliders = projectile.GetComponentsInChildren<Collider>();
        foreach (Collider pc in projectileColliders)
            foreach (Collider oc in ownColliders)
                if (pc != null && oc != null) Physics.IgnoreCollision(pc, oc);

        Projectile p = projectile.GetComponent<Projectile>();
        if (p != null)
        {
            p.damage = projectileDamage;
            p.owner = gameObject;
            p.Initialize(direction);
        }
    }

    /// <summary>Llamado por HealthManager cuando la vida llega a 0.</summary>
    public void Die()
    {
        if (isDead) return;
        isDead = true;
        isWalking = false;
        StopAllCoroutines();
        SetWalkingAnim(false);
        StartCoroutine(DeathSequence());
    }

    IEnumerator DeathSequence()
    {
        Debug.Log("🏆 ¡La mamá fue derrotada!");
        if (rageInstance != null) Destroy(rageInstance);
        VfxUtil.Spawn(deathVfx, transform.position + Vector3.up, Quaternion.identity, 4f);

        // No hay animación de muerte: la mamá cae hacia atrás
        Quaternion start = transform.rotation;
        Quaternion end = start * Quaternion.Euler(-85f, 0f, 0f);
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * 1.5f;
            transform.rotation = Quaternion.Slerp(start, end, t);
            yield return null;
        }

        yield return new WaitForSeconds(delayBeforeWin);

        if (GameManager.instance != null) GameManager.instance.GameWin();
        else UnityEngine.SceneManagement.SceneManager.LoadScene("GameWin");
    }
}
