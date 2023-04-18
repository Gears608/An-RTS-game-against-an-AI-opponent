using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackingState : State
{
    private UnitFSM _stateMachine;
    public AttackingState(UnitFSM stateMachine) : base("Attacking", stateMachine) 
    {
        _stateMachine = stateMachine;
    }

    public override void UpdateLogic()
    {
        _stateMachine.unit.Attack();
    }

    public override void UpdatePhysics()
    {
        _stateMachine.unit.NotMoving();
    }
}
