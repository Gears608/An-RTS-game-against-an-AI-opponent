using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DestroyableEntity : MonoBehaviour
{
    public int health;
    public int attackersCount;
    public float threatLevel;

    public WorldController worldController;
    [SerializeField]
    public PlayerClass owner;

    protected virtual void Start()
    {
        worldController = GameObject.Find("WorldController").GetComponent<WorldController>();
    }

    protected virtual void Update()
    {
        if(health == 0)
        {
            Destroy(gameObject);
        }
    }
}
