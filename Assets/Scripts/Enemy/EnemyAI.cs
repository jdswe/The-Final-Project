using UnityEngine;
using System.Collections;

/// <summary>
/// Jefe del nivel 1 (Cam, el Colosal CiberBully): mantiene distancia, lanza bolas de fuego
/// teledirigidas y esquiva los proyectiles del jugador.
/// </summary>
public class EnemyAI : EnemyDodge
{
    public Transform throwPoint;
    public float throwCooldown = 3f;
    public float minimumDistance = 7f;

    [Header("Muerte del jefe")]
    public float deathDelay = 2.5f;
    public string nextSceneOnDeath = "cinematic 3";

    protected float lastThrowTime = 0f;
    private bool isThrowing = false;

    protected override void Update()
    {
        if (AIBlocked() || isThrowing)
        {
            SetAnimBool("isRunning", false);
            return;
        }

        float distance = Vector3.Distance(transform.position, player.position);

        if (distance > detectionRange)
        {
            StopMoving();
            return;
        }

        if (distance > attackRange)
        {
            if (distance > minimumDistance)
                MoveTowardsPlayer();
            else
                StopMoving();

            if (!isAttacking && Time.time >= lastThrowTime + throwCooldown)
            {
                lastThrowTime = Time.time;
                StartCoroutine(AttackSequence());
            }
        }
        else
        {
            StopMoving();
        }

        // El jefe también esquiva (antes nunca lo hacía porque Update no llamaba a la base)
        DetectThreats();
    }

    protected override void OnInterrupted()
    {
        base.OnInterrupted();
        isThrowing = false;
    }

    public override IEnumerator AttackSequence()
    {
        isAttacking = true;
        isThrowing = true;

        if (animator != null) animator.CrossFade("monster trow", 0.1f);

        yield return new WaitForSeconds(0.5f); // Tiempo para coincidir con animación

        if (!AIBlocked()) LaunchFireball();

        yield return new WaitForSeconds(0.5f); // Espera antes de permitir otro ataque

        isThrowing = false;
        isAttacking = false;
    }

    private void LaunchFireball()
    {
        if (throwPoint == null)
        {
            Debug.LogWarning("⚠️ throwPoint no está asignado en el inspector.");
            return;
        }

        if (FireballPool.instance == null)
        {
            Debug.LogWarning("⚠️ No hay FireballPool en la escena.");
            return;
        }

        Vector3 spawnPos = throwPoint.position + throwPoint.forward * 0.5f;
        Vector3 direction = (player.position - spawnPos).normalized;
        Quaternion rot = Quaternion.LookRotation(direction);

        GameObject fireball = FireballPool.instance.GetFireball(spawnPos, rot);

        // Ignorar colisiones con el enemigo
        Collider fireballCol = fireball.GetComponent<Collider>();
        if (fireballCol != null)
        {
            foreach (Collider col in GetComponentsInChildren<Collider>())
            {
                if (col != null) Physics.IgnoreCollision(fireballCol, col);
            }
        }

        Fireball fireballScript = fireball.GetComponent<Fireball>();
        if (fireballScript != null)
        {
            fireballScript.SetTarget(player);
        }
    }

    public override void Die()
    {
        if (isDead) return;
        isDead = true;
        StopAllCoroutines();
        OnInterrupted();
        StopMoving();
        PlayDeathAnimation();
        StartCoroutine(BossDeathSequence());
    }

    private IEnumerator BossDeathSequence()
    {
        // Deja ver la animación de muerte antes de la cinemática
        yield return new WaitForSeconds(deathDelay);

        if (GameManager.instance != null) GameManager.instance.LoadScene(nextSceneOnDeath);
        else UnityEngine.SceneManagement.SceneManager.LoadScene(nextSceneOnDeath);
    }
}
