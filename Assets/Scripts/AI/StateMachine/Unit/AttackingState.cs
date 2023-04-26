using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackingState : State
{
    private UnitFSM _stateMachine;
    public AttackingState(UnitFSM stateMachine) : base(stateMachine) 
    {
        _stateMachine = stateMachine;
    }

    public override void UpdateActions()
    {
        _stateMachine.unit.Attack();
    }

    public override void FixedUpdateActions()
    {
        _stateMachine.unit.NotMoving();
    }
}
