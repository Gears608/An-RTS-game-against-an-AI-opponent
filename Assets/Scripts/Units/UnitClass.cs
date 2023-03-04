using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class UnitClass : MonoBehaviour
{
    public float movementSpeed;

    public WorldController worldController;

    private Stack<HierarchicalNode> hierarchicalPath;
    private TileMap<Vector2> currentFlowField;

    private Rigidbody2D rb;

    public bool floworint = false;

    private void Start()
    {
        hierarchicalPath = new Stack<HierarchicalNode>();
        rb = GetComponent<Rigidbody2D>();

    }

    public void SetPath(List<HierarchicalNode> hierarchicalPath)
    {
        this.hierarchicalPath.Clear();
        currentFlowField = null;

        HierarchicalNode n;

        if (hierarchicalPath.Count == 1)
        {
            //generate first flowfield
            this.hierarchicalPath.Push(hierarchicalPath[0]);
            n = this.hierarchicalPath.Peek();
            currentFlowField = worldController.GetFlowField(transform.position, new Vector2(n.x, n.y));
        }
        else
        {
            for (int i = 0; i < hierarchicalPath.Count - 1; i = i + 2)
            {
                this.hierarchicalPath.Push(hierarchicalPath[i]);
            }

            //generate first flowfield
            n = this.hierarchicalPath.Peek();
            currentFlowField = worldController.GetFlowField(transform.position, new Vector2(n.x, n.y));
        }
    }


    public void FixedUpdate()
    {
        CheckPath();
        //transform.Translate(FlowFieldSteering()*movementSpeed*Time.deltaTime);
        rb.velocity = FlowFieldSteering().normalized*movementSpeed;
    }

    private void CheckPath()
    {
        //pathfinding
        if (hierarchicalPath.Count > 1)
        {
            //if the unit is no longer on the flowfield
            if (worldController.components.GetObject(transform.position) == hierarchicalPath.Peek().component)
            {
                //if the unit is on the correct component
                if (worldController.components.GetObject(transform.position) == hierarchicalPath.Peek().component)
                {
                    //generate the next path
                    if (hierarchicalPath.Count > 1)
                    {
                        hierarchicalPath.Pop();
                    }
                    HierarchicalNode n = hierarchicalPath.Peek();
                    currentFlowField = worldController.GetFlowField(transform.position, new Vector2(n.x, n.y));
                }
                //if the unit has been pushed onto an incorrect tile
                else
                {
                    //recalculate the path

                    //we can change this to calculate a merging a* onto the original path

                    while (hierarchicalPath.Count > 1)
                    {
                        hierarchicalPath.Pop();
                    }
                    HierarchicalNode n = hierarchicalPath.Peek();
                    SetPath(worldController.FindHierarchicalPathMerging(transform.position, new Vector2(n.x, n.y), new List<HierarchicalNode>(hierarchicalPath)));
                }
            }
        }
    }

    private Vector2 FlowFieldSteering()
    {
        //movement
        if (currentFlowField != null)
        {
            //if the unit is on the correct component but is in an inaccessible sector
            if (currentFlowField.GetObject(transform.position) == default(Vector2))
            {
                //recalculate the path

                //we can change this to calculate a merging a* onto the original path

                while (hierarchicalPath.Count > 1)
                {
                    hierarchicalPath.Pop();
                }
                HierarchicalNode n = hierarchicalPath.Peek();
                SetPath(worldController.FindHierarchicalPath(transform.position, new Vector2(n.x, n.y)));
            }
            else
            {
                //move the unit
                Vector2 flow = currentFlowField.GetObject(transform.position);
                if (flow == new Vector2(2, 2))
                {
                    //Debug.Log("Destination Reached.");
                    hierarchicalPath.Pop();
                    currentFlowField = null;
                    return Vector2.zero;
                }
                else
                {
                    return flow;
                }
            }
        }

        return Vector2.zero;
    }

    private void OnDrawGizmosSelected()
    {
        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.alignment = TextAnchor.MiddleCenter;
        Gizmos.color = Color.yellow;

        if (hierarchicalPath != null)
        {
            if (hierarchicalPath.Count > 0)
            {
                Stack<HierarchicalNode> reverse = new Stack<HierarchicalNode>();

                while (hierarchicalPath.Count > 0)
                {
                    reverse.Push(hierarchicalPath.Pop());
                }

                if (reverse.Count > 0)
                {
                    Gizmos.DrawCube(new Vector3(reverse.Peek().x, reverse.Peek().y, -1) + Vector3.one / 2, Vector2.one);
                    while (reverse.Count > 1)
                    {
                        HierarchicalNode n = reverse.Pop();
                        Gizmos.DrawLine(new Vector2(n.x, n.y) + Vector2.one / 2, new Vector2(reverse.Peek().x, reverse.Peek().y) + Vector2.one / 2);
                        Gizmos.DrawCube(new Vector3(reverse.Peek().x, reverse.Peek().y, -1) + Vector3.one / 2, Vector2.one);

                        hierarchicalPath.Push(n);
                    }
                    hierarchicalPath.Push(reverse.Pop());
                }

                Gizmos.DrawLine((Vector2)transform.position + Vector2.one / 2, new Vector2(hierarchicalPath.Peek().x, hierarchicalPath.Peek().y) + Vector2.one / 2);
            }
        }
        if (currentFlowField != null) 
        {
            if (floworint)
            {
                for (int x = 0; x < currentFlowField.GetTileWidth(); x++)
                {
                    for (int y = 0; y < currentFlowField.GetTileHeight(); y++)
                    {
                        //flow
                        Gizmos.DrawLine(currentFlowField.GetWorldPositionFromIndex(x,y) + Vector2.one / 2, currentFlowField.GetWorldPositionFromIndex(x, y) + (Vector2)currentFlowField.GetObject(x,y)/2 + Vector2.one / 2);
                        Gizmos.DrawCube(currentFlowField.GetWorldPositionFromIndex(x, y) + Vector2.one / 2, Vector2.one/5);
                        //Handles.Label(currentFlowField.GetWorldPositionFromIndex(x, y) + Vector2.one / 2, ((Vector2)currentFlowField.GetObject(x, y)).ToString(), style);
                    }
                }
            }
            /*
            else
            {
                for (int x = 0; x < currentIntegrationField.GetTileWidth(); x++)
                {
                    for (int y = 0; y < currentIntegrationField.GetTileHeight(); y++)
                    {
                        //integration
                        Handles.Label(currentIntegrationField.GetWorldPositionFromIndex(x, y) + Vector2.one / 2, currentIntegrationField.GetObject(x, y).ToString(), style);
                    }
                }
            }
            */
        }
    }

}
