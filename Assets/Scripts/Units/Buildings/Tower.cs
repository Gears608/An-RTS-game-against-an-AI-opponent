using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tower : Building
{
    [SerializeField]
    private LineRenderer projectileRenderer;
    [SerializeField]
    private int damage;
    private float timer = 0f;
    [SerializeField]
    private float attackCooldown;
    [SerializeField]
    private DestroyableObject target;
    [SerializeField]
    private LayerMask enemyLayer;
    [SerializeField]
    private List<string> targets;
    [SerializeField]
    private float attackRadius;
    [SerializeField]
    private AttackRadius attackTrigger;

    protected override void Update()
    {
        base.Update();

        if (!worldController.IsGamePaused())
        {
            Attack();
        }
    }

    /*
     * A function which handles when and at what the tower should fire at
     */
    private void Attack()
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
            DoAction();
        }
    }

    /*
     * A function which decides and sets the target of the tower
     */
    private void DecideTarget()
    {
        List<DestroyableObject> nearbyThreats = attackTrigger.GetNearbyObjects();
        if (nearbyThreats.Count > 0)
        {
            float targetPriority = float.MaxValue;
            foreach (DestroyableObject nearby in nearbyThreats)
            {
                //checks for LoS
                RaycastHit2D hit = Physics2D.Raycast(transform.position, nearby.transform.position - transform.position, attackRadius, enemyLayer);
                if (hit.transform == nearby.transform)
                {
                    //decides which nearby entity has the highest priority
                    float nearbyPriority = (nearby.threatLevel) * 100 / (nearby.health * (nearby.attackersCount + 1) * Vector2.Distance(transform.position, nearby.transform.position));
                    if (nearbyPriority < targetPriority)
                    {
                        targetPriority = nearbyPriority;
                        target = nearby;
                    }
                }
            }
            if (target != null)
            {
                //increases the targets attackers count
                target.attackersCount++;
            }
        }
    }

    /*
     * A function which stops the tower from attacking
     */
    public override void StopAttacking()
    {
        target = null;
    }

    /*
     * A function which handles the attack action of the tower
     */
    private void DoAction()
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

    /*
     * A function which returns the current target of the tower
     * 
     * Returns DestroyableObject - the tower's target
     */
    public override DestroyableObject GetTarget()
    {
        return target;
    }
}
