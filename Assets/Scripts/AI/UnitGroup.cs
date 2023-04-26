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

    /*
     * A function which calcualtes the current strength of the unit group
     * 
     * Returns float - the current threat level of the unit group
     */
    public float GetGroupStrength()
    {
        float strength = 0;

        foreach(UnitClass unit in group)
        {
            strength += unit.threatLevel;
        }

        return strength;
    }

    /*
     * A function which removes a given unit from the group
     * 
     * UnitClass unit - the unit to remove
     */
    public void RemoveUnit(UnitClass unit)
    {
        group.Remove(unit);
        if(group.Count == 0)
        {
            destination.beingAttacked = false;
        }
    }
}
