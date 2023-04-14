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
        if(health == 0)
        {
            Destroy(gameObject);
        }
    }

    public List<DestroyableObject> GetNearbyObjects(float searchRadius, LayerMask searchLayer, List<string> tags)
    {
        List<DestroyableObject> nearbyObjects = GetNearbyObjects(searchRadius, searchLayer);
        List<DestroyableObject> output = new List<DestroyableObject>();

        foreach(DestroyableObject nearby in nearbyObjects)
        {
            if (tags.Contains(nearby.tag))
            {
                output.Add(nearby);
            }
        }

        return output;
    }

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

    public virtual DestroyableObject GetTarget() { return null; }
    public virtual void StopAttacking() { }

}
