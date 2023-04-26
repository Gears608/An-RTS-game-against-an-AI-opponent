using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class UnitClass : DestroyableObject
{
    public bool attacking;
    public bool defending;
    public bool patrolling;
    public bool retreating;

    private Stack<HierarchicalNode> hierarchicalPath;
    private TileGrid<Vector2> currentFlowField;
    private TileGrid<int> intfield;

    public Rigidbody2D rb;
    [SerializeField]
    private SpriteRenderer spriteRenderer;

    public bool showflowfield = false;
    public bool showintfield;
    public bool showpath;

    public Vector2 destination;

    [SerializeField]
    private float maxSpeed;

    [SerializeField]
    protected float timer = 0f;
    [SerializeField]
    protected float attackCooldown;
    [SerializeField]
    protected DestroyableObject target;
    [SerializeField]
    private LayerMask enemyLayer;
    [SerializeField]
    protected int damage;

    public float seperationRadius = 3;
    public float radius = 0.5f;

    public int cost;

    public Barracks home;

    [SerializeField]
    private float attackRadius;
    public AttackRadius attackTrigger;

    protected override void Start()
    {
        base.Start();
        worldController.AddUnit(this);
        hierarchicalPath = new Stack<HierarchicalNode>();
        rb = GetComponent<Rigidbody2D>();
    }

    protected override void Update()
    {
        if (!worldController.IsGamePaused())
        {
            if (health <= 0)
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
                owner.RemoveUnit(this);
            }
            base.Update();
            CheckPath();
        }
    }

    public void Movement()
    {
        Vector2 direction = (FlowFieldSteering() + (SeekingSteering(worldController.CohesionSteering(this)) * 0.1f) + (worldController.AlignmentSteering(this) * 0.3f) + (worldController.GetSeperation(this) * 1.2f)).normalized;

        Vector2 desiredVelocity = direction * 20;

        rb.AddForce(desiredVelocity);

        if (rb.velocity.magnitude > maxSpeed)
        {
            rb.velocity = rb.velocity.normalized * maxSpeed;
        }
        Debug.Log(rb.velocity.magnitude);

        if (rb.velocity.x > 0.1)
        {
            spriteRenderer.flipX = false;
        }
        else if (rb.velocity.x < -0.1f)
        {
            spriteRenderer.flipX = true;
        }
    }

    /*
     * A function which manages the units movement when idle
     */
    public void NotMoving()
    {
        Vector2 seperationSteer = worldController.GetSeperation(this) * 0.1f;
        Vector2 desiredVelocity = seperationSteer * maxSpeed;
        if (desiredVelocity.magnitude > 5)
        {
            desiredVelocity = seperationSteer.normalized * 5;
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

    public virtual void Idle() { }

    /*
     * A function which returns whether the unit is moving or not
     * 
     * Returns bool - true if moving, else false
     */
    public bool IsMoving()
    {
        return retreating || attacking || defending;
    }

    /*
     * A function which halts the moving process and clears the path of the unit
    */
    public void StopMoving()
    {
        attacking = false;
        patrolling = false;
        retreating = false;
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
    public void SetPath(List<HierarchicalNode> hierarchicalPath, Vector2 destination)
    {
        this.destination = destination;
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
        (currentFlowField, intfield) = worldController.GetFlowField(transform.position, new Vector2Int(this.hierarchicalPath.Peek().x, this.hierarchicalPath.Peek().y));
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
                    (currentFlowField, intfield) = worldController.GetFlowField(transform.position, new Vector2Int(n.x, n.y));
                }
                //if the unit has been pushed onto an incorrect tile
                else
                {
                    Debug.Log("Pushed off flowfield.");
                    //recalculate the path
                    List<HierarchicalNode> currentPath = new List<HierarchicalNode>(hierarchicalPath);
                    currentPath.Reverse();
                    SetPath(worldController.FindHierarchicalPathMerging(transform.position, destination, currentPath), destination);
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
                //recalculate the path
                List<HierarchicalNode> currentPath = new List<HierarchicalNode>(hierarchicalPath);
                currentPath.Reverse();
                SetPath(worldController.FindHierarchicalPathMerging(transform.position, destination, currentPath), destination);
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
                    return flow.normalized;
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

    /*
     * Function which handles attacking functionality
     */
    public void Attack()
    {
        timer += Time.deltaTime;
        //if the unit has no target
        if (target == null)
        {
            //decide on a new target
            DecideTarget();
        }
        else
        {
            DoUnitAction();
        }
    }

    /*
     * A function which decides and sets the target of the unit
     */
    private void DecideTarget()
    {
        List<DestroyableObject> nearbyThreats = attackTrigger.GetNearbyObjects();
        if (nearbyThreats.Count > 0)
        {
            float targetPriority = float.MaxValue;
            foreach (DestroyableObject nearby in nearbyThreats)
            {
                //checks for LoS
                RaycastHit2D hit = Physics2D.Raycast(transform.position, nearby.transform.position - transform.position, attackRadius, enemyLayer);
                if (hit.transform == nearby.transform)
                {
                    //decides which nearby entity has the highest priority
                    float nearbyPriority = (nearby.threatLevel) * 100 / (nearby.health * (nearby.attackersCount + 1) * Vector2.Distance(transform.position, nearby.transform.position));
                    if (nearbyPriority < targetPriority)
                    {
                        targetPriority = nearbyPriority;
                        target = nearby;
                    }
                }
            }
            if (target != null)
            {
                target.attackersCount++;
            }
        }
    }

    /*
     * A function which gets the current target of the unit
     * 
     * Returns DestroyableObject - the target of the unit
     */
    public override DestroyableObject GetTarget()
    {
        return target;
    }

    /*
     * A function which handles contains the attack action of the unit; overriden in specific unit classes for functionality
     */
    public virtual void DoUnitAction(){}

    private void OnDrawGizmos()
    {
        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.alignment = TextAnchor.MiddleCenter;
        Gizmos.color = Color.yellow;
        if (showpath)
        {
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
                        Gizmos.matrix = Matrix4x4.Translate(new Vector2(0, worldController.tileSize / 4f));
                        Vector2 temp = worldController.costField.GetWorldPositionFromIndex(reverse.Peek().x, reverse.Peek().y);
                        Gizmos.DrawLine(new Vector2(temp.x - (worldController.tileSize / 2f), temp.y), new Vector2(temp.x, temp.y + (worldController.tileSize / 4f)));
                        Gizmos.DrawLine(new Vector2(temp.x + (worldController.tileSize / 2f), temp.y), new Vector2(temp.x, temp.y + (worldController.tileSize / 4f)));
                        Gizmos.DrawLine(new Vector2(temp.x - (worldController.tileSize / 2f), temp.y), new Vector2(temp.x, temp.y - (worldController.tileSize / 4f)));
                        Gizmos.DrawLine(new Vector2(temp.x + (worldController.tileSize / 2f), temp.y), new Vector2(temp.x, temp.y - (worldController.tileSize / 4f)));
                        while (reverse.Count > 1)
                        {
                            HierarchicalNode n = reverse.Pop();
                            temp = worldController.costField.GetWorldPositionFromIndex(reverse.Peek().x, reverse.Peek().y);
                            Gizmos.DrawLine(new Vector2(temp.x - (worldController.tileSize / 2f), temp.y), new Vector2(temp.x, temp.y + (worldController.tileSize / 4f)));
                            Gizmos.DrawLine(new Vector2(temp.x + (worldController.tileSize / 2f), temp.y), new Vector2(temp.x, temp.y + (worldController.tileSize / 4f)));
                            Gizmos.DrawLine(new Vector2(temp.x - (worldController.tileSize / 2f), temp.y), new Vector2(temp.x, temp.y - (worldController.tileSize / 4f)));
                            Gizmos.DrawLine(new Vector2(temp.x + (worldController.tileSize / 2f), temp.y), new Vector2(temp.x, temp.y - (worldController.tileSize / 4f)));

                            Vector2 z = worldController.costField.GetWorldPositionFromIndex(n.x, n.y);
                            Gizmos.DrawLine(z, temp);

                            hierarchicalPath.Push(n);
                        }
                        hierarchicalPath.Push(reverse.Pop());
                    }

                    Vector2 t = worldController.costField.GetWorldPositionFromIndex(hierarchicalPath.Peek().x, hierarchicalPath.Peek().y);
                    Gizmos.DrawLine((Vector2)transform.position, t);
                    Gizmos.matrix = Matrix4x4.Translate(Vector2.zero);
                }
            }
        }
        if (currentFlowField != null) 
        {
            if (showflowfield)
            {
                for (int x = 0; x < currentFlowField.GetTileWidth(); x++)
                {
                    for (int y = 0; y < currentFlowField.GetTileHeight(); y++)
                    {
                        //flow
                        Gizmos.DrawLine(currentFlowField.GetWorldPositionFromIndex(x, y) + new Vector2(0, 0.25f), currentFlowField.GetWorldPositionFromIndex(x, y) + currentFlowField.GetObject(x, y) / 2 + new Vector2(0, 0.25f));
                        Gizmos.DrawCube(currentFlowField.GetWorldPositionFromIndex(x, y) + new Vector2(0, 0.25f), Vector2.one / 7);
                        //Handles.Label(currentFlowField.GetWorldPositionFromIndex(x, y) + new Vector2(0, worldController.tileSize / 4f), (currentFlowField.GetObject(x, y)).ToString(), style);
                    }
                }
            }
            else if(showintfield)
            {
                for (int x = 0; x < intfield.GetTileWidth(); x++)
                {
                    for (int y = 0; y < intfield.GetTileHeight(); y++)
                    {
                        //int
                        Handles.Label(intfield.GetWorldPositionFromIndex(x, y) + new Vector2(0, worldController.tileSize / 4f), (intfield.GetObject(x, y)).ToString(), style);
                    }
                }
            }
        }
    }

}