using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NotMoving : Condition
{
    private UnitClass unit;
    public NotMoving(UnitClass unit)
    {
        this.unit = unit;
    }
    public override bool Test()
    {
        return !unit.patrolling && !unit.attacking && !unit.defending && !unit.retreating;
    }
}
