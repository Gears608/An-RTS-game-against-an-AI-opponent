using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIUnit : UnitClass
{
    [SerializeField]
    private float minIdleTime;
    [SerializeField]
    private float maxIdleTime;
    private float idleTime;
    private float currentIdleTime;

    protected override void Start()
    {
        base.Start();

        idleTime = Random.Range(minIdleTime, maxIdleTime);
    }
    protected override void Update()
    {
        base.Update();

        if (!worldController.IsGamePaused())
        {
            if (currentState == State.Idle)
            {
                currentIdleTime += Time.deltaTime;
            }
        }
    }

    public bool ReadyForPatrol()
    {
        if (currentIdleTime >= idleTime)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    public void SetPatrolRoute(List<HierarchicalNode> route, Vector2 destination)
    {
        idleTime = Random.Range(minIdleTime, maxIdleTime);
        currentIdleTime = 0f;
        SetPath(route, null, destination);
        currentState = State.Patrol;
    }

}
