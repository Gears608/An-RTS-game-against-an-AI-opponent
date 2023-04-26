using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovementState : State
{
    private UnitFSM _stateMachine;
    public MovementState(UnitFSM stateMachine) : base(stateMachine)
    {
        _stateMachine = stateMachine;
    }

    public override void FixedUpdateActions()
    {
        _stateMachine.unit.Movement();
    }
}
