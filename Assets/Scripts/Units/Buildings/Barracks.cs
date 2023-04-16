using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Barracks : Building
{
    public int unitCount;
    public int maxUnitCount;

    [SerializeField]
    private List<UnitClass> units;

    public GameObject unitPrefab;
    protected override void Start()
    {
        base.Start();
        units = new List<UnitClass>();
        enableUnitMenu = true;
        buildingName = "Barracks";
        infoText = "placeholder text";
    }

    protected override void Update()
    {
        //if this barracks dies
        if(health <= 0)
        {
            //destroy all units associated with it
            foreach(UnitClass unit in units)
            {
                unit.health = 0;
            }
        }

        base.Update();
    }

    /*
     * A function which gets all the units belonging to this barracks
     * 
     * Returns List<UnitClass> - a list of units associated with this barracks
     */
    public List<UnitClass> GetUnits()
    {
        return units;
    }

    /*
     * A function which assigns a given unit to this barracks
     * 
     * UnitClass unit - the unit to assign
     */
    public void AddUnit(UnitClass unit)
    {
        units.Add(unit);
        unit.home = this;
        unitCount++;
    }

    /*
     * A function which unassign a given unit with this barracks
     * 
     * UnitClass unit - the unit to unassign
     */
    public void RemoveUnit(UnitClass unit)
    {
        units.Remove(unit);
        unit.home = null;
        unitCount--;
    }
}
