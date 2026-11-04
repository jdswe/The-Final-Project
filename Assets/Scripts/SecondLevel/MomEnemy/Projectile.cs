using UnityEngine;

/// <summary>
/// Chancla lanzada por la mamá. Hace daño al jugador; si el jugador la golpea
/// con su chancla (clic derecho) en el momento justo, la devuelve hacia la mamá.
/// </summary>
public class Projectile : MonoBehaviour
{
    public float lifeTime = 5f;
    public float speed = 10f;
    public float damage = 50f;
    public float spinSpeed = 720f;
    [Tooltip("Efecto (pack VFX) al impactar")]
    public GameObject hitVfx;

    [HideInInspector] public GameObject owner;

    private Rigidbody rb;
    private bool reflected = false;
    private bool hasHit = false;

    public bool IsReflected => reflected;

    public void Initialize(Vector3 direction)
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.linearVelocity = direction.normalized * speed;

        // Orientar visualmente el proyectil hacia la dirección
        transform.rotation = Quaternion.LookRotation(direction);

        Destroy(gameObject, lifeTime);
    }

    void Update()
    {
        // Giro de la chancla en el aire
        transform.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.Self);
    }

    /// <summary>El jugador devolvió la chancla: ahora vuela hacia la mamá y le hace daño.</summary>
    public void Reflect(Transform newTarget, float damageMultiplier = 2f)
    {
        if (reflected || rb == null) return;
        reflected = true;
        damage *= damageMultiplier;

        Vector3 dir = newTarget != null
            ? (newTarget.position + Vector3.up * 1f - transform.position).normalized
            : -rb.linearVelocity.normalized;

        // Ahora sí debe poder chocar con la mamá
        if (owner != null)
        {
            Collider[] mine = GetComponentsInChildren<Collider>();
            foreach (Collider oc in owner.GetComponentsInChildren<Collider>())
                foreach (Collider pc in mine)
                    if (oc != null && pc != null) Physics.IgnoreCollision(pc, oc, false);
        }

        rb.linearVelocity = dir * speed * 1.5f;
        transform.rotation = Quaternion.LookRotation(dir);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (hasHit) return;
        GameObject other = collision.gameObject;
        VfxUtil.Spawn(hitVfx, transform.position, Quaternion.identity, 2f);

        if (!reflected && other.CompareTag("Player"))
        {
            hasHit = true;
            HealthManager health = other.GetComponentInParent<HealthManager>();
            if (health != null) health.takeDamage(damage);
            Debug.Log("🩴 Jugador impactado por la chancla");
            Destroy(gameObject);
            return;
        }

        if (reflected && (other.CompareTag("Enemy") || (owner != null && other == owner)))
        {
            hasHit = true;
            HealthManager health = other.GetComponentInParent<HealthManager>();
            if (health != null) health.takeDamage(damage);
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySfx("slap");
            Debug.Log("🩴 ¡Chancla devuelta a la mamá!");
            Destroy(gameObject);
            return;
        }

        // Golpeó una pared o un mueble: desaparece
        if (owner == null || other != owner)
        {
            hasHit = true;
            Destroy(gameObject, 0.05f);
        }
    }
}
