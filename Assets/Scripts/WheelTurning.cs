using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WheelTurning : MonoBehaviour
{
    [SerializeField] private int WheelNumber;

    private Quaternion localRotation = Quaternion.Euler(0f, 90f, 0f);
    private int Angle = 0;
    private void Update()
    {

        //Debug.Log(Angle);
        transform.rotation = localRotation;
    }

    public void TurnUp()
    {
        Angle = Angle + 60;
        localRotation = Quaternion.Euler(0f, 90f, Angle);
        Debug.Log("turning up wheel " + WheelNumber);

        FindObjectOfType<EarthPuzzle2>().setWheelCodeUp(WheelNumber);
    }
    public void TurnDown()
    {
        Angle = Angle - 60;
        localRotation = Quaternion.Euler(0f, 90f, Angle);

        FindObjectOfType<EarthPuzzle2>().setWheelCodeDown(WheelNumber);
    }
}
