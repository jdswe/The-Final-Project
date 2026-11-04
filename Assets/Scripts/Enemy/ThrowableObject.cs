using UnityEngine;

public class ThrowableObject : MonoBehaviour
{
    public float damage = 10f;

    private void Awake()
    {

        switch (gameObject.tag)
        {
            case "objectSmall":
                damage = 5f;
                break;
            case "objectMedium":
                damage = 15f;
                break;
            case "objectBig":
                damage = 30f;
                break;
        }
    }
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            // Antes solo imprimía un log: ahora sí hace daño al jugador
            HealthManager health = collision.gameObject.GetComponentInParent<HealthManager>();
            if (health != null) health.takeDamage(damage);
        }

    }
    /*private void OnTriggerEnter(Collider collision)
    {

        if (collision.CompareTag("Player"))
        {
            Debug.Log($"Golpeó al jugador con {damage} de daño");
        }
    }*/
}

