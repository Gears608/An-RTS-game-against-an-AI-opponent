using System.Collections.Generic;
using UnityEditor;
using UnityEngine;


public class playerClass : MonoBehaviour
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
                unit.transform.GetComponent<SpriteRenderer>().color = Color.green;
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
                box.transform.GetComponent<SpriteRenderer>().color = Color.red;
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

    /*
     * 
     *  Pathfinding Functions
     * 
     */

    private void FindPaths()
    {
        List<HierarchicalNode> path = worldController.FindHierarchicalPath(selectedUnits[0].transform.position, GetMousePositionInWorld());
        selectedUnits[0].GetComponent<UnitClass>().SetPath(path);
        for (int i = 1; i < selectedUnits.Count; i++)
        {
            selectedUnits[i].GetComponent<UnitClass>().SetPath(worldController.FindHierarchicalPathMerging(selectedUnits[i].transform.position, GetMousePositionInWorld(), path));
        }
    }
}