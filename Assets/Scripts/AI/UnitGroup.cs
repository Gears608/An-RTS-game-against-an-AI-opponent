using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//extends flock here to apply flocking functionality to AI units
public class UnitGroup : Flock
{
    private Building destination;
    private float strength;
    private NonPlayerAgent owner;

    private void Start()
    {
        CalculateGroupStrength();
    }

    private void Update()
    {
        if (destination != null)
        {
            if (owner.CalculateBuildingStrength(destination)/1.5f > strength)
            {
                destination.beingAttacked = false;
                owner.IssueRetreatCommand(this);
            }
        }
    }

    public override void RemoveUnit(UnitClass unit)
    {
        strength -= unit.threatLevel;
        base.RemoveUnit(unit);
    }

    private float CalculateGroupStrength()
    {
        strength = 0f;
        foreach (UnitClass unit in group)
        {
            strength += unit.threatLevel;
        }

        return strength;
    }

    public void SetDestination(Building destination)
    {
        this.destination = destination;
    }
    public void SetOwner(NonPlayerAgent owner)
    {
        this.owner = owner;
    }

}
