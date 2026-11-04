using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class HealthManager : MonoBehaviour
{
    public float maxHealth;
    public float currentHealth;
    
    public bool hasHit;

    public Slider healthSlider;

    [Tooltip("Se invoca una vez cuando la vida llega a 0")]
    public UnityEvent onDeath;

    public bool IsDead => isDead;
    public float HealthPercent => maxHealth > 0 ? Mathf.Clamp01(currentHealth / maxHealth) : 0f;

     private bool isDead = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        currentHealth = maxHealth;
        
        if (healthSlider != null)
        {
            healthSlider.maxValue = maxHealth;
            healthSlider.value = currentHealth;
        }
    }


    public void takeDamage(float damageAmmount)
    {
        if (TryGetComponent<EnemyDodge>(out var dodge) && dodge.isDodging)
        {
            Debug.Log("🛡️ Daño evitado por dodge");
            return;
        }

        if (isDead) return; // Evita que tome daño muerto
        currentHealth = Mathf.Max(0f, currentHealth - damageAmmount);

        if(healthSlider != null)
        {
            healthSlider.value = currentHealth;
        }

        if (currentHealth <= 0)
        {
            Die();
        }

    }

    public void Die()
    {
        if (isDead) return; // Protege de doble muerte
        isDead = true;

        onDeath?.Invoke();

        if (CompareTag("Player"))
        {
            gameObject.SetActive(false);
            if (GameManager.instance != null) GameManager.instance.GameOver();
            else UnityEngine.SceneManagement.SceneManager.LoadScene("GameOver");
        }
        else if (CompareTag("Enemy"))
        {
            // Enemigos del nivel 1
            if (TryGetComponent<EnemyBase>(out var enemy)) enemy.Die();
            // Jefa del nivel 2 (Mamá)
            else if (TryGetComponent<MomEnemyController>(out var mom)) mom.Die();

        }
    }

    
    
}
