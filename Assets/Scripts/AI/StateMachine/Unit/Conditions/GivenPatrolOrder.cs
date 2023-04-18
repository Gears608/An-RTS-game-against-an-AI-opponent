using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GivenPatrolOrder : Condition
{
    private UnitClass unit;
    public GivenPatrolOrder(UnitClass unit)
    {
        this.unit = unit;
    }
    public override bool Test()
    {
        return unit.patrolling;
    }
}
