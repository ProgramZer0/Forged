using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(BoxCollider))]
public class TreeGen : MonoBehaviour
{
    [SerializeField] private List<Harvestables> CanSpawn;

    [SerializeField] private int TREE_STARTING_AMNT;
    [SerializeField] private int MAX_TREES;
    [SerializeField] private int MIN_DISTANCE;

    private int treesPlanted = 0;
    private int RangeofRay = 500;
    private BoxCollider SpawningBox;
    private List<Vector3> TreePositons = new List<Vector3>();
    void Start()
    {
        SpawningBox = GetComponent<BoxCollider>();
        GenerateTrees(TREE_STARTING_AMNT);
        //Debug.Log("Here at start");
        //Debug.Log(SpawningBox);
    }

    private void GenerateTrees(int v)
    {
        for(int i = v; i > 0; i--)
        {
            //Debug.Log("Gening " + i);
            if (FoundLocation())
            {
                treesPlanted++;
            }
        }
    }

    private bool FoundLocation()
    {
        if(treesPlanted < MAX_TREES)
        {
            //Debug.Log("Less than 30");

            Vector3 tempPos = new Vector3(transform.position.x + UnityEngine.Random.Range(((SpawningBox.size.x /2)* -1) , (SpawningBox.size.x / 2)), transform.position.y - 2, transform.position.z + UnityEngine.Random.Range(((SpawningBox.size.z / 2) * -1), (SpawningBox.size.z / 2)));
            //Debug.Log(tempPos);

            RaycastHit hit;
            //Debug.DrawRay(tempPos, Vector3.down, Color.red, 200, true);
            if (Physics.Raycast(tempPos, Vector3.down, out hit, RangeofRay))
            {
                if(treesPlanted == 0)
                {
                    TreePositons.Add(hit.point);
                    return true;
                }

                float dist = 0f;

                for(int i = 0; i < TreePositons.Count; i++)
                {
                    dist = Vector3.Distance(hit.point, TreePositons[i]);
                    //Debug.Log("distance for " + i + " is " + dist);
                    if (dist < MIN_DISTANCE) 
                        return false;
                    //Debug.Log(TreePositons.Count);
                }

                Spawn(hit.point);
                TreePositons.Add(hit.point);
                
                return true;
            }
        }
        return false;
    }

    private void Spawn(Vector3 spawnPoint)
    {
        int SpawningThing = UnityEngine.Random.Range(0, CanSpawn.Count);
        var tempRot = Quaternion.Euler(0, UnityEngine.Random.Range(-90, 90), 0);
        GameObject o = UnityEngine.Object.Instantiate(CanSpawn[SpawningThing].model, spawnPoint, tempRot);
        try
        {
            o.GetComponent<TreeUtils>().setGenerator(gameObject);
        }
        catch
        {

        }
        try
        {
            o.GetComponent<OreUtils>().setGenerator(gameObject);
        }
        catch
        {

        }
    }

    public void spawnTreeWait(int range1, int range2, Vector3 point)
    {
        float dist = 0f;

        for (int i = 0; i < TreePositons.Count; i++)
        {
            dist = Vector3.Distance(point, TreePositons[i]);
            
            if (dist < 10)
            {
                TreePositons.RemoveAt(i);
                treesPlanted--;
                break;
            }
        }

        StartCoroutine(WaitSpawn(UnityEngine.Random.Range(range1, range2)));
    }

    private IEnumerator WaitSpawn(int temp)
    {
        yield return new WaitForSeconds(temp);
        GenerateTrees(1);
    }
}
