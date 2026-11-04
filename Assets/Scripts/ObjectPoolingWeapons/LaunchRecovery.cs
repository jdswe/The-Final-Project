using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Se añade en tiempo de ejecución a enemigos sin EnemyBase (p. ej. la mamá) para que puedan
/// salir volando: apaga sus scripts de IA, activa la física y luego los recupera de pie.
/// </summary>
public class LaunchRecovery : MonoBehaviour
{
    private bool launched;
    private Coroutine routine;

    public void Launch(Rigidbody rb, Vector3 impulse, float stunTime)
    {
        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(LaunchRoutine(rb, impulse, Mathf.Max(0.3f, stunTime)));
    }

    IEnumerator LaunchRoutine(Rigidbody rb, Vector3 impulse, float stunTime)
    {
        bool wasKinematic = rb.isKinematic;
        RigidbodyConstraints constraints = rb.constraints;
        bool usedGravity = rb.useGravity;

        // Apagar IA (todo MonoBehaviour excepto vida y este) mientras vuela
        List<Behaviour> disabled = new List<Behaviour>();
        if (!launched)
        {
            foreach (MonoBehaviour mb in GetComponents<MonoBehaviour>())
            {
                if (mb == this || mb is HealthManager || !mb.enabled) continue;
                mb.enabled = false;
                disabled.Add(mb);
            }
        }
        launched = true;

        rb.isKinematic = false;
        rb.useGravity = true;
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        rb.linearVelocity = Vector3.zero;
        rb.AddForce(impulse, ForceMode.VelocityChange);

        yield return new WaitForSeconds(stunTime);

        // Recuperarse: de pie y con su configuración original
        if (!wasKinematic) rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
        rb.isKinematic = wasKinematic;
        rb.useGravity = usedGravity;
        rb.constraints = constraints;
        Vector3 euler = transform.eulerAngles;
        transform.rotation = Quaternion.Euler(0f, euler.y, 0f);

        // Reactivar la IA (si murió en el aire, su secuencia de muerte sigue corriendo igualmente)
        foreach (Behaviour b in disabled) if (b != null) b.enabled = true;

        launched = false;
        routine = null;
    }
}
