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

    public string infoText;

    protected override void Start()
    {
        base.Start();
    }
}
