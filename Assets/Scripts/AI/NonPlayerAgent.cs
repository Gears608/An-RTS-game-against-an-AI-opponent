using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NonPlayerAgent : PlayerClass
{
    private int prodPerTick; //the amount of curreny being generated per tick

    [SerializeField]
    private GameObject minePrefab;
    [SerializeField]
    private GameObject barracksPrefab;
    [SerializeField]
    private GameObject towerPrefab;

    [SerializeField]
    private GameObject unitPrefab;

    [SerializeField]
    private float buildingSearchRadius;

    private int mineCost;
    private int towerCost;
    private int barracksCost;

    [SerializeField]
    float buildRadius;

    [SerializeField]
    private PlayerAgent player;

    private enum ActionType { Barracks, Mine, Tower, Unit }

    [SerializeField]
    private List<Action> buildingPriorities = new List<Action>()
    {
        new Action(ActionType.Mine, 0),
        new Action(ActionType.Barracks, 0),
        new Action(ActionType.Tower, 0),
        new Action(ActionType.Unit, 0)
    };

    private void Start()
    {
        currentBarracks = 0;
        currentMines = 0;
        currentTowers = 0;

        mineCost = minePrefab.GetComponent<Building>().cost;
        barracksCost = barracksPrefab.GetComponent<Building>().cost;
        towerCost = towerPrefab.GetComponent<Building>().cost;
    }

    protected void Update()
    {
        if (!worldController.IsGamePaused())
        {
            CurrencySpendTree();
            UnitManagementTree();
        }
    }

    private void CurrencySpendTree()
    {
        ActionType type = GetBestChoice();
        if (type == ActionType.Unit)
        {
            //unit logic
            Barracks barracks = GetEmptyBarracks();
            if (barracks != null)
            {
                BuyUnit(barracks.transform.position, unitPrefab, barracks);
            }
        }
        else
        {
            if (RemainingBuilds(type))
            {
                Location placementLocation = ChooseLocation(GetBuildingPrefab(type));
                if (placementLocation.valid)
                {
                    if (gold >= GetBuildingCost(type))
                    {
                        PlaceBuilding(type, placementLocation.position);
                        IncreaseBuildingCounter(type);
                    }
                }
            }
        }
    }

    private void CalculatePriorities()
    {
        //sets the priority for a defensive building
        if (currentTowers < maxTowers)
        {
            GetInfo(ActionType.Tower).priority = NearbyThreatsCount() + 1;
        }
        else
        {
            GetInfo(ActionType.Tower).priority = 0;
        }

        if (currentMines < maxMines)
        {
            //sets the priority of a mine/currency generator
            if (prodPerTick == 0)
            {
                GetInfo(ActionType.Mine).priority = float.MaxValue;
            }
            else
            {
                GetInfo(ActionType.Mine).priority = (1f / prodPerTick * 50f) + 1;
            }
        }
        else
        {
            GetInfo(ActionType.Mine).priority = 0;
        }

        if (allUnits.Count < GetMaxUnits())
        {
            GetInfo(ActionType.Unit).priority = Mathf.Max((player.GetUnitCount() * 1.2f) - allUnits.Count, 1);
            GetInfo(ActionType.Barracks).priority = 0;
        }
        else
        {
            //Debug.Log(currentBarracks+" < "+maxBarracks);
            if (currentBarracks < maxBarracks)
            {
                GetInfo(ActionType.Barracks).priority = Mathf.Max((player.GetUnitCount() * 1.2f) - allUnits.Count, 1);
                GetInfo(ActionType.Unit).priority = 0;
            }
            else
            {
                GetInfo(ActionType.Barracks).priority = 0;
            }
        }
    }

    private ActionType GetBestChoice()
    {
        ActionType bestChoice = ActionType.Mine;
        float bestChoiceCost = 0;

        CalculatePriorities();

        foreach (Action building in buildingPriorities)
        {
            //Debug.Log(building.type.ToString() +" : "+building.priority);
            if (building.priority > bestChoiceCost)
            {
                bestChoiceCost = building.priority;
                bestChoice = building.type;
            }
        }

        return bestChoice;
    }

    private Location ChooseLocation(GameObject buildingPrefab)
    {
        Vector2 position = new Vector2();
        bool valid = false;
        //limits the attempts to find a position each frame reduce CPU load
        int attempts = 0;
        while (!valid && attempts < 100)
        {
            position = Random.insideUnitCircle * buildRadius;
            position += (Vector2)transform.position;

            if (worldController.CheckBuildingPlacement(position, buildingPrefab))
            {
                valid = true;
            }

            attempts++;
        }

        return new Location(position, valid);
    }

    private void PlaceBuilding(ActionType type, Vector2 position)
    {
        gold -= GetBuildingCost(type);

        if (type == ActionType.Mine)
        {
            prodPerTick++;
        }

        List<AIUnit> idleUnits = GetCurrentlyIdleUnits();
        foreach (AIUnit unit in idleUnits)
        {
            if (unit.IsPatrolling())
            {
                unit.StopMoving();
            }
        }

        allBuildings.Add(worldController.PlaceBuilding(position, GetBuildingPrefab(type), this));
    }

    private GameObject GetBuildingPrefab(ActionType type)
    {
        switch (type)
        {
            case ActionType.Mine:
                return minePrefab;
            case ActionType.Barracks:
                return barracksPrefab;
            case ActionType.Tower:
                return towerPrefab;
        }

        return null;
    }

    private void IncreaseBuildingCounter(ActionType type)
    {
        switch (type)
        {
            case ActionType.Mine:
                currentMines++;
                break;
            case ActionType.Barracks:
                currentBarracks++;
                break;
            case ActionType.Tower:
                currentTowers++;
                break;
        }
    }

    private bool RemainingBuilds(ActionType type)
    {
        switch (type)
        {
            case ActionType.Mine:
                return currentMines < maxMines;
            case ActionType.Barracks:
                return currentBarracks < maxBarracks;
            case ActionType.Tower:
                return currentTowers < maxTowers;
        }

        return false;
    }

    private Action GetInfo(ActionType type)
    {
        foreach (Action info in buildingPriorities)
        {
            if (info.type == type)
            {
                return info;
            }
        }

        return null;
    }

    private int GetBuildingCost(ActionType type)
    {
        switch (type)
        {
            case ActionType.Mine:
                return mineCost;
            case ActionType.Barracks:
                return barracksCost;
            case ActionType.Tower:
                return towerCost;
        }

        return -1;
    }

    private int NearbyThreatsCount()
    {
        HashSet<DestroyableObject> nearbyUnits = new HashSet<DestroyableObject>();

        foreach (Building building in allBuildings)
        {
            List<DestroyableObject> units = building.GetNearbyObjects(buildingSearchRadius, player.playerUnitMask);
            foreach (UnitClass unit in units)
            {
                nearbyUnits.Add(unit);
            }
        }

        return nearbyUnits.Count;
    }

    private int GetMaxUnits()
    {
        int maxUnits = 0;

        foreach (Building building in allBuildings)
        {
            if (building is Barracks)
            {
                Barracks barracks = building.GetComponent<Barracks>();
                maxUnits += barracks.maxUnitCount;
            }
        }
        return maxUnits;
    }

    private void BuyUnit(Vector2 position, GameObject prefab, Barracks home)
    {
        UnitClass unitClass = prefab.GetComponent<UnitClass>();
        if (unitClass.cost <= gold)
        {
            GameObject newUnit = Instantiate(prefab);
            AIUnit newUnitClass = newUnit.GetComponent<AIUnit>();
            allUnits.Add(newUnitClass);
            newUnitClass.owner = this;
            home.AddUnit(newUnitClass);
            gold -= unitClass.cost;
            newUnit.transform.position = position;
        }
    }

    private Barracks GetEmptyBarracks()
    {
        foreach (Building building in allBuildings)
        {
            if (building is Barracks)
            {
                Barracks barracks = building.GetComponent<Barracks>();
                if (barracks.unitCount < barracks.maxUnitCount)
                {
                    return barracks;
                }
            }
        }

        return null;
    }

    /*
     * A function which removes a unit from the units list
     * 
     * GameObject unit - the unit to remove
     */
    public override void RemoveBuilding(Building building)
    {
        if (building is Barracks)
        {
            currentBarracks--;
        }
        else if (building is GoldMine)
        {
            currentMines--;
        }
        else if (building is Tower)
        {
            currentTowers--;
        }

        allBuildings.Remove(building);
    }

    private void UnitManagementTree()
    {
        List<AIUnit> idleUnits = GetCurrentlyIdleUnits();

        if (idleUnits.Count > 0)
        {
            Building building = ChooseBuildingForAttack(CalculateCurrentStrength());
            if (building != null)
            {
                List<UnitClass> attackForce = UnitsForAttack(building.transform.position, CalculateBuildingStrength(building), idleUnits);
                IssueAttackCommand(building, attackForce);
            }
        }

        //get an updated list of idle units 
        idleUnits = GetCurrentlyIdleUnits();

        //for each of the remain idle units
        foreach (AIUnit unit in idleUnits)
        {
            //if the unit is ready to patrol
            if (unit.ReadyForPatrol())
            {
                //find a position to patrol to
                Vector2 position = Random.insideUnitCircle * buildRadius;
                position += (Vector2)transform.position;
                //get path to position
                List<HierarchicalNode> path = worldController.FindHierarchicalPath(transform.position, position);
                if (path != null)
                {
                    unit.SetPatrolRoute(path, position);
                }
            }
        }
    }
    private List<AIUnit> GetCurrentlyIdleUnits()
    {
        List<AIUnit> idleUnits = new List<AIUnit>();
        foreach (AIUnit unit in allUnits)
        {
            if (unit.IsIdle() || unit.IsPatrolling())
            {
                idleUnits.Add(unit);
            }
        }

        return idleUnits;
    }

    private Building ChooseBuildingForAttack(float currentStrength)
    {
        Building bestChoice = null;
        float choicePriority = 0;
        foreach (Building building in player.allBuildings)
        {
            //if the building is currently being attacked already
            if (!building.beingAttacked)
            {
                //get the cost of the new building
                float newPriority = CalculateBuildingPriority(building) * 1.2f;
                //if the new building is weaker than the current avaliable forces
                if (CalculateBuildingStrength(building) < currentStrength)
                {
                    //if the new building is a better choice
                    if (choicePriority < newPriority)
                    {
                        //set new best choice
                        choicePriority = newPriority;
                        bestChoice = building;
                    }
                }
            }
        }
        return bestChoice;
    }

    private float CalculateBuildingPriority(Building building)
    {
        float distance = Vector2.Distance(transform.position, building.transform.position);
        //priority scales on distance from base and strength around building
        return (1f / distance) * CalculateBuildingStrength(building);
    }

    private float CalculateBuildingStrength(Building building)
    {
        float threatLevel = building.threatLevel;
        List<DestroyableObject> nearbyUnits = building.GetNearbyObjects(buildingSearchRadius, player.playerUnitMask);
        foreach (DestroyableObject enemy in nearbyUnits)
        {
            threatLevel += enemy.threatLevel;
        }
        return threatLevel;
    }

    private float CalculateCurrentStrength()
    {
        float strength = 0f;

        foreach (UnitClass unit in allUnits)
        {
            strength += unit.threatLevel;
        }

        return strength;
    }

    private List<UnitClass> UnitsForAttack(Vector2 destination, float strength, List<AIUnit> idleUnits)
    {
        List<UnitClass> units = new List<UnitClass>();
        List<AttackingAgent> agents = new List<AttackingAgent>();
        float currentStrength = 0f;

        foreach (UnitClass unit in idleUnits)
        {
            //precompute distances before comparing
            agents.Add(new AttackingAgent(unit, Vector2.Distance(unit.transform.position, destination)));
        }

        agents.Sort(SortAgentByDistance);

        foreach(AttackingAgent agent in agents)
        {
            currentStrength += agent.unit.threatLevel;
            units.Add(agent.unit);
            //if there is a large enough strength of units availiable
            if(currentStrength >= strength)
            {
                return units;
            }
        }
        //if not enough units are avaliable
        return null;
    }

    private int SortAgentByDistance(AttackingAgent agent1, AttackingAgent agent2)
    {
        return agent1.distance.CompareTo(agent2.distance);
    }

    private void IssueAttackCommand(Building building, List<UnitClass> selectedUnits)
    {
        Vector2 destination = building.transform.position;
        if(FindPaths(destination, selectedUnits))
        {
            building.beingAttacked = true;
        }
    }

    public bool FindPaths(Vector2 destination, List<UnitClass> selectedUnits)
    {
        //checks if there are units selected and the position is valid
        if (selectedUnits.Count > 0 && worldController.IsValidPosition(destination))
        {
            GameObject groupController = new GameObject("UnitGroup");
            UnitGroup group = groupController.AddComponent<UnitGroup>();
            List<HierarchicalNode> path = null;
            HierarchicalNode destinationNode = worldController.AddNodeToGraph(destination);

            while (path == null && selectedUnits.Count > 0)
            {
                //if destination is unreachable
                if (destinationNode == null)
                {
                    selectedUnits.Clear();
                    continue;
                }

                path = worldController.FindHierarchicalPath(selectedUnits[0].transform.position, destinationNode);

                //if no path is found
                if (path == null)
                {
                    selectedUnits.RemoveAt(0);
                }
                else
                {
                    selectedUnits[0].SetPath(path, group, destination);

                    for (int i = 1; i < selectedUnits.Count; i++)
                    {
                        List<HierarchicalNode> mergingPath = worldController.FindHierarchicalPathMerging(selectedUnits[i].transform.position, destinationNode, path);
                        if (mergingPath == null)
                        {
                            selectedUnits.RemoveAt(i);
                        }
                        else
                        {
                            //Debug.Log(mergingPath.Count);
                            selectedUnits[i].SetPath(mergingPath, group, destination);
                        }
                    }
                }
            }

            worldController.RemoveNodeFromGraph(destinationNode);

            if (selectedUnits.Count > 0)
            {
                groupController.GetComponent<UnitGroup>().group = selectedUnits;
                return true;
            }
            else
            {
                Destroy(groupController);
                return false;
            }
        }
        else
        {
            return false;
        }
    }

    private class Location
    {
        public Vector2 position;
        public bool valid;

        public Location(Vector2 position, bool valid)
        {
            this.valid = valid;
            this.position = position;
        }
    }

    private class Action
    {
        public ActionType type;
        public float priority;

        public Action(ActionType type, float priority)
        {
            this.type = type;
            this.priority = priority;
        }
    }

    private class AttackingAgent
    {
        public UnitClass unit;
        public float distance;

        public AttackingAgent(UnitClass unit, float distance) 
        {
            this.unit = unit;
            this.distance = distance;
        }
    }
}