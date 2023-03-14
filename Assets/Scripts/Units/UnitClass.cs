using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class UnitClass : MonoBehaviour
{
    public WorldController worldController;

    private Stack<HierarchicalNode> hierarchicalPath;
    private TileMap<Vector2> currentFlowField;

    public Rigidbody2D rb;

    public bool floworint = false;

    public Vector2 destination;

    public Seperation seperation;
    public Flock flock;

    public float maxForce = 5;
    public float maxSpeed = 4;

    [SerializeField]
    private bool moving;

    public float seperationRadius = 3;
    public float radius = 0.5f;

    private void Start()
    {
        moving = false;
        hierarchicalPath = new Stack<HierarchicalNode>();
        rb = GetComponent<Rigidbody2D>();
    }

    public void SetPath(List<HierarchicalNode> hierarchicalPath, Flock flock, Vector2 destination)
    {
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
            n = hierarchicalPath[hierarchicalPath.Count-1];
            destination = new Vector2(n.x, n.y);
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
        
        Vector2 flowSteer = FlowFieldSteering();
        Vector2 cohesionSteer = new Vector2();
        Vector2 alignmentSteer = new Vector2();
        Vector2 seperationSteer = seperation.GetSeperation();

        if (moving)
        {
            if (flock != null)
            {
                cohesionSteer = SeekingSteering(flock.CohesionSteering(this));
                alignmentSteer = Steer(flock.AlignmentSteering(this));
            }

        }

        Debug.DrawLine(transform.position, (Vector2)transform.position + flowSteer, Color.blue);
        Debug.DrawLine(transform.position, (Vector2)transform.position + cohesionSteer * 0.05f, Color.yellow);
        Debug.DrawLine(transform.position, (Vector2)transform.position + alignmentSteer * 0.3f, Color.green);
        Debug.DrawLine(transform.position, (Vector2)transform.position + seperationSteer * 1.2f, Color.black);

        Vector2 velocity = flowSteer + (cohesionSteer * 0.05f) + (alignmentSteer * 0.3f) + (seperationSteer * 1.2f);
        //Debug.Log(flowSteer + ", "+ cohesionSteer + ", "+ alignmentSteer + ", " + seperationSteer);

        if (velocity.magnitude > maxForce)
        {
            velocity = velocity.normalized * maxForce;
        }

        //velocity += rb.velocity;

        if(velocity.magnitude > maxSpeed)
        {
            velocity = velocity * (maxForce / velocity.magnitude);
        }

        Debug.DrawLine(transform.position, (Vector2)transform.position + velocity, Color.red);

        rb.AddForce(velocity, ForceMode2D.Force);
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
                    List<HierarchicalNode> remainingPath = new List<HierarchicalNode>(hierarchicalPath);
                    while (hierarchicalPath.Count > 1)
                    {
                        hierarchicalPath.Pop();
                    }
                    HierarchicalNode n = hierarchicalPath.Peek();
                    SetPath(worldController.FindHierarchicalPathMerging(transform.position, new Vector2(n.x, n.y), remainingPath), flock, destination);
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
            if (currentFlowField.GetObject(transform.position) == default(Vector2))
            {
                //recalculate the path

                while (hierarchicalPath.Count > 1)
                {
                    hierarchicalPath.Pop();
                }
                HierarchicalNode n = hierarchicalPath.Peek();
                SetPath(worldController.FindHierarchicalPath(transform.position, new Vector2(n.x, n.y)),flock, destination);
            }
            else
            {
                //move the unit
                Vector2Int index = currentFlowField.GetIndexFromWorldPosition(transform.position);
                Vector2 flow = currentFlowField.GetObject(index.x, index.y);
                /*
                flow += currentFlowField.GetObject(index.x+1, index.y);
                flow += currentFlowField.GetObject(index.x+1, index.y+1);
                flow += currentFlowField.GetObject(index.x+1, index.y-1);
                flow += currentFlowField.GetObject(index.x-1, index.y);
                flow += currentFlowField.GetObject(index.x-1, index.y+1);
                flow += currentFlowField.GetObject(index.x-1, index.y-1);
                flow += currentFlowField.GetObject(index.x, index.y+1);
                flow += currentFlowField.GetObject(index.x, index.y-1);

                flow /= 9;
                */

                if (flow == new Vector2(2, 2))
                {
                    //if the unit has reached its destination
                    //moving = false;
                    //flock.RemoveUnit(this);
                    //flock = null;
                    //hierarchicalPath.Pop();
                    //currentFlowField = null;
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
