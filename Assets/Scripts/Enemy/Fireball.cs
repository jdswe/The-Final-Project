using UnityEngine;

public class Fireball : MonoBehaviour
{
    public float damage = 100f;
    public float speed = 6f;
    public float lifetime = 5f;
    public float detectionRadius = 2f; // Distancia mínima para hacer daño
    public GameObject explosionEffect; // Asigna un prefab de explosión
    [Tooltip("Grados por segundo que puede girar persiguiendo al jugador (para que se pueda esquivar)")]
    public float homingTurnRate = 90f;

    private Transform target;
    private bool isActive = false;

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        isActive = true;
        CancelInvoke();
        Invoke(nameof(Deactivate), lifetime);
    }

    void Update()
    {
        if (!isActive || target == null) return;

        // Movimiento teledirigido con giro limitado: antes perseguía perfecto y era imposible de esquivar
        Vector3 toTarget = (target.position + Vector3.up * 0.8f - transform.position).normalized;
        Vector3 direction = Vector3.RotateTowards(transform.forward, toTarget, homingTurnRate * Mathf.Deg2Rad * Time.deltaTime, 0f);
        transform.position += direction * speed * Time.deltaTime;
        transform.forward = direction;

        // Raycast corto para detectar si está cerca del player
        if (Physics.Raycast(transform.position, direction, out RaycastHit hit, detectionRadius, ~0, QueryTriggerInteraction.Ignore))
        {
            if (hit.collider.CompareTag("Player"))
            {
                ExplodeAndDamage(hit.collider.gameObject);
            }
        }
    }

    private void ExplodeAndDamage(GameObject targetObject)
    {
        // Comprobar si el jugador está defendiendo
        PlayerCombat combat = targetObject.GetComponent<PlayerCombat>();
        if (combat == null)
        {
            // Si no está en este GameObject, buscar en el padre (por si el collider está en un hijo)
            combat = targetObject.GetComponentInParent<PlayerCombat>();
        }

        if (combat != null && combat.isMeleeDefending)
        {
            Deactivate(); // Aun así explotamos y desaparece
            return;
        }

        // Aplicar daño si no está bloqueando
        HealthManager health = targetObject.GetComponent<HealthManager>();
        if (health == null)
        {
            health = targetObject.GetComponentInParent<HealthManager>();
        }

        if (health != null)
        {
            health.takeDamage(damage);
        }

        // Efecto de explosión
        if (explosionEffect != null)
        {
            Instantiate(explosionEffect, transform.position, Quaternion.identity);
        }

        Deactivate();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            ExplodeAndDamage(other.gameObject);
        }
    }

    private void OnEnable()
    {
        isActive = false;
        CancelInvoke();
    }

    private void OnDisable()
    {
        isActive = false;
    }

    private void Deactivate()
    {
        isActive = false;
        CancelInvoke();
        if (FireballPool.instance != null) FireballPool.instance.ReturnFireball(gameObject);
        else Destroy(gameObject);
    }
}
