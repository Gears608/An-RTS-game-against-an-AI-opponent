using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IdleState : State
{
    private UnitFSM _stateMachine;
    public IdleState(UnitFSM stateMachine) : base(stateMachine)
    {
        _stateMachine = stateMachine;
    }

    public override void UpdateActions()
    {
        _stateMachine.unit.Idle();
    }

    public override void FixedUpdateActions()
    {
        _stateMachine.unit.NotMoving();
    }
}
