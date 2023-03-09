using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Seperation : MonoBehaviour
{
    [SerializeField]
    private List<UnitClass> nearbyUnits;
    [SerializeField]
    private UnitClass thisUnit;

    private void Start()
    {
        nearbyUnits = new List<UnitClass>();
    }

    /*
     * a function to get the force to be applied to the unit to grant the change in velocity required to steer the object away from all units in its area
     */
    public Vector2 GetSeperation()
    {
        if (nearbyUnits.Count > 0)
        {
            Vector2 position = transform.position;
            Vector2 totalForce = new Vector2();
            foreach (UnitClass other in nearbyUnits)
            {
                Vector2 currentForce = position - (Vector2)other.transform.position;
                float direction = currentForce.magnitude;
                currentForce.Normalize();
                float radius = other.radius + thisUnit.radius;

                totalForce += currentForce * (1 - ((direction - radius) / (thisUnit.seperationRadius - radius)));
            }
            return totalForce * (thisUnit.maxForce / nearbyUnits.Count);
        }
        else 
        {
            return Vector2.zero;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(collision.tag == "PlayerUnit")
        {
            //adds nearby units to the list
            nearbyUnits.Add(collision.gameObject.GetComponent<UnitClass>());
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        //removes the nearby unit when it leaves the nearby area
        nearbyUnits.Remove(collision.gameObject.GetComponent<UnitClass>());
    }
}
