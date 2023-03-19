using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class UnitClass : MonoBehaviour
{
    public WorldController worldController;

    private Stack<HierarchicalNode> hierarchicalPath;
    private TileGrid<Vector2> currentFlowField;
    
    
    private TileGrid<int> intField;

    public Rigidbody2D rb;

    public bool floworint = false;

    public Vector2 destination;

    public Seperation seperation;
    public Flock flock;

    public float maxForce = 5;
    public float maxSpeed = 4;

    public bool moving;

    public float seperationRadius = 3;
    public float radius = 0.5f;

    private void Start()
    {
        moving = false;
        hierarchicalPath = new Stack<HierarchicalNode>();
        rb = GetComponent<Rigidbody2D>();
    }

    public void StopMoving()
    {
        //Debug.Log(gameObject.name + " stopped moving");
        moving = false;
        rb.AddForce(-rb.velocity*maxForce);
        currentFlowField = null;
        hierarchicalPath.Clear();
        seperation.AlertNeighbours(flock);
    }

    public void SetPath(List<HierarchicalNode> hierarchicalPath, Flock flock, Vector2 destination)
    {
        if (Selection.Contains(gameObject))
        {
            Debug.Log("New Path Set.");
        }
        this.destination = destination;
        moving = true;
        if(this.flock == null)
        {
            this.flock = flock;
        }
        else if(this.flock != flock)
        {
            this.flock.RemoveUnit(this);
            this.flock = flock;
        }
        this.hierarchicalPath.Clear();
        currentFlowField = null;

        this.hierarchicalPath = new Stack<HierarchicalNode>(hierarchicalPath);
        if (this.hierarchicalPath.Count > 1)
        {
            if (this.hierarchicalPath.Count % 2 == 0)
            {
                this.hierarchicalPath.Pop();
            }
            else
            {
                this.hierarchicalPath.Pop();
                this.hierarchicalPath.Pop();
            }
        }
        (currentFlowField, intField) = worldController.GetFlowField(transform.position, new Vector2Int(this.hierarchicalPath.Peek().x, this.hierarchicalPath.Peek().y));
    }


    public void FixedUpdate()
    {
        CheckPath();
        
        Vector2 flowSteer = new Vector2();
        Vector2 cohesionSteer = new Vector2();
        Vector2 alignmentSteer = new Vector2();
        Vector2 seperationSteer = new Vector2();

        if (moving)
        {
            flowSteer = FlowFieldSteering();
            if (flock != null)
            {
                cohesionSteer = SeekingSteering(flock.CohesionSteering(this));
                alignmentSteer = Steer(flock.AlignmentSteering(this));
            }
            seperationSteer = seperation.GetSeperation();
        }
        else
        {
            seperationSteer = seperation.GetSeperation() * 0.05f;
        }


        //Debug.DrawLine(transform.position, (Vector2)transform.position + flowSteer, Color.blue);
        //Debug.DrawLine(transform.position, (Vector2)transform.position + cohesionSteer * 0.05f, Color.yellow);
        //Debug.DrawLine(transform.position, (Vector2)transform.position + alignmentSteer * 0.3f, Color.green);
        //Debug.DrawLine(transform.position, (Vector2)transform.position + seperationSteer * 1.2f, Color.black);

        Vector2 velocity = flowSteer + (cohesionSteer * 0.05f) + (alignmentSteer * 0.3f) + (seperationSteer * 1.2f);

        if (velocity.magnitude > maxForce)
        {
            velocity = velocity.normalized * maxForce;
        }

        //velocity += rb.velocity;

        if(velocity.magnitude > maxSpeed)
        {
            velocity = velocity * (maxForce / velocity.magnitude);
        }

        //Debug.DrawLine(transform.position, (Vector2)transform.position + velocity, Color.red);

        rb.AddForce(velocity * new Vector2(1, 0.5f), ForceMode2D.Force);
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
                    if (hierarchicalPath.Count > 2)
                    {
                        hierarchicalPath.Pop();
                        hierarchicalPath.Pop();
                    }
                    HierarchicalNode n = hierarchicalPath.Peek();
                    (currentFlowField, intField) = worldController.GetFlowField(transform.position, new Vector2Int(n.x, n.y));
                }
                //if the unit has been pushed onto an incorrect tile
                else
                {
                    if (Selection.Contains(gameObject))
                    {
                        Debug.Log("Pushed off flowfield.");
                    }
                    //recalculate the path
                    List<HierarchicalNode> currentPath = new List<HierarchicalNode>(hierarchicalPath);
                    currentPath.Reverse();
                    SetPath(worldController.FindHierarchicalPathMerging(transform.position, destination, currentPath), flock, destination);
                }
            }
        }
    }


    //returns a 
    public Vector2 FlowFieldSteering()
    {
        //movement
        if (currentFlowField != null)
        {
            //if the unit is on the correct component but is in an inaccessible sector
            //Debug.Log(currentFlowField.GetIndexFromWorldPosition(transform.position));
            //Debug.Log(currentFlowField.GetObject(transform.position));
            if (currentFlowField.GetObject(transform.position) == default(Vector2))
            {
                if (Selection.Contains(gameObject))
                {
                    Debug.Log("Inaccessable zone.");
                }
                //recalculate the path
                List<HierarchicalNode> currentPath = new List<HierarchicalNode>(hierarchicalPath);
                currentPath.Reverse();
                SetPath(worldController.FindHierarchicalPathMerging(transform.position, destination, currentPath), flock, destination);
            }
            else
            {
                //move the unit
                Vector2Int index = currentFlowField.GetIndexFromWorldPosition(transform.position);
                Vector2 flow = currentFlowField.GetObject(index.x, index.y);

                if (flow == new Vector2(2, 2) && hierarchicalPath.Count == 1)
                {
                    //if the unit has reached its destination
                    StopMoving();
                    return Vector2.zero;
                }
                else
                {
                    flow.Normalize();
                    return Steer(flow);
                }
            }
        }

        return Vector2.zero;
    }

    /*
     * a function to get the force to be applied to the unit to grant the change in velocity required to steer the object towards the given destination
     * 
     * Vector2 destination - the point to be seeked
     * 
     */
    private Vector2 SeekingSteering(Vector2 destination)
    {
        Vector2 seekForce = destination - (Vector2)transform.position;
        if (seekForce.magnitude == 0)
        {
            return Vector2.zero;
        }
        seekForce *= maxSpeed / seekForce.magnitude;
        seekForce -= rb.velocity;
        return seekForce * (maxForce / maxSpeed);
    }

    private Vector2 Steer(Vector2 direction)
    {
        Vector2 velocity = direction * maxSpeed;
        velocity -= rb.velocity;
        return velocity * (maxForce / maxSpeed);
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
                    //Gizmos.DrawCube(new Vector3(reverse.Peek().x * worldController.tileSize, reverse.Peek().y * worldController.tileSize, -1) + Vector3.one / 2, Vector2.one);
                    Gizmos.matrix = Matrix4x4.Translate(new Vector2(0, worldController.tileSize/4f));
                    Vector2 temp = worldController.tileMap.GetWorldPositionFromIndex(reverse.Peek().x, reverse.Peek().y);
                    Gizmos.DrawLine(new Vector2(temp.x - (worldController.tileSize / 2f), temp.y), new Vector2(temp.x, temp.y + (worldController.tileSize / 4f)));
                    Gizmos.DrawLine(new Vector2(temp.x + (worldController.tileSize / 2f), temp.y), new Vector2(temp.x, temp.y + (worldController.tileSize / 4f)));
                    Gizmos.DrawLine(new Vector2(temp.x - (worldController.tileSize / 2f), temp.y), new Vector2(temp.x, temp.y - (worldController.tileSize / 4f)));
                    Gizmos.DrawLine(new Vector2(temp.x + (worldController.tileSize / 2f), temp.y), new Vector2(temp.x, temp.y - (worldController.tileSize / 4f)));
                    while (reverse.Count > 1)
                    {
                        HierarchicalNode n = reverse.Pop();
                        //Gizmos.DrawCube(new Vector3(reverse.Peek().x * worldController.tileSize, reverse.Peek().y * worldController.tileSize, -1) + Vector3.one / 2, Vector2.one);
                        temp = worldController.tileMap.GetWorldPositionFromIndex(reverse.Peek().x, reverse.Peek().y);
                        Gizmos.DrawLine(new Vector2(temp.x - (worldController.tileSize / 2f), temp.y), new Vector2(temp.x, temp.y + (worldController.tileSize / 4f)));
                        Gizmos.DrawLine(new Vector2(temp.x + (worldController.tileSize / 2f), temp.y), new Vector2(temp.x, temp.y + (worldController.tileSize / 4f)));
                        Gizmos.DrawLine(new Vector2(temp.x - (worldController.tileSize / 2f), temp.y), new Vector2(temp.x, temp.y - (worldController.tileSize / 4f)));
                        Gizmos.DrawLine(new Vector2(temp.x + (worldController.tileSize / 2f), temp.y), new Vector2(temp.x, temp.y - (worldController.tileSize / 4f)));

                        Vector2 z = worldController.tileMap.GetWorldPositionFromIndex(n.x, n.y);
                        Gizmos.DrawLine(z, temp);

                        hierarchicalPath.Push(n);
                    }
                    hierarchicalPath.Push(reverse.Pop());
                }

                Vector2 t = worldController.tileMap.GetWorldPositionFromIndex(hierarchicalPath.Peek().x, hierarchicalPath.Peek().y);
                Gizmos.DrawLine((Vector2)transform.position, t);
                Gizmos.matrix = Matrix4x4.Translate(Vector2.zero);
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
                        Handles.Label(currentFlowField.GetWorldPositionFromIndex(x, y) + new Vector2(0, worldController.tileSize / 4f), (currentFlowField.GetObject(x, y)).ToString(), style);
                    }
                }
            }
            else
            {
                for (int x = 0; x < intField.GetTileWidth(); x++)
                {
                    for (int y = 0; y < intField.GetTileHeight(); y++)
                    {
                        Handles.Label(intField.GetWorldPositionFromIndex(x, y) + new Vector2(0, worldController.tileSize/4f), (intField.GetObject(x, y)).ToString(), style);
                    }
                }
            }
        }
    }

}
