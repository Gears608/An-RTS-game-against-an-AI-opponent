using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class State
{
    public List<Transition> transitions;
    protected StateMachine stateMachine;

    public State(StateMachine stateMachine)
    {
        this.stateMachine = stateMachine;
        transitions = new List<Transition>();
    }

    public virtual void EnterState() { }

    public virtual void UpdateActions() { }

    public virtual void FixedUpdateActions() { }

    public virtual void ExitState() { }
}
    
