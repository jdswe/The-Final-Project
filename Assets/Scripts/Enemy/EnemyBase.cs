using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyBase : MonoBehaviour, IMovable
{
    [Header("Player Settings")]
    public Transform player;
    private HealthManager playerHealth;
    public float attackRange = 2f;
    public float detectionRange = 15f;
    public float pushForce = 5f;
    public float attackCooldown = 1f;
    public float attackDamage = 45f;
    protected bool isAttacking = false;

    protected Rigidbody enemyRb;
    protected float lastAttackTime = 0f;
    protected Animator animator;

    [SerializeField] private string nextZoneTag = "zoneMid";
    protected List<GameObject> nextZones = new List<GameObject>();

    [Header("Evitar obstáculos (edificios)")]
    public float obstacleCheckDistance = 1.5f;
    public float obstacleCheckRadius = 0.4f;

    [Header("Seguridad")]
    [Tooltip("Si el enemigo cae por debajo de esta altura vuelve a la última posición en el suelo")]
    public float fallLimitY = -50f;

    public virtual float Speed { get; set; } = 3f;

    // Estado
    protected bool isDead = false;
    private bool isStunned = false;
    public bool IsStunned => isStunned;
    public bool IsDead => isDead;

    private Vector3 lastGroundedPosition;
    private Coroutine stunRoutine;
    private Collider[] ownColliders;

    protected virtual void Awake()
    {
        ownColliders = GetComponentsInChildren<Collider>();
        lastGroundedPosition = transform.position;
    }

    protected virtual void Start()
    {
        if (player == null || !player.gameObject.scene.IsValid())
        {
            // La referencia del prefab apunta a un asset, no al jugador de la escena
            GameObject playerObj = GameObject.FindWithTag("Player");
            player = playerObj != null ? playerObj.transform : null;
        }

        enemyRb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
        if (player != null) playerHealth = player.GetComponent<HealthManager>();

        if (enemyRb != null)
        {
            enemyRb.isKinematic = false;
            enemyRb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic; // evita atravesar el suelo
            enemyRb.interpolation = RigidbodyInterpolation.Interpolate;
            enemyRb.constraints = RigidbodyConstraints.FreezeRotation;
        }
    }

    /// <summary>True cuando la IA no debe moverse ni atacar (muerto, aturdido o sin jugador).</summary>
    private SlowEffect slowEffect;
    private bool slowEffectChecked;

    // Congelado por el Cañón de Glitches: no se mueve ni ataca (ni lanza bolas de fuego)
    protected bool IsFrozen
    {
        get
        {
            if (!slowEffectChecked) { slowEffect = GetComponent<SlowEffect>(); slowEffectChecked = true; }
            return slowEffect != null && slowEffect.IsFrozen;
        }
    }

    protected bool AIBlocked()
    {
        return isDead || isStunned || IsFrozen || player == null || !player.gameObject.activeInHierarchy;
    }

    protected virtual void Update()
    {
        if (AIBlocked() || isAttacking)
        {
            SetAnimBool("isRunning", false);
            SetAnimBool("isAttacking", false);
            return;
        }

        float distance = Vector3.Distance(transform.position, player.position);

        if (distance > detectionRange)
        {
            // Muy lejos, solo Idle
            StopMoving();
            SetAnimBool("isAttacking", false);
            return;
        }

        if (distance > attackRange)
        {
            // Dentro del rango de detección, pero aún lejos para atacar
            MoveTowardsPlayer();
        }
        else
        {
            // Dentro del rango de ataque
            StopMoving();

            if (!isAttacking && Time.time >= lastAttackTime + attackCooldown)
            {
                StartCoroutine(AttackSequence());
            }
        }
    }

    protected virtual void FixedUpdate()
    {
        // Recuperación si cae fuera del mapa (por ejemplo tras ser lanzado)
        if (transform.position.y < fallLimitY)
        {
            RecoverFromFall();
            return;
        }

        if (!isStunned && !isDead && IsGrounded())
        {
            lastGroundedPosition = transform.position;
        }
    }

    protected bool IsGrounded()
    {
        return Physics.Raycast(transform.position + Vector3.up * 0.3f, Vector3.down, 0.6f, ~0, QueryTriggerInteraction.Ignore);
    }

    private void RecoverFromFall()
    {
        transform.position = lastGroundedPosition + Vector3.up * 0.5f;
        if (enemyRb != null)
        {
            enemyRb.position = transform.position;
            if (!enemyRb.isKinematic)
            {
                enemyRb.linearVelocity = Vector3.zero;
                enemyRb.angularVelocity = Vector3.zero;
            }
        }
        StandUpright();
        Debug.Log($"↩️ {name} cayó fuera del mapa, regresando a {lastGroundedPosition}");
    }

    protected void StopMoving()
    {
        if (enemyRb != null && !enemyRb.isKinematic)
            enemyRb.linearVelocity = new Vector3(0, enemyRb.linearVelocity.y, 0);
        SetAnimBool("isRunning", false);
    }

    protected virtual void MoveTowardsPlayer()
    {
        if (enemyRb == null) return;

        Vector3 direction = (player.position - transform.position);
        direction.y = 0;
        direction.Normalize();

        // Evitar paredes/edificios: si hay algo delante, rodearlo por el lado libre
        direction = AvoidObstacles(direction);

        Vector3 movement = direction * Speed;
        enemyRb.linearVelocity = new Vector3(movement.x, enemyRb.linearVelocity.y, movement.z);

        SetAnimBool("isRunning", true);
        SetAnimBool("isAttacking", false);

        if (direction.magnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 5f);
        }
    }

    private Vector3 AvoidObstacles(Vector3 desired)
    {
        if (desired.sqrMagnitude < 0.001f) return desired;
        if (!IsBlocked(desired)) return desired;

        // Probar direcciones alternativas a ambos lados
        for (int angle = 30; angle <= 120; angle += 30)
        {
            Vector3 right = Quaternion.Euler(0, angle, 0) * desired;
            if (!IsBlocked(right)) return right;
            Vector3 left = Quaternion.Euler(0, -angle, 0) * desired;
            if (!IsBlocked(left)) return left;
        }
        return desired;
    }

    private bool IsBlocked(Vector3 dir)
    {
        Vector3 origin = transform.position + Vector3.up * 1f;
        RaycastHit[] hits = Physics.SphereCastAll(origin, obstacleCheckRadius, dir, obstacleCheckDistance, ~0, QueryTriggerInteraction.Ignore);
        foreach (RaycastHit hit in hits)
        {
            if (IsOwnCollider(hit.collider)) continue;
            Transform t = hit.collider.transform;
            if (player != null && (t == player || t.IsChildOf(player))) continue;
            if (hit.collider.CompareTag("Enemy")) continue;
            if (Mathf.Abs(hit.normal.y) > 0.6f) continue; // suelo o rampa, no pared
            return true;
        }
        return false;
    }

    private bool IsOwnCollider(Collider c)
    {
        if (ownColliders == null) return false;
        foreach (Collider oc in ownColliders) if (oc == c) return true;
        return false;
    }

    public virtual IEnumerator AttackSequence()
    {
        isAttacking = true;

        // Forzamos la animación directamente
        if (animator != null) animator.CrossFade("monster punch", 0.1f);

        float attackDuration = GetAnimationLength("monster punch");

        yield return new WaitForSeconds(attackDuration * 0.3f);

        if (!AIBlocked())
        {
            float distance = Vector3.Distance(transform.position, player.position);
            if (distance <= attackRange + 0.5f)
            {
                if (playerHealth != null)
                {
                    playerHealth.takeDamage(attackDamage);
                }

                if (player.TryGetComponent(out Rigidbody playerRb))
                {
                    Vector3 pushDirection = (player.position - transform.position).normalized;
                    playerRb.AddForce(pushDirection * (pushForce * 0.2f), ForceMode.Impulse);
                }
            }
        }

        yield return new WaitForSeconds(attackDuration * 0.7f);

        isAttacking = false;
        lastAttackTime = Time.time;
    }

    public float GetAnimationLength(string animationName)
    {
        if (animator == null || animator.runtimeAnimatorController == null) return 1f;

        foreach (var clip in animator.runtimeAnimatorController.animationClips)
        {
            if (clip.name == animationName)
            {
                return clip.length;
            }
        }

        return 1f; // Duración por defecto si no lo encuentra
    }

    // ---------- Lanzamiento (armas con física exagerada) ----------

    /// <summary>
    /// Lanza al enemigo por los aires (física exagerada del GDD). Mientras está aturdido no se mueve ni ataca;
    /// tras stunTime se levanta y retoma la IA. Seguro contra caídas fuera del mapa.
    /// </summary>
    public virtual void Launch(Vector3 impulse, float stunTime)
    {
        if (isDead || enemyRb == null) return;

        if (stunRoutine != null) StopCoroutine(stunRoutine);
        StopAllAttackState();

        isStunned = true;
        enemyRb.isKinematic = false;
        enemyRb.constraints = RigidbodyConstraints.None; // permite dar vueltas en el aire
        enemyRb.linearVelocity = Vector3.zero;
        enemyRb.angularVelocity = Vector3.zero;
        enemyRb.AddForce(impulse, ForceMode.VelocityChange);
        enemyRb.AddTorque(Random.insideUnitSphere * impulse.magnitude * 0.5f, ForceMode.VelocityChange);

        SetAnimBool("isRunning", false);
        SetAnimBool("isAttacking", false);

        stunRoutine = StartCoroutine(StunRecovery(Mathf.Max(0.1f, stunTime)));
    }

    private void StopAllAttackState()
    {
        isAttacking = false;
        OnInterrupted();
    }

    /// <summary>Las subclases limpian aquí sus propios estados (lanzando, esquivando...).</summary>
    protected virtual void OnInterrupted() { }

    private IEnumerator StunRecovery(float stunTime)
    {
        yield return new WaitForSeconds(stunTime);

        // Esperar a tocar el suelo (máximo 3 s extra)
        float extra = 0f;
        while (!IsGrounded() && extra < 3f)
        {
            extra += Time.deltaTime;
            yield return null;
        }

        if (enemyRb != null)
        {
            enemyRb.angularVelocity = Vector3.zero;
            enemyRb.constraints = RigidbodyConstraints.FreezeRotation;
        }
        StandUpright();

        isStunned = false;
        stunRoutine = null;
    }

    private void StandUpright()
    {
        Vector3 forward = transform.forward;
        forward.y = 0;
        if (forward.sqrMagnitude < 0.001f) forward = Vector3.forward;
        transform.rotation = Quaternion.LookRotation(forward.normalized, Vector3.up);
        if (enemyRb != null) enemyRb.rotation = transform.rotation;
    }

    // ---------- Muerte ----------

    public virtual void Die()
    {
        if (isDead) return;
        isDead = true;
        isAttacking = false;
        StopAllCoroutines();
        OnInterrupted();

        StopMoving();
        PlayDeathAnimation();

        // Activa las siguientes zonas de spawn
        foreach (var zone in FindNextZones())
        {
            if (zone != null) zone.SetActive(true);
        }

        StartCoroutine(DeathDelay());
    }

    protected void PlayDeathAnimation()
    {
        if (animator == null) return;
        if (HasParameter("die", AnimatorControllerParameterType.Trigger))
            animator.SetTrigger("die");
        else
            animator.CrossFade("monster dying", 0.1f);
    }

    /// <summary>Busca las zonas (incluso inactivas) solo cuando se necesitan, no en cada Awake.</summary>
    private List<GameObject> FindNextZones()
    {
        nextZones.Clear();
        if (string.IsNullOrEmpty(nextZoneTag)) return nextZones;

        foreach (GameObject obj in Resources.FindObjectsOfTypeAll<GameObject>())
        {
            // Comparación sin distinguir mayúsculas: el tag real es "ZoneHIgh"
            if (obj.hideFlags == HideFlags.None && obj.scene.IsValid() &&
                string.Equals(obj.tag, nextZoneTag, System.StringComparison.OrdinalIgnoreCase))
            {
                nextZones.Add(obj);
            }
        }

        if (nextZones.Count == 0)
            Debug.LogWarning("❌ No se encontraron zonas con el tag: " + nextZoneTag);

        return nextZones;
    }

    private IEnumerator DeathDelay()
    {
        // Espera a que termine la animación
        yield return new WaitForSeconds(4f);
        Destroy(gameObject);
    }

    // ---------- Utilidades del Animator ----------

    protected void SetAnimBool(string param, bool value)
    {
        if (animator != null && HasParameter(param, AnimatorControllerParameterType.Bool))
            animator.SetBool(param, value);
    }

    private Dictionary<string, AnimatorControllerParameterType> paramCache;

    protected bool HasParameter(string param, AnimatorControllerParameterType type)
    {
        if (animator == null || animator.runtimeAnimatorController == null) return false;
        if (paramCache == null)
        {
            paramCache = new Dictionary<string, AnimatorControllerParameterType>();
            foreach (var p in animator.parameters) paramCache[p.name] = p.type;
        }
        return paramCache.TryGetValue(param, out var t) && t == type;
    }
}
