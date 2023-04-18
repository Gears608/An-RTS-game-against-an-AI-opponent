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

    public UnitGroup group;

    protected override void Start()
    {
        base.Start();

        idleTime = Random.Range(minIdleTime, maxIdleTime);
    }

    public override void Idle()
    {
        currentIdleTime += Time.deltaTime;
    }

    /*
     * A function which puts the unit into patrol state
     */
    public void SetPatrol()
    {
        patrolling = true;
    }

    /*
     * A function which removes the unit from patrol state
     */
    public void StopPatrol()
    {
        patrolling = false;
    }

    /*
     * A function which puts the unit into defending state
     */
    public void SetDefending()
    {
        defending = true;
    }

    /*
     * A function which puts the unit into retreating state
     */
    public void SetRetreating()
    {
        retreating = true;
        attacking = false;
        defending = false;
        patrolling = false;
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
        SetPath(route, destination);
        patrolling = true;
    }
}
