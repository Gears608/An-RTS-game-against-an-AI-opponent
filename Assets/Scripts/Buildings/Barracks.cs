using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Barracks : Building
{
    private void Start()
    {
        buildingName = "Barracks";
        infoText = "placeholder text";
    }

    public override bool EnableUnitMenu()
    {
        return true;
    }
}
