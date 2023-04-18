using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NoEnemyInRange : EnemyInRange
{
    public NoEnemyInRange(UnitClass unit) : base(unit) { }

    public override bool Test()
    {
        return !base.Test();
    }
}
