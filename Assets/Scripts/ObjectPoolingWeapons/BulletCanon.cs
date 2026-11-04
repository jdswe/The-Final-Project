using UnityEngine;

/// <summary>
/// "Cañón de Glitches" (GDD): congela a los enemigos y, a veces, los teletransporta
/// (glitch). Las balas tienen gravedad alterada: flotan y "tiemblan" en el aire.
/// </summary>
public class BulletCanon : MonoBehaviour
{
    [Header("Variables")]
    public float speed = 1000f; // Impulso inicial
    public float damage = 1f; // Daño reducido
    public float slowPercentage = 0.8f; // Porcentaje de ralentización (1 = congelado)
    public float slowDuration = 3f; // Duración del efecto de ralentización
    [SerializeField] private float angle;
    public Vector3 direction;
    public System.Action destroyed;

    [Header("Lifetime")]
    public float lifeTime = 5f; // Tiempo máximo de vida de la bala

    [Header("Glitch")]
    [Range(0f, 1f)] public float freezeAmount = 1f;      // Congelación total
    [Range(0f, 1f)] public float teleportChance = 0.35f; // Probabilidad de teletransportar
    public float teleportMinDistance = 3f;
    public float teleportMaxDistance = 7f;
    [Range(-1f, 1f)] public float gravityScale = 0.25f;  // Gravedad alterada (flota)
    public float jitter = 2f;                            // Temblor "glitch" en el aire
    public GameObject freezeVfx;
    public GameObject teleportVfx;

    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void OnEnable()
    {
        Vector3 forward = Camera.main != null ? Camera.main.transform.forward : transform.forward;
        if (angle != 0f)
        {
            Vector3 right = Vector3.Cross(Vector3.up, forward);
            if (right.sqrMagnitude > 0.0001f)
                forward = Quaternion.AngleAxis(-angle, right.normalized) * forward;
        }
        rb.AddForce(forward.normalized * speed, ForceMode.Impulse);

        // Iniciar temporizador de vida útil
        Invoke(nameof(Deactivate), lifeTime);
    }

    private void FixedUpdate()
    {
        if (rb == null || rb.isKinematic) return;

        // Gravedad alterada: compensa parte de la gravedad del motor
        if (rb.useGravity)
            rb.AddForce(-Physics.gravity * (1f - gravityScale), ForceMode.Acceleration);

        // Pequeño temblor aleatorio tipo glitch
        if (jitter > 0f)
            rb.AddForce(Random.insideUnitSphere * jitter, ForceMode.Acceleration);
    }

    private void OnDisable()
    {
        // Evita que un Invoke pendiente desactive la bala cuando se reutilice desde el pool
        CancelInvoke();
    }

    private void OnCollisionEnter(Collision collision)
    {
        HealthManager health = WeaponPhysics.GetEnemyHealth(collision.collider);
        if (health != null)
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySfx("shot canon");

            // Aplicar daño
            health.takeDamage(damage);

            Transform enemy = health.transform;

            // Glitch: a veces lo teletransporta
            if (Random.value < teleportChance)
            {
                Vector3 from = enemy.position;
                if (WeaponPhysics.GlitchTeleport(enemy, teleportMinDistance, teleportMaxDistance))
                {
                    SpawnVfx(teleportVfx, from);
                    SpawnVfx(teleportVfx, enemy.position);
                }
            }

            // Congelación (usa el efecto de ralentización con 100%)
            SlowEffect slowEffect = enemy.GetComponentInChildren<SlowEffect>();
            if (slowEffect == null) slowEffect = enemy.GetComponentInParent<SlowEffect>();
            if (slowEffect != null)
            {
                slowEffect.ApplySlow(Mathf.Max(slowPercentage, freezeAmount), slowDuration);
            }
            SpawnVfx(freezeVfx, enemy.position + Vector3.up);
        }

        // Desactivar la bala al colisionar
        Deactivate();
    }

    private static void SpawnVfx(GameObject prefab, Vector3 position)
    {
        if (prefab == null) return;
        GameObject vfx = Instantiate(prefab, position, Quaternion.identity);
        Destroy(vfx, 3f);
    }

    private void Deactivate()
    {
        // Cancelar cualquier invocación pendiente para evitar errores
        CancelInvoke();

        // Notificar al pool que la bala está lista para ser reutilizada
        destroyed?.Invoke();

        // Desactivar la bala
        gameObject.SetActive(false);
    }
}
