using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WheelTurning : MonoBehaviour
{
    [SerializeField] private int WheelNumber;

    private Quaternion localRotation = Quaternion.Euler(0f, 90f, 0f);
    private int Angle = 0;
    private EarthPuzzle2 earthPuzzle2Obj;

    private void Start()
    {
        earthPuzzle2Obj = FindFirstObjectByType<EarthPuzzle2>();
    }

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

        earthPuzzle2Obj.setWheelCodeUp(WheelNumber);
    }
    public void TurnDown()
    {
        Angle = Angle - 60;
        localRotation = Quaternion.Euler(0f, 90f, Angle);

        earthPuzzle2Obj.setWheelCodeDown(WheelNumber);
    }
}
