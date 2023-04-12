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
        if(health == 0)
        {
            foreach(UnitClass unit in units)
            {
                unit.health = 0;
            }
        }

        //owner.gold -= unitCount;

        base.Update();
    }

    public List<UnitClass> GetUnits()
    {
        return units;
    }

    public void AddUnit(UnitClass unit)
    {
        units.Add(unit);
        unit.home = this;
        unitCount++;
    }

    public void RemoveUnit(UnitClass unit)
    {
        units.Remove(unit);
        unit.home = null;
        unitCount--;
    }
}
