using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EnemyGenerator : MonoBehaviour
{
    [SerializeField] private List<Enemable> CanSpawn;
    [SerializeField] private int MAX_ENEMIES;
    [SerializeField] private int START_AMOUNT;
    [SerializeField] private int TIME_IN_BETWEEN_SPAWNS;

    private int enemiesSpawned = 0;
    
    void Start()
    {
        GenerateEnemies(START_AMOUNT);
    }

    private void GenerateEnemies(int v)
    {
        for (int i = v; i > 0; i--)
        {
            Spawn(transform.position);
            enemiesSpawned++;
        }
    }

    private void Spawn(Vector3 spawnPoint)
    {
        if(enemiesSpawned <= MAX_ENEMIES)
        {
            int SpawningThing = UnityEngine.Random.Range(0, CanSpawn.Count);
            var tempRot = Quaternion.Euler(0, UnityEngine.Random.Range(-90, 90), 0);
            GameObject o = UnityEngine.Object.Instantiate(CanSpawn[SpawningThing].model, spawnPoint, tempRot);
            try
            {
                o.GetComponent<NormalEnemies>().SetEnemyGenerator(gameObject);
            }
            catch
            {
                //nothing
            }
        }
    }

    public void spawnWait(int range1, int range2, Vector3 point)
    {   
        StartCoroutine(WaitSpawn(UnityEngine.Random.Range(range1, range2)));
    }

    private IEnumerator WaitSpawn(int temp)
    {
        yield return new WaitForSeconds(temp);
        GenerateEnemies(1);
    }
}
