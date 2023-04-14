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
        if(targets.Contains(collision.tag))
        {
            nearbyThreats.Add(collision.gameObject.GetComponent<DestroyableObject>());
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (targets.Contains(collision.tag))
        {
            DestroyableObject collisionClass = collision.GetComponent<DestroyableObject>();
            if (collisionClass == thisEntity.GetTarget())
            {
                thisEntity.GetTarget().attackersCount--;
                thisEntity.StopAttacking();
            }
            nearbyThreats.Remove(collisionClass);
        }
    }

    public List<DestroyableObject> GetNearbyObjects()
    {
        return nearbyThreats;
    }
}
