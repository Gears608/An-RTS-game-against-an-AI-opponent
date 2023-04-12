using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Building : DestroyableEntity
{
    public int height;
    public int width;

    public int cost;
    public string buildingName;
    public bool enableUnitMenu = false;

    [SerializeField]
    private LayerMask enemyLayer;
    [SerializeField]
    private float searchRadius;

    public string infoText;

    protected override void Start()
    {
        base.Start();
    }

    protected override void Update()
    {
        base.Update();

        if(health == 0)
        {
            owner.RemoveBuilding(this);
        }
    }

    public UnitClass[] GetNearbyEnemies()
    {
        Collider2D[] nearby = Physics2D.OverlapCircleAll(transform.position, searchRadius);
        UnitClass[] nearbyUnits = new UnitClass[nearby.Length];
        for (int i = 0; i < nearby.Length; i++)
        {
            nearbyUnits[i] = nearby[i].GetComponent<UnitClass>();
        }

        return nearbyUnits;
    }
}
