using UnityEngine;

/// <summary>
/// Ralentiza (o congela si el porcentaje es 1) a un enemigo que implemente IMovable.
/// Usado por el "Cañón de Glitches". Si ya está ralentizado, renueva la duración.
/// </summary>
public class SlowEffect : MonoBehaviour
{
    private float originalSpeed; // Velocidad original del enemigo
    private float slowedSpeed; // Velocidad reducida durante el efecto
    private float slowDuration; // Duración del efecto
    private bool isSlowed = false;
    private IMovable movable; // Referencia al movimiento del enemigo
    private Animator animator;
    private float originalAnimatorSpeed = 1f;
    [SerializeField] private ParticleSystem slowVfx;

    /// <summary>True mientras está congelado del todo (útil para que la IA no ataque).</summary>
    public bool IsFrozen { get; private set; }
    public bool IsSlowed => isSlowed;

    void Awake()
    {
        movable = GetComponent<IMovable>();
        animator = GetComponent<Animator>();
        if (movable == null)
        {
            Debug.LogError("❌ El enemigo no implementa la interfaz IMovable.");
        }
    }

    public void ApplySlow(float slowPercentage, float duration)
    {
        if (movable == null) return;
        slowPercentage = Mathf.Clamp01(slowPercentage);

        if (isSlowed)
        {
            // Renovar la duración en lugar de acumular
            CancelInvoke(nameof(ResetSpeed));
            Invoke(nameof(ResetSpeed), duration);
            return;
        }

        originalSpeed = movable.Speed;
        slowedSpeed = originalSpeed * (1f - slowPercentage); // Reducir la velocidad según el porcentaje
        slowDuration = duration;

        movable.Speed = slowedSpeed;
        isSlowed = true;
        IsFrozen = slowPercentage >= 0.99f;

        // La animación también se congela / ralentiza
        if (animator != null)
        {
            originalAnimatorSpeed = animator.speed;
            animator.speed = originalAnimatorSpeed * Mathf.Max(0f, 1f - slowPercentage);
        }

        // Activa las partículas
        if (slowVfx != null)
        {
            slowVfx.Play();
        }

        Invoke(nameof(ResetSpeed), slowDuration);
    }

    private void ResetSpeed()
    {
        movable.Speed = originalSpeed;
        isSlowed = false;
        IsFrozen = false;
        if (animator != null) animator.speed = originalAnimatorSpeed;
        if (slowVfx != null) slowVfx.Stop();
    }
}
