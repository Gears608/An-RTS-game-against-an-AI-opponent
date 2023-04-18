using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GivenRetreatOrder : Condition
{
    private AIUnit unit;
    private NonPlayerAgent unitOwner;
    public GivenRetreatOrder(AIUnit unit)
    {
        this.unit = unit;
        unitOwner = unit.owner as NonPlayerAgent;
    }
    public override bool Test()
    {
        if (unit.group == null)
        {
            return false;
        }
        else
        {
            return unit.group.GetGroupStrength() < unitOwner.CalculateBuildingStrength(unit.group.destination, unitOwner.enemyLayer, unitOwner.targets);
        }
    }
}
