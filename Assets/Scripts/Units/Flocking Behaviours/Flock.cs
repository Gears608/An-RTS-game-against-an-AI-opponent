using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Flock : MonoBehaviour
{
    public List<UnitClass> flock;

    [SerializeField]
    private float cohesionMin = 0f;
    [SerializeField]
    private int cohesionMax = 5;

    /*
     * a function to get the midpoint of the flock
     * 
     * UnitClass unit - the unit to be moved
     * 
     */
    public Vector2 CohesionSteering(UnitClass unit)
    {
        //calculates the midpoint of the flock
        Vector2 currentPosition = unit.transform.position;
        Vector2 midpoint = new Vector2();
        int nearbyCount = 0;

        foreach(UnitClass other in flock)
        {
            if(other == unit)
            {
                continue;
            }

            Vector2 otherPosition = other.transform.position;
            float distanace = Vector2.Distance(currentPosition, otherPosition);
            if (distanace < cohesionMax)
            {
                midpoint += otherPosition;
                nearbyCount ++;
            }
        }

        if(nearbyCount == 0)
        {
            return Vector2.zero;
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
     */
    public Vector2 AlignmentSteering(UnitClass unit)
    {
        Vector2 alignment = new Vector2();
        int nearbyCount = 0;

        foreach (UnitClass member in flock)
        {
            float distanace = Vector2.Distance(unit.transform.position, member.transform.position);
            if (distanace < cohesionMax && member.rb.velocity.magnitude > 0)
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
    public void RemoveUnit(UnitClass unit)
    {
        flock.Remove(unit);
        if(flock.Count < 1)
        {
            Destroy(this.gameObject);
        }
    }
}
