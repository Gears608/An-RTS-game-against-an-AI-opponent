using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyInfantry : AIUnit
{
    [SerializeField]
    private LineRenderer projectileRenderer;

    public override void StopAttacking()
    {
        base.StopAttacking();
        projectileRenderer.enabled = false;
    }

    public override void DoUnitAction()
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
