using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIUnit : UnitClass
{
    [SerializeField]
    private float minIdleTime;
    [SerializeField]
    private float maxIdleTime;
    private float idleTime;
    private float currentIdleTime;

    protected override void Start()
    {
        base.Start();

        idleTime = Random.Range(minIdleTime, maxIdleTime);
    }
    protected override void Update()
    {
        base.Update();

        if (!worldController.IsGamePaused())
        {
            if (currentState == State.Idle)
            {
                currentIdleTime += Time.deltaTime;
            }
        }
    }

    /*
     * A function which puts the unit into patrol state
     */
    public void SetPatrol()
    {
        currentState = State.Patrolling;
    }

    /*
     * A function which removes the unit from patrol state
     */
    public void StopPatrol()
    {
        currentState = State.Idle;
    }

    /*
     * A function which puts the unit into defending state
     */
    public void SetDefending()
    {
        currentState = State.Defending;
    }

    /*
     * A function which returns whether the unit is in idle state or not
     * 
     * Returns bool - true if the unit is idle else false
     */
    public bool IsIdle()
    {
        return currentState == State.Idle;
    }

    /*
     * A function which returns whether the unit is in patrol state or not
     * 
     * Reuturns bool - true if the unit is patrolling else false
     */
    public bool IsPatrolling()
    {
        return currentState == State.Patrolling;
    }

    /*
     * A function which returns whether or not the unit is ready for a new patrol route or not
     * 
     * Returns bool - true if the unit is ready for a new patrol else false
     */
    public bool ReadyForPatrol()
    {
        if (currentIdleTime >= idleTime)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    /*
     * A function which sets a new patrol route for the unit
     * 
     * List<HierarchicalNode> route - the hierarchical route of the unit represented as hierarchical nodes
     * Vector2 destination - the destination position of the unit
     */
    public void SetPatrolRoute(List<HierarchicalNode> route, Vector2 destination)
    {
        idleTime = Random.Range(minIdleTime, maxIdleTime);
        currentIdleTime = 0f;
        SetPath(route, null, destination);
        currentState = State.Patrolling;
    }
}
