using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackRadius : MonoBehaviour
{
    [SerializeField]
    private List<DestroyableEntity> nearbyThreats;

    private DestroyableEntity target;

    private void Start()
    {
        nearbyThreats = new List<DestroyableEntity>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(collision.gameObject.tag == "EnemyUnit" || collision.gameObject.tag == "EnemyBuilding")
        {
            nearbyThreats.Add(collision.gameObject.GetComponent<DestroyableEntity>());
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        nearbyThreats.Remove(collision.gameObject.GetComponent<DestroyableEntity>());
    }

    private void Update()
    {
        if(!nearbyThreats.Contains(target) || target == null)
        {
            DecideTarget();
        }
    }

    /*
     * A function which decides and sets the target of the unit
     */
    private void DecideTarget()
    {
        foreach(DestroyableEntity nearby in nearbyThreats)
        {
            //here we decide which nearby unit to fire at
        }
    }
}
