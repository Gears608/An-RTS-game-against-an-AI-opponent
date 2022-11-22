using System.Collections.Generic;
using UnityEngine;


public class playerClass : MonoBehaviour
{

    Camera playerCam;
    [SerializeField]
    LayerMask mask;
    [SerializeField]
    private List<Transform> selectedUnits = new List<Transform>();
    [SerializeField]
    RectTransform selectionBox;
    private Vector2 selectionBoxStartPos;

    void Start()
    {
        playerCam = Camera.main;
    }


    void Update()
    {
        unitSelection();
    }

    // for selecting and deselecting units
    void unitSelection()
    {
        Vector3 mousePos = Input.mousePosition;
        mousePos.z = 0 - transform.position.z;
        mousePos = playerCam.ScreenToWorldPoint(mousePos);
        Debug.DrawRay(transform.position, mousePos - transform.position, Color.blue);

        // on mouse down
        if (Input.GetMouseButtonDown(0))
        {
            foreach(Transform unit in selectedUnits)
            {
                unit.transform.GetComponent<SpriteRenderer>().color = Color.green;
            }
            selectedUnits.Clear();
            if (Physics.Raycast(playerCam.ScreenPointToRay(Input.mousePosition), out RaycastHit hit, 100, mask))
            {
                hit.transform.GetComponent<SpriteRenderer>().color = Color.red;
                selectedUnits.Add(hit.transform);
            }
            selectionBoxStartPos = mousePos;
        }

        // on mouse up
        if (Input.GetMouseButtonUp(0))
        {
            Collider[] unitsInArea = Physics.OverlapBox(selectionBox.transform.position, selectionBox.sizeDelta / 2, selectionBox.transform.rotation, mask);
            foreach(BoxCollider box in unitsInArea)
            {
                box.transform.GetComponent<SpriteRenderer>().color = Color.red;
                selectedUnits.Add(box.transform);
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
}