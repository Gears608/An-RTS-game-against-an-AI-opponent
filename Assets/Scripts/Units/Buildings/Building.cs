using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Building : DestroyableObject
{
    public int height;
    public int width;

    public int cost;
    public string buildingName;
    public bool enableUnitMenu = false;

    public string infoText;

    protected override void Update()
    {
        //if this building dies
        if (health <= 0)
        {
            //remove it from the owners building list
            owner.RemoveBuilding(this);
        }

        base.Update();
    }
}