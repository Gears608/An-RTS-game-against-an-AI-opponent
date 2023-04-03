using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using TMPro;

public class PlayerClass : MonoBehaviour
{
    enum Modes { Building, Managing, Interacting, Destroying }
    [SerializeField]
    Modes mode;

    Camera playerCam; // initialises a variable to hold the camera
    [SerializeField]
    LayerMask playerUnitMask;  // initialises a variable to hold the layer mask of the players units
    [SerializeField]
    private List<UnitClass> selectedUnits = new List<UnitClass>();  // initialises a list to hold all the currently selected units
    [SerializeField]
    private RectTransform selectionBox;  // initialises a variable to hold the visual prompt of the selection box
    private Vector2 selectionBoxStartPos;  // initialises a variable which hold the starting position of the selection box
    [SerializeField]
    private WorldController worldController;

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

    public int gold;
    [SerializeField]
    private TMP_Text goldDisplay;

    private Vector2 spawnPoint;

    void Start()
    {
        playerCam = Camera.main;  // gets the main camera
        mode = Modes.Managing;
    }


    void Update()
    {
        goldDisplay.text = "Gold: " + gold.ToString();

        switch (mode) 
        {
            case Modes.Managing:
                UnitSelection();
                if (Input.GetMouseButtonDown(1))
                {
                    FindPaths();
                }
                break;
            case Modes.Building:

                BuildingLoop();

                break;
            case Modes.Destroying:
                DestroyLoop();
                break;
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
                Collider2D selection = Physics2D.OverlapPoint(mousePos);
                if (selection != null)
                {
                    if (selection.gameObject.tag == "PlayerUnit")
                    {
                        selection.transform.Find("Selected").gameObject.SetActive(true);
                        selectedUnits.Add(selection.GetComponentInParent<UnitClass>());
                    }
                    else if (selection.gameObject.tag == "PlayerBuildingUI")
                    {
                        Building z = selection.GetComponentInParent<Building>();
                        popupWindow.PopulateWindow(z.buildingName, z.infoText);
                        if (z.enableUnitMenu)
                        {
                            unitMenu.SetActive(true);
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
                    box.transform.Find("Selected").gameObject.SetActive(true);
                    selectedUnits.Add(box.GetComponentInParent<UnitClass>());
                }
                selectionBox.gameObject.SetActive(false);
            }
        }

        // while mouse held down
        if (Input.GetMouseButton(0))
        {
            selectionBoxUpdate(mousePos);
        }
    }

    /*
     * A function which updates the size of the selection box visual based on the users new cursor position
     * 
     * Vector2 newMousePos - the new mouse position
     */
    private void selectionBoxUpdate(Vector2 newMousePos)
    {
        if (!selectionBox.gameObject.activeSelf)
        {
            selectionBox.gameObject.SetActive(true);
        }

        float width = newMousePos.x - selectionBoxStartPos.x;
        float height = newMousePos.y - selectionBoxStartPos.y;
        selectionBox.anchoredPosition = selectionBoxStartPos + new Vector2(width/2, height/2);
        selectionBox.sizeDelta = new Vector2(Mathf.Abs(width), Mathf.Abs(height));
    }

    /*
     * A function which removes a unit from the selected units list
     * 
     * GameObject unit - the unit to remove
     */
    public void removeSelectedUnit(UnitClass unit)
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
            if (Input.GetMouseButtonDown(0))
            {
                if (worldController.CheckBuildingPlacement(mousePos, buildingPrefab) && !UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
                {
                    int cost = buildingPrefab.GetComponent<Building>().cost;
                    if (gold >= cost)
                    {
                        worldController.PlaceBuilding(mousePos, buildingPrefab, this);
                        gold -= cost;
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
            worldController.DestroyBuilding(mousePos);
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
        if (mode != Modes.Building)
        {
            buildingMenu.SetActive(true);
            mode = Modes.Building;
            selectionBox.gameObject.SetActive(false);
        }
        else
        {
            DisableUI();
            mode = Modes.Managing;
        }
    }

    /*
     * A function which changes the users current mode to destroying mode if not currently in building mode, or managing if the user is currently in destroying mode
     */
    public void ChangeDestroyMode()
    {
        if (mode != Modes.Destroying)
        {
            DisableUI();
            mode = Modes.Destroying;
        }
        else
        {
            mode = Modes.Managing;
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
    public void BuyUnit(GameObject unit)
    {
        UnitClass unitClass = unit.GetComponent<UnitClass>();
        if(unitClass.cost <= gold)
        {
            GameObject newUnit = Instantiate(unit);
            newUnit.GetComponent<UnitClass>().owner = this;
            gold -= unitClass.cost;
            newUnit.transform.position = spawnPoint;
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
            GameObject flockController = new GameObject("Flock");
            Flock flock = flockController.AddComponent<Flock>();
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
                    removeSelectedUnit(selectedUnits[0]);
                }
                else
                {
                    selectedUnits[0].SetPath(path, flock, mousePos);

                    for (int i = 1; i < selectedUnits.Count; i++)
                    {
                        List<HierarchicalNode> mergingPath = worldController.FindHierarchicalPathMerging(selectedUnits[i].transform.position, destinationNode, path);
                        if (mergingPath == null)
                        {
                            removeSelectedUnit(selectedUnits[i]);
                        }
                        else
                        {
                            //Debug.Log(mergingPath.Count);
                            selectedUnits[i].SetPath(mergingPath, flock, mousePos);
                        }
                    }
                }
            }

            worldController.RemoveNodeFromGraph(destinationNode);

            List<UnitClass> units = new List<UnitClass>();
            foreach (UnitClass unit in selectedUnits)
            {
                units.Add(unit);
            }

            if (units.Count > 0)
            {
                flockController.GetComponent<Flock>().flock = units;
            }
            else
            {
                Destroy(flockController);
            }
        }
    }
}