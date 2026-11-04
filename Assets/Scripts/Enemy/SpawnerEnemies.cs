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
            Debug.Log("Spawner activado: el jugador entró en la zona.");
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

            Instantiate(enemyPrefab, spawnPoint.position, Quaternion.identity);
            
            Debug.Log("Instanciando enemigo en: " + spawnPoint.position);
            yield return new WaitForSeconds(Random.Range(2f, 5f)); 
        }
    }
}
