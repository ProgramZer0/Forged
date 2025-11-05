using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Enemy", menuName = "Assets/Enemy")]
public class Enemable : ScriptableObject
{
    public string enemyId; //ID use for generating
    public string enemyName; //For ease of view
    public EnemyType enemyType;
    public Items dropItem; //droping item
    public int itemMaxAmount; //amount dropped
    public int ViewDistance = 15; //The player detection distance 
    public float Speed = 5; //Speed of the Enemy
    public float MaxHp = 3; //Max HP of the enemy
    public float AttackPower = 1; //How hard it hits
    public float AttackSpeed = 1; //How fast it hits
    public GameObject model; //required model to spawn
}


public enum EnemyType
{
    Normal,
    Medium,
    Heavy,
    Boss
}