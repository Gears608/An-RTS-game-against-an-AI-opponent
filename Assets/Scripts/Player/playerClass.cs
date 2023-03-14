using System.Collections.Generic;
using UnityEditor;
using UnityEngine;


public class PlayerClass : MonoBehaviour
{

    Camera playerCam; // initialises a variable to hold the camera
    [SerializeField]
    LayerMask playerUnitMask;  // initialises a variable to hold the layer mask of the players units
    [SerializeField]
    private List<GameObject> selectedUnits = new List<GameObject>();  // initialises a list to hold all the currently selected units
    [SerializeField]
    RectTransform selectionBox;  // initialises a variable to hold the visual prompt of the selection box
    private Vector2 selectionBoxStartPos;  // initialises a variable which hold the starting position of the selection box
    public WorldController worldController;

    void Start()
    {
        playerCam = Camera.main;  // gets the main camera
    }


    void Update()
    {
        UnitSelection();
        if (Input.GetMouseButtonDown(1))
        {
            FindPaths();
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
        Debug.DrawRay(transform.position, mousePos - transform.position, Color.blue);

        // on mouse down
        if (Input.GetMouseButtonDown(0))
        {
            foreach(GameObject unit in selectedUnits)
            {
                unit.transform.GetComponentInChildren<SpriteRenderer>().color = Color.green;
            }
            selectedUnits.Clear();
            
            selectionBoxStartPos = mousePos;
        }

        // on mouse up
        if (Input.GetMouseButtonUp(0))
        {
            Collider2D[] unitsInArea = Physics2D.OverlapAreaAll(selectionBoxStartPos, mousePos, playerUnitMask);
            foreach(Collider2D box in unitsInArea)
            {
                box.transform.GetComponentInChildren<SpriteRenderer>().color = Color.red;
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
        unit.transform.GetComponentInChildren<SpriteRenderer>().color = Color.green;
        selectedUnits.Remove(unit);
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

        while (path == null && selectedUnits.Count > 0)
        {
            path = worldController.FindHierarchicalPath(selectedUnits[0].transform.position, mousePos);

            if(path == null)
            {
                removeSelectedUnit(selectedUnits[0]);
            }
            else
            {
                selectedUnits[0].GetComponent<UnitClass>().SetPath(path, flock, mousePos);
                for (int i = 1; i < selectedUnits.Count; i++)
                {
                    List<HierarchicalNode> mergingPath = worldController.FindHierarchicalPathMerging(selectedUnits[i].transform.position, mousePos, path);
                    if (mergingPath == null)
                    {
                        removeSelectedUnit(selectedUnits[i]);
                    }
                    else
                    {
                        Debug.Log(mergingPath.Count);
                        selectedUnits[i].GetComponent<UnitClass>().SetPath(mergingPath, flock, mousePos);
                    }
                }
            }
        }

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