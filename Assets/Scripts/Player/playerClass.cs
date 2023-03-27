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
    private List<GameObject> selectedUnits = new List<GameObject>();  // initialises a list to hold all the currently selected units
    [SerializeField]
    RectTransform selectionBox;  // initialises a variable to hold the visual prompt of the selection box
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
    private int gold;
    [SerializeField]
    private TMP_Text goldDisplay;

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

    // returns the current position of the mouse cursor on the world map
    private Vector3 GetMousePositionInWorld()
    {
        Vector3 mousePos = Input.mousePosition;
        mousePos.z = 0 - transform.position.z;
        mousePos = playerCam.ScreenToWorldPoint(mousePos);
        return mousePos;
    }

    /*
     * 
     *  Unit Selection Functions
     * 
     */

    // for selecting and deselecting units
    private void UnitSelection()
    {
        Vector3 mousePos = GetMousePositionInWorld();

        // on mouse down
        if (Input.GetMouseButtonDown(0))
        {
            foreach (GameObject unit in selectedUnits)
            {
                unit.transform.Find("Selected").gameObject.SetActive(false);
            }
            selectedUnits.Clear();

            selectionBoxStartPos = mousePos;
        }

        // on mouse up
        if (Input.GetMouseButtonUp(0))
        {
            Collider2D[] unitsInArea = Physics2D.OverlapAreaAll(selectionBoxStartPos, mousePos, playerUnitMask);
            foreach (Collider2D box in unitsInArea)
            {
                box.transform.Find("Selected").gameObject.SetActive(true);
                selectedUnits.Add(box.gameObject);
            }
            selectionBox.gameObject.SetActive(false);
        }

        // while mouse held down
        if (Input.GetMouseButton(0))
        {
            selectionBoxUpdate(mousePos);
        }
    }

    // draws the selection box
    void selectionBoxUpdate(Vector2 newMousePos)
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

    void removeSelectedUnit(GameObject unit)
    {
        unit.transform.Find("Selected").gameObject.SetActive(false);
        selectedUnits.Remove(unit);
    }

    /*
     * Building
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
                        worldController.PlaceBuilding(mousePos, buildingPrefab);
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
            if (Input.GetMouseButtonDown(1))
            {
                Vector2Int o = worldController.tileMap.GetIndexFromWorldPosition(mousePos);
                Collider2D[] y = Physics2D.OverlapCircleAll(worldController.tileMap.GetWorldPositionFromIndex(o.x, o.y) + new Vector2(0, 1 / 4f), 1 / 4f, LayerMask.GetMask("Impassable"), 1f, -1f);
                foreach (Collider2D x in y)
                {
                    Debug.Log(y);
                }
            }
        }
    }

    private void DestroyLoop()
    {
        if (Input.GetMouseButtonDown(0) && !UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
        {
            Vector3 mousePos = GetMousePositionInWorld();
            Collider2D building = Physics2D.OverlapPoint(mousePos);
            if(building != null)
            {
                if(building.gameObject.tag == "PlayerBuilding")
                {
                    Destroy(building.gameObject);
                }
            }
        }
    }

    /*
     * UI
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

    private void DisableUI()
    {
        selectionBox.gameObject.SetActive(false);
        buildingVisual.SetActive(false);
        buildingMenu.SetActive(false);
        buildingPrefab = null;
    }

    public void SetBuildingPrefab(GameObject buildingPrefab)
    {
        buildingVisual.GetComponent<SpriteRenderer>().sprite = buildingPrefab.GetComponentInChildren<SpriteRenderer>().sprite;
        buildingVisual.SetActive(true);
        this.buildingPrefab = buildingPrefab;
    }

    /*
     * 
     *  Pathfinding Functions
     * 
     */

    private void FindPaths()
    {
        GameObject flockController = new GameObject("Flock");
        Flock flock = flockController.AddComponent<Flock>();
        List<HierarchicalNode> path = null;
        Vector2 mousePos = GetMousePositionInWorld();

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
            if(path == null)
            {
                removeSelectedUnit(selectedUnits[0]);
            }
            else
            {
                selectedUnits[0].GetComponent<UnitClass>().SetPath(path, flock, mousePos);

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
                        selectedUnits[i].GetComponent<UnitClass>().SetPath(mergingPath, flock, mousePos);
                    }
                }
            }
        }

        worldController.RemoveNodeFromGraph(destinationNode);

        List<UnitClass> units = new List<UnitClass>();
        foreach (GameObject unit in selectedUnits)
        {
            units.Add(unit.GetComponent<UnitClass>());
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