using UnityEngine;
using System.Collections;

public class SpawnerEnemies : MonoBehaviour
{
    [System.Serializable]
    public class ZoneSpawn
    {
        public string nameZone;
        public Transform[] pointsSpawn;
        public GameObject[] enemiesAvailable;
        public int numberEnemies;
        public string songName;
    }

    public ZoneSpawn[] zones; //Zonas de spawn
    private bool playerInside = false; // Evita múltiples activaciones

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !playerInside) // Detectar eltag del player
        {
            if (zones != null && zones.Length > 0 && AudioManager.Instance != null)
                AudioManager.Instance.PlaySfx(zones[0].songName);
            playerInside = true;
            StartSpawning();
        }
    }

    void StartSpawning()
    {
        foreach (var zone in zones)
        {
            StartCoroutine(SpawnerZone(zone));
        }
    }

    IEnumerator SpawnerZone(ZoneSpawn zone)
    {
        if (zone.pointsSpawn == null || zone.pointsSpawn.Length == 0 || zone.enemiesAvailable == null || zone.enemiesAvailable.Length == 0)
            yield break;

        for (int i = 0; i < zone.numberEnemies; i++)
        {
            Transform spawnPoint = zone.pointsSpawn[Random.Range(0, zone.pointsSpawn.Length)];
            GameObject enemyPrefab = zone.enemiesAvailable[Random.Range(0, zone.enemiesAvailable.Length)];

            if (spawnPoint == null || enemyPrefab == null) continue;

            // Aparecer ligeramente sobre el suelo para no quedar atrapado dentro del terreno
            Instantiate(enemyPrefab, spawnPoint.position + Vector3.up * 0.2f, Quaternion.identity);
            yield return new WaitForSeconds(Random.Range(2f, 5f)); 
        }
    }
}
