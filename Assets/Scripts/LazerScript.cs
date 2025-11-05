using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//[ExecuteInEditMode]

public class LazerScript : MonoBehaviour
{
    [SerializeField] private bool hasLight = false;
    [SerializeField] private GameObject laserObj;

    [SerializeField] private GameObject NullLaser;
    [SerializeField] private TextMesh textTip;

    private string textforTip;
    private static int RangeofTip = 5;
    private static int LaserRange = 80;
    private LineRenderer lineRenderer;
    private Vector3 Original;
    private void Start()
    {
        lineRenderer = gameObject.GetComponentInParent<LineRenderer>();
        Original = transform.position;
        Original.y = 290;
        lineRenderer.SetPosition(0, Original);
        laserObj = NullLaser;
        if (hasLight)
            DrawLazer();
    }
    private void Update()
    {
        if(hasLight)
        {
            DrawLazer();
        } 
        else
        {
            lineRenderer.SetPosition(1, Original);

            if (laserObj.name.Contains("Puzzle3Final"))
            {

                laserObj.GetComponent<LazerScriptLast>().SetLight(false);
                laserObj = NullLaser;
            }
            if (laserObj.name.Contains("Laser"))
            {

                laserObj.GetComponent<LazerScript>().SetLight(false);
                laserObj = NullLaser;
            }
        }

        if (Vector3.Distance(gameObject.transform.position, FindObjectOfType<Controls>().GetPlayerPos()) < RangeofTip)
        {
            RaycastHit hit;
            if (Physics.Raycast(gameObject.transform.position, (FindObjectOfType<Controls>().GetPlayerPos() - transform.position), out hit, RangeofTip))
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
            //Debug.Log(hit.point);

            lineRenderer.SetPosition(1, hit.point);

            if (hit.collider.gameObject.name.Contains("Puzzle3Final"))
            {
                hit.collider.gameObject.GetComponent<LazerScriptLast>().SetLight(true);
                setLaser(hit.collider.gameObject);
            }
            else if (hit.collider.gameObject.name.Contains("Laser"))
            {
                hit.collider.gameObject.GetComponent<LazerScript>().SetLight(true);
                setLaser(hit.collider.gameObject);
            }
            else
            {
                
                if (!hit.collider.gameObject.name.Contains("Laser"))
                {
                    
                    if (laserObj.name.Contains("Puzzle3Final"))
                    {

                        laserObj.GetComponent<LazerScriptLast>().SetLight(false);
                        laserObj = NullLaser;
                    }
                    if (laserObj.name.Contains("Laser"))
                    {
                        laserObj.GetComponent<LazerScript>().SetLight(false);
                        laserObj = NullLaser;
                    }
                }
            }
        }
    }

    public void SetLight(bool light)
    {
        hasLight = light;
    }

    private void setLaser(GameObject laserTemp)
    {
        laserObj = laserTemp;
    }
}
