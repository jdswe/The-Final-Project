using UnityEngine;

public class EnemyDodge : EnemyBase
{
    public float dodgeCooldown = 3f;
    public float dodgeForce = 10f;
    protected float lastDodgeTime = 0f;

    public float threatDetectionRadius = 10f; // radio de detección anticipada
    public float threatDetectionAngle = 45f;

    public float invulnerabilityDuration = 0.5f;
    public bool isDodging = false;

    public LayerMask threatLayer;

    protected override void Update()
    {
        base.Update();
        DetectThreats(); // ahora escanea cada frame
    }

    private void DetectThreats()
    {
        if (Time.time < lastDodgeTime + dodgeCooldown || isDodging) return;


        Collider[] threats = Physics.OverlapSphere(transform.position, threatDetectionRadius, threatLayer);

        if (threats.Length == 0)
        {
            return;
        }

        foreach (Collider threat in threats)
        {

            if (!threat.CompareTag("bulletDuck") && !threat.CompareTag("Canon") && !threat.CompareTag("Sword"))
            {
                continue;
            }

            Rigidbody threatRb = threat.GetComponent<Rigidbody>();
            if (threatRb != null)
            {
                Vector3 toEnemy = (transform.position - threat.transform.position).normalized;
                Vector3 threatDir = threatRb.linearVelocity.normalized;

                float angle = Vector3.Angle(threatDir, toEnemy);

                if (angle <= threatDetectionAngle)
                {
                    Dodge();
                    break;
                }
                else
                {
                }
            }
            else
            {
                Dodge();
                break;
            }
        }
    }


    protected void Dodge()
    {
        if (isDodging) return;

        isDodging = true;

        bool dodgeLeft = Random.value > 0.5f;
        Vector3 dodgeDir = Vector3.Cross((player.position - transform.position).normalized, Vector3.up);
        if (!dodgeLeft) dodgeDir = -dodgeDir;

        enemyRb.AddForce(dodgeDir * dodgeForce, ForceMode.Impulse);

        animator.SetBool("isLeft", dodgeLeft);
        animator.SetTrigger("dodge");
        animator.Play(dodgeLeft ? "monster dodge left" : "monster dodge right");

        Debug.Log("🌀 Esquivando hacia " + (dodgeLeft ? "izquierda" : "derecha"));

        StartCoroutine(DodgeCooldown());
    }

    private System.Collections.IEnumerator DodgeCooldown()
    {
        lastDodgeTime = Time.time;
        yield return new WaitForSeconds(invulnerabilityDuration);
        isDodging = false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, threatDetectionRadius);
    }
}
