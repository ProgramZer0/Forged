using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//[ExecuteInEditMode]
public class LazerScriptFirst : MonoBehaviour
{
    [SerializeField] private GameObject laserObj = null;
    [SerializeField] private TextMesh textTip;

    private string textforTip;
    private static int RangeofTip = 5;
    private static int LaserRange = 80;
    private LineRenderer lineRenderer;
    private Vector3 Original;
    private Controls playerControls;
    private void Start()
    {
        playerControls = FindFirstObjectByType<Controls>();
        lineRenderer = gameObject.GetComponentInParent<LineRenderer>();
        Original = transform.position;
        Original.y = 290;
        lineRenderer.SetPosition(0, Original);
        DrawLazer();
    }
    private void Update()
    {
        DrawLazer();
        if (Vector3.Distance(gameObject.transform.position, playerControls.GetPlayerPos()) < RangeofTip)
        {
            RaycastHit hit;
            if (Physics.Raycast(gameObject.transform.position, (playerControls.GetPlayerPos() - transform.position), out hit, RangeofTip))
            {
                if (hit.collider.tag == "Player")
                {
                    textforTip = "Press R to turn";
                    textTip.text = textforTip;
                    if (Input.GetKeyDown(KeyCode.R))
                    {
                        transform.rotation = Quaternion.Euler(0, transform.rotation.eulerAngles.y + 45, 0);
                    }
                }
            }
        }
        else
        {
            textforTip = "";
            textTip.text = textforTip;
        }
    }


    [ContextMenu("Turn 45")]
    private void turn45()
    {
        transform.rotation = Quaternion.Euler(0, transform.rotation.eulerAngles.y + 45, 0);
    }

    private void DrawLazer()
    {
        RaycastHit hit;

        Vector3 pos = transform.position;
        pos.y = 290;

        if (Physics.Raycast(pos, transform.TransformDirection(Vector3.left), out hit, LaserRange))
        {
            
            lineRenderer.SetPosition(1, hit.point);

            try
            {
                if (hit.collider.gameObject.GetComponent<LazerScript>())
                {
                    hit.collider.gameObject.GetComponent<LazerScript>().SetLight(true);
                    setLaser(hit.collider.gameObject);
                }
                else
                {
                    if (laserObj.GetComponent<LazerScript>())
                    {

                        laserObj.GetComponent<LazerScript>().SetLight(false);
                        laserObj = null;
                    }
                }
            }
            catch
            {
                //nothin
            }
        }
    }
    private void setLaser(GameObject laserTemp)
    {
        laserObj = laserTemp;
    }
}
