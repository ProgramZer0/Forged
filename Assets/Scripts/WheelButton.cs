using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WheelButton : MonoBehaviour
{
    [SerializeField] bool isUpButton;
    [SerializeField] GameObject wheel;
    public void Turn() {
        Debug.Log("Turning");
        if (isUpButton)
        {
            wheel.GetComponent<WheelTurning>().TurnUp();
            StartCoroutine(animateButton());
        } else
        {
            wheel.GetComponent<WheelTurning>().TurnDown();
            StartCoroutine(animateButton());
        }
    }

    private IEnumerator animateButton()
    {
        transform.position = new Vector3(transform.position.x, transform.position.y - .006f, transform.position.z);
        yield return new WaitForSeconds(1);
        transform.position = new Vector3(transform.position.x, transform.position.y + .006f, transform.position.z);
    }
}
