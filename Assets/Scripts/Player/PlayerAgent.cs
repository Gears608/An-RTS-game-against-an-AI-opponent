using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerAgent : PlayerClass
{
    private enum State { Building, Managing, Destroying, Interacting }
    [SerializeField]
    private State mode;

    protected Camera playerCam; // initialises a variable to hold the camera
    [SerializeField]
    protected LayerMask UILayer;
    [SerializeField]
    private List<UnitClass> selectedUnits = new List<UnitClass>();  // initialises a list to hold all the currently selected units
    [SerializeField]
    private RectTransform selectionBox;  // initialises a variable to hold the visual prompt of the selection box
    private Vector2 selectionBoxStartPos;  // initialises a variable which hold the starting position of the selection box

    [SerializeField]
    private GameObject buildingPrefab;
    [SerializeField]
    private GameObject buildingVisual;
    [SerializeField]
    private GameObject buildingMenu;
    [SerializeField]
    private PopupWindow popupWindow;
    [SerializeField]
    private GameObject unitMenu;
    [SerializeField]
    private GameObject unitMenuContent;
    [SerializeField]
    private GameObject unitMenuItem;

    [SerializeField]
    private TMP_Text goldDisplay;

    private void Start()
    {
        playerCam = Camera.main;  // gets the main camera
        mode = State.Managing;
        currentBarracks = 0;

        allUnits = new List<UnitClass>();
    }


    protected virtual void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            worldController.PauseGame();
        }

        if (!worldController.IsGamePaused())
        {
            goldDisplay.text = "Gold: " + gold.ToString();

            switch (mode)
            {
                case State.Managing:
                    UnitSelection();
                    if (Input.GetMouseButtonDown(1))
                    {
                        FindPaths();
                    }
                    break;
                case State.Building:
                    BuildingLoop();
                    break;
                case State.Destroying:
                    DestroyLoop();
                    break;
                case State.Interacting:
                    break;
            }
        }
    }

    /*
     * A function which gets the current mouse position and converts it to a world position
     *
     * Returns a Vector3 world position of the cursor
     */
    private Vector3 GetMousePositionInWorld()
    {
        Vector3 mousePos = Input.mousePosition;
        mousePos.z = 0 - transform.position.z;
        mousePos = playerCam.ScreenToWorldPoint(mousePos);
        return mousePos;
    }

    /*
     * 
     *  Unit Management Functions
     * 
     */

    /*
     * A function which handles the selection process for units.
     */
    private void UnitSelection()
    {
        Vector3 mousePos = GetMousePositionInWorld();

        // on mouse down
        if (Input.GetMouseButtonDown(0))
        {
            foreach (UnitClass unit in selectedUnits)
            {
                unit.transform.Find("Selected").gameObject.SetActive(false);
            }
            selectedUnits.Clear();

            selectionBoxStartPos = mousePos;
        }

        // on mouse up
        if (Input.GetMouseButtonUp(0))
        {
            if (Vector3.Distance(selectionBoxStartPos, mousePos) < 0.1f)
            {
                Collider2D selection = Physics2D.OverlapPoint(mousePos, UILayer);
                if (selection != null)
                {
                    if (selection.tag == "PlayerUnitUI")
                    {
                        selection.transform.parent.Find("Selected").gameObject.SetActive(true);
                        selectedUnits.Add(selection.transform.parent.GetComponentInParent<UnitClass>());
                    }
                    else if (selection.gameObject.tag == "PlayerBuildingUI")
                    {
                        Building z = selection.GetComponentInParent<Building>();
                        popupWindow.PopulateWindow(z.buildingName, z.infoText);
                        mode = State.Interacting;
                        if (z.enableUnitMenu)
                        {
                            unitMenu.SetActive(true);
                            if (z.buildingName == "Barracks")
                            {
                                Barracks y = selection.GetComponentInParent<Barracks>();
                                int x = y.maxUnitCount - y.unitCount;
                                if (x > 0)
                                {
                                        GameObject newMenuItem = Instantiate(unitMenuItem);
                                        newMenuItem.transform.SetParent(unitMenuContent.transform);
                                        newMenuItem.transform.localScale = Vector3.one;
                                        Button newMenuItemButton = newMenuItem.GetComponentInChildren<Button>();
                                        newMenuItemButton.onClick.AddListener(delegate { BuyUnit(y.unitPrefab, newMenuItemButton, y); });
                                }
                                else
                                {
                                    unitMenu.SetActive(false);
                                    popupWindow.PopulateWindow(z.buildingName, "All units deployed");
                                }
                            }

                            spawnPoint = selection.ClosestPoint(mousePos);
                        }

                    }
                }
            }
            else
            {
                Collider2D[] unitsInArea = Physics2D.OverlapAreaAll(selectionBoxStartPos, mousePos, playerUnitMask);
                foreach (Collider2D box in unitsInArea)
                {
                    if (box.tag == "PlayerUnitUI")
                    {
                        box.transform.parent.Find("Selected").gameObject.SetActive(true);
                        selectedUnits.Add(box.transform.parent.GetComponentInParent<UnitClass>());
                    }
                }
                selectionBox.gameObject.SetActive(false);
            }
        }

        // while mouse held down
        if (Input.GetMouseButton(0))
        {
            SelectionBoxUpdate(mousePos);
        }
    }

    /*
     * A function which updates the size of the selection box visual based on the users new cursor position
     * 
     * Vector2 newMousePos - the new mouse position
     */
    private void SelectionBoxUpdate(Vector2 newMousePos)
    {
        if (!selectionBox.gameObject.activeSelf)
        {
            selectionBox.gameObject.SetActive(true);
        }

        float width = newMousePos.x - selectionBoxStartPos.x;
        float height = newMousePos.y - selectionBoxStartPos.y;
        selectionBox.anchoredPosition = selectionBoxStartPos + new Vector2(width / 2, height / 2);
        selectionBox.sizeDelta = new Vector2(Mathf.Abs(width), Mathf.Abs(height));
    }

    /*
     * A function which removes a unit from the selected units list
     * 
     * GameObject unit - the unit to remove
     */
    public void RemoveSelectedUnit(UnitClass unit)
    {
        unit.transform.Find("Selected").gameObject.SetActive(false);
        selectedUnits.Remove(unit);
    }

    /*
     * Building
     */

    /*
     * A function which handles the building mode
     */
    private void BuildingLoop()
    {
        Vector3 mousePos = GetMousePositionInWorld();
        buildingVisual.transform.position = worldController.WorldToGridPosition(mousePos + new Vector3(0, 0.5f));

        if (buildingPrefab != null)
        {
            if (Input.GetMouseButtonDown(1))
            {
                buildingPrefab = null;
                buildingVisual.SetActive(false);
            }
            if (Input.GetMouseButtonDown(0) && !UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
            {
                if (worldController.CheckBuildingPlacement(mousePos, buildingPrefab))
                {
                    Building script = buildingPrefab.GetComponent<Building>();
                    if (gold >= script.cost)
                    {
                        switch (script.buildingName)
                        {
                            case "Barracks":
                                if(currentBarracks < maxBarracks)
                                {
                                    currentBarracks++;
                                }
                                else
                                {
                                    Debug.Log("Max Barracks Placed.");
                                    return;
                                }
                                break;
                            case "Gold Mine":
                                if (currentMines < maxMines)
                                {
                                    currentMines++;
                                }
                                else
                                {
                                    Debug.Log("Max Mines Placed.");
                                    return;
                                }
                                break;
                            case "Tower":
                                if (currentTowers < maxTowers)
                                {
                                    Debug.Log("Max Towers Placed.");
                                    currentTowers++;
                                }
                                else
                                {
                                    return;
                                }
                                break;
                        }

                        Building building = worldController.PlaceBuilding(mousePos, buildingPrefab, this);

                        if (building != null)
                        {
                            allBuildings.Add(building);
                            gold -= script.cost;
                        }
                    }
                    else
                    {
                        Debug.Log("Not enough gold!");
                    }
                }
                else
                {
                    Debug.Log("Cannot place here!");
                }
            }
        }
    }

    /*
     * A function which handles the destroying mode
     */
    private void DestroyLoop()
    {
        if (Input.GetMouseButtonDown(0) && !UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
        {
            Vector3 mousePos = GetMousePositionInWorld();
            Building building = worldController.DestroyBuilding(mousePos);
            if (building != null)
            {
                if (building.tag == "PlayerBuilding")
                {
                    allBuildings.Remove(building);
                    building.health = 0;
                }
            }
        }
    }

    /*
     * UI
     */

    /*
     * A function which changes the users current mode to building mode if not currently in building mode, or managing if the user is currently in building mode
     */
    public void ChangeBuildingMode()
    {
        if (!worldController.IsGamePaused())
        {
            if (mode != State.Building)
            {
                buildingMenu.SetActive(true);
                mode = State.Building;
                selectionBox.gameObject.SetActive(false);
            }
            else
            {
                DisableUI();
                mode = State.Managing;
            }
        }
    }

    /*
     * A function which changes the users current mode to destroying mode if not currently in building mode, or managing if the user is currently in destroying mode
     */
    public void ChangeDestroyMode()
    {
        if (!worldController.IsGamePaused())
        {
            if (mode != State.Destroying)
            {
                DisableUI();
                mode = State.Destroying;
            }
            else
            {
                mode = State.Managing;
            }
        }
    }

    /*
     * A function which clears all optional UI elements
     */
    public void DisableUI()
    {
        selectionBox.gameObject.SetActive(false);
        unitMenu.SetActive(false);
        popupWindow.gameObject.SetActive(false);
        buildingVisual.SetActive(false);
        buildingMenu.SetActive(false);
        buildingPrefab = null;
        foreach (Transform child in unitMenuContent.transform)
        {
            Destroy(child.gameObject);
        }
        mode = State.Managing;
    }

    /*
     * A function which sets the current building prefab of the player
     * 
     * GameObject buildingPrefab - the prefab of the gameobject to be set to
     */
    public void SetBuildingPrefab(GameObject buildingPrefab)
    {
        buildingVisual.GetComponent<SpriteRenderer>().sprite = buildingPrefab.GetComponentInChildren<SpriteRenderer>().sprite;
        buildingVisual.SetActive(true);
        this.buildingPrefab = buildingPrefab;
    }

    /*
     * A function which buys a unit and spawns them into the world
     * 
     * GameObject unit - the prefab of the unit to buy
     */
    public void BuyUnit(GameObject unit, Button button, Barracks home)
    {
        UnitClass unitClass = unit.GetComponent<UnitClass>();
        if (unitClass.cost <= gold)
        {
            GameObject newUnit = Instantiate(unit);
            UnitClass newUnitClass = newUnit.GetComponent<UnitClass>();
            allUnits.Add(newUnitClass);
            newUnitClass.owner = this;
            home.AddUnit(newUnitClass);
            gold -= unitClass.cost;
            newUnit.transform.position = spawnPoint;
        }

        if(home.unitCount == home.maxUnitCount)
        {
            Destroy(button.transform.parent.gameObject);
            unitMenu.SetActive(false);
            popupWindow.PopulateWindow(home.buildingName, "All units deployed");
        }
    }

    /*
     * 
     *  Pathfinding Functions
     * 
     */

    /*
     * A function which finds paths for all the currently selected units from their current positions to the mouse position
     */
    private void FindPaths()
    {
        Vector2 mousePos = GetMousePositionInWorld();
        //checks if there are units selected and the position is valid
        if (selectedUnits.Count > 0 && worldController.IsValidPosition(mousePos))
        {
            List<HierarchicalNode> path = null;
            HierarchicalNode destinationNode = worldController.AddNodeToGraph(mousePos);

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
                    //removes the unit from selected units since it cannot path to the destination
                    RemoveSelectedUnit(selectedUnits[0]);
                }
                else
                {
                    //sets the path for the leader unit
                    selectedUnits[0].SetPath(path, mousePos);
                    selectedUnits[0].attacking = true;
                    //finds and sets merging paths for all subsequent units
                    for (int i = 1; i < selectedUnits.Count; i++)
                    {
                        List<HierarchicalNode> mergingPath = worldController.FindHierarchicalPathMerging(selectedUnits[i].transform.position, destinationNode, path);
                        //removes the unit if no path can be found
                        if (mergingPath == null)
                        {
                            RemoveSelectedUnit(selectedUnits[i]);
                        }
                        else
                        {
                            selectedUnits[i].SetPath(mergingPath, mousePos);
                            selectedUnits[i].attacking = true;
                        }
                    }
                }
            }
            worldController.RemoveNodeFromGraph(destinationNode);
        }
    }
}
