using UnityEngine;

/// <summary>
/// "Lanzador de Patos" (GDD): patos de goma que rebotan, hacen "cuac" y explotan de forma
/// desmesurada, mandando a volar a los enemigos (¡incluido el jefe!).
/// </summary>
public class Bullet3D : MonoBehaviour
{
    [Header("Variables")]
    public float speed = 1000f; // Impulso inicial
    public float damage = 10f; // Daño del impacto directo
    [SerializeField] private float angle; // Ángulo extra de elevación (parábola)
    public Vector3 direction;
    public System.Action destroyed;

    [Header("Lifetime")]
    public float lifeTime = 5f; // Tiempo máximo de vida de la bala

    [Header("Pato explosivo")]
    public int maxBounces = 3;               // Rebotes antes de explotar solo
    public float explosionRadius = 4f;
    public float splashDamage = 40f;         // Daño en área
    public float launchForce = 18f;          // ¡Física exagerada!
    public float upwardsLift = 1.2f;
    public float launchStunTime = 1.5f;
    public float bounceBoost = 1.15f;        // Cada rebote acelera un poco (absurdo)
    public GameObject explosionVfx;
    public string quackSfx = "quack";

    private Rigidbody rb;
    private int bounceCount;
    private bool exploded;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void OnEnable()
    {
        bounceCount = 0;
        exploded = false;

        Vector3 shootDirection = GetAimDirection();
        rb.AddForce(shootDirection * speed, ForceMode.Impulse);
        rb.AddTorque(Random.insideUnitSphere * 5f, ForceMode.Impulse); // giro loco del pato

        // Iniciar temporizador de vida útil
        Invoke(nameof(Explode), lifeTime);
    }

    /// <summary>Dirección de la cámara (incluye apuntar arriba/abajo) + elevación opcional.</summary>
    private Vector3 GetAimDirection()
    {
        Vector3 forward = Camera.main != null ? Camera.main.transform.forward : transform.forward;
        if (angle != 0f)
        {
            Vector3 right = Vector3.Cross(Vector3.up, forward);
            if (right.sqrMagnitude > 0.0001f)
                forward = Quaternion.AngleAxis(-angle, right.normalized) * forward;
        }
        return forward.normalized;
    }

    private void OnDisable()
    {
        // Evita que un Invoke pendiente desactive la bala cuando se reutilice desde el pool
        CancelInvoke();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (exploded) return;

        if (WeaponPhysics.GetEnemyHealth(collision.collider) != null)
        {
            Explode(collision.gameObject);
            return;
        }

        // Rebote: "cuac" y un poco más de velocidad
        bounceCount++;
        PlaySfx(quackSfx);
        if (rb != null) rb.linearVelocity *= bounceBoost;

        if (bounceCount >= maxBounces) Explode();
    }

    private void Explode() => Explode(null);

    private void Explode(GameObject directHit)
    {
        if (exploded) return;
        exploded = true;

        PlaySfx(quackSfx);
        if (directHit != null) PlaySfx("shot pistol");

        WeaponPhysics.Explode(transform.position, explosionRadius, splashDamage, launchForce, upwardsLift,
                              launchStunTime, directHit, damage);

        if (explosionVfx != null)
        {
            GameObject vfx = Instantiate(explosionVfx, transform.position, Quaternion.identity);
            vfx.transform.localScale *= 1.5f; // explosión desmesurada
            Destroy(vfx, 3f);
        }

        Deactivate();
    }

    private static void PlaySfx(string name)
    {
        if (AudioManager.Instance != null && !string.IsNullOrEmpty(name)) AudioManager.Instance.PlaySfx(name);
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
