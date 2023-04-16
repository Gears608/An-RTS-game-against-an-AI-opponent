using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackRadius : MonoBehaviour
{
    [SerializeField]
    private List<DestroyableObject> nearbyThreats;
    [SerializeField]
    private List<string> targets;
    [SerializeField]
    private DestroyableObject thisEntity;

    private void Start()
    {
        nearbyThreats = new List<DestroyableObject>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        //if an enemy has entered the units radius
        if(targets.Contains(collision.tag))
        {
            //add them to the nearby enemies list
            nearbyThreats.Add(collision.gameObject.GetComponent<DestroyableObject>());
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        //if an enemy leaves the units radius
        if (targets.Contains(collision.tag))
        {
            DestroyableObject collisionClass = collision.GetComponent<DestroyableObject>();
            //if the object leaving was the current target
            if (collisionClass == thisEntity.GetTarget())
            {
                //stop attacking the target
                thisEntity.GetTarget().attackersCount--;
                thisEntity.StopAttacking();
            }
            //remove the object from the nearby enemies list
            nearbyThreats.Remove(collisionClass);
        }
    }

    /*
     * A function which returns a list of nearby threats to the unit
     * 
     * Returns List<DestroyableObject> - a list of nearby threats
     */
    public List<DestroyableObject> GetNearbyObjects()
    {
        return nearbyThreats;
    }
}
