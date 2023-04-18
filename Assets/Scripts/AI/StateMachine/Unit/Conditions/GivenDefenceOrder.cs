using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GivenDefenceOrder : Condition
{
    private UnitClass unit;
    public GivenDefenceOrder(UnitClass unit)
    {
        this.unit = unit;
    }
    public override bool Test()
    {
        return unit.defending;
    }
}
