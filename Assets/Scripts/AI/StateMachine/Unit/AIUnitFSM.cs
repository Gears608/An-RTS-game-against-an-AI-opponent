using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIUnitFSM : UnitFSM
{
    private State retreatingState;

    public void Setup()
    {
        //building the state machine
        idleState = new IdleState(this);
        movementState = new MovementState(this);
        attackingState = new AttackingState(this);
        retreatingState = new RetreatingState(this);

        //any unit can enter into attacking state if an enemy unit comes close
        Transition transition = new Transition(new EnemyInRange(unit), attackingState);
        idleState.transitions.Add(transition);
        movementState.transitions.Add(transition);

        //when there is no enemies nearby the unit will return to idle
        transition = new Transition(new NoEnemyInRange(unit), idleState);
        attackingState.transitions.Add(transition);

        //when the unit is given a move order
        transition = new Transition(new GivenMoveOrder(unit), movementState);
        idleState.transitions.Add(transition);

        //when the unit is given a patrol order
        transition = new Transition(new GivenPatrolOrder(unit), movementState);
        idleState.transitions.Add(transition);

        //when the unit is given a retreat order
        transition = new Transition(new GivenRetreatOrder(unit as AIUnit), retreatingState);
        attackingState.transitions.Add(transition);
        movementState.transitions.Add(transition);
        idleState.transitions.Add(transition);

        //when the unit stops moving
        transition = new Transition(new NotMoving(unit), idleState);
        movementState.transitions.Add(transition);
        retreatingState.transitions.Add(transition);
    }

    public override State GetInitialState()
    {
        return idleState;
    }
}
