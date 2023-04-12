using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class PlayerClass : MonoBehaviour
{
    [SerializeField]
    protected List<UnitClass> allUnits;
    [SerializeField]
    protected List<Building> allBuildings;
    [SerializeField]
    protected WorldController worldController;

    public int gold;

    protected Vector2 spawnPoint;

    [SerializeField]
    protected int maxBarracks;
    [SerializeField]
    protected int maxMines;
    [SerializeField]
    protected int maxTowers;

    [SerializeField]
    protected int currentMines;
    [SerializeField]
    protected int currentTowers;
    [SerializeField]
    protected int currentBarracks;

    private void Start()
    {
        allBuildings = new List<Building>();
        allUnits = new List<UnitClass>();
    }

    /*
     * A function which adds a unit to the units list
     * 
     * UnitClass unit - the unit to remove
     */
    public void AddUnit(UnitClass unit)
    {
        allUnits.Add(unit);
    }

    /*
     * A function which removes a unit from the units list
     * 
     * UnitClass unit - the unit to remove
     */
    public void RemoveUnit(UnitClass unit)
    {
        allUnits.Remove(unit);
    }

    /*
     * A function which removes a building from the buildings list
     * 
     * Building building - the building to remove
     */
    public virtual void RemoveBuilding(Building building)
    {
        allBuildings.Remove(building);
    }

    public int GetUnitCount()
    {
        return allUnits.Count;
    }
}