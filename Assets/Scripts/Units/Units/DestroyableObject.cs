using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DestroyableObject : MonoBehaviour
{
    public int health;
    public int attackersCount;
    public float threatLevel;

    public bool beingAttacked;

    public WorldController worldController;
    [SerializeField]
    public PlayerClass owner;

    protected virtual void Start()
    {
        worldController = GameObject.Find("WorldController").GetComponent<WorldController>();
    }

    protected virtual void Update()
    {
        //if this object is destroyed; health reaches 0
        if(health <= 0)
        {
            Destroy(gameObject);
        }
    }

    /*
     * A function which gets all nearby objects with speficied parameters
     * 
     * float searchRadius - the radius around the object to search
     * LayerMask searchLayer - the layers to search on
     * List<string> tags - the tags that the objects must have
     * 
     * Returns List<DestroyableObject> - the list of nearby objects
     */
    public List<DestroyableObject> GetNearbyObjects(float searchRadius, LayerMask searchLayer, List<string> tags)
    {
        //obtains a list of nearby objects on the layer in the radius
        List<DestroyableObject> nearbyObjects = GetNearbyObjects(searchRadius, searchLayer);
        List<DestroyableObject> output = new List<DestroyableObject>();

        foreach(DestroyableObject nearby in nearbyObjects)
        {
            //checks the tags
            if (tags.Contains(nearby.tag))
            {
                output.Add(nearby);
            }
        }

        return output;
    }

    /*
     * A function which gets all nearby objects with speficied parameters
     * 
     * float searchRadius - the radius around the object to search
     * LayerMask searchLayer - the layers to search on
     * 
     * Returns List<DestroyableObject> - the list of nearby objects
     */
    public List<DestroyableObject> GetNearbyObjects(float searchRadius, LayerMask searchLayer)
    {
        Collider2D[] nearby = Physics2D.OverlapCircleAll(transform.position, searchRadius, searchLayer);
        List<DestroyableObject> nearbyUnits = new List<DestroyableObject>();
        foreach (Collider2D col in nearby)
        {
            DestroyableObject unit = col.GetComponent<DestroyableObject>();
            if (col != null)
            {
                nearbyUnits.Add(unit);
            }
        }

        return nearbyUnits;
    }

    /*
     * A function which will return the target of the object; this will be overridden in specific classes which require targets
     * 
     * Returns DestroyableObject - this objects target object or null if there isn't one or this function does not apply to this object
     */
    public virtual DestroyableObject GetTarget() { return null; }

    /*
     * A function which stops this object from attacking; will be overridden in specific classes where attacking is required
     */
    public virtual void StopAttacking() { }

}
