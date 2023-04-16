using System.Collections.Generic;
using UnityEngine;

public class NonPlayerAgent : PlayerClass
{
    List<UnitGroup> unitGroups;

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
    public LayerMask enemyLayer;
    public List<string> targets;

    [SerializeField]
    private LayerMask friendlyLayer;
    [SerializeField]
    private List<string> friendlyTags;

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

        unitGroups = new List<UnitGroup>();
    }

    protected void Update()
    {
        if (!worldController.IsGamePaused())
        {
            //calls tree to handle resource management
            CurrencySpendTree();
            //calls tree to handle unit management
            UnitManagementTree();
        }
    }

    /*
     * A function which handles the management of the AI's resources
     */
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

    /*
     * A function which calculates the priorites of spending currency
     */
    private void CalculateActionPriorities()
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

    /*
     * A function which chooses the best choice of action to take
     * 
     * returns ActionType - the best choice of action
     */
    private ActionType GetBestChoice()
    {
        ActionType bestChoice = ActionType.Mine;
        float bestChoiceCost = 0;

        CalculateActionPriorities();

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

    /*
     * A function which chooses a random location within the AI's building radius for a given building
     * 
     * GameObject buildingPrefab - the building to be placed
     * 
     * Returns Location - the choice of location
     */
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

    /*
     * A function which places a given building type in the world at a given position
     * 
     * ActionType type - the building type to place
     * Vector2 position - the position to place building at
     */
    private void PlaceBuilding(ActionType type, Vector2 position)
    {
        gold -= GetBuildingCost(type);

        if (type == ActionType.Mine)
        {
            prodPerTick++;
        }

        //repaths patrol routes for idle units to prevent collisions and units getting stuck
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

    /*
     * A function which gets the prefab of a specified action type
     * 
     * ActionType type - the type in question
     * 
     * Returns GameObject - the prefab of type
     */
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

    /*
     * A function which increases the internal counter for the given building type
     * 
     * ActionType type - the type to increase counter of
     */
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

    /*
     * A function which calculates if there are remaining builds left of a given building type
     * 
     * ActionType type - the type of building
     * 
     * Returns bool - true if there are remaining build left of the specified type, else false
     */
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

    /*
     * A function which gets the Action for a given ActionType
     * 
     * ActionType type - the ActionType in question
     * 
     * Returns Action - the action for the given type or null if one does not exist 
     */
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

    /*
     * A function which gets the cost of a given ActionType
     * 
     * ActionType type - the type to get cost of
     * 
     * Returns int - the cost of the given type or -1 if type is not found
     */
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

    /*
     * A function which calculates the number of enemy units nearby to the base
     * 
     * Returns int - the number of enemy units
     */
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

    /*
     * A function which calculates the maximum number of units for the current count of barracks
     * 
     * Returns int - the maximum unit count
     */
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

    /*
     * A function which buys and spawns a new unit of given type at a given position
     * 
     * Vector2 position - the position to spawn at
     * GameObject prefab - the prefab of the unit to spawn
     * Barracks home - the barracks that the unit will belong to
     */
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

    /*
     * A function which gets a barracks with space for more units if one is availiable
     * 
     * Returns Barracks - an barracks with space for more units or null if none are avaliable
     */
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
     * A function which handles actions of units
     */
    private void UnitManagementTree()
    {
        List<AIUnit> idleUnits = GetCurrentlyIdleUnits();

        //check for defence orders
        foreach (Building building in allBuildings)
        {
            float buildingStrength = CalculateBuildingStrength(building, friendlyLayer, friendlyTags);
            float enemyPresence = CalculateBuildingStrength(building, enemyLayer, targets) - building.threatLevel;
            
            //if there is a large enemy presence round a building
            if(buildingStrength < enemyPresence)
            {
                //bring in more units to defend

                //if there are enough idle units to defend
                if(CalculateCurrentStrength(idleUnits) >= enemyPresence)
                {
                    //issue defend command to sublist of idle units
                    List<UnitClass> attackForce = UnitsForAttack(building.transform.position, CalculateBuildingStrength(building, enemyLayer, targets), idleUnits);
                    if (attackForce != null)
                    {
                        UnitGroup newGroup = IssueAttackCommand(building, attackForce);
                        if (newGroup != null)
                        {
                            newGroup.SetDefending();
                        }
                        foreach (AIUnit unit in attackForce)
                        {
                            unit.SetDefending();
                        }
                    }
                }
                else if(CalculateCurrentStrength(GetAllUnits()) >= enemyPresence)
                {
                    //issue defend command to sublist of all units

                    //get required units from idle units
                    List<UnitClass> attackForce = new List<UnitClass>();

                    foreach(UnitClass unit in idleUnits)
                    {
                        attackForce.Add(unit);
                    }

                    //get remaining required units from already busy units
                    List<UnitGroup> groupsToAdd = UnitGroupForDefence(building.transform.position, enemyPresence - CalculateCurrentStrength(idleUnits));

                    if (groupsToAdd != null)
                    {
                        foreach (UnitGroup group in groupsToAdd)
                        {
                            attackForce.AddRange(group.group);
                        }
                    }

                    UnitGroup newGroup = IssueAttackCommand(building, attackForce);
                    if(newGroup != null)
                    {
                        newGroup.SetDefending();
                        foreach (AIUnit unit in attackForce)
                        {
                            unit.SetDefending();
                        }
                    }
                }
                else
                {
                    //issue defend command to all units
                    UnitGroup newGroup = IssueAttackCommand(building, allUnits);
                    if (newGroup != null)
                    {
                        newGroup.SetDefending();
                        foreach (AIUnit unit in allUnits)
                        {
                            unit.SetDefending();
                        }
                    }
                }
            }

            idleUnits = GetCurrentlyIdleUnits();
        }


        //check for attack orders
        if (idleUnits.Count > 0)
        {
            Building building = ChooseBuildingForAttack(CalculateCurrentStrength(idleUnits));
            if (building != null)
            {
                List<UnitClass> attackForce = UnitsForAttack(building.transform.position, CalculateBuildingStrength(building, enemyLayer, targets), idleUnits);
                //if a sufficient force is availiable 
                if (attackForce != null)
                {
                    IssueAttackCommand(building, attackForce).SetAttacking();
                }
            }
        }

        //get an updated list of idle units 
        idleUnits = GetCurrentlyIdleUnits();

        //check for idle patrol orders
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

    /*
     * A function which gets all the units which are currently awaiting instruction
     * 
     * Returns List<AIUnit> - the idle units
     */
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

    /*
     * A function which gets all the units as AIUnits
     * 
     * Returns List<AIUnit> - all units
     */
    private List<AIUnit> GetAllUnits()
    {
        List<AIUnit> units = new List<AIUnit>();
        foreach (AIUnit unit in allUnits)
        {
            units.Add(unit);
        }

        return units;
    }

    /*
     * A function which selects the highest priority enemy structure to attack
     * 
     * float currentStrength - the current strength of the ai force as a float
     * 
     * Returns Building - the highest priority building
     */
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
                if (CalculateBuildingStrength(building, enemyLayer, targets) < currentStrength)
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

    /*
     * A function which calculates the priority of a given building
     * 
     * Building building - the building in question
     * 
     * Returns float - the priority of the building
     */
    private float CalculateBuildingPriority(Building building)
    {
        float distance = Vector2.Distance(transform.position, building.transform.position);
        //priority scales on distance from base and strength around building
        return (1f / distance) * CalculateBuildingStrength(building, enemyLayer, targets);
    }

    /*
     * A function which calculates the combat strength of a given building and its surroundings
     * 
     * Building building - the building in question
     * 
     * Returns float - the strength of the building
     */
    public float CalculateBuildingStrength(Building building, LayerMask layer, List<string> tags)
    {
        float threatLevel = 0;
        List<DestroyableObject> nearbyUnits = building.GetNearbyObjects(buildingSearchRadius, layer, tags);
        foreach (DestroyableObject enemy in nearbyUnits)
        {
            threatLevel += enemy.threatLevel;
        }
        return threatLevel;
    }

    /*
     * A function which calculates the combat strength of a given list of units
     * 
     * List<AIUnit> units - the units to get strength of 
     * 
     * Returns float - the sum strength of all units in the list
     */
    public float CalculateCurrentStrength(List<AIUnit> units)
    {
        float strength = 0f;

        foreach (AIUnit unit in units)
        {
            strength += unit.threatLevel;
        }

        return strength;
    }

    /*
     * A function which calculates a list of the best units for attacking an objective
     * 
     * Vector2 destination - the position to attack
     * float strength - the strength of the position
     * List<AIUnit> idleUnits - the list of units to select from
     * 
     * Returns List<UnitClass> - a list of the best choice of units for the attack or null if not enough units are availiable
     */
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

    /*
     * A function which calculates a list of the best units groups for defending an objective
     * 
     * Vector2 destination - the position to defend
     * float strengthRequired - the strength required to defend the position
     * 
     * Returns List<UnitGroup> - a list of the best choice of unit groups for the defence or null if not enough units are availiable
     */
    private List<UnitGroup> UnitGroupForDefence(Vector2 destination, float strengthRequired)
    {
        List<UnitGroup> output = new List<UnitGroup>();
        List<AttackingAgentGroup> agentGroups = new List<AttackingAgentGroup>();
        float currentStrength = 0f;

        foreach (UnitGroup unitGroup in unitGroups)
        {
            //checks if the group is already defending
            if (!unitGroup.IsDefending())
            {
                //precompute distances before comparing
                agentGroups.Add(new AttackingAgentGroup(unitGroup, Vector2.Distance(unitGroup.GetMidPoint(), destination)));
            }
        }

        agentGroups.Sort(SortAgentGroupByDistance);

        foreach(AttackingAgentGroup group in agentGroups)
        {
            currentStrength += group.unitGroup.GetCurrentStrength();
            output.Add(group.unitGroup);
            //if there is a large enough strength of units availiable
            if(currentStrength >= strengthRequired)
            {
                return output;
            }
        }
        //if not enough units are avaliable
        return null;
    }

    /*
     * A function for comparing two attacking agents based on their distance
     * 
     * AttackingAgent agent1 - the first agent
     * AttackingAgent agent2 - the second agent
     */
    private int SortAgentByDistance(AttackingAgent agent1, AttackingAgent agent2)
    {
        return agent1.distance.CompareTo(agent2.distance);
    }

    /*
     * A function for comparing two attacking agent groups based on their distance
     * 
     * AttackingAgentGroup agentGroup1 - the first agent
     * AttackingAgentGroup agentGroup2 - the second agent
     */
    private int SortAgentGroupByDistance(AttackingAgentGroup agentGroup1, AttackingAgentGroup agentGroup2)
    {
        return agentGroup1.distance.CompareTo(agentGroup2.distance);
    }

    /*
     * A function which issues an attack command for a given building to a given list of units
     * 
     * Building building - the building to attack
     * List<UnitClass> selectedUnits - the units selected for the attack
     */
    private UnitGroup IssueAttackCommand(Building building, List<UnitClass> selectedUnits)
    {
        UnitGroup group = FindPaths(building, selectedUnits);
        if(group != null)
        {
            building.beingAttacked = true;
        }

        return group;
    }

    /*
     * A function which issues a retreat command for a given unit group
     * 
     * UnitGroup group - the group to retreat
     */
    public void IssueRetreatCommand(UnitGroup group)
    {
        //find a position to patrol to
        Vector2 position = Random.insideUnitCircle * buildRadius;
        position += (Vector2)transform.position;
        FindPaths(position, group);
    }

    /*
     * A function which finds and applies paths to a given position to a given list of units
     * 
     * Building building - the building to path to
     * List<UnitClass> selectedUnits - the list of units to path
     * 
     * Returns bool - true if the pathing was a success or partial success else false
     */
    public UnitGroup FindPaths(Building building, List<UnitClass> selectedUnits)
    {
        Vector2 destination = worldController.GetNearbyClearTile(building.transform.position);
        //checks if there are units selected and the position is valid
        if (selectedUnits.Count > 0 && worldController.IsValidPosition(destination))
        {
            GameObject groupController = new GameObject("UnitGroup");
            UnitGroup group = groupController.AddComponent<UnitGroup>();
            group.SetDestination(building);
            group.SetOwner(this);
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
                    List<UnitClass> failedPaths = new List<UnitClass>();
                    foreach (UnitClass unit in selectedUnits)
                    {
                        List<HierarchicalNode> mergingPath = worldController.FindHierarchicalPathMerging(unit.transform.position, destinationNode, path);
                        if (mergingPath == null)
                        {
                            failedPaths.Add(unit);
                        }
                        else
                        {
                            unit.SetPath(mergingPath, group, destination);
                        }
                    }
                    foreach(UnitClass unit in failedPaths)
                    {
                        selectedUnits.Remove(unit);
                    }
                }
            }

            worldController.RemoveNodeFromGraph(destinationNode);

            if (selectedUnits.Count > 0)
            {
                groupController.GetComponent<UnitGroup>().group = selectedUnits;
                unitGroups.Add(group);
                return group;
            }
            else
            {
                Destroy(groupController);
                return null;
            }
        }
        else
        {
            return null;
        }
    }

    /*
     * A function which finds and applies paths to a given position to a given list of units
     * 
     * Vector2 destination - the position to path to
     * UnitGroup existingGroup - an existing unitgroup to apply a path to
     * 
     * Returns bool - true if the pathing was a success or partial success else false
     */
    public bool FindPaths(Vector2 destination, UnitGroup existingGroup)
    {
        List<UnitClass> selectedUnits = existingGroup.group;

        foreach(UnitClass unit in selectedUnits)
        {
            unit.StopMoving();
        }

        //checks if there are units selected and the position is valid
        if (selectedUnits.Count > 0 && worldController.IsValidPosition(destination))
        {
            existingGroup.SetDestination(null);
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
                    selectedUnits[0].SetPath(path, existingGroup, destination);

                    for (int i = 1; i < selectedUnits.Count; i++)
                    {
                        List<HierarchicalNode> mergingPath = worldController.FindHierarchicalPathMerging(selectedUnits[i].transform.position, destinationNode, path);
                        if (mergingPath == null)
                        {
                            selectedUnits.RemoveAt(i);
                        }
                        else
                        {
                            selectedUnits[i].SetPath(mergingPath, existingGroup, destination);
                        }
                    }
                }
            }

            worldController.RemoveNodeFromGraph(destinationNode);

            if (selectedUnits.Count > 0)
            {
                existingGroup.group = selectedUnits;
                return true;
            }
            else
            {
                Destroy(existingGroup.gameObject);
                return false;
            }
        }
        else
        {
            return false;
        }
    }

    /*
     * A class which holds a position and a bool if the position is valid or not
     */
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

    /*
     *  A class which holds an action type and priority of said action
     */
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

    /*
     *  A class which holds a unit and that units distance from a position
     */
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

    /*
     *  A class which holds a unit goup and that groups average distance from a position
     */
    private class AttackingAgentGroup
    {
        public UnitGroup unitGroup;
        public float distance;

        public AttackingAgentGroup(UnitGroup unitGroup, float distance)
        {
            this.unitGroup = unitGroup;
            this.distance = distance;
        }
    }
}