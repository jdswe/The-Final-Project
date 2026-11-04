using UnityEngine;
using System.Collections.Generic;

public class FireballPool : MonoBehaviour
{
    public static FireballPool instance;

    public GameObject fireballPrefab;
    public int poolSize = 10;

    private Queue<GameObject> fireballPool = new Queue<GameObject>();

    private void Awake()
    {
        instance = this;
        if (fireballPrefab == null) { Debug.LogError("FireballPool: falta fireballPrefab"); return; }

        for (int i = 0; i < poolSize; i++)
        {
            GameObject fireball = Instantiate(fireballPrefab);
            fireball.SetActive(false);
            fireballPool.Enqueue(fireball);
        }
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    public GameObject GetFireball(Vector3 position, Quaternion rotation)
    {
        GameObject fireball;

        if (fireballPool.Count > 0)
        {
            fireball = fireballPool.Dequeue();
        }
        else
        {
            fireball = Instantiate(fireballPrefab);
        }

        fireball.transform.position = position;
        fireball.transform.rotation = rotation;
        fireball.SetActive(true);

        return fireball;
    }

    public void ReturnFireball(GameObject fireball)
    {
        if (fireball == null || !fireball.activeSelf) return; // evita encolar dos veces la misma bola
        fireball.SetActive(false);
        fireballPool.Enqueue(fireball);
    }
}
