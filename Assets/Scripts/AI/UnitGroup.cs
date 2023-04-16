using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//extends flock here to apply flocking functionality to AI units
public class UnitGroup : Flock
{
    [SerializeField]
    private Building destination;
    private float strength;
    private NonPlayerAgent owner;

    public enum State { Attacking, Defending }
    private State currentState;

    private void Start()
    {
        //calculates the initial strength of the group
        strength = 0f;
        foreach (UnitClass unit in group)
        {
            strength += unit.threatLevel;
        }
    }

    private void Update()
    {
        if (destination != null)
        {
            if (currentState == State.Attacking)
            {
                //decide if group should retreat or not
                if (owner.CalculateBuildingStrength(destination, owner.enemyLayer, owner.targets) / 1.5f > strength)
                {
                    destination.beingAttacked = false;
                    owner.IssueRetreatCommand(this);
                }
            }
        }
        else
        {
            owner.IssueRetreatCommand(this);
        }
    }

    /*
     * A function which sets the groups current state to attacking
     */
    public void SetAttacking()
    {
        currentState = State.Attacking;
    }

    /*
     * A function which sets the groups current state to defending
     */
    public void SetDefending()
    {
        currentState = State.Defending;
    }

    /*
     * A function which returns whether the group is defending or not
     * 
     * Returns bool - true if this group is defending else false
     */
    public bool IsDefending()
    {
        return currentState == State.Defending;
    }

    /*
     * A function to remove a unit from the group
     */
    public override void RemoveUnit(UnitClass unit)
    {
        strength -= unit.threatLevel;
        group.Remove(unit);
        if (group.Count < 1)
        {
            if (currentState == State.Attacking)
            {
                if (destination != null)
                {
                    destination.beingAttacked = false;
                }
            }
            Destroy(gameObject);
        }
    }

    /*
     * A function which sets the destination of the unit group
     * 
     * Building destination - the building the unit group are pathing to
     * State state - the state of the group of units
     */
    public void SetDestination(Building destination)
    {
        this.destination = destination;
    }

    /*
     * A function which sets the owner of the unit group
     */
    public void SetOwner(NonPlayerAgent owner)
    {
        this.owner = owner;
    }

    /*
     * A function which gets the average position of the unit group
     * 
     * Returns Vector2 - the average position of the group
     */
    public Vector2 GetMidPoint()
    {
        Vector2 midpoint = new Vector2();

        foreach(UnitClass unit in group)
        {
            midpoint += (Vector2)unit.transform.position;
        }

        midpoint /= group.Count;

        return midpoint;
    }

    /*
     * A function which returns the current strength of the group
     * 
     * Returns float - the current strength
     */
    public float GetCurrentStrength()
    {
        return strength;
    }

}
