using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Barracks : Building
{
    protected override void Start()
    {
        base.Start();
        enableUnitMenu = true;
        buildingName = "Barracks";
        infoText = "placeholder text";
    }
}
