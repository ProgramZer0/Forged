using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Harvests : MonoBehaviour
{
    public Harvestables harvestable;

    private float RangeofHarvest;
    private Inventory inventory;
    private int hotbarSelected;

    private void Start()
    {
        RangeofHarvest = 4f;
        hotbarSelected = 0;
    }
    private void FixedUpdate()
    {
        hotbarSelected = FindObjectOfType<Controls>().getHotbarSelected();
        if (Vector3.Distance(gameObject.transform.position, FindObjectOfType<Controls>().GetPlayerPos()) < RangeofHarvest)
        {
            RaycastHit hit;
            if (Physics.Raycast(gameObject.transform.position, (FindObjectOfType<Controls>().GetPlayerPos() - transform.position), out hit, RangeofHarvest, FindObjectOfType<TerrainUtils>().getTreeMask()))
            {
                if (hit.collider.tag == "Player")
                {
                    if (Input.GetKeyDown(KeyCode.Mouse0))
                    {
                        if (hotbarSelected == 3)
                        { 
                            if (harvestable.harvestName.Contains("Wood"))
                            {
                                Debug.Log("is wood");
                                Vector3 temchop;
                                temchop = transform.position;
                                temchop.y = temchop.y + 1f;
                                FindObjectOfType<Controls>().GetAnimator().SetBool("HitWithAxeSide", true);

                                GetComponent<TreeUtils>().FellTree();
                                Destroy(gameObject);
                                for (int i = 0; i < harvestable.itemAmmount; i++)
                                {
                                    GameObject o = UnityEngine.Object.Instantiate(harvestable.dropItem.model, temchop, Quaternion.Euler(90, 90, 90));
                                    temchop.y = temchop.y + .5f;
                                }
                            }
                        }

                        if (hotbarSelected == 2)
                        {
                            if (harvestable.harvestName.Contains("Ore"))
                            {
                                Debug.Log("is ore");

                                Vector3 temchop;
                                temchop = transform.position;
                                temchop.y = temchop.y + 1f;
                                //animate.SetBool("HitWithAxeSide", true);

                                GetComponent<OreUtils>().FellOre();

                                Destroy(gameObject);
                                for (int i = 0; i < harvestable.itemAmmount; i++)
                                {
                                    GameObject o = UnityEngine.Object.Instantiate(harvestable.dropItem.model, temchop, Quaternion.Euler(90, 90, 90));
                                    temchop.y = temchop.y + 1f;
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
