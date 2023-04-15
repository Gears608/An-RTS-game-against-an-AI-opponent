using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Flock : MonoBehaviour
{
    public List<UnitClass> group;

    [SerializeField]
    private int cohesionMax = 5;

    /*
     * a function to get the midpoint of the flock
     * 
     * UnitClass unit - the unit to be moved
     * 
     * Returns a Vector2 which represents the midpoint of all nearby units in the flock
     */
    public Vector2 CohesionSteering(UnitClass unit)
    {
        //calculates the midpoint of the flock
        Vector3 currentPosition = unit.transform.position;
        Vector2 midpoint = new Vector2();
        int nearbyCount = 0;

        foreach(UnitClass member in group)
        {
            if(member == unit)
            {
                continue;
            }

            Vector2 otherPosition = member.transform.position;
            Vector2 nearby = currentPosition - member.transform.position;
            if (new Vector2(nearby.x, nearby.y * 2f).magnitude < cohesionMax)
            {
                midpoint += otherPosition;
                nearbyCount ++;
            }
        }

        if(nearbyCount == 0)
        {
            return currentPosition;
        }

        midpoint /= nearbyCount;

        //returns the midpoint
        return midpoint;
    }

    /*
     * a function to get the average velocity of the units around the given unit within the flock
     * 
     * UnitClass unit - the unit to be moved
     * 
     * Returns a Vector2 which is the average direction of all nearby units in the flock
     */
    public Vector2 AlignmentSteering(UnitClass unit)
    {
        Vector2 alignment = new Vector2();
        int nearbyCount = 0;

        foreach (UnitClass member in group)
        {
            Vector2 nearby = unit.transform.position - member.transform.position;
            if (new Vector2(nearby.x, nearby.y * 2f).magnitude < cohesionMax && member.rb.velocity.magnitude > 0)
            {
                alignment += member.rb.velocity.normalized;
                nearbyCount++;
            }
        }

        if (nearbyCount == 0)
        {
            return Vector2.zero;
        }

        alignment /= nearbyCount;

        return alignment.normalized;
    }

    /*
     * a function to remove a unit from the flock
     * 
     * UnitClass unit - the unit to be removed
     * 
     */
    public virtual void RemoveUnit(UnitClass unit)
    {
        group.Remove(unit);
        if(group.Count < 1)
        {
            Destroy(gameObject);
        }
    }
}
