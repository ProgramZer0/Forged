using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TurntoCam : MonoBehaviour
{

    private Camera main;

    void Start()
    {
        main = FindObjectOfType<Controls>().getMainCamera();
    }

    // Update is called once per frame
    void LateUpdate()
    {
        transform.LookAt(main.transform);
        transform.Rotate(0, 180, 0);
    }
}
