using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovementState : State
{
    private UnitFSM _stateMachine;
    public MovementState(UnitFSM stateMachine) : base("Moving", stateMachine)
    {
        _stateMachine = stateMachine;
    }

    public override void UpdatePhysics()
    {
        _stateMachine.unit.Movement();
    }
}
