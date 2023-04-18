using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RetreatingState : State
{
    private AIUnitFSM _stateMachine;
    public RetreatingState(AIUnitFSM stateMachine) : base("Retreating", stateMachine)
    {
        _stateMachine = stateMachine;
    }

    public override void EnterState()
    {
        (_stateMachine.unit.owner as NonPlayerAgent).IssueRetreatCommand(_stateMachine.unit as AIUnit);
        (_stateMachine.unit as AIUnit).SetRetreating();
    }

    public override void UpdatePhysics()
    {
        _stateMachine.unit.Movement();
    }
}
