using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitGroup
{
    public List<UnitClass> group;
    public Building destination;

    public UnitGroup(List<UnitClass> group, Building destination)
    {
        this.group = group;
        this.destination = destination;
    }

    public float GetGroupStrength()
    {
        float strength = 0;

        foreach(UnitClass unit in group)
        {
            strength += unit.threatLevel;
        }

        return strength;
    }

    public void RemoveUnit(UnitClass unit)
    {
        group.Remove(unit);
        if(group.Count == 0)
        {
            destination.beingAttacked = false;
        }
    }
}
