using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GivenMoveOrder : Condition
{
    private UnitClass unit;
    public GivenMoveOrder(UnitClass unit)
    {
        this.unit = unit;
    }
    public override bool Test()
    {
        return unit.attacking;
    }
}
