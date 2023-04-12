using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class UnitClass : DestroyableEntity
{
    private enum State { Moving, Attacking, Idle }
    private State currentState;

    private Stack<HierarchicalNode> hierarchicalPath;
    private TileGrid<Vector2> currentFlowField;

    public Rigidbody2D rb;
    [SerializeField]
    private SpriteRenderer spriteRenderer;

    public bool floworint = false;

    public Vector2 destination;

    public Flock flock;

    public float maxForce;
    public float maxSpeed;

    public float seperationRadius = 3;
    public float radius = 0.5f;

    public int cost;

    public Barracks home;

    protected override void Start()
    {
        base.Start();
        worldController.AddUnit(this);
        hierarchicalPath = new Stack<HierarchicalNode>();
        rb = GetComponent<Rigidbody2D>();
        currentState = State.Idle;
    }

    protected override void Update()
    {
        if (!worldController.IsGamePaused())
        {
            if (health == 0)
            {
                if (home != null)
                {
                    home.RemoveUnit(this);
                }
                worldController.RemoveUnit(this);
                if (owner != null)
                {
                    if (owner is PlayerAgent)
                    {
                        PlayerAgent owner_ = owner.GetComponent<PlayerAgent>();
                        owner_.RemoveSelectedUnit(this);
                    }
                }
                if (flock != null)
                {
                    flock.RemoveUnit(this);
                }
                owner.RemoveUnit(this);
            }
            base.Update();
            CheckPath();
        }
    }

    public void FixedUpdate()
    {
        Vector2 flowSteer = new Vector2();
        Vector2 cohesionSteer = new Vector2();
        Vector2 alignmentSteer = new Vector2();
        Vector2 seperationSteer = new Vector2();

        if (currentState == State.Moving)
        {
            flowSteer = FlowFieldSteering();
            if (flock != null)
            {
                cohesionSteer = SeekingSteering(flock.CohesionSteering(this));
                alignmentSteer = (flock.AlignmentSteering(this));
            }
            seperationSteer = worldController.GetSeperation(this);
        }
        else
        {
            seperationSteer = worldController.GetSeperation(this) * 0.1f;
        }


        //Debug.DrawLine(transform.position, (Vector2)transform.position + flowSteer, Color.blue);
        //Debug.DrawLine(transform.position, (Vector2)transform.position + cohesionSteer, Color.yellow);
        //Debug.DrawLine(transform.position, (Vector2)transform.position + alignmentSteer, Color.green);
        //Debug.DrawLine(transform.position, (Vector2)transform.position + seperationSteer, Color.black);

        Vector2 direction = flowSteer + (cohesionSteer * 0.1f) + (alignmentSteer * 0.3f) + (seperationSteer * 1.2f);

        Vector2 desiredVelocity = direction * 5;

        if (desiredVelocity.magnitude > maxSpeed)
        {
            desiredVelocity = direction.normalized * 5;
        }

        rb.AddForce(desiredVelocity);

        if (rb.velocity.magnitude > maxSpeed)
        {
            rb.velocity = rb.velocity.normalized * maxSpeed;
        }

        if (rb.velocity.x > 0.1)
        {
            spriteRenderer.flipX = false;
        }
        else if (rb.velocity.x < -0.1f)
        {
            spriteRenderer.flipX = true;
        }
    }


    public void StartAttacking()
    {
        currentState = State.Attacking;
    }

    public void StopAttacking()
    {
        if(currentFlowField == null)
        {
            currentState = State.Idle;
        }
        else
        {
            currentState = State.Moving;
        }
    }

    public bool IsMoving()
    {
        return currentState == State.Moving;
    }

    public bool IsIdle()
    {
        return currentState == State.Idle;
    }

    /*
     * A function which halts the moving process and clears the path of the unit
    */
    public void StopMoving()
    {
        currentState = State.Idle;
        rb.velocity = Vector2.zero;
        currentFlowField = null;
        hierarchicalPath.Clear();
        worldController.AlertNeighbours(this);
    }

    /*
     * A function which sets the current path of the unit
     * 
     *  List<HierarchicalNode> hierarchicalPath - the path to be followed
     *  Flock flock - the flock of other units heading to the destination
     *  Vector2 destination - the destination point
    */
    public void SetPath(List<HierarchicalNode> hierarchicalPath, Flock flock, Vector2 destination)
    {
        if (Selection.Contains(gameObject))
        {
            Debug.Log("New Path Set.");
        }
        this.destination = destination;
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
        currentFlowField = worldController.GetFlowField(transform.position, new Vector2Int(this.hierarchicalPath.Peek().x, this.hierarchicalPath.Peek().y));
        currentState = State.Moving;
    }

    /*
     * A function which checks the validity of the current path
    */
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
                    currentFlowField = worldController.GetFlowField(transform.position, new Vector2Int(n.x, n.y));
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


    /*
     * A function which returns a Vector2 direction which points in the direction that the flowfield is pointing
     * 
     *  Returns a Vector2 direction
    */
    public Vector2 FlowFieldSteering()
    {
        //movement
        if (currentFlowField != null)
        {
            //if the unit is on the correct component but is in an inaccessible sector
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
                    return (flow);
                }
            }
        }

        return Vector2.zero;
    }

    /*
     * A function which returns a Vector2 direction pointing at a given destination from an origin point of the current position
     * 
     *  Vector2 destination - the vector of the destination to point towards
     *  
     *  Returns a normalised vector2 as the direction 
    */
    private Vector2 SeekingSteering(Vector2 destination)
    {
        Vector2 seekForce = destination - (Vector2)transform.position;
        if (seekForce.magnitude == 0)
        {
            return Vector2.zero;
        }
        return seekForce.normalized;
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
                    Gizmos.matrix = Matrix4x4.Translate(new Vector2(0, worldController.tileSize/4f));
                    Vector2 temp = worldController.tileMap.GetWorldPositionFromIndex(reverse.Peek().x, reverse.Peek().y);
                    Gizmos.DrawLine(new Vector2(temp.x - (worldController.tileSize / 2f), temp.y), new Vector2(temp.x, temp.y + (worldController.tileSize / 4f)));
                    Gizmos.DrawLine(new Vector2(temp.x + (worldController.tileSize / 2f), temp.y), new Vector2(temp.x, temp.y + (worldController.tileSize / 4f)));
                    Gizmos.DrawLine(new Vector2(temp.x - (worldController.tileSize / 2f), temp.y), new Vector2(temp.x, temp.y - (worldController.tileSize / 4f)));
                    Gizmos.DrawLine(new Vector2(temp.x + (worldController.tileSize / 2f), temp.y), new Vector2(temp.x, temp.y - (worldController.tileSize / 4f)));
                    while (reverse.Count > 1)
                    {
                        HierarchicalNode n = reverse.Pop();
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
        }
    }

}