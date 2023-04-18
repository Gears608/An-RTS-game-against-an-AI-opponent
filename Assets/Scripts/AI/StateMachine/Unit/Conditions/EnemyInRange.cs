using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyInRange : Condition
{
    protected UnitClass unit;

    public EnemyInRange(UnitClass unit)
    {
        this.unit = unit;
    }

    public override bool Test()
    {
        return unit.attackTrigger.GetNearbyObjects().Count > 0 && !unit.retreating;
    }
}
