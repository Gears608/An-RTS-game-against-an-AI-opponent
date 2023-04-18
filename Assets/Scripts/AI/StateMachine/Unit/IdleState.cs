using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IdleState : State
{
    private UnitFSM _stateMachine;
    public IdleState(UnitFSM stateMachine) : base("Idle", stateMachine)
    {
        _stateMachine = stateMachine;
    }

    public override void UpdateLogic()
    {
        _stateMachine.unit.Idle();
    }

    public override void UpdatePhysics()
    {
        _stateMachine.unit.NotMoving();
    }
}
