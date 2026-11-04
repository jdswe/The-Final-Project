using UnityEngine;

public class PlayerCombat : MonoBehaviour
{
    private Animator animator;
    private WeaponController currentWeapon;
    private float attackCooldown = 0.5f;
    MovementPlayer player;

    [SerializeField] ParticleSystem fireVfx;
    

    [SerializeField] private GameObject hitBox;

    [Header("Chancla del Destino: auto-apuntado perfecto")]
    [SerializeField] private float autoAimRange = 8f;      // Busca al enemigo más cercano en este radio
    [SerializeField] private float guaranteedHitRange = 3.5f; // Dentro de este rango el golpe nunca falla
    private HealthManager meleeTarget;

    public bool isMeleeDefending { get; private set; } = false;

    void Start()
    {
        animator = GetComponent<Animator>();
        player = GetComponent<MovementPlayer>();
        if (hitBox != null) hitBox.SetActive(false);
    }

    void Update()
    {
        if (Input.GetButtonDown("Fire1"))
        {
            if (fireVfx != null) fireVfx.Play();
            HandleFire();
        }
        else if (Input.GetButtonDown("Fire2"))
        {
            HandleMelee();
        }

        if (animator.GetBool("isAttacking"))
        {
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            if (stateInfo.normalizedTime >= 1.0f && stateInfo.IsTag("MeleeAttack"))
            {
                ResetAttack();
            }
        }
    }

    private void HandleFire()
    {
        if (animator.GetBool("hasSword")) return;

        

        if (animator.GetBool("hasWeapon") && currentWeapon != null)
        {
            currentWeapon.FireBullet();
        }
        
    }
    private void HandleMelee()
    {
        animator.SetBool("isAttacking", true);

        // Auto-apuntado: girar hacia el enemigo más cercano
        meleeTarget = FindNearestEnemy();
        if (meleeTarget != null)
        {
            Vector3 look = meleeTarget.transform.position - transform.position;
            look.y = 0f;
            if (look.sqrMagnitude > 0.001f)
            {
                Quaternion rot = Quaternion.LookRotation(look);
                Rigidbody rb = GetComponent<Rigidbody>();
                if (rb != null) rb.MoveRotation(rot);
                transform.rotation = rot;
            }
        }

        isMeleeDefending = true; // ← Activamos la defensa

        Invoke(nameof(ActivateHitbox), 0.2f);
        Invoke(nameof(DeactivateHitbox), 0.4f);

        Invoke(nameof(ResetAttack), attackCooldown);
        Invoke(nameof(DisableDefense), attackCooldown); // ← Desactivamos la defensa después del ataque
    }

    private void ActivateHitbox()
    {
        if (hitBox != null) hitBox.SetActive(true);

        // Impacto garantizado si el objetivo sigue cerca
        if (meleeTarget != null && hitBox != null &&
            Vector3.Distance(transform.position, meleeTarget.transform.position) <= guaranteedHitRange)
        {
            MeleeHitbox mh = hitBox.GetComponent<MeleeHitbox>();
            if (mh != null) mh.Strike(meleeTarget);
        }
        meleeTarget = null;
    }

    private HealthManager FindNearestEnemy()
    {
        HealthManager best = null;
        float bestDist = float.MaxValue;
        foreach (Collider c in Physics.OverlapSphere(transform.position, autoAimRange, ~0, QueryTriggerInteraction.Ignore))
        {
            HealthManager h = WeaponPhysics.GetEnemyHealth(c);
            if (h == null || h.IsDead) continue;
            float d = (h.transform.position - transform.position).sqrMagnitude;
            if (d < bestDist) { bestDist = d; best = h; }
        }
        return best;
    }

    private void DisableDefense()
    {
        isMeleeDefending = false;
    }

    private void DeactivateHitbox()
    {
        if (hitBox != null) hitBox.SetActive(false);
    }

    private void ResetAttack()
    {
        animator.SetBool("isAttacking", false);
    }

    public void SetCurrentWeapon(WeaponController newWeapon)
    {
        if (newWeapon == null)
        {
            
            return;
        }

        currentWeapon = newWeapon;
        
    }

    public void EquipDefaultWeaponFromSignal()
    {
        WeaponWheelController.weaponID = 1;
        if (player == null) player = GetComponent<MovementPlayer>();
        if (player != null) player.SwitchWeapon(WeaponWheelController.weaponID);
    }
}
