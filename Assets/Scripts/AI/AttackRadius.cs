using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackRadius : MonoBehaviour
{
    [SerializeField]
    private LineRenderer projectileRenderer;
    [SerializeField]
    private List<DestroyableEntity> nearbyThreats;
    [SerializeField]
    private DestroyableEntity target;
    [SerializeField]
    private List<string> targets;
    [SerializeField]
    private LayerMask ignore;
    [SerializeField]
    private float attackCooldown;
    [SerializeField]
    private int damage;
    [SerializeField]
    private float range;
    [SerializeField]
    private UnitClass thisUnit;

    private float timer = 0f;

    private void Start()
    {
        nearbyThreats = new List<DestroyableEntity>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(targets.Contains(collision.tag))
        {
            nearbyThreats.Add(collision.gameObject.GetComponent<DestroyableEntity>());
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (targets.Contains(collision.tag))
        {
            DestroyableEntity collisionClass = collision.GetComponent<DestroyableEntity>();
            if (collisionClass == target)
            {
                target.attackersCount--;
                target = null;
                projectileRenderer.enabled = false;
            }
            nearbyThreats.Remove(collisionClass);
        }
    }

    private void Update()
    {
        if (!thisUnit.worldController.IsGamePaused())
        {
            timer += Time.deltaTime;
            //if the unit has no target
            if (target == null)
            {
                //decide on a new target
                DecideTarget();
            }
            else
            {
                if (timer >= attackCooldown)
                {
                    //produced a projectile like effect using a line
                    projectileRenderer.enabled = true;
                    projectileRenderer.SetPosition(0, transform.position);
                    projectileRenderer.SetPosition(1, target.transform.GetChild(1).position);
                    //damages enemy
                    target.health -= damage;
                    //resets timer
                    timer = 0f;
                }
                if (timer >= attackCooldown / 4f)
                {
                    projectileRenderer.enabled = false;
                }
            }
        }
    }

    /*
     * A function which decides and sets the target of the unit
     */
    private void DecideTarget()
    {
        if (nearbyThreats.Count > 0)
        {
            float targetPriority = float.MaxValue;
            foreach (DestroyableEntity nearby in nearbyThreats)
            {
                //checks for LoS
                RaycastHit2D hit = Physics2D.Raycast(transform.position, nearby.transform.position - transform.position, range, ~ignore);
                if (hit.transform == nearby.transform) 
                {
                    //decides which nearby entity has the highest priority
                    float nearbyPriority = (nearby.threatLevel)*100 / (nearby.health * (nearby.attackersCount+1) * Vector2.Distance(transform.position, nearby.transform.position));
                    if (nearbyPriority < targetPriority)
                    {
                        targetPriority = nearbyPriority;
                        target = nearby;
                    }
                }
            }
            if(target != null)
            {
                target.attackersCount++;
            }
        }
    }
}
