using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Enemable))]
[RequireComponent(typeof(Rigidbody))]
public class Enemy : MonoBehaviour
{
    private GameObject Generator;
    private Enemable scriptable;

    public void Awake()
    {
        scriptable = GetComponent<Enemable>();
    }

    public bool CanSeePlayer()
    {
        return Physics.Raycast(gameObject.transform.position, (FindFirstObjectByType<Controls>().GetPlayerPos()), scriptable.ViewDistance);
    }

    public void DetectPlayer()
    {
        if (CanSeePlayer())
        {

        }
    }

    public void SetEnemyGenerator(GameObject temp)
    {
        Generator = temp;
    }
    


    public void Roam()
    {
        Vector3 direction = RandomRotateVector();
        float moveForce = UnityEngine.Random.Range(0.2f,4);

        Move(direction, moveForce, false);
    }

    public void Move(Vector3 direction, float durration, bool isChasing)
    {
        RaycastHit hit;
        if(!Physics.Raycast(transform.position, direction, out hit, durration * scriptable.Speed))
        {
            gameObject.GetComponent<Rigidbody>().AddRelativeForce(direction * durration);  
        }
        else
        {
            if(Vector3.Distance(transform.position, hit.point) < 3)
            {
                TurnTowards(direction, RandomRotateVector());
            }
            else
            {
                
            }
        }
        

        if (isChasing)
        {
            //find new location then move,
        }
    }

    private IEnumerator TurnTowards(Vector3 fromDirection, Vector3 turningDirection)
    {
        Vector3 turningVectors;
        float turnSpeed = Vector3.Distance(fromDirection, turningDirection) * 10;
        turningVectors = turningDirection / turnSpeed;
        turningDirection = turningVectors;
        for (int i = 0; i > turnSpeed; i++)
        {
            transform.rotation *= Quaternion.FromToRotation(fromDirection, turningDirection);
            turningDirection = turningVectors + turningDirection;
            yield return new WaitForSeconds(.1f);
        }
    }

    public Vector3 RandomRotateVector()
    {
        return new Vector3(UnityEngine.Random.Range(-1, 1), 0, UnityEngine.Random.Range(-1, 1));
    }
}
